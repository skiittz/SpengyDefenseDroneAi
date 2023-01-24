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
    // This template is intended for extension classes. For most purposes you're going to want a normal
    // utility class.
    // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods
    partial class Program
    {
        public void ScanForTarget(string cameraName)
        {
            Echo("Searching for targets");
            var cameras = new List<IMyCameraBlock>();
            GridTerminalSystem.GetBlocksOfType(cameras);
            GridTerminalSystem.GetBlocksOfType(cameras, x => x.CustomName.ToUpper() == cameraName);

            if (cameras.Count != 1)
            {
                Echo("Error locating camera for raycast");
                return;
            }

            var camera = cameras.Single();
            var target = camera.Scan().EnemyPosition();
            if (!target.HasValue)
            {
                Echo("No Target");
                return;
            }


            Echo($"Target found: {DistanceBetween(camera.GetPosition(), target.Value)}");
            Echo(target.Value.ToString());
            IGC.BroadcastTarget(target.Value, configuration.For(ConfigName.MyName));
        }
    }
    
    static class TargetDesignator
    {
        public static MyDetectedEntityInfo Scan(this IMyCameraBlock camera, int range = 10000)
        {
            camera.EnableRaycast = true;
            return camera.Raycast(range);        
        }

        public static Vector3D? EnemyPosition(this MyDetectedEntityInfo detected)
        {
            return detected.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies ? detected.Position : (Vector3D?)null;
        }
    }
}
