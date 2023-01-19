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
        public class Configuration
        {
            private Dictionary<ConfigName, string> configs;
            public static readonly Dictionary<ConfigName, string> Defaults = new Dictionary<ConfigName, string> {
                        {ConfigName.Tag,"SDDS"},
                        {ConfigName.Mode, "TargetOnly"},
                        {ConfigName.RadioChannel,"SDDS" },
                        {ConfigName.AttackSpeedLimit, "100" },
                        {ConfigName.DockSpeedLimit, "10" },
                        {ConfigName.GeneralSpeedLimit,"35" },
                        {ConfigName.LowPowerThreshold,"20" },
                        {ConfigName.LowH2Threshold,"50" },
                        {ConfigName.LowReactorThreshold,"1" },
                        {ConfigName.DockClearance,"40" },
                        {ConfigName.PersonalKey,"None" },
                        {ConfigName.FactionKey,"None" },
                        {ConfigName.EnableLowAmmoCheck,"true"}
            };

            public Configuration()
            {
                configs = Defaults;
            }
            public void LoadFrom(string customData)
            {
                if (customData == string.Empty)
                {                    
                    return;
                }

                var lines = customData.Split('\n');
                configs.Clear();
                foreach (var line in lines)
                {
                    var config = line.Split(':');                   
                    configs.Add(config[0].ConfigFromHumanReadableName(), config[1]);
                }
            }

            public override string ToString()
            {
                var customData = string.Empty;
                foreach (var config in configs)
                {
                    customData += $"{config.Key.ToHumanReadableName()}:{config.Value}\n";
                }
                customData = customData.TrimEnd('\r').TrimEnd('\n');

                return customData;
            }

            public string For(ConfigName configName)
            {
                if (!configs.ContainsKey(configName))
                    return string.Empty;
                return configs[configName];
            }
        }       

        public Mode CurrentMode()
        {
            return configuration.For(ConfigName.Mode).ModeFromHumanReadableName();
        }
    }
}
