using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class DefenderBrain : IAdvancedAiBrain
    {
        public DefenderBrain(string storage, MyGridProgram gridProgram, Configuration configuration,
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

            GridProgram = gridProgram;
            this.configuration = configuration;
            this.state = _state;
            this.listeners = listeners;
            this.GetBasicBlocks();
            MyBrainType = BrainType.Defend;
            this.wcPbApi = wcPbApi;
            this.weaponCoreIsActive = weaponCoreIsActive;
            this.SetUpRadioListeners();
            cortices = this.CreateCortices().ToList();
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
            if (state.Status == Status.Attacking)
                this.EnemyCheck();

            if (state.Enroute)
            {
                var distanceToWaypoint = this.DistanceToWaypoint();
                switch (state.Status)
                {
                    case Status.Docking:
                        if (connector.Status == MyShipConnectorStatus.Connectable)
                            this.Dock();
                        break;
                    case Status.Returning:
                        if (connector.Status == MyShipConnectorStatus.Connected)
                            state.CompleteStateAndChangeTo(Status.Waiting, this);

                        this.EnemyCheck();

                        if (distanceToWaypoint < 3)
                            state.CompleteStateAndChangeTo(Status.Docking, this);
                        break;
                    case Status.Waiting:
                        if (connector.Status == MyShipConnectorStatus.Connected)
                            state.Enroute = false;
                        else if (distanceToWaypoint < 50)
                            state.CompleteStateAndChangeTo(Status.Returning, this);
                        break;
                    case Status.PreparingToAttack:
                        if (distanceToWaypoint < 3)
                        {
                            this.Attack(state.PendingTarget);
                        }

                        break;
                    case Status.Attacking:
                        if (distanceToWaypoint < 50)
                            state.CompleteStateAndChangeTo(Status.Returning, this);
                        this.EnemyCheck();
                        break;
                }
            }
            else
            {
                GridProgram.Runtime.UpdateFrequency = UpdateFrequency.Update10;
                switch (state.Status)
                {
                    case Status.Waiting:
                        if (connector.Status == MyShipConnectorStatus.Connected)
                        {
                            GridProgram.Echo(argument);
                            if (argument.Equals(CommandType.NewTarget.ToHumanReadableName()) && !this.NeedsService())
                            {
                                var packet = listeners[0].AcceptMessage();
                                GridProgram.Echo(packet.ToString());
                                Vector3D targetPosition;
                                if (Vector3D.TryParse((string)packet.Data, out targetPosition))
                                {
                                    state.PendingTarget = targetPosition;
                                    state.Status = Status.PreparingToAttack;
                                    this.UnDock();
                                }
                            }
                            else
                            {
                                GridProgram.Echo(Prompts.WaitingForSignal);
                                this.EnemyCheck();
                            }
                        }
                        else
                        {
                            state.CompleteStateAndChangeTo(Status.Docking, this);
                        }

                        break;
                    case Status.Returning:
                        this.Cortex<INavigationCortex>().Go(state.DockApproach);
                        break;
                    case Status.Docking:
                        state.CurrentDestination = state.DockPos;
                        this.Cortex<INavigationCortex>().Go(state.DockPos, forceKeenModel:true);
                        break;
                }
            }

            if (state.Status == Status.Attacking)
                this.EnemyCheck();

            this.SetRuntimeFrequency();
            this.ManageAntennas();
        }

        public void StatusReport()
        {
            GridProgram.Echo($"{Prompts.CurrentMode}: Defending");
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

            GridProgram.Echo("Ive cleared the data!");
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
            return !state.DockPos.IsZero() && !state.DockApproach.IsZero();
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
                    GridProgram.Echo("Inside command handler/reset");
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

            GridProgram.Runtime.UpdateFrequency = UpdateFrequency.Update100;
            return true;
        }

        public void RefreshDockApproach()
        {
            state.DockApproach = state.DockPos +
                                 connector.WorldMatrix.Backward *
                                 int.Parse(configuration.For(ConfigName.DockClearance));
        }
    }
}