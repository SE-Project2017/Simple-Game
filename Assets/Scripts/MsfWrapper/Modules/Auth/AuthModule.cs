﻿using System.Collections.Generic;

using Barebones.MasterServer;
using Barebones.Networking;

using Utils;

namespace MsfWrapper.Modules.Auth
{
    public class AuthModule : Barebones.MasterServer.AuthModule
    {
        public const string Username = "username";
        public const string Password = "password";
        public const string Email = "email";
        public const string Version = "version";

        protected override bool ValidateEmail(string email)
        {
            return true;
        }

        protected override async void HandleLogIn(IIncommingMessage message)
        {
            var encryptedData = message.AsBytes();
            var securityExt =
                message.Peer.GetExtension<PeerSecurityExtension>();
            var aesKey = securityExt.AesKey;
            var decrypted =
                await Msf.Security.DecryptAES(encryptedData, aesKey);
            var data = new Dictionary<string, string>().FromBytes(decrypted);
            int version;
            if (!data.ContainsKey(Version) ||
                !int.TryParse(data[Version], out version) ||
                version != Utilities.VersionCode)
            {
                message.Respond("Client outdated".ToBytes(),
                                ResponseStatus.Unauthorized);
                return;
            }
            base.HandleLogIn(message);
        }
    }
}
