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
    partial class Program : MyGridProgram
    {
        public State MyState;
        List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
        public IMyProgrammableBlock sam_controller;
        public IMyRemoteControl remote;
        public IMyShipConnector connector;
        public List<IMyGasTank> h2Tanks;
        public List<IMyBatteryBlock> batteries;
        public List<IMyReactor> reactors;
        private readonly Configuration configuration;
        public bool isAuthorized;
        public Program()
        {
            configuration = new Configuration();
            
            if (Me.CustomData != string.Empty)
                configuration.LoadFrom(Me.CustomData);
            else
                Me.CustomData = configuration.ToString();

            if (Storage != string.Empty)
                try {
                    MyState = State.Deserialize(Storage); 
                }catch(Exception e)
                {
                    Echo($"{Prompts.CouldNotDeserializeState}: {e.Message}");
                    Echo(Storage);
                    Runtime.UpdateFrequency = UpdateFrequency.None;

                    MyState = new State();
                    return;
                }
            else
                MyState = new State();

            GetBasicBlocks();
            //reset dock approach in case clearance setting was changeed
            MyState.DockApproach = MyState.DockApproach + (connector.WorldMatrix.Backward * int.Parse(configuration.For(ConfigName.DockClearance)));

            MyState.SetControllers(remote, sam_controller);

            if (!MyState.IsSetUpFor(CurrentMode()))
                Runtime.UpdateFrequency = UpdateFrequency.None;
            else
                Runtime.UpdateFrequency = UpdateFrequency.Update100;

            SetUpRadioListeners();          

            var authenticator = new Authenticator(configuration.For(ConfigName.PersonalKey), configuration.For(ConfigName.FactionKey), OwnerId(), FactionTag());
            string authorizationMessage;
            isAuthorized = authenticator.IsAuthorized(out authorizationMessage);
            Echo(authorizationMessage);
        }

        public void Save()
        {
            if(MyState.IsSetUpFor(CurrentMode()))
                Storage = MyState.Serialize();                
        }


        public void Main(string argument, UpdateType updateSource)
        {
            CheckAndFireFixedWeapons();

            if (CheckCommandIssuedViaRadio(argument))
                return;

            if (argument.ToUpper().Contains("SCAN "))
            {
                ScanForTarget(argument.ToUpper().Replace("SCAN ", ""));
                return;
            }

            if (argument.Contains(Special.Debug_ArgFlag))
            {
                if (argument == $"{Special.Debug_ArgFlag}{Special.Debug_Enroute}")
                    MyState.Enroute = !MyState.Enroute;
                else if(argument.Contains(Special.Debug_StateFlag))
                {
                    var cmd = argument.Replace($"{Special.Debug_ArgFlag}{Special.Debug_StateFlag}", "");                   
                    Status status;
                    if(Enum.TryParse(cmd, out status))
                        MyState.Status = status;
                }
            }

            if (argument.ToUpper() == Prompts.RESET)
            {
                Echo(Prompts.ResettingInternalData);
                ClearData();
                return;
            }
            if(argument.ToUpper() == Prompts.OFF)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                if (CurrentMode() != Mode.TargetOnly)
                {
                    remote.ClearWaypoints();
                    remote.SetAutoPilotEnabled(false);
                }
                return;
            }
            if (!isAuthorized)
            {
                Echo($"{Prompts.Invalid} {WordKey()}");
                Echo($"{Prompts.YourOwnerIdFactionTag}: {OwnerId()}/{FactionTag()}");
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }
            if (argument.ToUpper() == Prompts.ON)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }
            if (argument.ToUpper() == Prompts.SETUP) 
            {
                Echo(Prompts.AttemptingAutoSetUp);              
                if (SetUp())
                {
                    Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    Echo(Prompts.SetupSuccessfulDroneIsReady);
                }
                else
                {
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    Echo(Prompts.SetupFailedDroneIsNotOperational);
                    return; 
                }
            }
            if(argument.ToUpper() == Prompts.RETURN)
            {
                MyState.Status = Status.Returning;
                Go(MyState.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)));
            }

            if (!MyState.IsSetUpFor(CurrentMode()))
            {
                Echo(Prompts.DockAndRunSetup);
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }

            CheckScuttle();

            Echo($"{Prompts.CurrentMode}: {CurrentMode().ToHumanReadableName()}");
            Echo($"{Prompts.CurrentStatus}: {MyState.Status.ToHumanReadableName()}");
            Echo($"{Prompts.NavigationModel}: {MyState.NavigationModel.ToHumanReadableName()}");
            Echo($"{Prompts.Enroute}: {MyState.Enroute}");

            if(configuration.IsEnabled(ConfigName.EnableRelayBroadcast) && argument == "NewTarget")
            {
                var packet = listeners[0].AcceptMessage();
                var antenna = FirstTaggedOrDefault<IMyRadioAntenna>();
                antenna.EnableBroadcasting = true;
                IGC.Relay(packet, configuration.For(ConfigName.MyName));
            }
           
            if (MyState.Enroute)
                Echo($"{Prompts.MovingTo} : {(remote?.CurrentWaypoint == null ? Prompts._null : remote.CurrentWaypoint.ToString())}");            

            if ( CurrentMode() == Mode.TargetOnly)
            {
                EnemyCheck();
                return;
            }           
            else if (CurrentMode() == Mode.Defend)
            {
                DeployLogic(argument);
            }
            else if(CurrentMode() == Mode.Patrol)
            {
                PatrolLogic();
            }
        }

        void ClearData()
        {
            Storage = string.Empty;
            MyState = new State();
            if (remote != null)
            {
                remote.ClearWaypoints();
                remote.SetAutoPilotEnabled(false);
            }
            Save();
        }
    }
}
