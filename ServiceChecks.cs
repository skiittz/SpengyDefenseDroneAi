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
    public static class ServiceFunctions
    {
        public static bool NeedsService(MyGridProgram mgp, Configuration configuration, List<IMyBatteryBlock> batteries, List<IMyReactor> reactors, List<IMyGasTank> h2Tanks)
        {
            return LowAmmo(mgp, bool.Parse(configuration.For(ConfigName.EnableLowAmmoCheck))) || 
                LowH2(mgp, h2Tanks, decimal.Parse(configuration.For(ConfigName.LowH2Threshold))) || 
                LowPower(mgp,batteries, decimal.Parse(configuration.For(ConfigName.LowPowerThreshold))) || 
                LowReactorFuel(mgp, reactors, decimal.Parse(configuration.For(ConfigName.LowReactorThreshold)));
        }

        public static bool LowAmmo(MyGridProgram mgp, bool enableLowAmmoCheck)
        {
            if (!enableLowAmmoCheck)
                return false;

            var turrets = new List<IMyLargeTurretBase>();
            mgp.GridTerminalSystem.GetBlocksOfType(turrets, block => block.IsSameConstructAs(mgp.Me));
            foreach (var turret in turrets)
            {
                var inventory = new List<MyInventoryItem>();
                turret.GetInventory().GetItems(inventory);
                if (!inventory.Any())
                {
                    mgp.Echo(Prompts.LowAmmo);
                    return true;
                }
            }

            mgp.Echo(Prompts.AmmoGood);
            return false;
        }

        public static bool LowPower(MyGridProgram mgp, List<IMyBatteryBlock> batteries, decimal powerThreshold)
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
            if (powerLevel < powerThreshold)
                mgp.Echo(Prompts.LowPower);
            else
                mgp.Echo(Prompts.PowerGood);
            return powerLevel < powerThreshold;
        }

        public static bool LowH2(MyGridProgram mgp, List<IMyGasTank> h2Tanks, decimal lowH2Threshold)
        {
            if (h2Tanks == null || !h2Tanks.Any())
                return false;

            var averageFuel = (decimal)h2Tanks.Select(x => x.FilledRatio).Average() * 100;

            if (averageFuel < lowH2Threshold)
                mgp.Echo(Prompts.LowH2);
            else
                mgp.Echo(Prompts.H2Good);

            return averageFuel < lowH2Threshold;
        }

        public static bool LowReactorFuel(MyGridProgram mgp, List<IMyReactor> reactors, decimal lowReactorThreshold)
        {            
            if (reactors == null || !reactors.Any())
                return false;

            foreach (var reactor in reactors)
            {
                var inventory = new List<MyInventoryItem>();
                reactor.GetInventory().GetItems(inventory);
                if (!inventory.Any())
                {
                    mgp.Echo(Prompts.ReactorsLowOnFuel);
                    return true;
                }
                var fuel = (decimal)inventory.First().Amount;
                if (fuel < lowReactorThreshold)
                {
                    mgp.Echo(Prompts.ReactorsLowOnFuel);
                    return true;
                }
            }
            mgp.Echo(Prompts.ReactorFuelGood);
            return false;
        }
    }
}
