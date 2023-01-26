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
        public static void Go(Vector3D destination, bool docking, int speedLimit, IAdvancedAiBrain brain)
        {
            brain.GridProgram.Runtime.UpdateFrequency = UpdateFrequency.Update10;
            string msg = string.Empty;
            switch (brain.navigationModel)
            {
                case NavigationModel.Keen:
                    brain.state.Enroute = KeenNav_Controller.Go(brain.remote, destination, docking, speedLimit, out msg);
                    break;
                case NavigationModel.SAM:
                    brain.state.Enroute = SAM_Controller.Go(brain.samController, destination, out msg);
                    break;
            }
            brain.GridProgram.Echo(msg);
            if (brain.state.Enroute)
                brain.state.CurrentDestination = destination;
        }
       
        public static void UnDock(int speedLimit, IAdvancedAiBrain brain)
        {
            brain.batteries.ForEach(x => x.ChargeMode = ChargeMode.Auto);
            brain.h2Tanks.ForEach(x => x.Stockpile = false);

            brain.connector.Disconnect();

            Go(brain.state.DockApproach, false, speedLimit, brain);
        }

        public static void Dock(Program.IAdvancedAiBrain brain)
        {
            brain.connector.Connect();

            brain.batteries.ForEach(x => x.ChargeMode = ChargeMode.Recharge);
            brain.h2Tanks.ForEach(x => x.Stockpile = true);

            brain.state.CompleteStateAndChangeTo(Status.Waiting);
        }

        public static double DistanceToWaypoint(IAdvancedAiBrain brain)
        {
            return DistanceToWaypoint(brain.state.CurrentDestination, brain.remote, brain.GridProgram);
        }

        public static double DistanceToWaypoint(Vector3D destination, IMyRemoteControl remote, MyGridProgram mgp)
        {
            var distance = Math.Sqrt(Math.Pow(remote.GetPosition().X - destination.X, 2) + Math.Pow(remote.GetPosition().Y - destination.Y, 2) + Math.Pow(remote.GetPosition().Z - destination.Z, 2));
            mgp.Echo($"{Prompts.DistanceToWaypoint}: {distance}");
            return distance;
        }

        public double DistanceBetween(Vector3D p1, Vector3D p2)
        {
            var distance = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
            return distance;
        }
    }
}
