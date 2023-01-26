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
            if (argument != "")
            {
                Echo($"Running: {argument}");
                var args = argument.Split(' ');

                CommandType cmd;
                if (Enum.TryParse(args[0], out cmd))
                {
                    var success = myBrain.HandleCommand(cmd, args.Skip(1).ToArray());
                    
                    if (cmd == CommandType.Reset && success)
                        ClearProgramData();
                    if (cmd == CommandType.Setup && success)
                    {
                        NavigationModel navModel;
                        if (myBrain.MyBrainType != BrainType.TargetOnly)
                            navModel = (myBrain as IAdvancedAiBrain).navigationModel;
                        else
                            navModel = NavigationModel.Keen;

                        myBrain.configuration.CleanUp(myBrain.configuration.For<BrainType>(ConfigName.BrainType), navModel);
                    }
                        
                    Echo($"{argument}: {(success ? "Success" : "Failed")}");
                }
                else
                    Echo("I do not recognize that command");

                return;
            }
            if (!isAuthorized)
            {
                Echo($"{Prompts.Invalid} {Authenticator.WordKey()}");
                Echo($"{Prompts.YourOwnerIdFactionTag}: {Authenticator.OwnerId(Me)}/{Authenticator.FactionTag(Me)}");
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }

            if (!myBrain.IsSetUp())
            {
                Echo(Prompts.DockAndRunSetup);
                Runtime.UpdateFrequency = UpdateFrequency.None;
                return;
            }

            myBrain.StatusReport();
            myBrain.Process(argument);            
        }

        void ClearProgramData()
        {
            Storage = string.Empty;
            Save();
        }
    }
}
