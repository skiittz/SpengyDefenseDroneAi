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
    static class TargetBroadcaster
    {
        public static void BroadcastTarget(this IMyIntergridCommunicationSystem IGC, MyDetectedEntityInfo target, string channel)
        {
            IGC.BroadcastTarget(target.Position, channel);
        }
        public static void BroadcastTarget(this IMyIntergridCommunicationSystem IGC, Vector3D target, string channel)
        {            
            IGC.SendBroadcastMessage(channel, target.ToString(), TransmissionDistance.TransmissionDistanceMax);
        }

        public static void Relay(this IMyIntergridCommunicationSystem IGC, MyIGCMessage packet, string channel)
        {
            IGC.SendBroadcastMessage(channel, packet.Data, TransmissionDistance.TransmissionDistanceMax);
        }
    }
}
