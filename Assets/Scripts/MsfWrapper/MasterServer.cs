using Barebones.MasterServer;

namespace MsfWrapper
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
                new ObservableInt(ProfileKey.MultiplayerWins),
                new ObservableInt(ProfileKey.MultiplayerLosses),
                new ObservableInt(ProfileKey.MultiplayerGamesPlayed),
                new ObservableInt(ProfileKey.SingleplayerGamesPlayed),
                new ObservableInt(ProfileKey.MatchmakingRating),
            };
        }
    }
}
