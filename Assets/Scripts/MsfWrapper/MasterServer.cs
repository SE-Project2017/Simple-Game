using Barebones.MasterServer;
using UnityEngine;

namespace MsfWrapper
{
    public class MasterServer : MasterServerBehaviour
    {
        protected override void Start()
        {
            Application.targetFrameRate = 60;
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
                new ObservableInt(ProfileKey.SingleplayerBestGrade),
            };
        }
    }
}
