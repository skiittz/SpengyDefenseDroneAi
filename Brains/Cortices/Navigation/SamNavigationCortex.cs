using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    public class SamNavigationCortex : INavigationCortex
    {
        private readonly IAdvancedAiBrain _brain;

        public SamNavigationCortex(IAdvancedAiBrain brain)
        {
            this._brain = brain;
        }

        private const string GpsColorDesignation = "#FF75C9F1";

        private static string SamFormattedDestination(Vector3D target)
        {
            return $"GPS:Destination:{target.X}:{target.Y}:{target.Z}:{GpsColorDesignation}:";
        }

        public void Go(Vector3D target, bool forceKeenModel = false)
        {
            if (forceKeenModel)
            {
                new KeenNavigationCortex(_brain).Go(target);
                return;
            }

            var command = $"START {SamFormattedDestination(target)}";
            if (_brain.samController.TryRun(command))
            {
                _brain.GridProgram.Echo($"Issued command to SAM: {command}");
                _brain.state.Enroute = true;
                _brain.state.CurrentDestination = target;
                return;
            }

            _brain.GridProgram.Echo($"SAM failed command: {command}");
            _brain.state.Enroute = false;
            return;
        }
    }
}
