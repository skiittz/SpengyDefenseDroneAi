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
            brain.listeners.ForEach(x =>
            {
                var command = brain.ParseRadioCommand(x.Tag);
                if(command.HasValue)
                    x.SetMessageCallback(command.Value.ToHumanReadableName());
            });
        }

        private static string FormatCommandWithMyName(this IAdvancedAiBrain brain, string command)
        {
            var myName = brain.configuration.For(ConfigName.DroneIdentifier);
            return $"{myName}: {command}";
        }

        public static CommandType? ParseRadioCommand(this IAdvancedAiBrain brain, string argument)
        {
            CommandType command;
            var valueToRemove = $"{brain.configuration.For(ConfigName.DroneIdentifier)}: ";
            if (Enum.TryParse(argument.Replace(valueToRemove,"").Trim(), out command))
                return command;

            return null;
        }
    }
}
