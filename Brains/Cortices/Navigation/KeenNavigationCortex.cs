using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class KeenNavigationCortex : INavigationCortex
    {
        private readonly IAdvancedAiBrain _brain;

        public KeenNavigationCortex(IAdvancedAiBrain brain)
        {
            this._brain = brain;
        }

        public void Go(Vector3D destination, bool forceKeenModel = false)
        {
            var isDocking = _brain.state.Status == Status.Docking;
            _brain.remote.SetAutoPilotEnabled(false);
            _brain.remote.SpeedLimit = GetSpeedLimit();
            _brain.remote.ClearWaypoints();
            _brain.remote.AddWaypoint(destination, Prompts.Destination);
            _brain.remote.SetDockingMode(isDocking);
            _brain.remote.SetCollisionAvoidance(true);
            _brain.remote.FlightMode = FlightMode.OneWay;
            _brain.remote.SetAutoPilotEnabled(true);

            _brain.GridProgram.Echo($"Moving to waypoint using Keen Code: {destination}");
            _brain.state.Enroute = true;
            _brain.state.CurrentDestination = destination;
        }

        private int GetSpeedLimit()
        {
            string speedConfig;
            switch (_brain.state.Status)
            {
                case Status.PreparingToAttack:
                case Status.Attacking:
                    speedConfig = _brain.configuration.For(ConfigName.AttackSpeedLimit);
                    break;
               case Status.Docking:
                   speedConfig = _brain.configuration.For(ConfigName.DockSpeedLimit);
                   break;
                default:
                    speedConfig = _brain.configuration.For(ConfigName.GeneralSpeedLimit);
                    break;
            }

            return int.Parse(speedConfig);
        }
    }
}
