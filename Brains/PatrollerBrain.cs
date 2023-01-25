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
        public class PatrollerBrain : AdvancedAiBrain
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
            public PatrollerBrain(Program.State state, MyGridProgram mgp, Configuration configuration)
            {
                this.state = state;
                this.mgp = mgp;
                this.configuration = configuration;
                this.GetBasicBlocks();
            }

            public void Process(string argument)
            {
                if (connector.Status == MyShipConnectorStatus.Connected)
                {
                    if (!NeedsService())
                    {
                        state.Status = Status.Patrolling;
                        UnDock();
                    }
                    else
                        state.Status = Status.Waiting;
                }
                else
                {
                    if ((state.Status == Status.Patrolling || state.Status == Status.Waiting || state.Status == Status.Attacking))
                        EnemyCheck(GridProgram, configuration, batteries, reactors, h2Tanks);
                    if (state.Enroute)
                    {
                        var distanceToWaypoint = DistanceToWaypoint();
                        if (distanceToWaypoint < 3)
                        {
                            switch (state.Status)
                            {
                                case Status.Docking:
                                    if (connector.Status == MyShipConnectorStatus.Connectable)
                                        Dock(state);
                                    break;
                                case Status.Returning:
                                    state.CompleteStateAndChangeTo(Status.Docking);
                                    break;
                                case Status.Waiting:
                                    if (NeedsService(mgp, configuration, batteries, reactors, h2Tanks))
                                        state.CompleteStateAndChangeTo(Status.Returning);
                                    else
                                        state.CompleteStateAndChangeTo(Status.Waiting);
                                    break;
                                case Status.Patrolling:
                                    state.CompleteStateAndChangeTo(Status.Patrolling);
                                    break;
                                case Status.Attacking:
                                    state.CompleteStateAndChangeTo(Status.Waiting);
                                    EnemyCheck();
                                    break;
                            }
                        }
                    }
                    else
                    {
                        switch (state.Status)
                        {
                            case Status.Returning:
                                Go(state.DockApproach, false, int.Parse(Program.configuration.For(ConfigName.GeneralSpeedLimit)));
                                break;
                            case Status.Docking:
                                state.CurrentDestination = state.DockPos;
                                string msg;
                                state.Enroute = KeenNav_Controller.Go(remote, state.DockPos, true, int.Parse(configuration.For(ConfigName.DockSpeedLimit)), out msg);
                                mgp.Echo(msg);
                                break;
                            case Status.Patrolling:
                                state.CompleteStateAndChangeTo(Status.Waiting);
                                break;
                            case Status.Waiting:
                                if (connector.Status == MyShipConnectorStatus.Unconnected)
                                    if (NeedsService(mgp, configuration, batteries, reactors, h2Tanks))
                                        state.CompleteStateAndChangeTo(Status.Returning);
                                    else
                                    {
                                        state.SetNextPatrolWaypoint();
                                        ResumePatrol(mgp, state, sam_controller);
                                    }
                                else
                                    EnemyCheck(mgp);
                                break;
                            case Status.PreparingToAttack:
                                Attack(state);
                                break;
                        }
                    }
                }
            }

            public void ResumePatrol(MyGridProgram mgp, State state, IMyProgrammableBlock sam_controller)
            {
                mgp.Echo($"{Prompts.PatrolPoint} {state.CurrentPatrolPoint}");
                state.CompleteStateAndChangeTo(Status.Patrolling);
                Go(state.PatrolRoute[state.CurrentPatrolPoint], false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), state, mgp, sam_controller);
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
        }
    }
}
