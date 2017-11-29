using Barebones.MasterServer;

namespace Msf
{
    public class MasterServer : MasterServerBehaviour
    {
        protected override void Start()
        {
            StartCoroutine(StartOnNextFrame());
            var profileModule = FindObjectOfType<ProfilesModule>();
            profileModule.ProfileFactory = (username, peer) => new ObservableServerProfile(username)
            {
                new ObservableString(ProfileKey.Name, username),
                new ObservableInt(ProfileKey.Wins),
                new ObservableInt(ProfileKey.Losses),
                new ObservableInt(ProfileKey.GamesPlayed),
            };
        }
    }
}
