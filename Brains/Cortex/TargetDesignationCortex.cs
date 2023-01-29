using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    public static class TargetDesignationCortex
    {
        public static void ScanForTarget(this IAiBrain brain, string cameraName, int range)
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
            var target = camera.Scan(range).EnemyPosition();
            if (!target.HasValue)
            {
                brain.GridProgram.Echo("No Target");
                return;
            }


            brain.GridProgram.Echo(
                $"Target found: {NavigationCortex.DistanceBetween(camera.GetPosition(), target.Value)}");
            brain.GridProgram.Echo(target.Value.ToString());
            brain.BroadcastTarget(target.Value);
        }

        public static MyDetectedEntityInfo Scan(this IMyCameraBlock camera, int range = 10000)
        {
            camera.EnableRaycast = true;
            return camera.Raycast(range);
        }

        public static Vector3D? EnemyPosition(this MyDetectedEntityInfo detected)
        {
            return detected.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies
                ? detected.Position
                : (Vector3D?)null;
        }
    }
}