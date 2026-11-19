using Barebones.Networking;

namespace Multiplayer.Packets
{
    public class FrameUpdatePacket : SerializablePacket
    {
        public int FrameCount;
        public NetworkPlayer.PlayerEvent[] Events;

        public override void ToBinaryWriter(EndianBinaryWriter writer)
        {
            writer.Write(FrameCount);
            writer.Write(Events.Length);
            for (int i = 0; i < Events.Length; ++i)
            {
                writer.Write((byte) Events[i].Type);
                writer.Write(Events[i].Data);
            }
        }

        public override void FromBinaryReader(EndianBinaryReader reader)
        {
            FrameCount = reader.ReadInt32();
            int count = reader.ReadInt32();
            Events = new NetworkPlayer.PlayerEvent[count];
            for (int i = 0; i < count; ++i)
            {
                Events[i] = new NetworkPlayer.PlayerEvent
                {
                    Type = (NetworkPlayer.PlayerEvent.EventType) reader.ReadByte(),
                    Data = reader.ReadInt32(),
                };
            }
        }
    }
}
