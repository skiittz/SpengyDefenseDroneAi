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
        public void Go(Vector3D destination, bool docking, int speedLimit)
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            string msg = "";
            switch (MyState.NavigationModel)
            {
                case NavigationModel.Keen:
                    MyState.Enroute = KeenNav_Controller.Go(remote, destination, docking, speedLimit, out msg);
                    break;
                case NavigationModel.SAM:
                    MyState.Enroute = SAM_Controller.Go(sam_controller, destination, out msg);
                    break;
            }
            Echo(msg);

            if (MyState.Enroute)
                MyState.CurrentDestination = destination;
        }
       
        public void UnDock()
        {
            var batteries = new List<IMyBatteryBlock>();
            var h2Tanks = new List<IMyGasTank>();

            GridTerminalSystem.GetBlocksOfType(batteries, block => block.IsSameConstructAs(Me));
            GridTerminalSystem.GetBlocksOfType(h2Tanks, block => block.IsSameConstructAs(Me) && block.BlockDefinition.SubtypeName.Contains("Hydro"));

            batteries.ForEach(x => x.ChargeMode = ChargeMode.Auto);
            h2Tanks.ForEach(x => x.Stockpile = false);

            connector.Disconnect();

            Go(MyState.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)));
        }

        public void Dock()
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

        public double DistanceToWaypoint()
        {
            return DistanceToWaypoint(MyState.CurrentDestination);
        }

        public double DistanceToWaypoint(Vector3D destination)
        {
            var distance = Math.Sqrt(Math.Pow(remote.GetPosition().X - destination.X, 2) + Math.Pow(remote.GetPosition().Y - destination.Y, 2) + Math.Pow(remote.GetPosition().Z - destination.Z, 2));
            Echo($"{Prompts.DistanceToWaypoint}: {distance}");
            return distance;
        }
    }
}
