using System.Collections.Generic;
using System.Linq;

namespace IngameScript
{
    public interface ICortex
    {
    }

    public interface IAdvancedCortex : ICortex
    {
    }

    public static class CortexExtensions
    {
        public static T Cortex<T>(this IAiBrain brain) where T : class, ICortex
        {
            return brain.cortices.Single(x => x.GetType() == typeof(T)) as T;
        }

        public static IEnumerable<ICortex> CreateCortices(this IAiBrain brain)
        {
            yield return new BroadcastingCortex(brain);
            if (brain.weaponCoreIsActive)
                yield return new WeaponCoreTargetFinderCortex(brain);
            else
                yield return new VanillaTargetFinderCortex(brain);
        }

        public static IEnumerable<ICortex> CreateCortices(this IAdvancedAiBrain brain)
        {
            foreach (var cortex in (brain as IAiBrain).CreateCortices())
                yield return cortex;

            if (brain.samController != null && brain.samController.IsWorking)
                yield return new SamNavigationCortex(brain);
            else
                yield return new KeenNavigationCortex(brain);
        }
    }
}