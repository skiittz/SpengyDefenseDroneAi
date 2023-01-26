﻿using Sandbox.Game.EntityComponents;
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
        public IAiBrain GetBrain(State state, MyGridProgram mgp, Configuration configuration)
        {
            switch (configuration.For<Mode>(ConfigName.Mode))
            {
                case Mode.Defend:
                    return new DefenderBrain(state, mgp, configuration).GetBasicBlocks();
                case Mode.Patrol:
                    return new PatrollerBrain(state, mgp, configuration).GetBasicBlocks();
                default:
                    return new TargetterBrain(mgp);
            }
        }

        public interface IAiBrain
        {
            Configuration configuration { get; set; }
            MyGridProgram GridProgram { get; set; }

            void Process(string argument);
            void StatusReport();
            void ClearData();
            void TurnOff();
            bool IsSetUp();
            bool HandleCommand(Program.CommandType commandType);
            string SerializeState();
            bool SetUp();            
        }

        public interface IAdvancedAiBrain : IAiBrain
        {
            IMyRemoteControl remote { get; set; }
            IMyShipConnector connector { get; set; }
            IMyProgrammableBlock samController { get; set; }
            List<IMyBatteryBlock> batteries { get; set; }
            List<IMyReactor> reactors { get; set; }
            List<IMyGasTank> h2Tanks { get; set; }           
            NavigationModel navigationModel { get; set; }
            State state { get; set; }
            void RefreshDockApproach();
        }
    }
}
