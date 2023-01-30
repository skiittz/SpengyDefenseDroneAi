using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;
using VRage.Game;

namespace IngameScript
{
    public class VanillaTargetFinderCortex : ITurretTargetFinderCortex
    {
        private readonly IAiBrain _brain;

        public VanillaTargetFinderCortex(IAiBrain brain)
        {
            _brain = brain;
        }

        public MyDetectedEntityInfo? FindTarget()
        {
            var turrets = new List<IMyLargeTurretBase>();
            _brain.GridProgram.GridTerminalSystem.GetBlocksOfType(turrets,
                block => block.IsSameConstructAs(_brain.GridProgram.Me));

            var target = turrets.FirstOrDefault(x => x.HasTarget)?.GetTargetedEntity();

            if (target.HasValue)
                _brain.GridProgram.Echo($"{Prompts.EnemyDetected}: {target.Value.Position}");
            else
            {
                var sensors = new List<IMySensorBlock>();
                _brain.GridProgram.GridTerminalSystem.GetBlocksOfType(sensors,
                    block => block.IsSameConstructAs(_brain.GridProgram.Me));

                foreach (var sensor in sensors)
                {
                    var detectedEnemies = new List<MyDetectedEntityInfo>();
                    sensor.DetectedEntities(detectedEnemies);
                    var targets = detectedEnemies.Where(x => x.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                        .ToList();

                    if (!targets.Any()) continue;

                    target = targets.FirstOrDefault();
                }
            }

            return target;
        }
    }
}
