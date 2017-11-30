using Barebones.MasterServer;
using Barebones.Networking;

namespace MsfWrapper
{
    public static class MsfContext
    {
        public static IClientSocket Connection
        {
            get { return Barebones.MasterServer.Msf.Connection; }
        }

        public static MsfServer Server { get; private set; }
        public static MsfClient Client { get; private set; }
        public static MsfArg Args { get; private set; }

        static MsfContext()
        {
            Server = new MsfServer(Connection);
            Client = new MsfClient(Connection);
            Args = new MsfArg();
        }
    }
}
