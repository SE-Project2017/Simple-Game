using System;
using System.Collections;

using Barebones.MasterServer;
using Barebones.Networking;

using Msf;

using Multiplayer;

using UI;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

using Utils;

using ConnectionToMaster = Msf.ConnectionToMaster;

namespace App
{
    public class ClientController : Singleton<ClientController>
    {
        public GameFoundPacket GameInfo;

        public event Action<string> OnPlayerNameChange;
        public event Action<int> OnWinCountChange;
        public event Action<int> OnLossCountChange;
        public event Action<int> OnGameCountChange;

        public string PlayerName
        {
            get { return mPlayerName; }
            private set
            {
                mPlayerName = value;
                if (OnPlayerNameChange != null)
                {
                    OnPlayerNameChange.Invoke(value);
                }
            }
        }

        public int Wins
        {
            get { return mWins; }
            private set
            {
                mWins = value;
                if (OnWinCountChange != null)
                {
                    OnWinCountChange.Invoke(value);
                }
            }
        }

        public int Losses
        {
            get { return mLosses; }
            private set
            {
                mLosses = value;
                if (OnLossCountChange != null)
                {
                    OnLossCountChange.Invoke(value);
                }
            }
        }

        public int GamesPlayed
        {
            get { return mGamesPlayed; }
            private set
            {
                mGamesPlayed = value;
                if (OnGameCountChange != null)
                {
                    OnGameCountChange.Invoke(value);
                }
            }
        }

        private string mUsername;
        private string mPassword;
        private State mState = State.Idle;

        private string mPlayerName;
        private int mWins;
        private int mLosses;
        private int mGamesPlayed;

        public void Start()
        {
            MsfContext.Connection.SetHandler((short) OperationCode.GameFound, OnGameFound);
            MsfContext.Connection.Disconnected += () => { StartCoroutine(OnDisconnected()); };
            MsfContext.Client.Auth.LoggedIn += () =>
            {
                var profile = new ObservableProfile
                {
                    new ObservableString(ProfileKey.Name, string.Empty),
                    new ObservableInt(ProfileKey.Wins),
                    new ObservableInt(ProfileKey.Losses),
                    new ObservableInt(ProfileKey.GamesPlayed),
                };
                RetriveProfile(profile);
            };
        }

        public void OnStartSearchGame()
        {
            Assert.IsTrue(mState == State.Idle);
            MsfContext.Connection.Peer.SendMessage(
                MessageHelper.Create((short) OperationCode.StartSearchGame));
            mState = State.SearchingGame;
        }

        public void OnGameEnd()
        {
            Assert.IsTrue(mState == State.PlayingMultiplayer);
            mState = State.Idle;
        }

        public void OnLoggedIn(string username, string password)
        {
            mUsername = username;
            mPassword = password;
        }

        private void OnGameFound(IIncommingMessage message)
        {
            if (mState != State.SearchingGame)
            {
                return;
            }
            GameInfo = message.Deserialize(new GameFoundPacket());
            mState = State.PlayingMultiplayer;
            StartCoroutine(Utilities.FadeOutLoadScene("MultiplayerGame"));
        }

        private IEnumerator OnDisconnected()
        {
            while (SceneManager.GetActiveScene().name == "MultiplayerGame")
            {
                yield return null;
            }
            var dialog = new AlertDialog.Builder()
                .SetMessage("Disconnected from server, retrying...")
                .SetNeutralButton("Cancel", Application.Quit)
                .Show();
            ConnectionToMaster.Instance.Connect();
            while (!MsfContext.Connection.IsConnected)
            {
                yield return null;
            }
            dialog.Close();
            if (mUsername == null || mPassword == null)
            {
                yield break;
            }
            dialog = new AlertDialog.Builder().SetMessage("Loging in...").Show();
            MsfContext.Client.Auth.LogIn(mUsername, mPassword, (info, error) =>
            {
                dialog.Close();
                if (info == null)
                {
                    new AlertDialog.Builder()
                        .SetMessage(string.Format("Login failed: {0}", error))
                        .SetNeutralButton("OK",
                            () => StartCoroutine(Utilities.FadeOutLoadScene("Login")))
                        .Show();
                }
            });
        }

        private void RetriveProfile(ObservableProfile profile)
        {
            MsfContext.Client.Profiles.GetProfileValues(profile, (successful, error) =>
            {
                if (!successful)
                {
                    RetriveProfile(profile);
                    return;
                }
                var nameProp = profile.GetProperty<ObservableString>(ProfileKey.Name);
                PlayerName = nameProp.Value;
                nameProp.OnDirty += property => PlayerName = nameProp.Value;
                var winsProp = profile.GetProperty<ObservableInt>(ProfileKey.Wins);
                Wins = winsProp.Value;
                winsProp.OnDirty += property => Wins = winsProp.Value;
                var lossesProp = profile.GetProperty<ObservableInt>(ProfileKey.Losses);
                Losses = lossesProp.Value;
                lossesProp.OnDirty += property => Losses = lossesProp.Value;
                var gamesPlayedProp = profile.GetProperty<ObservableInt>(ProfileKey.GamesPlayed);
                GamesPlayed = gamesPlayedProp.Value;
                gamesPlayedProp.OnDirty += property => GamesPlayed = gamesPlayedProp.Value;
            });
        }

        private enum State
        {
            Idle,
            SearchingGame,
            PlayingMultiplayer,
        }
    }
}
