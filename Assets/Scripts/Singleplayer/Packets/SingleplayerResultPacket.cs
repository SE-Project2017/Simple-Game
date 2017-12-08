using Barebones.Networking;

namespace Singleplayer.Packets
{
    public class SingleplayerResultPacket : SerializablePacket
    {
        public int Grade;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Grade);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Grade = reader.ReadInt32();
        }
    }
}
