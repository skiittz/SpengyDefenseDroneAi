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
        public List<IMySmallMissileLauncher> GetFixedWeapons()
        {
            var weapons = new List<IMySmallMissileLauncher>();
            GridTerminalSystem.GetBlocksOfType(weapons, block => block.IsSameConstructAs(Me) && IsTaggedForUse(block, "FIXEDREF"));

            return weapons;
        }

        public void CheckAndFireFixedWeapons()
        {
            var weapons = GetFixedWeapons();
            if (!weapons.Any())
                return;

            bool found;
            var camera = SingleTagged<IMyCameraBlock>("FIXEDREF", out found);
            if (!found)
                return;

            var target = camera.Scan(1400).EnemyPosition();
            if (!target.HasValue)
                return;

            var range = DistanceBetween(camera.GetPosition(), target.Value);
            foreach(var weapon in weapons)
            {
                weapon.ShootOnce();
            }
        }
    }
}
