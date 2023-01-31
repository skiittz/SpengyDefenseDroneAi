using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class TargetterBrain : IAiBrain
    {
        public TargetterBrain(MyGridProgram gridProgram, Configuration configuration, bool weaponCoreIsActive,
            WcPbApi wcPbApi)
        {
            GridProgram = gridProgram;
            this.configuration = configuration;
            MyBrainType = BrainType.TargetOnly;
            this.wcPbApi = wcPbApi;
            this.weaponCoreIsActive = weaponCoreIsActive;
        }

        public BrainType MyBrainType { get; set; }
        public List<ICortex> cortices { get; set; }
        public MyGridProgram GridProgram { get; set; }
        public Configuration configuration { get; set; }
        public bool weaponCoreIsActive { get; set; }
        public WcPbApi wcPbApi { get; set; }

        public void Process(string argument)
        {
            this.CheckAndFireFixedWeapons();
            this.CheckScuttle();
            GridProgram.Echo("Checking for enemies");
            this.EnemyCheck();
            this.ManageAntennas();
            cortices = this.CreateCortices().ToList();
        }

        public void StatusReport()
        {
            GridProgram.Echo($"{Prompts.CurrentMode}: Targetting");
        }

        public void ClearData()
        {
        }

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
                    catch
                    {
                        return false;
                    }
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

        public bool SetUp()
        {
            GridProgram.Echo("Targetting set up complete");
            GridProgram.Runtime.UpdateFrequency = UpdateFrequency.Update100;
            return true;
        }
    }
}