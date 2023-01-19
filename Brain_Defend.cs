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
        public void DeployLogic(string argument)
        {
            if (MyState.Status == Status.Attacking)
                EnemyCheck();

            if (MyState.Enroute)
            {
                var distanceToWaypoint = DistanceToWaypoint();
                switch (MyState.Status)
                {                   
                    case Status.Docking:
                        if (connector.Status == MyShipConnectorStatus.Connectable)
                            Dock();
                        break;
                    case Status.Returning:
                        if (connector.Status == MyShipConnectorStatus.Connected)
                            MyState.CompleteStateAndChangeTo(Status.Waiting);
                        if (!NeedsService())
                            EnemyCheck();
                        if (distanceToWaypoint < 3)
                            MyState.CompleteStateAndChangeTo(Status.Docking);
                        break;
                    case Status.Waiting:
                        if (connector.Status == MyShipConnectorStatus.Connected)
                            MyState.Enroute = false;
                        else if (distanceToWaypoint < 50)
                            MyState.CompleteStateAndChangeTo(Status.Returning);
                        break;
                    case Status.PreparingToAttack:
                        if (distanceToWaypoint < 3)
                        {
                            Attack(MyState.PendingTarget);
                            MyState.PendingTarget = Vector3D.Zero;
                        }
                        break;
                    case Status.Attacking:
                        if(distanceToWaypoint < 50)
                            MyState.CompleteStateAndChangeTo(Status.Returning);
                        EnemyCheck();
                        break;
                }
            }
            else
            {
                switch (MyState.Status)
                {
                    case Status.Waiting:
                        if (connector.Status == MyShipConnectorStatus.Connected)
                        {
                            Echo(argument);
                            if (argument.Equals("NewTarget")/*Special.NewTaraget_RadioSignal)*/ && !NeedsService())
                            {
                                var packet = listeners[0].AcceptMessage();
                                Echo(packet.ToString());
                                Vector3D targetPosition;
                                if (Vector3D.TryParse((string)packet.Data, out targetPosition))
                                {
                                    MyState.PendingTarget = targetPosition;
                                    MyState.Status = Status.PreparingToAttack;
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
                            MyState.CompleteStateAndChangeTo(Status.Docking);
                        break;
                    case Status.Returning:
                        Go(MyState.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)));
                        break;
                    case Status.Docking:
                        Go(MyState.DockPos, true, int.Parse(configuration.For(ConfigName.DockSpeedLimit)));
                        break;
                }
            }
            if (MyState.Status == Status.Attacking)
                EnemyCheck();
        }
    }
}
