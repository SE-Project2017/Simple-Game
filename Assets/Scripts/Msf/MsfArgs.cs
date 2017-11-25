namespace Assets.Scripts.Msf
{
    public class MsfArgs : Barebones.MasterServer.MsfArgs
    {
        public string MachineAddress
        {
            get { return MachineIp; }
        }

        public string PlayerAToken { get; private set; }
        public string PlayerBToken { get; private set; }

        public new MsfArgNames Names = new MsfArgNames();

        public MsfArgs()
        {
            PlayerAToken = ExtractValue(Names.PlayerAToken);
            PlayerBToken = ExtractValue(Names.PlayerBToken);
        }

        public new class MsfArgNames : Barebones.MasterServer.MsfArgs.MsfArgNames
        {
            public string PlayerAToken { get { return "--player-a-token"; } }
            public string PlayerBToken { get { return "--player-b-token"; } }
        }
    }
}
