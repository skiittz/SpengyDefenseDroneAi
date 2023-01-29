using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;
using VRageMath;

namespace IngameScript
{
    public class BroadcastingCortex : ICortex
    {
        public CortexType Type { get; set; }
        public IAiBrain Brain { get; set; }

        public BroadcastingCortex(IAiBrain brain)
        {
            Type = CortexType.BroadcastingCortex;
            Brain = brain;
        }

        public void BroadcastTarget(MyDetectedEntityInfo target)
        {
            BroadcastTarget(target.Position);
        }

        public void BroadcastTarget(Vector3D target)
        {
            EnableAntenna();
            Brain.GridProgram.IGC.SendBroadcastMessage(Brain.configuration.For(ConfigName.RadioChannel),
                target.ToString());
        }

        public void Relay(MyIGCMessage packet)
        {
            if (!Brain.configuration.IsEnabled(ConfigName.EnableRelayBroadcast))
                return;

            EnableAntenna();
            Brain.GridProgram.IGC.SendBroadcastMessage(Brain.configuration.For(ConfigName.RadioChannel), packet.Data);
        }

        private void EnableAntenna()
        {
            var antenna =
                Brain.GridProgram.FirstTaggedOrDefault<IMyRadioAntenna>(Brain.configuration.For(ConfigName.Tag));
            antenna.EnableBroadcasting = true;
        }

        public void ManageAntennas()
        {
            if (!Brain.configuration.IsEnabled(ConfigName.UseBurstTransmissions))
                return;

            var antennas = new List<IMyRadioAntenna>();
            Brain.GridProgram.GridTerminalSystem.GetBlocksOfType(antennas,
                block => block.IsSameConstructAs(Brain.GridProgram.Me));

            antennas.ForEach(x => x.EnableBroadcasting = false);
        }
    }
    //public static class StaticBroadcastingCortex
    //{
    //    public static void BroadcastTarget(this IAiBrain brain, MyDetectedEntityInfo target)
    //    {
    //        brain.BroadcastTarget(target.Position);
    //    }

    //    public static void BroadcastTarget(this IAiBrain brain, Vector3D target)
    //    {
    //        brain.EnableAntenna();
    //        brain.GridProgram.IGC.SendBroadcastMessage(brain.configuration.For(ConfigName.RadioChannel),
    //            target.ToString());
    //    }

    //    public static void Relay(this IAiBrain brain, MyIGCMessage packet)
    //    {
    //        if (!brain.configuration.IsEnabled(ConfigName.EnableRelayBroadcast))
    //            return;

    //        brain.EnableAntenna();
    //        brain.GridProgram.IGC.SendBroadcastMessage(brain.configuration.For(ConfigName.RadioChannel), packet.Data);
    //    }

    //    private static void EnableAntenna(this IAiBrain brain)
    //    {
    //        var antenna =
    //            brain.GridProgram.FirstTaggedOrDefault<IMyRadioAntenna>(brain.configuration.For(ConfigName.Tag));
    //        antenna.EnableBroadcasting = true;
    //    }

    //    public static void ManageAntennas(this IAiBrain brain)
    //    {
    //        if (!brain.configuration.IsEnabled(ConfigName.UseBurstTransmissions))
    //            return;

    //        var antennas = new List<IMyRadioAntenna>();
    //        brain.GridProgram.GridTerminalSystem.GetBlocksOfType(antennas,
    //            block => block.IsSameConstructAs(brain.GridProgram.Me));

    //        antennas.ForEach(x => x.EnableBroadcasting = false);
    //    }
    //}
}