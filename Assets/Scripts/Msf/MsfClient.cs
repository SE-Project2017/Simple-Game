using Assets.Scripts.Msf.Modules.Auth;

using Barebones.Networking;

namespace Assets.Scripts.Msf
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
