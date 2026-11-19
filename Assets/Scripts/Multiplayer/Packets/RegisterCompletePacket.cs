using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class RegisterCompletePacket : SerializablePacket
    {
        public ServerController.GameInfo GameInfo;
        public ServerController.PlayerType LocalPlayerType;
        public string LocalUsername;
        public string RemoteUsername;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            WriteUInt64Array(writer, GameInfo.GeneratorSeeds);
            WriteUInt64Array(writer, GameInfo.PlayerASeed);
            WriteUInt64Array(writer, GameInfo.PlayerBSeed);
            writer.Write((int) LocalPlayerType);
            writer.Write(LocalUsername);
            writer.Write(RemoteUsername);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            GameInfo = new ServerController.GameInfo
            {
                GeneratorSeeds = ReadUInt64Array(reader),
                PlayerASeed = ReadUInt64Array(reader),
                PlayerBSeed = ReadUInt64Array(reader),
            };
            LocalPlayerType = (ServerController.PlayerType) reader.ReadInt32();
            LocalUsername = reader.ReadString();
            RemoteUsername = reader.ReadString();
        }

        private static void WriteUInt64Array(EndianBinaryWriter writer, ulong[] values)
        {
            writer.Write(values.Length);
            for (int i = 0; i < values.Length; ++i)
            {
                writer.Write(values[i]);
            }
        }

        private static ulong[] ReadUInt64Array(EndianBinaryReader reader)
        {
            int length = reader.ReadInt32();
            var values = new ulong[length];
            for (int i = 0; i < length; ++i)
            {
                values[i] = reader.ReadUInt64();
            }
            return values;
        }
    }
}
