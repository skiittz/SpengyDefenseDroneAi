﻿using System;
using System.Collections.Generic;

namespace IngameScript
{
    public enum ConfigName
    {
        Tag,
        BrainType,
        RadioChannel,
        SAMAutoPilotTag,
        AttackSpeedLimit,
        DockSpeedLimit,
        GeneralSpeedLimit,
        LowPowerThreshold,
        LowH2Threshold,
        LowReactorThreshold,
        DockClearance,
        PersonalKey,
        FactionKey,
        EnableLowAmmoCheck,
        UseBurstTransmissions,
        EnableSuicide,
        EnableRelayBroadcast,
        FixedWeaponReferenceTag,
        DroneDockTag,
        DroneIdentifier
    }

    public class Configuration
    {
        public static readonly Dictionary<ConfigName, string> Defaults = new Dictionary<ConfigName, string>
        {
            { ConfigName.Tag, "SDDS" },
            { ConfigName.BrainType, "TargetOnly" },
            { ConfigName.RadioChannel, "SDDS" },
            { ConfigName.SAMAutoPilotTag, "SAM" },
            { ConfigName.AttackSpeedLimit, "100" },
            { ConfigName.DockSpeedLimit, "10" },
            { ConfigName.GeneralSpeedLimit, "35" },
            { ConfigName.LowPowerThreshold, "20" },
            { ConfigName.LowH2Threshold, "50" },
            { ConfigName.LowReactorThreshold, "1" },
            { ConfigName.DockClearance, "40" },
            { ConfigName.PersonalKey, "None" },
            { ConfigName.FactionKey, "None" },
            { ConfigName.EnableLowAmmoCheck, "true" },
            { ConfigName.UseBurstTransmissions, "true" },
            { ConfigName.EnableSuicide, "true" },
            { ConfigName.EnableRelayBroadcast, "true" }
        };

        private readonly Dictionary<ConfigName, string> configs;

        public Configuration()
        {
            configs = Defaults;
        }
        //Part of cleanup code that caused problems.  may revisit in future.
        //public static IEnumerable<ConfigName> ApplicableConfigs(BrainType mode, INavigationCortex navModel)
        //{
        //    yield return ConfigName.Tag;
        //    yield return ConfigName.BrainType;
        //    yield return ConfigName.RadioChannel;
        //    yield return ConfigName.PersonalKey;
        //    yield return ConfigName.FactionKey;
        //    yield return ConfigName.UseBurstTransmissions;
        //    yield return ConfigName.EnableSuicide;
        //    yield return ConfigName.EnableRelayBroadcast;
        //    yield return ConfigName.FixedWeaponReferenceTag;
        //    yield return ConfigName.SAMAutoPilotTag;

        //    if (mode != BrainType.TargetOnly)
        //    {
        //        if (navModel.GetType() == typeof(KeenNavigationCortex))
        //        {
        //            yield return ConfigName.AttackSpeedLimit;
        //            yield return ConfigName.GeneralSpeedLimit;
        //        }

        //        yield return ConfigName.DockSpeedLimit;

        //        yield return ConfigName.DockClearance;
        //        yield return ConfigName.LowPowerThreshold;
        //        yield return ConfigName.LowH2Threshold;
        //        yield return ConfigName.LowReactorThreshold;
        //        yield return ConfigName.EnableLowAmmoCheck;
        //    }
        //}

        public void LoadFrom(string customData)
        {
            if (customData == string.Empty) return;

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
            foreach (var config in configs) customData += $"{config.Key.ToHumanReadableName()}:{config.Value}\n";
            customData = customData.TrimEnd('\r').TrimEnd('\n');

            return customData;
        }

        public string For(ConfigName configName)
        {
            if (!configs.ContainsKey(configName))
                return string.Empty;
            return configs[configName];
        }

        public bool IsEnabled(ConfigName configName)
        {
            bool output;
            return bool.TryParse(For(configName), out output) && output;
        }

        public T For<T>(ConfigName configName) where T : struct
        {
            T parseResult;
            Enum.TryParse(For(configName), out parseResult);
            return parseResult;
        }

        //causing more trouble than it is worth.  may revisit later.
        //public Configuration CleanUp(BrainType mode, NavigationModel navModel)
        //{
        //    return new Configuration
        //    {
        //        configs = configs.Where(x => ApplicableConfigs(mode, navModel).Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value)
        //    };

        //}
    }
}