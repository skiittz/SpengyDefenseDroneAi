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
        public class DefenderBrain : AiBrain
        {
            private Program.State myState { get; set; }

            public DefenderBrain(Program.State state)
            {
                myState = state;
            }
            public void Process(string argument, IMyShipConnector connector)
            {

                if (myState.Status == Status.Attacking)
                    EnemyCheck();

                if (myState.Enroute)
                {
                    var distanceToWaypoint = DistanceToWaypoint();
                    switch (myState.Status)
                    {
                        case Status.Docking:
                            if (connector.Status == MyShipConnectorStatus.Connectable)
                                Dock();
                            break;
                        case Status.Returning:
                            if (connector.Status == MyShipConnectorStatus.Connected)
                                myState.CompleteStateAndChangeTo(Status.Waiting);
                            if (!NeedsService())
                                EnemyCheck();
                            if (distanceToWaypoint < 3)
                                myState.CompleteStateAndChangeTo(Status.Docking);
                            break;
                        case Status.Waiting:
                            if (connector.Status == MyShipConnectorStatus.Connected)
                                myState.Enroute = false;
                            else if (distanceToWaypoint < 50)
                                myState.CompleteStateAndChangeTo(Status.Returning);
                            break;
                        case Status.PreparingToAttack:
                            if (distanceToWaypoint < 3)
                            {
                                Attack(myState.PendingTarget);
                                myState.PendingTarget = Vector3D.Zero;
                            }
                            break;
                        case Status.Attacking:
                            if (distanceToWaypoint < 50)
                                myState.CompleteStateAndChangeTo(Status.Returning);
                            if (!NeedsService())
                                EnemyCheck();
                            break;
                    }
                }
                else
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update10;
                    switch (myState.Status)
                    {
                        case Status.Waiting:
                            if (connector.Status == MyShipConnectorStatus.Connected)
                            {
                                Echo(argument);
                                if (argument.Equals("NewTarget") && !NeedsService())
                                {
                                    var packet = listeners[0].AcceptMessage();
                                    Echo(packet.ToString());
                                    Vector3D targetPosition;
                                    if (Vector3D.TryParse((string)packet.Data, out targetPosition))
                                    {
                                        myState.PendingTarget = targetPosition;
                                        myState.Status = Status.PreparingToAttack;
                                        UnDock();
                                    }
                                }
                                else
                                {
                                    Echo(Prompts.WaitingForSignal);
                                    EnemyCheck();
                                }
                            }
                            else
                                myState.CompleteStateAndChangeTo(Status.Docking);
                            break;
                        case Status.Returning:
                            Go(myState.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)));
                            break;
                        case Status.Docking:
                            string msg;
                            myState.CurrentDestination = myState.DockPos;
                            myState.Enroute = KeenNav_Controller.Go(remote, myState.DockPos, true, int.Parse(configuration.For(ConfigName.DockSpeedLimit)), out msg);
                            Echo(msg);
                            break;
                    }
                }
                if (myState.Status == Status.Attacking)
                    EnemyCheck();
            }
        }
    }
}
