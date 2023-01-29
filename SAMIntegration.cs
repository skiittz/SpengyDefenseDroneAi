using Sandbox.ModAPI.Ingame;
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