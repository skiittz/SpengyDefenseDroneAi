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
        public void CheckScuttle()
        {
            bool enableScuttle = false;
            if (!bool.TryParse(configuration.For(ConfigName.EnableSuicide), out enableScuttle) || !enableScuttle)
                return;

            var turrets = new List<IMyLargeTurretBase>();
            GridTerminalSystem.GetBlocksOfType(turrets, block => block.IsSameConstructAs(Me));
                                 
            if (!turrets.Any())
                Scuttle();

            var remotes = new List<IMyRemoteControl>();
            GridTerminalSystem.GetBlocksOfType(remotes, block => block.IsSameConstructAs(Me));

            if (!remotes.Any())
                Scuttle();
        }
        public void Scuttle()
        {
            var warheads = new List<IMyWarhead>();
            GridTerminalSystem.GetBlocksOfType(warheads, block => block.IsSameConstructAs(Me));
            warheads.ForEach(x => { x.IsArmed = true; x.Detonate(); });
        }
    }
}
