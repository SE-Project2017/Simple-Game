using Barebones.Networking;

namespace Assets.Scripts.Msf
{
    public static class MsfContext
    {
        public static IClientSocket Connection
        {
            get { return Barebones.MasterServer.Msf.Connection; }
        }

        public static MsfClient Client { get; private set; }

        static MsfContext()
        {
            Client = new MsfClient(Connection);
        }
    }
}
