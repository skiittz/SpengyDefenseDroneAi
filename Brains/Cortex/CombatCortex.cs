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
            FindTarget(brain);
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
            var antenna = brain.GridProgram.FirstTaggedOrDefault<IMyRadioAntenna>(brain.configuration.For(ConfigName.Tag));

            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(turrets, block => block.IsSameConstructAs(brain.GridProgram.Me));
            bool targetDetected = false;
            MyDetectedEntityInfo? target = null;
            foreach (var turret in turrets)
            {
                if (turret.HasTarget)
                {
                    targetDetected = true;
                    target = turret.GetTargetedEntity();
                    brain.GridProgram.Echo($"{Prompts.EnemyDetected}: " + target.Value.Position);
                    antenna.EnableBroadcasting = true;
                    brain.BroadcastTarget(target.Value);                  
                    break;
                }
            }

            if (!targetDetected)
            {
                var sensors = new List<IMySensorBlock>();
                brain.GridProgram.GridTerminalSystem.GetBlocksOfType(sensors, block => block.IsSameConstructAs(brain.GridProgram.Me));

                foreach (var sensor in sensors)
                {
                    var detectedEnemies = new List<MyDetectedEntityInfo>();
                    sensor.DetectedEntities(detectedEnemies);
                    var targets = detectedEnemies.Where(x => x.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies).ToList();

                    if (!targets.Any())
                        return null;

                    targetDetected = true;
                    target = targets.First();
                    brain.BroadcastTarget(target.Value);
                }
            }

            if (!targetDetected && brain.configuration.IsEnabled(ConfigName.UseBurstTransmissions))
                antenna.EnableBroadcasting = false;

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
            brain.Go(attackPos, false, (int)speedLimit);
        }
    }
}
