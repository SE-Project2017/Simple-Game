using System;

using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class ClientQueryGameResultPacket : SerializablePacket
    {
        public Guid MatchID;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(MatchID.ToByteArray());
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            MatchID = new Guid(reader.ReadBytes(16));
        }
    }
}
