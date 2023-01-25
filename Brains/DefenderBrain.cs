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
        public class DefenderBrain : AdvancedAiBrain
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
            
            public DefenderBrain(Program.State state, MyGridProgram gridProgram, Configuration configuration)
            {
                this.GridProgram = gridProgram;
                this.configuration = configuration;
                this.state = state;
                this.GetBasicBlocks();
            }

            public void Process(string argument)
            {

                if (state.Status == Status.Attacking)
                    EnemyCheck();

                if (state.Enroute)
                {
                    var distanceToWaypoint = DistanceToWaypoint(state);
                    switch (state.Status)
                    {
                        case Status.Docking:
                            if (connector.Status == MyShipConnectorStatus.Connectable)
                                Dock();
                            break;
                        case Status.Returning:
                            if (connector.Status == MyShipConnectorStatus.Connected)
                                state.CompleteStateAndChangeTo(Status.Waiting);
                            if (!NeedsService(mgp, configuration, batteries, reactors, h2Tanks))
                                EnemyCheck(mgp);
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
                                Attack(state);
                                state.PendingTarget = Vector3D.Zero;
                            }
                            break;
                        case Status.Attacking:
                            if (distanceToWaypoint < 50)
                                state.CompleteStateAndChangeTo(Status.Returning);
                            if (!NeedsService(mgp,configuration,batteries,reactors,h2Tanks))
                                EnemyCheck(mgp);
                            break;
                    }
                }
                else
                {
                    mgp.Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    switch (state.Status)
                    {
                        case Status.Waiting:
                            if (connector.Status == MyShipConnectorStatus.Connected)
                            {
                                mgp.Echo(argument);
                                if (argument.Equals("NewTarget") && !NeedsService(mgp, configuration, batteries, reactors, h2Tanks))
                                {
                                    var packet = listeners[0].AcceptMessage();
                                    mgp.Echo(packet.ToString());
                                    Vector3D targetPosition;
                                    if (Vector3D.TryParse((string)packet.Data, out targetPosition))
                                    {
                                        state.PendingTarget = targetPosition;
                                        state.Status = Status.PreparingToAttack;
                                        UnDock(mgp, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), state, connector, sam_controller);
                                    }
                                }
                                else
                                {
                                    mgp.Echo(Prompts.WaitingForSignal);
                                    EnemyCheck(mgp);
                                }
                            }
                            else
                                state.CompleteStateAndChangeTo(Status.Docking);
                            break;
                        case Status.Returning:
                            Go(state.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), state, mgp, sam_controller, remote);
                            break;
                        case Status.Docking:
                            string msg;
                            state.CurrentDestination = state.DockPos;
                            state.Enroute = KeenNav_Controller.Go(remote, state.DockPos, true, int.Parse(configuration.For(ConfigName.DockSpeedLimit)), out msg);
                            mgp.Echo(msg);
                            break;
                    }
                }
                if (state.Status == Status.Attacking)
                    EnemyCheck(mgp);
            }

            public void StatusReport()
            {
                GridProgram.Echo($"{Prompts.CurrentMode}: {CurrentMode().ToHumanReadableName()}");
                GridProgram.Echo($"{Prompts.CurrentStatus}: {state.Status.ToHumanReadableName()}");
                GridProgram.Echo($"{Prompts.NavigationModel}: {state.NavigationModel.ToHumanReadableName()}");
                GridProgram.Echo($"{Prompts.Enroute}: {state.Enroute}");
            }

            public void ClearData()
            {
                GridProgram.Storage = string.Empty;
                state = new State();
                if(remote != null)
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
        }
    }
}
