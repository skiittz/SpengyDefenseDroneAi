using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class PatrollerBrain : IAdvancedAiBrain
    {
        public PatrollerBrain(string storage, MyGridProgram GridProgram, Configuration configuration,
            List<IMyBroadcastListener> listeners, bool weaponCoreIsActive, WcPbApi wcPbApi)
        {
            State _state;
            try
            {
                _state = storage == string.Empty ? new State() : State.Deserialize(storage);
            }
            catch
            {
                _state = new State();
            }
            this.state = _state;
            this.GridProgram = GridProgram;
            this.configuration = configuration;
            this.listeners = listeners;
            this.GetBasicBlocks();
            MyBrainType = BrainType.Patrol;
            this.wcPbApi = wcPbApi;
            this.weaponCoreIsActive = weaponCoreIsActive;
            this.SetUpRadioListeners();
        }

        public BrainType MyBrainType { get; set; }
        public List<ICortex> cortices { get; set; }
        public IMyRemoteControl remote { get; set; }
        public IMyShipConnector connector { get; set; }
        public IMyProgrammableBlock samController { get; set; }
        public List<IMyBatteryBlock> batteries { get; set; }
        public List<IMyGasTank> h2Tanks { get; set; }
        public List<IMyReactor> reactors { get; set; }

        public MyGridProgram GridProgram { get; set; }
        public Configuration configuration { get; set; }
        public State state { get; set; }
        public List<IMyBroadcastListener> listeners { get; set; }
        public bool weaponCoreIsActive { get; set; }
        public WcPbApi wcPbApi { get; set; }

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
                {
                    state.Status = Status.Waiting;
                }
            }
            else
            {
                if (state.Status == Status.Patrolling || state.Status == Status.Waiting || state.Status == Status.Attacking)
                    if (argument.Equals(CommandType.NewTarget.ToHumanReadableName()))
                    {
                        var packet = listeners[0].AcceptMessage();
                        GridProgram.Echo(packet.ToString());
                        Vector3D targetPosition;
                        if (Vector3D.TryParse((string)packet.Data, out targetPosition))
                        {
                            state.PendingTarget = targetPosition;
                            state.CompleteStateAndChangeTo(Status.PreparingToAttack, this);
                        }
                    }
                    else
                        this.EnemyCheck();
                if (state.Enroute)
                {
                    var distanceToWaypoint = this.DistanceToWaypoint();
                    if (distanceToWaypoint < 50)
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
                else
                {
                    switch (state.Status)
                    {
                        case Status.Returning:
                            this.Cortex<INavigationCortex>().Go(state.DockApproach);
                            break;
                        case Status.Docking:
                            state.CurrentDestination = state.DockPos;
                            this.Cortex<INavigationCortex>().Go(state.DockPos, forceKeenModel:true);
                            break;
                        case Status.Patrolling:
                            state.CompleteStateAndChangeTo(Status.Waiting, this);
                            break;
                        case Status.Waiting:
                            if (connector.Status == MyShipConnectorStatus.Unconnected)
                                if (this.NeedsService())
                                {
                                    state.CompleteStateAndChangeTo(Status.Returning, this);
                                }
                                else
                                {
                                    state.SetNextPatrolWaypoint(this);
                                    ResumePatrol();
                                }
                            else
                                this.EnemyCheck();

                            break;
                        case Status.PreparingToAttack:
                            this.Attack(state.PendingTarget);
                            break;
                    }
                }
            }

            this.SetRuntimeFrequency();
            this.ManageAntennas();
        }

        public void StatusReport()
        {
            GridProgram.Echo($"{Prompts.CurrentMode}: Patrolling");
            GridProgram.Echo($"{Prompts.CurrentStatus}: {state.Status.ToHumanReadableName()}");
            this.Cortex<INavigationCortex>().EchoModel();
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
            RefreshDockApproach(); //refresh in case clearance config has been changed
            return !state.DockPos.IsZero() && !state.DockApproach.IsZero() && state.PatrolRoute.Any();
        }

        public bool HandleCommand(CommandType commandType, string[] args = default(string[]))
        {
            switch (commandType)
            {
                case CommandType.NewTarget:
                    Process(commandType.ToHumanReadableName());
                    return true;
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
                    catch
                    {
                        return false;
                    }
                case CommandType.DebugEnroute:
                    bool value;
                    if (bool.TryParse(args[0], out value))
                    {
                        state.Enroute = value;
                        return true;
                    }

                    return false;
                case CommandType.DebugStatus:
                    try
                    {
                        state.Status = args[0].StatusFromHumanReadableName();
                    }
                    catch
                    {
                        GridProgram.Echo("I dont recognize that value....");
                        return false;
                    }

                    return true;
                case CommandType.Return:
                    state.Status = Status.Returning;
                    this.Cortex<INavigationCortex>().Go(state.DockApproach);
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

            foreach (var waypoint in waypoints) state.PatrolRoute.Add(waypoint.Coords);

            GridProgram.Runtime.UpdateFrequency = UpdateFrequency.Update100;
            return true;
        }

        public void RefreshDockApproach()
        {
            state.DockApproach = state.DockPos +
                                 connector.WorldMatrix.Backward *
                                 int.Parse(configuration.For(ConfigName.DockClearance));
        }

        public void ResumePatrol()
        {
            GridProgram.Echo($"{Prompts.PatrolPoint} {state.CurrentPatrolPoint}");
            state.CompleteStateAndChangeTo(Status.Patrolling, this);
            this.Cortex<INavigationCortex>().Go(state.PatrolRoute[state.CurrentPatrolPoint]);
        }
    }
}