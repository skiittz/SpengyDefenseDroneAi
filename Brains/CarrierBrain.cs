using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public class CarrierBrain : IAiBrain
    {
        public BrainType MyBrainType { get; set; }
        public List<ICortex> cortices { get; set; }
        public Configuration configuration { get; set; }
        public MyGridProgram GridProgram { get; set; }
        public bool weaponCoreIsActive { get; set; }
        public WcPbApi wcPbApi { get; set; }
        private Dictionary<IMyShipConnector, string> CarriedDrones;
        public CarrierBrain(string storage, MyGridProgram gridProgram, Configuration configuration, bool weaponCoreIsActive, WcPbApi wcPbApi)
        {
            GridProgram = gridProgram;
            this.configuration = configuration;
            MyBrainType = BrainType.TargetOnly;
            this.wcPbApi = wcPbApi;
            this.weaponCoreIsActive = weaponCoreIsActive;
            LoadDronesFrom(storage);
            cortices = this.CreateCortices().ToList();
        }
        public void Process(string argument)
        {
            this.CheckAndFireFixedWeapons();
            this.CheckScuttle();
            GridProgram.Echo("Checking for enemies");
            this.EnemyCheck();
            this.ManageAntennas();
        }

        public void StatusReport()
        {
            GridProgram.Echo($"{Prompts.CurrentMode}: Carrier");
            GridProgram.Echo("-----------------------------");
            GridProgram.Echo("Registered Drones:");
            foreach (var drone in CarriedDrones)
            {
                GridProgram.Echo($"--{drone.Value}: {(RegisteredDroneIsDocked(drone) ? "Docked" : "Not Docked")}");
            }
        }

        private bool RegisteredDroneIsDocked(KeyValuePair<IMyShipConnector, string> registeredDrone)
        {
            if (registeredDrone.Key.Status != MyShipConnectorStatus.Connected)
                return false;

            var droneId = string.Empty;
            if(!GetDroneIdentifierAttachedTo(registeredDrone.Key, out droneId))
                return false;

            return registeredDrone.Value == droneId;
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
            string result = string.Empty;
            foreach (var carriedDrone in CarriedDrones)
            {
                result += $"{carriedDrone.Key.EntityId}|{carriedDrone.Value};";
            }

            return result;
        }

        private void LoadDronesFrom(string storage)
        {
            CarriedDrones = new Dictionary<IMyShipConnector, string>();
            if (storage == string.Empty)
                return;

            var lines = storage.Split(';');
            foreach (var line in lines)
            {
                var data = line.Split('|');

                long dockId;
                if (!long.TryParse(data[0],out dockId))
                    continue;

                var dock = GridProgram.GridTerminalSystem.GetBlockWithId(dockId) as IMyShipConnector;
                if(dock != null)
                    CarriedDrones.Add(dock, data[1]);
            }
        }

        public bool SetUp()
        {
            GridProgram.Echo("Carrier set up complete");
            GridProgram.Runtime.UpdateFrequency = UpdateFrequency.Update100;

            var droneDocks = new List<IMyShipConnector>();
            GridProgram.GridTerminalSystem.GetBlocksOfType(droneDocks, block => block.IsSameConstructAs(GridProgram.Me) && block.IsTaggedForUse(configuration.For(ConfigName.DroneDockTag)));

            foreach (var droneDock in droneDocks)
            {
                if (droneDock.Status != MyShipConnectorStatus.Connected)
                    continue;

                string droneId;
                if (GetDroneIdentifierAttachedTo(droneDock, out droneId))
                    CarriedDrones.Add(droneDock, droneId);
            }

            return true;
        }

        private bool GetDroneIdentifierAttachedTo(IMyShipConnector droneDock, out string id)
        {
            id = string.Empty;
            
            var droneConnector = droneDock.OtherConnector;
            var terminalBlocks = new List<IMyTerminalBlock>();
            var droneIdConfigName = ConfigName.DroneIdentifier.ToHumanReadableName();
            GridProgram.GridTerminalSystem.GetBlocksOfType(terminalBlocks, block => block.IsSameConstructAs(droneConnector) && block.CustomData.Contains(droneIdConfigName));

            if (!terminalBlocks.Any())
                return false;

            id = terminalBlocks
                .First()
                .CustomData
                .Split('\n')
                .Single(x => x.Contains(droneIdConfigName))
                .Split(':')[1];
            return true;
        }
    }
}
