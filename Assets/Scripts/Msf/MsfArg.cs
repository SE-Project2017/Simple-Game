namespace Msf
{
    public class MsfArg : Barebones.MasterServer.MsfArgs
    {
        public string MachineAddress { get { return MachineIp; } }

        public string PlayerAToken { get; private set; }
        public string PlayerBToken { get; private set; }
        public string PlayerAName { get; private set; }
        public string PlayerBName { get; private set; }

        public MsfArgName Name = new MsfArgName();

        public MsfArg()
        {
            PlayerAToken = ExtractValue(Name.PlayerAToken);
            PlayerBToken = ExtractValue(Name.PlayerBToken);
            PlayerAName = ExtractValue(Name.PlayerAName);
            PlayerBName = ExtractValue(Name.PlayerBName);
        }

        public class MsfArgName : MsfArgNames
        {
            public string PlayerAToken { get { return "--player-a-token"; } }
            public string PlayerBToken { get { return "--player-b-token"; } }
            public string PlayerAName { get { return "--player-a-name"; } }
            public string PlayerBName { get { return "--player-b-name"; } }
        }
    }
}
