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
    public static class BroadcastingCortex
    {
        public static void BroadcastTarget(this IAiBrain brain, MyDetectedEntityInfo target)
        {
            brain.BroadcastTarget(target.Position);
        }

        public static void BroadcastTarget(this IAiBrain brain, Vector3D target)
        {
            brain.EnableAntenna();
            brain.GridProgram.IGC.SendBroadcastMessage(brain.configuration.For(ConfigName.RadioChannel), target.ToString(), TransmissionDistance.TransmissionDistanceMax);
        }

        public static void Relay(this IAiBrain brain, MyIGCMessage packet)
        {
            if (!brain.configuration.IsEnabled(ConfigName.EnableRelayBroadcast))
                return;

            brain.EnableAntenna();
            brain.GridProgram.IGC.SendBroadcastMessage(brain.configuration.For(ConfigName.RadioChannel), packet.Data, TransmissionDistance.TransmissionDistanceMax);
        }
        private static void EnableAntenna(this IAiBrain brain)
        {
            var antenna = brain.GridProgram.FirstTaggedOrDefault<IMyRadioAntenna>(brain.configuration.For(ConfigName.Tag));
            antenna.EnableBroadcasting = true;
        }
        public static void ManageAntennas(this IAiBrain brain)
        {
            if (!brain.configuration.IsEnabled(ConfigName.UseBurstTransmissions))
                return;

            var antennas = new List<IMyRadioAntenna>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(antennas, block => block.IsSameConstructAs(brain.GridProgram.Me));

            antennas.ForEach(x => x.EnableBroadcasting = false);
        }
    }
}
