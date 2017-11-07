using Barebones.Networking;

namespace Assets.Scripts.Msf
{
    public class MsfContext
    {
        public static IClientSocket Connection => Barebones.MasterServer.Msf.Connection;
        
        public static MsfClient Client { get; }

        static MsfContext()
        {
            Client = new MsfClient(Connection);
        }
    }
}
