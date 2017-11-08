namespace Assets.Scripts.Msf
{
    public class MsfArgs : Barebones.MasterServer.MsfArgs
    {
        public string MachineAddress
        {
            get { return MachineIp; }
        }
    }
}
