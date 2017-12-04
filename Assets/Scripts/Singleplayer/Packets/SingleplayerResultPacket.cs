using Barebones.Networking;

namespace Singleplayer.Packets
{
    public class SingleplayerResultPacket : SerializablePacket
    {
        public int Placeholder;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Placeholder);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Placeholder = reader.ReadInt32();
        }
    }
}
