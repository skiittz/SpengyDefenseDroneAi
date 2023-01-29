using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;

namespace IngameScript
{
    public interface ICortex
    {
        CortexType Type { get; set; }
        IAiBrain Brain { get; set; }
    }

    public enum CortexType
    {
        BroadcastingCortex,
        CombatCortex,
        FixedWeaponControllerCortex,
        MaintenanceCortex,
        NavigationCortex,
        RadioHandlerCortex,
        SuicideCortex,
        TargetDesignatorCortex
    }

    public static class CortexExtensions
    {
        public static ICortex ProcessWith(this IAiBrain brain, CortexType type)
        {
            return brain.cortices.For(type);
        }

        private static ICortex For(this IEnumerable<ICortex> cortices, CortexType requestedType)
        {
            return cortices.Single(x => x.Type == requestedType);
        }

        public static IEnumerable<ICortex> CreateCortices(this IAiBrain brain)
        {
            yield return new BroadcastingCortex(brain);
        }
    }
}
