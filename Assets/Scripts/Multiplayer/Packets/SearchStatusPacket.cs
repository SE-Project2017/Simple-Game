using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class SearchStatusPacket : SerializablePacket
    {
        public string Username;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Username);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Username = reader.ReadString();
        }
    }
}
