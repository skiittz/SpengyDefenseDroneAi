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
        public IAiBrain myBrain;
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
                    Storage = string.Empty;
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    return;
                }
            else
                MyState = new State();

            myBrain = BrainFunctions.GetBrain(MyState, this, configuration, listeners);

            if (!myBrain.IsSetUp())
                Runtime.UpdateFrequency = UpdateFrequency.None;
            else
                Runtime.UpdateFrequency = UpdateFrequency.Update100;

            IGC.RegisterBroadcastListener(configuration.For(ConfigName.RadioChannel));
            IGC.GetBroadcastListeners(listeners);
            listeners[0].SetMessageCallback("NewTarget");

            var authenticator = new Authenticator(configuration.For(ConfigName.PersonalKey), configuration.For(ConfigName.FactionKey), Authenticator.OwnerId(Me), Authenticator.FactionTag(Me));
            string authorizationMessage;
            isAuthorized = authenticator.IsAuthorized(out authorizationMessage);
            Echo(authorizationMessage);            
        }

        public void Save()
        {
            if (myBrain != null && myBrain.IsSetUp())
                Storage = myBrain.SerializeState();
        }


        public void Main(string argument, UpdateType updateSource)
        {
            Echo($"Running: {argument}");
            if (argument.ToUpper() == Prompts.RESET)
            {
                Echo(Prompts.ResettingInternalData);
                ClearData();
                return;
            }
            if (argument.ToUpper() == Prompts.SETUP)
            {
                Echo(Prompts.AttemptingAutoSetUp);
                if (myBrain.HandleCommand(CommandType.Setup))
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

            FixedWeaponsHandler.CheckAndFireFixedWeapons(this);

            if (argument.ToUpper().Contains("SCAN "))
            {
                ScanForTarget(argument.ToUpper().Replace("SCAN ", ""), myBrain);
                return;
            }

            if (myBrain.SetManualOverride(argument))
                return;

            if(argument.ToUpper() == Prompts.OFF)
            {
                myBrain.TurnOff();
                return;
            }
            if (!isAuthorized)
            {
                Echo($"{Prompts.Invalid} {Authenticator.WordKey()}");
                Echo($"{Prompts.YourOwnerIdFactionTag}: {Authenticator.OwnerId(Me)}/{Authenticator.FactionTag(Me)}");
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }
            if (argument.ToUpper() == Prompts.ON)
            {
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }
            
            if(argument.ToUpper() == Prompts.RETURN)
            {
                myBrain.HandleCommand(CommandType.Return);
            }

            if (!myBrain.IsSetUp())
            {
                Echo(Prompts.DockAndRunSetup);
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }

            CheckScuttle(myBrain.configuration);

            if(myBrain.configuration.IsEnabled(ConfigName.EnableRelayBroadcast) && argument == "NewTarget")
            {
                var packet = listeners[0].AcceptMessage();
                var antenna = this.FirstTaggedOrDefault<IMyRadioAntenna>(myBrain.configuration.For(ConfigName.Tag));
                antenna.EnableBroadcasting = true;
                IGC.Relay(packet, myBrain.configuration.For(ConfigName.RadioChannel));
            }

            myBrain.StatusReport();
            myBrain.Process(argument);            
        }

        void ClearData()
        {
            Storage = string.Empty;
            if(myBrain != null)
                myBrain.ClearData();            
            Save();
        }
    }
}
