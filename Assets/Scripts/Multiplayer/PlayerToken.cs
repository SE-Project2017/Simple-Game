using System;
using System.Linq;
using System.Security.Cryptography;

using Barebones.Networking;

using UnityEngine.Assertions;

namespace Multiplayer
{
    public struct PlayerToken
    {
        public byte[] Data;

        private const int Length = 16;

        public string ToBase64()
        {
            Assert.IsTrue(Data.Length == Length);
            return Convert.ToBase64String(Data);
        }

        public void ToBinaryWriter(EndianBinaryWriter writer)
        {
            Assert.IsTrue(Data.Length == Length);
            writer.Write(Data);
        }

        public void FromBinaryReader(EndianBinaryReader reader)
        {
            Data = reader.ReadBytes(Length);
        }

        public override bool Equals(object obj)
        {
            return obj is PlayerToken && this == (PlayerToken) obj;
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public static PlayerToken New()
        {
            PlayerToken token = new PlayerToken {Data = new byte[Length]};
            var provider = new RNGCryptoServiceProvider();
            provider.GetBytes(token.Data);
            return token;
        }

        public static PlayerToken FromBase64(string base64)
        {
            var token = new PlayerToken {Data = Convert.FromBase64String(base64)};
            Assert.IsTrue(token.Data.Length == Length);
            return token;
        }

        public static bool operator ==(PlayerToken lhs, PlayerToken rhs)
        {
            return lhs.Data.SequenceEqual(rhs.Data);
        }

        public static bool operator !=(PlayerToken lhs, PlayerToken rhs)
        {
            return !(lhs == rhs);
        }
    }
}
