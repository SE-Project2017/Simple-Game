namespace Msf
{
    public class MsfArgs : Barebones.MasterServer.MsfArgs
    {
        public string MachineAddress { get { return MachineIp; } }

        public string PlayerAToken { get; private set; }
        public string PlayerBToken { get; private set; }
        public string PlayerAName { get; private set; }
        public string PlayerBName { get; private set; }

        public new MsfArgNames Names = new MsfArgNames();

        public MsfArgs()
        {
            PlayerAToken = ExtractValue(Names.PlayerAToken);
            PlayerBToken = ExtractValue(Names.PlayerBToken);
            PlayerAName = ExtractValue(Names.PlayerAName);
            PlayerBName = ExtractValue(Names.PlayerBName);
        }

        public new class MsfArgNames : Barebones.MasterServer.MsfArgs.MsfArgNames
        {
            public string PlayerAToken { get { return "--player-a-token"; } }

            public string PlayerBToken { get { return "--player-b-token"; } }

            public string PlayerAName { get { return "--player-a-name"; } }

            public string PlayerBName { get { return "--player-b-name"; } }
        }
    }
}
