using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class PatrollerBrain : IAdvancedAiBrain
        {
            public IMyRemoteControl remote { get; set; }
            public IMyShipConnector connector { get; set; }
            public IMyProgrammableBlock samController { get; set; }
            public List<IMyBatteryBlock> batteries { get; set; }
            public List<IMyGasTank> h2Tanks { get; set; }
            public List<IMyReactor> reactors { get; set; }

            public MyGridProgram GridProgram { get; set; }
            public Configuration configuration { get; set; }
            public NavigationModel navigationModel { get; set; }
            public State state { get; set; }

            public PatrollerBrain(Program.State state, MyGridProgram GridProgram, Configuration configuration)
            {
                this.state = state;
                this.GridProgram = GridProgram;
                this.configuration = configuration;
                this.GetBasicBlocks();
            }

            public void Process(string argument)
            {
                if (connector.Status == MyShipConnectorStatus.Connected)
                {
                    if (!NeedsService(GridProgram, configuration, batteries, reactors, h2Tanks))
                    {
                        state.Status = Status.Patrolling;
                        UnDock(int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)),this);
                    }
                    else
                        state.Status = Status.Waiting;
                }
                else
                {
                    if ((state.Status == Status.Patrolling || state.Status == Status.Waiting || state.Status == Status.Attacking))
                        EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);
                    if (state.Enroute)
                    {
                        var distanceToWaypoint = DistanceToWaypoint(this);
                        if (distanceToWaypoint < 3)
                        {
                            switch (state.Status)
                            {
                                case Status.Docking:
                                    if (connector.Status == MyShipConnectorStatus.Connectable)
                                        Dock(this);
                                    break;
                                case Status.Returning:
                                    state.CompleteStateAndChangeTo(Status.Docking);
                                    break;
                                case Status.Waiting:
                                    if (NeedsService(GridProgram, configuration, batteries, reactors, h2Tanks))
                                        state.CompleteStateAndChangeTo(Status.Returning);
                                    else
                                        state.CompleteStateAndChangeTo(Status.Waiting);
                                    break;
                                case Status.Patrolling:
                                    state.CompleteStateAndChangeTo(Status.Patrolling);
                                    break;
                                case Status.Attacking:
                                    state.CompleteStateAndChangeTo(Status.Waiting);
                                    EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        switch (state.Status)
                        {
                            case Status.Returning:
                                Go(state.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), this);
                                break;
                            case Status.Docking:
                                state.CurrentDestination = state.DockPos;
                                string msg;
                                state.Enroute = KeenNav_Controller.Go(remote, state.DockPos, true, int.Parse(configuration.For(ConfigName.DockSpeedLimit)), out msg);
                                GridProgram.Echo(msg);
                                break;
                            case Status.Patrolling:
                                state.CompleteStateAndChangeTo(Status.Waiting);
                                break;
                            case Status.Waiting:
                                if (connector.Status == MyShipConnectorStatus.Unconnected)
                                    if (NeedsService(GridProgram, configuration, batteries, reactors, h2Tanks))
                                        state.CompleteStateAndChangeTo(Status.Returning);
                                    else
                                    {
                                        state.SetNextPatrolWaypoint();
                                        ResumePatrol(GridProgram, state, samController);
                                    }
                                else
                                    EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);
                                break;
                            case Status.PreparingToAttack:
                                Attack(state, float.Parse(configuration.For(ConfigName.AttackSpeedLimit)), remote, this);
                                break;
                        }
                    }
                }

                this.SetRuntimeFrequency();
            }

            public void ResumePatrol(MyGridProgram GridProgram, State state, IMyProgrammableBlock sam_controller)
            {
                GridProgram.Echo($"{Prompts.PatrolPoint} {state.CurrentPatrolPoint}");
                state.CompleteStateAndChangeTo(Status.Patrolling);
                Go(state.PatrolRoute[state.CurrentPatrolPoint], false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), this);
            }

            public void StatusReport()
            {
                GridProgram.Echo($"{Prompts.CurrentMode}: Patrolling");
                GridProgram.Echo($"{Prompts.CurrentStatus}: {state.Status.ToHumanReadableName()}");
                GridProgram.Echo($"{Prompts.NavigationModel}: {navigationModel.ToHumanReadableName()}");
                GridProgram.Echo($"{Prompts.Enroute}: {state.Enroute}");
            }

            public void ClearData()
            {
                state = new State();
                if (remote != null)
                {
                    remote.ClearWaypoints();
                    remote.SetAutoPilotEnabled(false);
                }
            }

            public void TurnOff()
            {
                GridProgram.Runtime.UpdateFrequency = UpdateFrequency.None;

                remote.ClearWaypoints();
                remote.SetAutoPilotEnabled(false);

                state.Status = Status.Waiting;
                state.Enroute = false;
            }

            public bool IsSetUp()
            {
                RefreshDockApproach();//refresh in case clearance config has been changed
                return !state.DockPos.IsZero() && !state.DockApproach.IsZero() && state.PatrolRoute.Any();
            }

            public bool HandleCommand(Program.CommandType commandType)
            {
                switch (commandType)
                {
                    case CommandType.Return:
                        state.Status = Status.Returning;
                        Go(state.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), this);
                        return true;
                    case CommandType.Setup:
                        SetUp();
                        return true;
                    default:
                        GridProgram.Echo("I do not know that command!");
                        return false;
                }
            }

            public string SerializeState()
            {
                return state.Serialize();
            }

            public bool SetUp()
            {
                if (connector.Status != MyShipConnectorStatus.Connected)
                {
                    GridProgram.Echo(Prompts.MustBeDockedToHomeConnectorToRunSetup);
                    return false;
                }

                state.DockPos = remote.GetPosition();
                RefreshDockApproach();

                var waypoints = new List<MyWaypointInfo>();
                remote.GetWaypointInfo(waypoints);
                if (!waypoints.Any())
                {
                    GridProgram.Echo(Prompts.RemoteNeedsWaypointsToPatrol);
                    return false;
                }
                else
                {
                    foreach (var waypoint in waypoints)
                    {
                        state.PatrolRoute.Add(waypoint.Coords);
                    }
                }

                return true;
            }

            public void RefreshDockApproach()
            {
                state.DockApproach = remote.GetPosition() + (connector.WorldMatrix.Backward * int.Parse(configuration.For(ConfigName.DockClearance)));
            }

        }
    }
}
