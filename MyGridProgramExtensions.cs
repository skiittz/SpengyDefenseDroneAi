using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public static class MyGridProgramExtensions
    {
        public static T FirstTaggedOrDefault<T>(this MyGridProgram mgp, string tag) where T : class, IMyTerminalBlock
        {
            var list = new List<T>();
            mgp.GridTerminalSystem.GetBlocksOfType(list, block => block.IsSameConstructAs(mgp.Me));

            var result = list.FirstOrDefault(x => x.IsTaggedForUse(tag));
            if (result == null)
                return list.FirstOrDefault();
            return result;
        }

        public static T SingleTagged<T>(this MyGridProgram mgp, string tag, out bool found)
            where T : class, IMyTerminalBlock
        {
            var list = new List<T>();
            mgp.GridTerminalSystem
                .GetBlocksOfType(list, block => block.IsSameConstructAs(mgp.Me) && block.Name != mgp.Me.Name);

            list = list.Where(x => IsTaggedForUse(x, tag)).ToList();
            found = list.Count == 1;
            return found ? list.Single() : default(T);
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