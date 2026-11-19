using System;

using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class JoinGamePacket : SerializablePacket
    {
        public Guid Token;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Token.ToByteArray());
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Token = new Guid(reader.ReadBytes(16));
        }
    }
}
