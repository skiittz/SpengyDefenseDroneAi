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
    static class AiBrainExtensions
    {
        public static void GetBasicBlocks(this Program.AdvancedAiBrain brain)
        {
            brain.remote = brain.GridProgram.FirstTaggedOrDefault<IMyRemoteControl>(brain.configuration.For(Program.ConfigName.Tag));
            brain.connector = brain.GridProgram.FirstTaggedOrDefault<IMyShipConnector>(brain.configuration.For(Program.ConfigName.Tag));

            bool samFound;
            brain.sam_controller = brain.GridProgram.SingleTagged<IMyProgrammableBlock>(brain.configuration.For(Program.ConfigName.SAMAutoPilotTag), out samFound);
            if (samFound && brain.samController.IsWorking)
                brain.navigationModel = Program.NavigationModel.SAM;
            else
                brain.navigationModel = Program.NavigationModel.Keen;

            brain.h2Tanks = new List<IMyGasTank>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(h2Tanks, block => block.IsSameConstructAs(Me) && block.BlockDefinition.SubtypeName.Contains("Hydro"));

            brain.batteries = new List<IMyBatteryBlock>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(batteries, block => block.IsSameConstructAs(Me));

            brain.reactors = new List<IMyReactor>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(reactors, block => block.IsSameConstructAs(Me));
        }


    }
}
