﻿using System;

using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class GameEndedPacket : SerializablePacket
    {
        public int SpawnID;
        public Guid MatchID;

        public int PlayerAMmr;
        public int PlayerBMmr;

        public int PlayerAMmrChange;
        public int PlayerBMmrChange;

        public string PlayerAName;
        public string PlayerBName;

        public ServerController.GameResult Result;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(SpawnID);
            writer.Write(MatchID.ToByteArray());

            writer.Write(PlayerAMmr);
            writer.Write(PlayerBMmr);

            writer.Write(PlayerAMmrChange);
            writer.Write(PlayerBMmrChange);

            writer.Write(PlayerAName);
            writer.Write(PlayerBName);

            writer.Write((int) Result);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            SpawnID = reader.ReadInt32();
            MatchID = new Guid(reader.ReadBytes(16));

            PlayerAMmr = reader.ReadInt32();
            PlayerBMmr = reader.ReadInt32();

            PlayerAMmrChange = reader.ReadInt32();
            PlayerBMmrChange = reader.ReadInt32();

            PlayerAName = reader.ReadString();
            PlayerBName = reader.ReadString();

            Result = (ServerController.GameResult) reader.ReadInt32();
        }
    }
}
