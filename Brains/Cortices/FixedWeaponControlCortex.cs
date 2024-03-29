﻿using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public static class FixedWeaponControlCortex
    {
        public static void CheckAndFireFixedWeapons(this IAiBrain brain)
        {
            brain.GetWeaponGroups().ForEach(brain.CheckAndFireFixedWeaponsInGroup);
        }

        private static List<IMyBlockGroup> GetWeaponGroups(this IAiBrain brain)
        {
            var groups = new List<IMyBlockGroup>();
            brain.GridProgram.GridTerminalSystem
                .GetBlockGroups(groups,
                    group => group.Name.Contains(brain.configuration.For(ConfigName.FixedWeaponReferenceTag)));

            return groups;
        }

        private static void CheckAndFireFixedWeaponsInGroup(this IAiBrain brain, IMyBlockGroup group)
        {
            var cameras = new List<IMyCameraBlock>();
            group.GetBlocksOfType(cameras);

            foreach (var camera in cameras)
            {
                var target = camera.Scan(1400).EnemyPosition();
                if (target.HasValue)
                {
                    var weapons = new List<IMySmallMissileLauncher>();
                    group.GetBlocksOfType(weapons);

                    if (brain.weaponCoreIsActive)
                    {
                        foreach (var weapon in weapons)
                        {
                            brain.wcPbApi.FireWeaponOnce(weapon);
                        }
                    }
                    else
                    {
                        foreach (var weapon in weapons)
                            weapon.ShootOnce();
                    }
                }
            }
        }
    }
}