using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public interface IBroadcastingCortex : ICortex
    {
        void BroadcastTarget(MyDetectedEntityInfo target);
        void BroadcastTarget(Vector3D target);
        void Relay(MyIGCMessage packet);
        void ManageAntennas();
    }
    public class BroadcastingCortex : IBroadcastingCortex
    {
        public IAiBrain brain { get; set; }

        public BroadcastingCortex(IAiBrain brain)
        {
            this.brain = brain;
        }

        public void BroadcastTarget(MyDetectedEntityInfo target)
        {
            BroadcastTarget(target.Position);
        }

        public void BroadcastTarget(Vector3D target)
        {
            EnableAntenna();
            brain.GridProgram.IGC.SendBroadcastMessage(brain.configuration.For(ConfigName.RadioChannel),
                target.ToString());
        }

        public void Relay(MyIGCMessage packet)
        {
            if (!brain.configuration.IsEnabled(ConfigName.EnableRelayBroadcast))
                return;

            EnableAntenna();
            brain.GridProgram.IGC.SendBroadcastMessage(brain.configuration.For(ConfigName.RadioChannel), packet.Data);
        }

        private void EnableAntenna()
        {
            var antenna = brain.GridProgram.FirstTaggedOrDefault<IMyRadioAntenna>(brain.configuration.For(ConfigName.Tag));
            antenna.EnableBroadcasting = true;
        }

        public void ManageAntennas()
        {
            if (!brain.configuration.IsEnabled(ConfigName.UseBurstTransmissions))
                return;

            var antennas = new List<IMyRadioAntenna>();
            brain.GridProgram.GridTerminalSystem.GetBlocksOfType(antennas,
                block => block.IsSameConstructAs(brain.GridProgram.Me));

            antennas.ForEach(x => x.EnableBroadcasting = false);
        }
    }

    public static class BroadcastingCortexExtensions
    {
        public static void ManageAntennas(this IAiBrain brain)
        {
            brain.Cortex<IBroadcastingCortex>().ManageAntennas();
        }

        public static void Relay(this IAiBrain brain, MyIGCMessage packet)
        {
            brain.Cortex<IBroadcastingCortex>().Relay(packet);
        }

        public static void BroadcastTarget(this IAiBrain brain, MyDetectedEntityInfo target)
        {
            brain.Cortex<IBroadcastingCortex>().BroadcastTarget(target);
        }

        public static void BroadcastTarget(this IAiBrain brain, Vector3D target)
        {
            brain.Cortex<IBroadcastingCortex>().BroadcastTarget(target);
        }
    }
}