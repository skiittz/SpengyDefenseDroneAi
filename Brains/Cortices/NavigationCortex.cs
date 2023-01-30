using System;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public enum NavigationModel
    {
        Keen,
        SAM
    }

    public static class StaticNavigationCortex
    {
        public static void UnDock(this IAdvancedAiBrain brain)
        {
            brain.batteries.ForEach(x => x.ChargeMode = ChargeMode.Auto);
            brain.h2Tanks.ForEach(x => x.Stockpile = false);

            brain.connector.Disconnect();

            brain.Cortex<INavigationCortex>().Go(brain.state.DockApproach);
        }

        public static void Dock(this IAdvancedAiBrain brain)
        {
            brain.connector.Connect();

            brain.batteries.ForEach(x => x.ChargeMode = ChargeMode.Recharge);
            brain.h2Tanks.ForEach(x => x.Stockpile = true);

            brain.state.CompleteStateAndChangeTo(Status.Waiting, brain);
        }

        public static double DistanceToWaypoint(this IAdvancedAiBrain brain)
        {
            return brain.DistanceToWaypoint(brain.state.CurrentDestination);
        }

        public static double DistanceToWaypoint(this IAdvancedAiBrain brain, Vector3D destination)
        {
            return DistanceBetween(brain.remote.GetPosition(), destination);
        }

        public static double DistanceBetween(Vector3D p1, Vector3D p2)
        {
            var distance = Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2) + Math.Pow(p1.Z - p2.Z, 2));
            return distance;
        }
    }
}