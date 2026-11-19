using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class PlayerEndedPacket : SerializablePacket
    {
        public ServerController.GameResult Result;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write((int) Result);
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            Result = (ServerController.GameResult) reader.ReadInt32();
        }
    }
}
