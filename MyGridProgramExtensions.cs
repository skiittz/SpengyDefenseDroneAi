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
    public static class MyGridProgramExtensions
    {

        public static T FirstTaggedOrDefault<T>(this MyGridProgram mgp, string tag) where T : IMyTerminalBlock
        {
            var list = new List<T>();
            mgp.GridTerminalSystem.GetBlocksOfType(list, block => block.IsSameConstructAs(mgp.Me));
            return list.FirstOrDefault(x => IsTaggedForUse(x, tag)) ?? list.FirstOrDefault();
        }

        public static T SingleTagged<T>(this MyGridProgram mgp, string tag, out bool found) where T : IMyTerminalBlock
        {
            var list = new List<T>();
            mgp.GridTerminalSystem
                .GetBlocksOfType(list, block => block.IsSameConstructAs(mgp.Me) && block.Name != mgp.Me.Name);

            list = list.Where(x => IsTaggedForUse(x, tag)).ToList();
            found = list.Count == 1;
            return found ? list.Single() : null;
        }

        public static T FirstTaggedOrDefault<T>(this IEnumerable<T> obj, string tag) where T : IMyTerminalBlock
        {
            return obj.FirstOrDefault(x => x.IsTaggedForUse(tag));
        }

        public static bool IsTaggedForUse(this IMyTerminalBlock block, string tag) 
        {
            return block.CustomName.Contains(tag) || block.CustomData.Contains(tag);
        }
    }
}
