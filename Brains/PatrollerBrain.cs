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
        public class PatrollerBrain : AiBrain
        {
            private Program.State MyState { get; set; }
            public PatrollerBrain(Program.State state)
            {
                MyState = state;
            }

            public void Process(string argument, IMyShipConnector connector)
            {
                if (connector.Status == MyShipConnectorStatus.Connected)
                {
                    if (!NeedsService())
                    {
                        MyState.Status = Status.Patrolling;
                        UnDock();
                    }
                    else
                        MyState.Status = Status.Waiting;
                }
                else
                {
                    if ((MyState.Status == Status.Patrolling || MyState.Status == Status.Waiting || MyState.Status == Status.Attacking))
                        EnemyCheck();
                    if (MyState.Enroute)
                    {
                        var distanceToWaypoint = DistanceToWaypoint();
                        if (distanceToWaypoint < 3)
                        {
                            switch (MyState.Status)
                            {
                                case Status.Docking:
                                    if (connector.Status == MyShipConnectorStatus.Connectable)
                                        Dock();
                                    break;
                                case Status.Returning:
                                    MyState.CompleteStateAndChangeTo(Status.Docking);
                                    break;
                                case Status.Waiting:
                                    if (NeedsService())
                                        MyState.CompleteStateAndChangeTo(Status.Returning);
                                    else
                                        MyState.CompleteStateAndChangeTo(Status.Waiting);
                                    break;
                                case Status.Patrolling:
                                    MyState.CompleteStateAndChangeTo(Status.Patrolling);
                                    break;
                                case Status.Attacking:
                                    MyState.CompleteStateAndChangeTo(Status.Waiting);
                                    EnemyCheck();
                                    break;
                            }
                        }
                    }
                    else
                    {
                        switch (MyState.Status)
                        {
                            case Status.Returning:
                                Go(MyState.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)));
                                break;
                            case Status.Docking:
                                MyState.CurrentDestination = MyState.DockPos;
                                string msg;
                                MyState.Enroute = KeenNav_Controller.Go(remote, MyState.DockPos, true, int.Parse(configuration.For(ConfigName.DockSpeedLimit)), out msg);
                                //Echo(msg);
                                break;
                            case Status.Patrolling:
                                MyState.CompleteStateAndChangeTo(Status.Waiting);
                                break;
                            case Status.Waiting:
                                if (connector.Status == MyShipConnectorStatus.Unconnected)
                                    if (NeedsService())
                                        MyState.CompleteStateAndChangeTo(Status.Returning);
                                    else
                                    {
                                        MyState.SetNextPatrolWaypoint();
                                        ResumePatrol();
                                    }
                                else
                                    EnemyCheck();
                                break;
                            case Status.PreparingToAttack:
                                Program.Attack(MyState.PendingTarget);
                                break;
                        }
                    }
                }
            }

            public void ResumePatrol()
            {
                Echo($"{Prompts.PatrolPoint} {MyState.CurrentPatrolPoint}");
                MyState.CompleteStateAndChangeTo(Status.Patrolling);
                Go(MyState.PatrolRoute[MyState.CurrentPatrolPoint], false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)));
            }
        }
    }
}
