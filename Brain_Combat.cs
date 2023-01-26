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
    public static class CombatFunctions
    {
        public static void EnemyCheck(MyGridProgram mgp, Configuration configuration, List<IMyBatteryBlock> batteries, List<IMyReactor> reactors, List<IMyGasTank> h2Tanks, IAdvancedAiBrain brain)
        {
            if (ServiceFunctions.NeedsService(mgp, configuration, batteries, reactors, h2Tanks))
                return;

            var turrets = new List<IMyLargeTurretBase>();
            var antenna = mgp.FirstTaggedOrDefault<IMyRadioAntenna>(configuration.For(ConfigName.Tag));

            mgp.GridTerminalSystem.GetBlocksOfType(turrets, block => block.IsSameConstructAs(mgp.Me));
            bool targetDetected = false;
            foreach (var turret in turrets)
            {
                if (turret.HasTarget)
                {
                    targetDetected = true;
                    var target = turret.GetTargetedEntity();
                    mgp.Echo($"{Prompts.EnemyDetected}: " + target.Position);
                    antenna.EnableBroadcasting = true;
                    mgp.IGC.BroadcastTarget(target, configuration.For(ConfigName.RadioChannel));

                    if (brain != null)
                    {
                        brain.state.PendingTarget = target.Position;
                        Attack(brain.state, int.Parse(configuration.For(ConfigName.AttackSpeedLimit)), brain.remote, brain);
                    }
                    break;
                }
            }

            if (!targetDetected)
            {
                var sensors = new List<IMySensorBlock>();
                mgp.GridTerminalSystem.GetBlocksOfType(sensors, block => block.IsSameConstructAs(mgp.Me));

                foreach (var sensor in sensors)
                {
                    var detectedEnemies = new List<MyDetectedEntityInfo>();
                    sensor.DetectedEntities(detectedEnemies);
                    var targets = detectedEnemies.Where(x => x.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies).ToList();

                    if (!targets.Any())
                        return;

                    mgp.IGC.BroadcastTarget(targets.First(), configuration.For(ConfigName.RadioChannel));
                }
            }

            if (!targetDetected && configuration.IsEnabled(ConfigName.UseBurstTransmissions))
                antenna.EnableBroadcasting = false;
        }

        public static void Attack(State MyState, float attackSpeedLimit, IMyRemoteControl remote, IAdvancedAiBrain brain)
        {
            var target = MyState.PendingTarget;
            brain.GridProgram.Echo(Prompts.Attacking);
            MyState.Status = Status.Attacking;

            var distance = NavigationFunctions.DistanceToWaypoint(target, remote, brain.GridProgram);
            var vmulti = distance / 600;
            var targetDir = Vector3D.Subtract(target, remote.GetPosition());
            targetDir = Vector3D.Multiply(targetDir, vmulti);
            var attackPos = Vector3D.Add(remote.GetPosition(), targetDir);

            var speedLimit = distance < 600 ? (float)Math.Pow(distance / 600, 4) * attackSpeedLimit : attackSpeedLimit;
            speedLimit = Math.Max(5, speedLimit);
            NavigationFunctions.Go(attackPos, false, (int)speedLimit, brain);
        }
    }
}
