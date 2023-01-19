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
        public bool NeedsService()
        {
            return LowAmmo() || LowH2() || LowPower() || LowReactorFuel();
        }

        public bool LowAmmo()
        {
            if (!bool.Parse(configuration.For(ConfigName.EnableLowAmmoCheck)))
                return false;

            var turrets = new List<IMyLargeTurretBase>();
            GridTerminalSystem.GetBlocksOfType(turrets, block => block.IsSameConstructAs(Me));
            foreach (var turret in turrets)
            {
                var inventory = new List<MyInventoryItem>();
                turret.GetInventory().GetItems(inventory);
                if (!inventory.Any())
                {
                    Echo(Prompts.LowAmmo);
                    return true;
                }
            }

            Echo(Prompts.AmmoGood);
            return false;
        }

        public bool LowPower()
        {
            if (batteries == null || !batteries.Any())
                return false;

            var currentPower = 0f;
            var maxPower = 0f;

            foreach (var battery in batteries)
            {
                currentPower += battery.CurrentStoredPower;
                maxPower += battery.MaxStoredPower;
            }

            var powerLevel = Math.Round((decimal)(currentPower / maxPower) * 100, 2);
            if (powerLevel < decimal.Parse(configuration.For(ConfigName.LowPowerThreshold)))
                Echo(Prompts.LowPower);
            else
                Echo(Prompts.PowerGood);
            return powerLevel < decimal.Parse(configuration.For(ConfigName.LowPowerThreshold));
        }

        public bool LowH2()
        {
            if (h2Tanks == null || !h2Tanks.Any())
                return false;

            var averageFuel = (decimal)h2Tanks.Select(x => x.FilledRatio).Average() * 100;

            if (averageFuel < decimal.Parse(configuration.For(ConfigName.LowH2Threshold)))
                Echo(Prompts.LowH2);
            else
                Echo(Prompts.H2Good);

            return averageFuel < decimal.Parse(configuration.For(ConfigName.LowH2Threshold));
        }

        public bool LowReactorFuel()
        {
            if (reactors == null || !reactors.Any())
                return false;

            foreach (var reactor in reactors)
            {
                var inventory = new List<MyInventoryItem>();
                reactor.GetInventory().GetItems(inventory);
                if (!inventory.Any())
                {
                    Echo(Prompts.ReactorsLowOnFuel);
                    return true;
                }
                var fuel = (decimal)inventory.First().Amount;
                if (fuel < decimal.Parse(configuration.For(ConfigName.LowReactorThreshold)))
                {
                    Echo(Prompts.ReactorsLowOnFuel);
                    return true;
                }
            }
            Echo(Prompts.ReactorFuelGood);
            return false;
        }
    }
}
