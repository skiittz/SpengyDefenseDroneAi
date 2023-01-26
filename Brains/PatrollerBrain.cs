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
        }

        public void Process(string argument)
        {
            this.CheckAndFireFixedWeapons();
            this.CheckScuttle();

            if (connector.Status == MyShipConnectorStatus.Connected)
            {
                if (!this.NeedsService())
                {
                    state.Status = Status.Patrolling;
                    this.UnDock();
                }
                else
                    state.Status = Status.Waiting;
            }
            else
            {
                if ((state.Status == Status.Patrolling || state.Status == Status.Waiting || state.Status == Status.Attacking))
                    this.EnemyCheck();
                if (state.Enroute)
                {
                    var distanceToWaypoint = this.DistanceToWaypoint();
                    if (distanceToWaypoint < 3)
                    {
                        switch (state.Status)
                        {
                            case Status.Docking:
                                if (connector.Status == MyShipConnectorStatus.Connectable)
                                    this.Dock();
                                break;
                            case Status.Returning:
                                state.CompleteStateAndChangeTo(Status.Docking, this);
                                break;
                            case Status.Waiting:
                                if (this.NeedsService())
                                    state.CompleteStateAndChangeTo(Status.Returning, this);
                                else
                                    state.CompleteStateAndChangeTo(Status.Waiting, this);
                                break;
                            case Status.Patrolling:
                                state.CompleteStateAndChangeTo(Status.Patrolling, this);
                                break;
                            case Status.Attacking:
                                state.CompleteStateAndChangeTo(Status.Waiting, this);
                                this.EnemyCheck();
                                break;
                        }
                    }
                }
                else
                {
                    switch (state.Status)
                    {
                        case Status.Returning:
                            this.Go(state.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)));
                            break;
                        case Status.Docking:
                            state.CurrentDestination = state.DockPos;
                            string msg;
                            state.Enroute = KeenNav_Controller.Go(remote, state.DockPos, true, int.Parse(configuration.For(ConfigName.DockSpeedLimit)), out msg);
                            GridProgram.Echo(msg);
                            break;
                        case Status.Patrolling:
                            state.CompleteStateAndChangeTo(Status.Waiting, this);
                            break;
                        case Status.Waiting:
                            if (connector.Status == MyShipConnectorStatus.Unconnected)
                                if (this.NeedsService())
                                    state.CompleteStateAndChangeTo(Status.Returning, this);
                                else
                                {
                                    state.SetNextPatrolWaypoint(this);
                                    ResumePatrol(GridProgram, state, samController);
                                }
                            else
                                this.EnemyCheck();
                            break;
                        case Status.PreparingToAttack:
                            this.Attack();
                            break;
                    }
                }
            }

            this.SetRuntimeFrequency();
            this.ManageAntennas();
        }

        public void ResumePatrol(MyGridProgram GridProgram, State state, IMyProgrammableBlock sam_controller)
        {
            GridProgram.Echo($"{Prompts.PatrolPoint} {state.CurrentPatrolPoint}");
            state.CompleteStateAndChangeTo(Status.Patrolling, this);
            this.Go(state.PatrolRoute[state.CurrentPatrolPoint], false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)));
        }

        public void StatusReport()
        {
            GridProgram.Echo($"{Prompts.CurrentMode}: Patrolling");
            GridProgram.Echo($"{Prompts.CurrentStatus}: {state.Status.ToHumanReadableName()}");
            GridProgram.Echo($"{Prompts.NavigationModel}: {navigationModel.ToHumanReadableName()}");
            GridProgram.Echo($"{Prompts.Enroute}: {state.Enroute}");
            if (state.Enroute)
                GridProgram.Echo($"{Prompts.DistanceToWaypoint}: {Math.Round(this.DistanceToWaypoint())}");
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

        public bool HandleCommand(CommandType commandType, string[] args = default(string[]))
        {
            switch (commandType)
            {
                case CommandType.On:
                    GridProgram.Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    return true;
                case CommandType.Off:
                    TurnOff();
                    return true;
                case CommandType.Scan:
                    try
                    {
                        this.ScanForTarget(args[0], int.Parse(args[1]));
                        return true;
                    }
                    catch { return false; }
                case CommandType.DebugEnroute:
                    bool value;
                    if (bool.TryParse(args[0], out value))
                    {
                        this.state.Enroute = value;
                        return true;
                    }
                    else
                        return false;
                case CommandType.DebugStatus:
                    try
                    {
                        this.state.Status = args[0].StatusFromHumanReadableName();
                    }
                    catch
                    {
                        GridProgram.Echo("I dont recognize that value....");
                        return false;
                    }
                    return true;
                case CommandType.Return:
                    state.Status = Status.Returning;
                    this.Go(state.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)));
                    return true;
                case CommandType.Setup:
                    SetUp();
                    return true;
                case CommandType.Reset:
                    ClearData();
                    TurnOff();
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