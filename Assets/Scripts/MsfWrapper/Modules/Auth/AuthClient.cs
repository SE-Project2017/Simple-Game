using System.Collections.Generic;

using Barebones.MasterServer;
using Barebones.Networking;

using Utils;

namespace MsfWrapper.Modules.Auth
{
    public class AuthClient : MsfAuthClient
    {
        public AuthClient(IClientSocket connection) : base(connection)
        {
            connection.Disconnected += () => IsLoggedIn = false;
        }

        public void Register(string username, string password, SuccessCallback callback)
        {
            Register(
                new Dictionary<string, string>
                {
                    {AuthModule.Username, username},
                    {AuthModule.Password, password},
                    {AuthModule.Email, username}
                },
                callback);
        }

        public new void LogIn(string username, string password, LoginCallback callback)
        {
            LogIn(
                new Dictionary<string, string>
                {
                    {AuthModule.Username, username},
                    {AuthModule.Password, password},
                    {AuthModule.Version, Utilities.VersionCode.ToString()}
                }, callback);
        }
    }
}
