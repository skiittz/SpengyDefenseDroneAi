using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    public static class CombatCortex
    {
        public static void EnemyCheck(this IAdvancedAiBrain brain)
        {
            if (brain.NeedsService())
                return;

            var target = brain.Cortex<ITurretTargetFinderCortex>().FindTarget();
            
            if (!target.HasValue) return;

            brain.BroadcastTarget(target.Value);
            brain.Attack(target.Value.Position);
        }

        public static void EnemyCheck(this IAiBrain brain)
        {
            var target = brain.Cortex<ITurretTargetFinderCortex>().FindTarget();
            if(target.HasValue) brain.BroadcastTarget(target.Value);
        }

       public static void Attack(this IAdvancedAiBrain brain, Vector3D target)
        {
            brain.state.PendingTarget = Vector3D.Zero;
            brain.GridProgram.Echo(Prompts.Attacking);
            brain.state.Status = Status.Attacking;

            var distance = brain.DistanceToWaypoint(target);
            var vmulti = distance / 600;
            var targetDir = Vector3D.Subtract(target, brain.remote.GetPosition());
            targetDir = Vector3D.Multiply(targetDir, vmulti);
            var attackPos = Vector3D.Add(brain.remote.GetPosition(), targetDir);

            brain.Cortex<INavigationCortex>().Go(attackPos);
        }
    }
}