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
        static List<IMyBroadcastListener> listeners = new List<IMyBroadcastListener>();
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
            listeners[0].SetMessageCallback("NEWTARGET");

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
                Echo($"Running: {argument.ToUpper()}");
                var args = argument.Split(' ');

                CommandType cmd;
                if (args[0].ToUpper().TryCommandTypeFromHumanReadableName(out cmd))
                {
                    var remainingArgs = args.Skip(1).ToArray();
                    var success = myBrain.HandleCommand(cmd, remainingArgs);
                    
                    if (cmd == CommandType.Reset && success)
                        ClearProgramData();
                    //if (cmd == CommandType.Setup && success)
                    //{
                    //    NavigationModel navModel;
                    //    if (myBrain.MyBrainType != BrainType.TargetOnly)
                    //        navModel = (myBrain as IAdvancedAiBrain).navigationModel;
                    //    else
                    //        navModel = NavigationModel.Keen;

                    //    myBrain.configuration = myBrain.configuration.CleanUp(myBrain.configuration.For<BrainType>(ConfigName.BrainType), navModel);
                    //}
                        
                    Echo($"{argument.ToUpper()}: {(success ? "Success" : "Failed")}");
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

            if (myBrain.configuration.IsEnabled(ConfigName.EnableRelayBroadcast) && argument == "NewTarget")
            {
                var packet = listeners[0].AcceptMessage();
                var antenna = this.FirstTaggedOrDefault<IMyRadioAntenna>(myBrain.configuration.For(ConfigName.Tag));
                antenna.EnableBroadcasting = true;
                myBrain.Relay(packet);
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
