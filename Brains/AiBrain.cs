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
    public enum BrainType
    {
        Patrol,
        Defend,
        TargetOnly
    }

    public static class BrainFunctions
    {
        public static IAiBrain GetBrain(State state, MyGridProgram mgp, Configuration configuration, List<IMyBroadcastListener> listeners)
        {
            switch (configuration.For(ConfigName.BrainType).BrainTypeFromHumanReadableName())
            {
                case BrainType.Defend:
                    return new DefenderBrain(state, mgp, configuration, listeners).GetBasicBlocks();
                case BrainType.Patrol:
                    return new PatrollerBrain(state, mgp, configuration, listeners).GetBasicBlocks();
                default:
                    return new TargetterBrain(mgp);
            }
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
            bool HandleCommand(CommandType commandType);
            string SerializeState();
            bool SetUp();            
        }

        public interface IAdvancedAiBrain : IAiBrain
        {
        List<IMyBroadcastListener> listeners { get; set; }

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
