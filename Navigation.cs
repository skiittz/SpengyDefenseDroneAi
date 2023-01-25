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
        public static void Go(Vector3D destination, bool docking, int speedLimit, State MyState, MyGridProgram mgp, IMyProgrammableBlock sam_controller, IMyRemoteControl remote)
        {
            mgp.Runtime.UpdateFrequency = UpdateFrequency.Update10;
            string msg = string.Empty;
            switch (MyState.NavigationModel)
            {
                case NavigationModel.Keen:
                    MyState.Enroute = KeenNav_Controller.Go(remote, destination, docking, speedLimit, out msg);
                    break;
                case NavigationModel.SAM:
                    MyState.Enroute = SAM_Controller.Go(sam_controller, destination, out msg);
                    break;
            }
            mgp.Echo(msg);
            if (MyState.Enroute)
                MyState.CurrentDestination = destination;
        }
       
        public static void UnDock(MyGridProgram mgp, int speedLimit, State MyState, IMyShipConnector connector, IMyProgrammableBlock sam_controller, IMyRemoteControl remote)
        {
            var batteries = new List<IMyBatteryBlock>();
            var h2Tanks = new List<IMyGasTank>();

            mgp.GridTerminalSystem.GetBlocksOfType(batteries, block => block.IsSameConstructAs(mgp.Me));
            mgp.GridTerminalSystem.GetBlocksOfType(h2Tanks, block => block.IsSameConstructAs(mgp.Me) && block.BlockDefinition.SubtypeName.Contains("Hydro"));

            batteries.ForEach(x => x.ChargeMode = ChargeMode.Auto);
            h2Tanks.ForEach(x => x.Stockpile = false);

            connector.Disconnect();

            Go(MyState.DockApproach, false, speedLimit, MyState, mgp, sam_controller, remote);
        }

        public void Dock(State MyState)
        {
            connector.Connect();
            var batteries = new List<IMyBatteryBlock>();
            var h2Tanks = new List<IMyGasTank>();

            GridTerminalSystem.GetBlocksOfType(batteries, block => block.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(h2Tanks, block => block.IsSameConstructAs(Me) && block.BlockDefinition.SubtypeName.Contains("Hydro"));

            batteries.ForEach(x => x.ChargeMode = ChargeMode.Recharge);
            h2Tanks.ForEach(x => x.Stockpile = true);

            MyState.CompleteStateAndChangeTo(Status.Waiting);
        }

        public double DistanceToWaypoint(State MyState)
        {
            return DistanceToWaypoint(MyState.CurrentDestination);
        }

        public double DistanceToWaypoint(Vector3D destination)
        {
            var distance = Math.Sqrt(Math.Pow(remote.GetPosition().X - destination.X, 2) + Math.Pow(remote.GetPosition().Y - destination.Y, 2) + Math.Pow(remote.GetPosition().Z - destination.Z, 2));
            Echo($"{Prompts.DistanceToWaypoint}: {distance}");
            return distance;
        }

        public double DistanceBetween(Vector3D p1, Vector3D p2)
        {
            var distance = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
            return distance;
        }
    }
}
