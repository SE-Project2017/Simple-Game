using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class ClientGameResultPacket : SerializablePacket
    {
        public string PlayerAName;
        public string PlayerBName;

        public int PlayerAMmr;
        public int PlayerBMmr;

        public int PlayerAMmrChange;
        public int PlayerBMmrChange;

        public ServerController.PlayerType PlayerType;
        public ServerController.GameResult Result;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(PlayerAName);
            writer.Write(PlayerBName);

            writer.Write(PlayerAMmr);
            writer.Write(PlayerBMmr);

            writer.Write(PlayerAMmrChange);
            writer.Write(PlayerBMmrChange);

            writer.Write((int) PlayerType);
            writer.Write((int) Result);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            PlayerAName = reader.ReadString();
            PlayerBName = reader.ReadString();

            PlayerAMmr = reader.ReadInt32();
            PlayerBMmr = reader.ReadInt32();

            PlayerAMmrChange = reader.ReadInt32();
            PlayerBMmrChange = reader.ReadInt32();

            PlayerType = (ServerController.PlayerType) reader.ReadInt32();
            Result = (ServerController.GameResult)reader.ReadInt32();
        }
    }
}
