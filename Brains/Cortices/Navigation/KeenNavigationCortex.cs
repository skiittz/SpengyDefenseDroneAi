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

        public void Stop()
        {
            _brain.remote.ClearWaypoints();
            _brain.remote.SetAutoPilotEnabled(false);
        }

        private int GetSpeedLimit()
        {
            int speedLimit;
            switch (_brain.state.Status)
            {
                case Status.PreparingToAttack:
                case Status.Attacking:
                    var target = _brain.state.PendingTarget;
                    var distance = _brain.DistanceToWaypoint(target);
                    var attackSpeedLimit = int.Parse(_brain.configuration.For(ConfigName.AttackSpeedLimit));
                    speedLimit = distance < 600 ? (int)Math.Pow(distance / 600, 4) * attackSpeedLimit : attackSpeedLimit;
                    speedLimit = Math.Max(5, speedLimit);
                    break;
               case Status.Docking:
                   speedLimit = int.Parse(_brain.configuration.For(ConfigName.DockSpeedLimit));
                   break;
                default:
                    speedLimit = int.Parse(_brain.configuration.For(ConfigName.GeneralSpeedLimit));
                    break;
            }

            return speedLimit;
        }

        public void EchoModel()
        {
            _brain.GridProgram.Echo($"{Prompts.NavigationModel}: Keen SWH");
        }

        public void Dock()
        {
            _brain.batteries.ForEach(x => x.ChargeMode = ChargeMode.Auto);
            _brain.h2Tanks.ForEach(x => x.Stockpile = false);

            _brain.connector.Disconnect();

            _brain.Cortex<INavigationCortex>().Go(_brain.state.DockApproach);
        }

        public void UnDock()
        {
            _brain.batteries.ForEach(x => x.ChargeMode = ChargeMode.Auto);
            _brain.h2Tanks.ForEach(x => x.Stockpile = false);

            _brain.connector.Disconnect();

            _brain.Cortex<INavigationCortex>().Go(_brain.state.DockApproach);
        }
    }
}
