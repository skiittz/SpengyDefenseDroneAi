using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    public static class RadioCommandCortex
    {
        public static void SetUpRadioListeners(this IAdvancedAiBrain brain)
        {
            brain.listeners = new List<IMyBroadcastListener>();
            foreach (var cmdType in Enum.GetValues(typeof(CommandType)).Cast<CommandType>())
            {
                var radioCommand = cmdType.ToHumanReadableName();
                brain.GridProgram.IGC.RegisterBroadcastListener(radioCommand);
                brain.GridProgram.IGC.RegisterBroadcastListener(brain.FormatCommandWithMyName(radioCommand));
            }

            brain.GridProgram.IGC.GetBroadcastListeners(brain.listeners);
            // ReSharper disable once PossibleInvalidOperationException 
            brain.listeners.ForEach(x => x.SetMessageCallback(brain.ParseRadioCommand(x.Tag).Value.ToHumanReadableName()));
        }

        private static string FormatCommandWithMyName(this IAdvancedAiBrain brain, string command)
        {
            var myName = brain.configuration.For(ConfigName.DroneIdentifier);
            return $"{myName}: {command}";
        }

        public static CommandType? ParseRadioCommand(this IAdvancedAiBrain brain, string argument)
        {
            CommandType command;
            if (Enum.TryParse(argument.Replace(brain.configuration.For(ConfigName.DroneIdentifier),"").Trim(), out command))
                return command;

            return null;
        }
    }
}
