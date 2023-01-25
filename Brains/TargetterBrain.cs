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
        public class TargetterBrain : AiBrain
        {
            public MyGridProgram GridProgram { get; set; }
            public Configuration configuration { get; set; }

            public TargetterBrain(MyGridProgram gridProgram)
            {
                this.GridProgram = gridProgram;
            }
            public void Process(string argument)
            {
                EnemyCheck();
            }

            public void StatusReport()
            {
                GridProgram.Echo($"{Prompts.CurrentMode}: {CurrentMode().ToHumanReadableName()}");
            }

            public void ClearData() { }

            public void TurnOff()
            {
                GridProgram.Runtime.UpdateFrequency = UpdateFrequency.None;
            }
        }
    }
}
