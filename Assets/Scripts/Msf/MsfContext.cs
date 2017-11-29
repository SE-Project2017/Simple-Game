using Barebones.Networking;

namespace Msf
{
    public static class MsfContext
    {
        public static IClientSocket Connection
        {
            get { return Barebones.MasterServer.Msf.Connection; }
        }

        public static MsfClient Client { get; private set; }
        public static MsfArg Args { get; private set; }

        static MsfContext()
        {
            Client = new MsfClient(Connection);
            Args = new MsfArg();
        }
    }
}
