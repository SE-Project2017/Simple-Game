using Barebones.Networking;

namespace Assets.Scripts.Multiplayer
{
    public class GameFoundPacket : SerializablePacket
    {
        public GameServerDetailsPacket GameServerDetails;
        public ServerController.PlayerType PlayerType;
        public PlayerToken Token;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            GameServerDetails.ToBinaryWriter(writer);
            writer.Write((int) PlayerType);
            Token.ToBinaryWriter(writer);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            GameServerDetails = new GameServerDetailsPacket();
            GameServerDetails.FromBinaryReader(reader);
            PlayerType = (ServerController.PlayerType) reader.ReadInt32();
            Token.FromBinaryReader(reader);
        }
    }
}
