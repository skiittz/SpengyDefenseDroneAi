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
    static class KeenNav_Controller
    {
        public static bool Go(IMyRemoteControl remote, Vector3D Destination, bool Docking, int speedLimit, out string msg)
        {
            remote.SetAutoPilotEnabled(false);
            remote.SpeedLimit = speedLimit;
            remote.ClearWaypoints();
            remote.AddWaypoint(Destination, Prompts.Destination);
            remote.SetDockingMode(Docking);
            remote.SetCollisionAvoidance(!Docking);
            remote.SetAutoPilotEnabled(true);

            msg = $"Moving to waypoint using Keen Code: {Destination}";

            return true;
        }
    }
}
