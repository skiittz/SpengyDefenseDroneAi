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
    public static class SAM_Controller
    {
        public static string GpsColorDesignation = "#FF75C9F1";       
        public static string SAMFormattedDestination(Vector3D target)
        {
            return $"GPS:Destination:{target.X}:{target.Y}:{target.Z}:{GpsColorDesignation}:";
        }

        public static bool Go(this IMyProgrammableBlock controller, Vector3D target, out string message)
        {
            var command = $"START {SAMFormattedDestination(target)}";
            if (controller.TryRun(command))
            {
                message = $"Issued command to SAM: {command}";
                return true;
            }

            message = $"SAM failed command: {command}";
            return false;
        }
    }    
}
