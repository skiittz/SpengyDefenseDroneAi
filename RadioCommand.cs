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
    partial class Program
    {
        public enum RadioCommand
        {
            AttackThisTarget,
            StopAndReturn,
            PerformSelfSetup,
            Suicide,
            RamTarget,
            Scout
        }

        public void SetUpRadioListeners()
        {
            foreach (var radioCommand in Enum.GetNames(typeof(RadioCommands)))
            {
                IGC.RegisterBroadcastListener(radioCommand);
                IGC.RegisterBroadcastListener(FormatCommandWithMyName(radioCommand);
                IGC.GetBroadcastListeners(listeners);
                listeners.ForEach(x => x.SetMessageCallback(radioCommand));
            }
        }

        public bool CheckCommandIssuedViaRadio(string argument)
        {
            var command = ParseRadioCommand(argument);
            if (command == null)
                return false;

            switch (command)
            {

            }

            return true;
        }

        public RadioCommand? ParseRadioCommand(string argument)
        {
            RadioCommand command;
            if (Enum.TryParse(argument.Replace(configuration.For(ConfigName.MyName)), out command))
                return command;

            return null;

        }

        public string FormatCommandWithMyName(RadioCommand command)
        {
            return $"{configuration.For(ConfigName.MyName)}: {command}";
        }
    }
}
