using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

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
        public static IAiBrain GetBrain(State state, MyGridProgram mgp, Configuration configuration,
            List<IMyBroadcastListener> listeners, bool weaponCoreIsActive, WcPbApi wcPbApi)
        {
            switch (configuration.For(ConfigName.BrainType).BrainTypeFromHumanReadableName())
            {
                case BrainType.Defend:
                    return new DefenderBrain(state, mgp, configuration, listeners, weaponCoreIsActive, wcPbApi)
                        .GetBasicBlocks();
                case BrainType.Patrol:
                    return new PatrollerBrain(state, mgp, configuration, listeners, weaponCoreIsActive, wcPbApi)
                        .GetBasicBlocks();
                default:
                    return new TargetterBrain(mgp, configuration, weaponCoreIsActive, wcPbApi);
            }
        }
    }

    public interface IAiBrain
    {
        BrainType MyBrainType { get; set; }
        Configuration configuration { get; set; }
        MyGridProgram GridProgram { get; set; }
        bool weaponCoreIsActive { get; set; }
        WcPbApi wcPbApi { get; set; }
        void Process(string argument);
        void StatusReport();
        void ClearData();
        void TurnOff();
        bool IsSetUp();
        bool HandleCommand(CommandType commandType, string[] args = default(string[]));
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