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
        public enum CommandType
        {
            Return,
            Setup
        }
    static class ManualCommands
    {
        public static bool SetManualOverride(this IAiBrain brain, string argument){            
            if (brain.configuration.For<BrainType>(ConfigName.BrainType) == BrainType.TargetOnly)
                return true;

            return (brain as IAdvancedAiBrain).SetManualOverride(argument);
        }

        public static bool SetManualOverride(this IAdvancedAiBrain brain, string argument)
        {
            if (argument.Contains(Special.Debug_ArgFlag))
            {
                if (argument == $"{Special.Debug_ArgFlag}{Special.Debug_Enroute}")
                {
                    brain.state.Enroute = !brain.state.Enroute;
                    return true;
                }
                else if (argument.Contains(Special.Debug_StateFlag))
                {
                    var cmd = argument.Replace($"{Special.Debug_ArgFlag}{Special.Debug_StateFlag}", "");
                    Status status;
                    if (Enum.TryParse(cmd, out status))
                    {
                        brain.state.Status = status;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
