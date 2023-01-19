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
        public void Go(Vector3D Destination, bool Docking, int speedLimit)
        {
            remote.SetAutoPilotEnabled(false);
            MyState.Enroute = true;            
            remote.ClearWaypoints();
            remote.AddWaypoint(Destination, Prompts.Destination);
            remote.SetAutoPilotEnabled(true);
            remote.SpeedLimit = speedLimit;
            remote.SetDockingMode(Docking);
            remote.SetCollisionAvoidance(!Docking);
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
            return DistanceToWaypoint(remote.CurrentWaypoint.Coords);
        }

        public double DistanceToWaypoint(Vector3D destination)
        {
            var distance = Math.Sqrt(Math.Pow(remote.GetPosition().X - destination.X, 2) + Math.Pow(remote.GetPosition().Y - destination.Y, 2) + Math.Pow(remote.GetPosition().Z - destination.Z, 2));
            Echo($"{Prompts.DistanceToWaypoint}: {distance}");
            return distance;
        }
    }
}
