﻿using System;

using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class GameServerDetailsPacket : SerializablePacket
    {
        public string Address;
        public int Port;
        public Guid MatchID;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(Address);
            writer.Write(Port);
            writer.Write(MatchID.ToByteArray());
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Address = reader.ReadString();
            Port = reader.ReadInt32();
            MatchID = new Guid(reader.ReadBytes(16));
        }
    }
}
