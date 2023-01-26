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
    public static class SuicideCortex
    {
        public static void CheckScuttle(this IAiBrain brain)
        {
            if (!brain.configuration.IsEnabled(ConfigName.EnableSuicide))
                return;

            if (brain.GridProgram.Me.IsBeingHacked)
                brain.Scuttle();

            var warheads = new List<IMyWarhead>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(warheads, block => block.IsSameConstructAs(brain.GridProgram.Me));

            if (warheads.Any(x => x.IsBeingHacked))
                brain.Scuttle();
        }
        public static void Scuttle(this IAiBrain brain)
        {
            var warheads = new List<IMyWarhead>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(warheads, block => block.IsSameConstructAs(brain.GridProgram.Me));
            warheads.ForEach(x => { x.IsArmed = true; x.Detonate(); });
        }
    }
}
