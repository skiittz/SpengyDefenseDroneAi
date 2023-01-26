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
        public static class MaintenanceCortex
        {
        public static bool NeedsService(this IAdvancedAiBrain brain)
        {
            return brain.LowAmmo() || brain.LowH2() || brain.LowPower() || brain.LowReactorFuel();
        }

        private static bool LowAmmo(this IAdvancedAiBrain brain)
        {
            if (!brain.configuration.IsEnabled(ConfigName.EnableLowAmmoCheck))
                return false;

            var turrets = new List<IMyLargeTurretBase>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(turrets, block => block.IsSameConstructAs(brain.GridProgram.Me));
            foreach (var turret in turrets)
            {
                var inventory = new List<MyInventoryItem>();
                turret.GetInventory().GetItems(inventory);
                if (!inventory.Any())
                {
                    brain.GridProgram.Echo(Prompts.LowAmmo);
                    return true;
                }
            }

            brain.GridProgram.Echo(Prompts.AmmoGood);
            return false;
        }

        private static bool LowPower(this IAdvancedAiBrain brain)
        {
            if (brain.batteries == null || !brain.batteries.Any())
                return false;

            var currentPower = 0f;
            var maxPower = 0f;

            foreach (var battery in brain.batteries)
            {
                currentPower += battery.CurrentStoredPower;
                maxPower += battery.MaxStoredPower;
            }

            var powerThreshold = decimal.Parse(brain.configuration.For(ConfigName.LowPowerThreshold));
            var powerLevel = Math.Round((decimal)(currentPower / maxPower) * 100, 2);
            if (powerLevel < powerThreshold)
                brain.GridProgram.Echo(Prompts.LowPower);
            else
                brain.GridProgram.Echo(Prompts.PowerGood);
            return powerLevel < powerThreshold;
        }

        private static bool LowH2(this IAdvancedAiBrain brain)
        {
            if (brain.h2Tanks == null || !brain.h2Tanks.Any())
                return false;

            var averageFuel = (decimal)brain.h2Tanks.Select(x => x.FilledRatio).Average() * 100;

            var lowH2Threshold = decimal.Parse(brain.configuration.For(ConfigName.LowH2Threshold));
            if (averageFuel < lowH2Threshold)
                brain.GridProgram.Echo(Prompts.LowH2);
            else
                brain.GridProgram.Echo(Prompts.H2Good);

            return averageFuel < lowH2Threshold;
        }

        private static bool LowReactorFuel(this IAdvancedAiBrain brain)
        {
            if (brain.reactors == null || !brain.reactors.Any())
                return false;

            foreach (var reactor in brain.reactors)
            {
                var inventory = new List<MyInventoryItem>();
                reactor.GetInventory().GetItems(inventory);
                if (!inventory.Any())
                {
                    brain.GridProgram.Echo(Prompts.ReactorsLowOnFuel);
                    return true;
                }
                var fuel = (decimal)inventory.First().Amount;
                var lowReactorThreshold = decimal.Parse(brain.configuration.For(ConfigName.LowReactorThreshold));
                if (fuel < lowReactorThreshold)
                {
                    brain.GridProgram.Echo(Prompts.ReactorsLowOnFuel);
                    return true;
                }
            }
            brain.GridProgram.Echo(Prompts.ReactorFuelGood);
            return false;
        }
    }
}
