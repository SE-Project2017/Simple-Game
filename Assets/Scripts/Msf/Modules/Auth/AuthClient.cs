using System.Collections.Generic;

using Barebones.MasterServer;
using Barebones.Networking;

namespace Assets.Scripts.Msf.Modules.Auth
{
    public class AuthClient : MsfAuthClient
    {
        public AuthClient(IClientSocket connection) : base(connection) { }

        public void Register(string username, string password, SuccessCallback callback)
        {
            Register(
                new Dictionary<string, string>
                {
                    {"username", username},
                    {"password", password},
                    {"email", username}
                },
                callback);
        }
    }
}
