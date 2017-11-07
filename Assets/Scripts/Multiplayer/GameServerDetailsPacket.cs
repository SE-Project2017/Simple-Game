using Barebones.Networking;

namespace Assets.Scripts.Multiplayer
{
    public class GameServerDetailsPacket : SerializablePacket
    {
        public string Address;
        public int Port;
        public int SpawnID;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Address);
            writer.Write(Port);
            writer.Write(SpawnID);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Address = reader.ReadString();
            Port = reader.ReadInt32();
            SpawnID = reader.ReadInt32();
        }
    }
}
