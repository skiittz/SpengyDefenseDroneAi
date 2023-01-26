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
    public class DefenderBrain : IAdvancedAiBrain
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


        public DefenderBrain(State state, MyGridProgram gridProgram, Configuration configuration, List<IMyBroadcastListener> listeners)
        {
            this.GridProgram = gridProgram;
            this.configuration = configuration;
            this.state = state;
            this.listeners = listeners;
            this.GetBasicBlocks();
        }

        public void Process(string argument)
        {

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
                            this.Attack();
                            state.PendingTarget = Vector3D.Zero;
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
                            if (argument.Equals("NewTarget") && !this.NeedsService())
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
                            state.CompleteStateAndChangeTo(Status.Docking, this);
                        break;
                    case Status.Returning:
                        this.Go(state.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)));
                        break;
                    case Status.Docking:
                        string msg;
                        state.CurrentDestination = state.DockPos;
                        state.Enroute = KeenNav_Controller.Go(remote, state.DockPos, true, int.Parse(configuration.For(ConfigName.DockSpeedLimit)), out msg);
                        GridProgram.Echo(msg);
                        break;
                }
            }
            if (state.Status == Status.Attacking)
                this.EnemyCheck();

            this.SetRuntimeFrequency();
        }

        public void StatusReport()
        {
            GridProgram.Echo($"{Prompts.CurrentMode}: Defending");
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
            return !state.DockPos.IsZero() && !state.DockApproach.IsZero();
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

            return true;
        }

        public void RefreshDockApproach()
        {
            state.DockApproach = state.DockPos + (connector.WorldMatrix.Backward * int.Parse(configuration.For(ConfigName.DockClearance)));
        }
    }
}
