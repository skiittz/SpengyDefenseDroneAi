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
        //public State MyState;
        static List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
        //public IMyProgrammableBlock sam_controller;
        //public IMyRemoteControl remote;
        //public IMyShipConnector connector;
        //public List<IMyGasTank> h2Tanks;
        //public List<IMyBatteryBlock> batteries;
        //public List<IMyReactor> reactors;
        //private Configuration configuration;
        public bool isAuthorized;
        public AiBrain myBrain;
        public Program()
        {
            var configuration = new Configuration();
            
            if (Me.CustomData != string.Empty)
                configuration.LoadFrom(Me.CustomData);
            else
                Me.CustomData = configuration.ToString();

            State MyState;
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

            IGC.RegisterBroadcastListener(configuration.For(ConfigName.RadioChannel));
            IGC.GetBroadcastListeners(listeners);
            listeners[0].SetMessageCallback("NewTarget");

            var authenticator = new Authenticator(configuration.For(ConfigName.PersonalKey), configuration.For(ConfigName.FactionKey), OwnerId(), FactionTag());
            string authorizationMessage;
            isAuthorized = authenticator.IsAuthorized(out authorizationMessage);
            Echo(authorizationMessage);

            myBrain = GetBrain(MyState, this, configuration);
        }

        public void Save()
        {
            if(MyState.IsSetUpFor(CurrentMode()))
                Storage = MyState.Serialize();                
        }


        public void Main(string argument, UpdateType updateSource)
        {
            CheckAndFireFixedWeapons(this);

            if (argument.ToUpper().Contains("SCAN "))
            {
                ScanForTarget(argument.ToUpper().Replace("SCAN ", ""), myBrain);
                return;
            }

            if (myBrain.SetManualOverride(argument))
                return;

            if (argument.ToUpper() == Prompts.RESET)
            {
                Echo(Prompts.ResettingInternalData);
                ClearData();
                return;
            }
            if(argument.ToUpper() == Prompts.OFF)
            {
                myBrain.TurnOff();
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
                Go(MyState.DockApproach, false, int.Parse(configuration.For(ConfigName.GeneralSpeedLimit)), MyState, this, sam_controller, remote);
            }

            if (!MyState.IsSetUpFor(CurrentMode()))
            {
                Echo(Prompts.DockAndRunSetup);
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }

            CheckScuttle();

            if(configuration.IsEnabled(ConfigName.EnableRelayBroadcast) && argument == "NewTarget")
            {
                var packet = listeners[0].AcceptMessage();
                var antenna = this.FirstTaggedOrDefault<IMyRadioAntenna>(configuration.For(ConfigName.Tag));
                antenna.EnableBroadcasting = true;
                IGC.Relay(packet, configuration.For(ConfigName.RadioChannel));
            }

            myBrain.StatusReport();
            myBrain.Process(argument);
            Runtime.UpdateFrequency = MyState.Enroute && DistanceToWaypoint(MyState) < 1000
                ? UpdateFrequency.Update10 
                : UpdateFrequency.Update100;
        }

        void ClearData()
        {
            myBrain.ClearData();            
            Save();
        }
    }
}
