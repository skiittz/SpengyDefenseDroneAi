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
        public void ScanForTarget(string cameraName, IAiBrain brain)
        {
            brain.GridProgram.Echo("Searching for targets");
            var cameras = new List<IMyCameraBlock>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(cameras);
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(cameras, x => x.CustomName.ToUpper() == cameraName);

            if (cameras.Count != 1)
            {
                brain.GridProgram.Echo("Error locating camera for raycast");
                return;
            }

            var camera = cameras.Single();
            var target = camera.Scan().EnemyPosition();
            if (!target.HasValue)
            {
                brain.GridProgram.Echo("No Target");
                return;
            }


            brain.GridProgram.Echo($"Target found: {NavigationFunctions.DistanceBetween(camera.GetPosition(), target.Value)}");
            brain.GridProgram.Echo(target.Value.ToString());
            brain.GridProgram.IGC.BroadcastTarget(target.Value, brain.configuration.For(ConfigName.RadioChannel));
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
