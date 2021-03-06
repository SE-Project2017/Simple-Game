﻿using Barebones.Networking;

using MsfWrapper.Modules.Auth;

namespace MsfWrapper
{
    public class MsfClient : Barebones.MasterServer.MsfClient
    {
        public new AuthClient Auth { get; private set; }

        public MsfClient(IClientSocket connection) : base(connection)
        {
            Auth = new AuthClient(connection);
        }
    }
}
