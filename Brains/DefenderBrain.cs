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
            state.SetControllers(remote, samController);
        }

        public void Process(string argument)
        {

            if (state.Status == Status.Attacking)
                CombatFunctions.EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);

            if (state.Enroute)
            {
                var distanceToWaypoint = NavigationFunctions.DistanceToWaypoint(this);
                switch (state.Status)
                {
                    case Status.Docking:
                        if (connector.Status == MyShipConnectorStatus.Connectable)
                            NavigationFunctions.Dock(this);
                        break;
                    case Status.Returning:
                        if (connector.Status == MyShipConnectorStatus.Connected)
                            state.CompleteStateAndChangeTo(Status.Waiting);

                        CombatFunctions.EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);

                        if (distanceToWaypoint < 3)
                            state.CompleteStateAndChangeTo(Status.Docking);
                        break;
                    case Status.Waiting:
                        if (connector.Status == MyShipConnectorStatus.Connected)
                            state.Enroute = false;
                        else if (distanceToWaypoint < 50)
                            state.CompleteStateAndChangeTo(Status.Returning);
                        break;
                    case Status.PreparingToAttack:
                        if (distanceToWaypoint < 3)
                        {
                            CombatFunctions.Attack(state, float.Parse(configuration.For(ConfigName.AttackSpeedLimit)), remote, this);
                            state.PendingTarget = Vector3D.Zero;
                        }
                        break;
                    case Status.Attacking:
                        if (distanceToWaypoint < 50)
                            state.CompleteStateAndChangeTo(Status.Returning);
                        CombatFunctions.EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);
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
                            if (argument.Equals("NewTarget") && !ServiceFunctions.NeedsService(GridProgram, configuration, batteries, reactors, h2Tanks))
                            {
                                var packet = listeners[0].AcceptMessage();
                                GridProgram.Echo(packet.ToString());
                                Vector3D targetPosition;
                                if (Vector3D.TryParse((string)packet.Data, out targetPosition))
                                {
                                    state.PendingTarget = targetPosition;
                                    state.Status = Status.PreparingToAttack;
                                    NavigationFunctions.UnDock(int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), this);
                                }
                            }
                            else
                            {
                                GridProgram.Echo(Prompts.WaitingForSignal);
                                CombatFunctions.EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);
                            }
                        }
                        else
                            state.CompleteStateAndChangeTo(Status.Docking);
                        break;
                    case Status.Returning:
                        NavigationFunctions.Go(state.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), this);
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
                CombatFunctions.EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks, this);

            this.SetRuntimeFrequency();
        }

        public void StatusReport()
        {
            GridProgram.Echo($"{Prompts.CurrentMode}: Defending");
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
            return !state.DockPos.IsZero() && !state.DockApproach.IsZero();
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

            return true;
        }

        public void RefreshDockApproach()
        {
            state.DockApproach = state.DockPos + (connector.WorldMatrix.Backward * int.Parse(configuration.For(ConfigName.DockClearance)));
        }
    }
}
