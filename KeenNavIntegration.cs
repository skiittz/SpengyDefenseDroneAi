using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    internal static class KeenNav_Controller
    {
        public static bool Go(IMyRemoteControl remote, Vector3D Destination, bool Docking, int speedLimit,
            out string msg)
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