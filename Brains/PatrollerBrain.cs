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
        public List<IMyBroadcastListener> listeners { get; set; }
        public PatrollerBrain(State state, MyGridProgram GridProgram, Configuration configuration, List<IMyBroadcastListener> listeners)
        {
            this.state = state;
            this.GridProgram = GridProgram;
            this.configuration = configuration;
            this.listeners = listeners;
            this.GetBasicBlocks();
            state.SetControllers(remote, samController);
        }

        public void Process(string argument)
        {
            if (connector.Status == MyShipConnectorStatus.Connected)
            {
                if (!ServiceFunctions.NeedsService(GridProgram, configuration, batteries, reactors, h2Tanks))
                {
                    state.Status = Status.Patrolling;
                    NavigationFunctions.UnDock(int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), this);
                }
                else
                    state.Status = Status.Waiting;
            }
            else
            {
                if ((state.Status == Status.Patrolling || state.Status == Status.Waiting || state.Status == Status.Attacking))
                    CombatFunctions.EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);
                if (state.Enroute)
                {
                    var distanceToWaypoint = NavigationFunctions.DistanceToWaypoint(this);
                    if (distanceToWaypoint < 3)
                    {
                        switch (state.Status)
                        {
                            case Status.Docking:
                                if (connector.Status == MyShipConnectorStatus.Connectable)
                                    NavigationFunctions.Dock(this);
                                break;
                            case Status.Returning:
                                state.CompleteStateAndChangeTo(Status.Docking);
                                break;
                            case Status.Waiting:
                                if (ServiceFunctions.NeedsService(GridProgram, configuration, batteries, reactors, h2Tanks))
                                    state.CompleteStateAndChangeTo(Status.Returning);
                                else
                                    state.CompleteStateAndChangeTo(Status.Waiting);
                                break;
                            case Status.Patrolling:
                                state.CompleteStateAndChangeTo(Status.Patrolling);
                                break;
                            case Status.Attacking:
                                state.CompleteStateAndChangeTo(Status.Waiting);
                                CombatFunctions.EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);
                                break;
                        }
                    }
                }
                else
                {
                    switch (state.Status)
                    {
                        case Status.Returning:
                            NavigationFunctions.Go(state.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), this);
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
                                if (ServiceFunctions.NeedsService(GridProgram, configuration, batteries, reactors, h2Tanks))
                                    state.CompleteStateAndChangeTo(Status.Returning);
                                else
                                {
                                    state.SetNextPatrolWaypoint();
                                    ResumePatrol(GridProgram, state, samController);
                                }
                            else
                                CombatFunctions.EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);
                            break;
                        case Status.PreparingToAttack:
                            CombatFunctions.Attack(state, float.Parse(configuration.For(ConfigName.AttackSpeedLimit)), remote, this);
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
            NavigationFunctions.Go(state.PatrolRoute[state.CurrentPatrolPoint], false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), this);
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

        public bool HandleCommand(CommandType commandType)
        {
            switch (commandType)
            {
                case CommandType.Return:
                    state.Status = Status.Returning;
                    NavigationFunctions.Go(state.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), this);
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
            state.DockApproach = state.DockPos + (connector.WorldMatrix.Backward * int.Parse(configuration.For(ConfigName.DockClearance)));
        }

    }
}