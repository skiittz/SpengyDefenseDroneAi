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
        public class TargetterBrain : IAiBrain
        {
        public BrainType MyBrainType { get; set; }
        public MyGridProgram GridProgram { get; set; }
            public Configuration configuration { get; set; }

            public TargetterBrain(MyGridProgram gridProgram)
            {
                this.GridProgram = gridProgram;
                this.MyBrainType = BrainType.TargetOnly;
            }
            public void Process(string argument)
            {
                this.EnemyCheck();
            }

            public void StatusReport()
            {
                GridProgram.Echo($"{Prompts.CurrentMode}: Targetting");
            }

            public void ClearData() { }

            public void TurnOff()
            {
                GridProgram.Runtime.UpdateFrequency = UpdateFrequency.None;
            }

            public bool IsSetUp()
            {
                return true;
            }

        public bool HandleCommand(CommandType commandType, string[] args = default(string[]))
        {
            switch (commandType)
            {
                case CommandType.NewTarget:
                    Process(commandType.ToHumanReadableName());
                    return true;
                case CommandType.On:
                    GridProgram.Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    return true;
                case CommandType.Off:
                    TurnOff();
                    return true;
                case CommandType.Scan:
                    try
                    {
                        this.ScanForTarget(args[0], int.Parse(args[1]));
                        return true;
                    }
                    catch { return false; }
                case CommandType.Reset:
                    ClearData();
                    TurnOff();
                    return true;
                case CommandType.Setup:
                    SetUp();
                    return true;
                default:
                    GridProgram.Echo("I do not know that command!");
                    return false;
            }
        }

            public string SerializeState()
            {
                return string.Empty;
            }

            public bool SetUp() { 
                GridProgram.Echo("Targetting set up complete");
                GridProgram.Runtime.UpdateFrequency = UpdateFrequency.Update100;
                return true;
            }
        }
}
