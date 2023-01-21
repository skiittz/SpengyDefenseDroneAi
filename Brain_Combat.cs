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
        public void EnemyCheck()
        {
            var turrets = new List<IMyLargeTurretBase>();
            var antennae = FirstTaggedOrDefault<IMyRadioAntenna>();

            GridTerminalSystem.GetBlocksOfType(turrets, block => block.IsSameConstructAs(Me));
            bool targetDetected = false;
            foreach (var turret in turrets)
            {
                if (turret.HasTarget)
                {
                    targetDetected = true;
                    var target = turret.GetTargetedEntity();
                    Echo($"{Prompts.EnemyDetected}: " + target.Position);
                    antennae.EnableBroadcasting = true;
                    IGC.SendBroadcastMessage(configuration.For(ConfigName.RadioChannel), target.Position.ToString(), TransmissionDistance.TransmissionDistanceMax);
                    if (CurrentMode() != Mode.TargetOnly && !NeedsService())
                        Attack(target.Position);
                    break;
                }
            }

            if (!targetDetected)
            {
                var sensors = new List<IMySensorBlock>();
                GridTerminalSystem.GetBlocksOfType(sensors, block => block.IsSameConstructAs(Me));

                foreach(var sensor in sensors)
                {
                    var detectedEnemies = new List<MyDetectedEntityInfo>();
                    sensor.DetectedEntities(detectedEnemies);
                    var targets = detectedEnemies.Where(x => x.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies).ToList();

                    if (!targets.Any())
                        return;

                    IGC.SendBroadcastMessage(configuration.For(ConfigName.RadioChannel), targets.First().Position.ToString(), TransmissionDistance.TransmissionDistanceMax);
                }
            }
        }

        public void Attack(Vector3D target)
        {
            Echo(Prompts.Attacking);
            MyState.Status = Status.Attacking;

            var distance = DistanceToWaypoint(target);
            var vmulti = distance / 600;
            var targetDir = Vector3D.Subtract(target, remote.GetPosition());
            targetDir = Vector3D.Multiply(targetDir, vmulti);
            var attackPos = Vector3D.Add(remote.GetPosition(), targetDir);

            var speedLimit = distance < 600 ? (float)Math.Pow(distance / 600, 4) * float.Parse(configuration.For(ConfigName.AttackSpeedLimit)) : float.Parse(configuration.For(ConfigName.AttackSpeedLimit));
            speedLimit = Math.Max(float.Parse(configuration.For(ConfigName.DockSpeedLimit)), speedLimit);
            Go(attackPos, false, (int)speedLimit);
        }
    }
}
