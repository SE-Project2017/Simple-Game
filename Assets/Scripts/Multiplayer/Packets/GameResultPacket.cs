using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class GameResultPacket : SerializablePacket
    {
        public OutcomeType Outcome;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write((int) Outcome);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Outcome = (OutcomeType) reader.ReadInt32();
        }

        public enum OutcomeType
        {
            Win,
            Lose,
            Draw,
            DataOutOfSync,
        }
    }
}
