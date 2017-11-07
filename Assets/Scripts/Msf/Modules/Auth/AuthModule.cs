namespace Assets.Scripts.Msf.Modules.Auth
{
    public class AuthModule : Barebones.MasterServer.AuthModule
    {
        protected override bool ValidateEmail(string email)
        {
            return true;
        }
    }
}
