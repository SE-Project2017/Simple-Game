using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class GameEndedPacket : SerializablePacket
    {
        public int SpawnID;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(SpawnID);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            SpawnID = reader.ReadInt32();
        }
    }
}
