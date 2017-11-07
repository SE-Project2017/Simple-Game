using Barebones.Networking;

namespace Assets.Test.Scripts
{
    public class GameServerDetailsPacket : SerializablePacket
    {
        public string MachineIp;
        public int MachinePort;
        public int SpawnId;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(MachineIp);
            writer.Write(MachinePort);
            writer.Write(SpawnId);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            MachineIp = reader.ReadString();
            MachinePort = reader.ReadInt32();
            SpawnId = reader.ReadInt32();
        }
    }
}
