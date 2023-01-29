using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public static class SuicideCortex
    {
        public static void CheckScuttle(this IAiBrain brain)
        {
            if (!brain.configuration.IsEnabled(ConfigName.EnableSuicide))
                return;

            if (brain.GridProgram.Me.IsBeingHacked)
                brain.Scuttle();

            var warheads = new List<IMyWarhead>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(warheads,
                block => block.IsSameConstructAs(brain.GridProgram.Me));

            if (warheads.Any(x => x.IsBeingHacked))
                brain.Scuttle();
        }

        public static void Scuttle(this IAiBrain brain)
        {
            var warheads = new List<IMyWarhead>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(warheads,
                block => block.IsSameConstructAs(brain.GridProgram.Me));
            warheads.ForEach(x =>
            {
                x.IsArmed = true;
                x.Detonate();
            });
        }
    }
}