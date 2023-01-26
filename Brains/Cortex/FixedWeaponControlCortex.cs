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
    public static class FixedWeaponControlCortex
    {
        public static void CheckAndFireFixedWeapons(this IAdvancedAiBrain brain)
        {
            brain.GetWeaponGroups().ForEach(g => g.CheckAndFireFixedWeaponsInGroup());
        }
        private static List<IMyBlockGroup> GetWeaponGroups(this IAdvancedAiBrain brain)
        {
            var groups = new List<IMyBlockGroup>();
            brain.GridProgram.GridTerminalSystem
                .GetBlockGroups(groups, group => group.Name.Contains(brain.configuration.For(ConfigName.FixedWeaponReferenceTag)));

            return groups;
        }

        private static void CheckAndFireFixedWeaponsInGroup(this IMyBlockGroup group)
        {
            var cameras = new List<IMyCameraBlock>();
            group.GetBlocksOfType(cameras);

            foreach(var camera in cameras)
            {
                var target = camera.Scan(1400).EnemyPosition();
                if (target.HasValue) {
                    var weapons = new List<IMySmallMissileLauncher>();
                    group.GetBlocksOfType(weapons);

                    foreach (var weapon in weapons)
                        weapon.ShootOnce();
                }
            }
        }
    }
}
