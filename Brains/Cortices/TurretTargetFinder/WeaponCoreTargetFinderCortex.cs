using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class WeaponCoreTargetFinderCortex : ITurretTargetFinderCortex
    {
        private readonly IAiBrain _brain;

        public WeaponCoreTargetFinderCortex(IAiBrain brain)
        {
            _brain = brain;
        }

        public MyDetectedEntityInfo? FindTarget()
        {
            var potentialTargets = new Dictionary<MyDetectedEntityInfo, float>();
            _brain.wcPbApi.GetSortedThreats(_brain.GridProgram.Me, potentialTargets);
            
            if (potentialTargets.Any())
                return potentialTargets.First().Key;

            return null;
        }
    }
}
