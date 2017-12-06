using System;

using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class GameFoundPacket : SerializablePacket
    {
        public GameServerDetailsPacket GameServerDetails;
        public ServerController.PlayerType PlayerType;
        public Guid Token;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            GameServerDetails.ToBinaryWriter(writer);
            writer.Write((int) PlayerType);
            writer.Write(Token.ToByteArray());
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            GameServerDetails = new GameServerDetailsPacket();
            GameServerDetails.FromBinaryReader(reader);
            PlayerType = (ServerController.PlayerType) reader.ReadInt32();
            Token = new Guid(reader.ReadBytes(16));
        }
    }
}
