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

            var target = FindTarget(brain);
            brain.SetPendingTarget(target);
        }

        public static void EnemyCheck(this IAiBrain brain)
        {
            brain.FindTarget();
        }

        private static void SetPendingTarget(this IAdvancedAiBrain brain, MyDetectedEntityInfo? target)
        {
            if (target == null)
                return;

            brain.state.PendingTarget = target.Value.Position;
            brain.Attack();
        }

        private static MyDetectedEntityInfo? FindTarget(this IAiBrain brain)
        {
            var turrets = new List<IMyLargeTurretBase>();
            var antenna =
                brain.GridProgram.FirstTaggedOrDefault<IMyRadioAntenna>(brain.configuration.For(ConfigName.Tag));

            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(turrets,
                block => block.IsSameConstructAs(brain.GridProgram.Me));
            var targetDetected = false;
            MyDetectedEntityInfo? target = null;
            brain.GridProgram.Echo("Number of turrets: " + turrets.Count());
            if (brain.weaponCoreIsActive)
            {
                var potentialTargets = new Dictionary<MyDetectedEntityInfo, float>();
                brain.wcPbApi.GetSortedThreats(brain.GridProgram.Me, potentialTargets);
                if (potentialTargets.Any())
                {
                    targetDetected = true;
                    target = potentialTargets.First().Key;
                }
            }
            else
            {
                foreach (var turret in turrets)
                    if (turret.HasTarget)
                    {
                        targetDetected = true;
                        target = turret.GetTargetedEntity();
                        break;
                    }
            }

            if (targetDetected)
            {
                brain.GridProgram.Echo($"{Prompts.EnemyDetected}: " + target.Value.Position);
                brain.BroadcastTarget(target.Value);
            }
            else
            {
                var sensors = new List<IMySensorBlock>();
                brain.GridProgram.GridTerminalSystem.GetBlocksOfType(sensors,
                    block => block.IsSameConstructAs(brain.GridProgram.Me));

                foreach (var sensor in sensors)
                {
                    var detectedEnemies = new List<MyDetectedEntityInfo>();
                    sensor.DetectedEntities(detectedEnemies);
                    var targets = detectedEnemies.Where(x => x.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                        .ToList();

                    if (!targets.Any())
                        return null;

                    target = targets.First();
                    brain.BroadcastTarget(target.Value);
                    break;
                }
            }

            return target;
        }

        public static void Attack(this IAdvancedAiBrain brain)
        {
            var target = brain.state.PendingTarget;
            brain.GridProgram.Echo(Prompts.Attacking);
            brain.state.Status = Status.Attacking;

            var distance = brain.DistanceToWaypoint(target);
            var vmulti = distance / 600;
            var targetDir = Vector3D.Subtract(target, brain.remote.GetPosition());
            targetDir = Vector3D.Multiply(targetDir, vmulti);
            var attackPos = Vector3D.Add(brain.remote.GetPosition(), targetDir);

            var attackSpeedLimit = float.Parse(brain.configuration.For(ConfigName.AttackSpeedLimit));
            var speedLimit = distance < 600 ? (float)Math.Pow(distance / 600, 4) * attackSpeedLimit : attackSpeedLimit;
            speedLimit = Math.Max(5, speedLimit);
            brain.Cortex<INavigationCortex>().Go(attackPos);
        }
    }
}