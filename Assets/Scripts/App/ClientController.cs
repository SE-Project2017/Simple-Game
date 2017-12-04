using System;
using System.Collections;

using Barebones.MasterServer;
using Barebones.Networking;

using MsfWrapper;

using Multiplayer;
using Multiplayer.Packets;

using UI;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

using Utils;

using ConnectionToMaster = MsfWrapper.ConnectionToMaster;

namespace App
{
    public class ClientController : Singleton<ClientController>
    {
        public MainMenuUI.Tab MainMenuTab = MainMenuUI.Tab.Versus;
        public GameFoundPacket GameInfo;

        public event Action<string> OnPlayerNameChange;
        public event Action<int> OnWinCountChange;
        public event Action<int> OnLossCountChange;
        public event Action<int> OnGameCountChange;
        public event Action OnSearchStarted;
        public event Action OnSearchStopped;

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
                    new ObservableString(ProfileKey.Name),
                    new ObservableInt(ProfileKey.Wins),
                    new ObservableInt(ProfileKey.Losses),
                    new ObservableInt(ProfileKey.GamesPlayed),
                };
                RetriveProfile(profile);
            };
            StartCoroutine(QuerySearchStatus());
        }

        public void OnStartSearchGame()
        {
            Assert.IsTrue(mState == State.Idle);
            MsfContext.Connection.Peer.SendMessage(
                MessageHelper.Create((short) OperationCode.StartSearchGame));
            mState = State.SearchingGame;
            if (OnSearchStarted != null)
            {
                OnSearchStarted.Invoke();
            }
        }

        public void OnStartSingleplayerGame()
        {
            Assert.IsTrue(mState == State.Idle);
            mState = State.PlayingSingleplayer;
            StartCoroutine(Utilities.FadeOutLoadScene("SingleplayerGame"));
        }

        public void OnSingleplayerGameEnd()
        {
            Assert.IsTrue(mState == State.PlayingSingleplayer);
            mState = State.Idle;
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
            GameInfo = message.Deserialize(new GameFoundPacket());
            StartCoroutine(Utilities.FadeOutLoadScene("MultiplayerGame"));
            if (mState == State.SearchingGame && OnSearchStopped != null)
            {
                OnSearchStopped.Invoke();
            }
            mState = State.PlayingMultiplayer;
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

        private IEnumerator QuerySearchStatus()
        {
            while (IsRunning)
            {
                if (mState == State.SearchingGame && MsfContext.Connection.IsConnected)
                {
                    MsfContext.Connection.Peer.SendMessage(
                        MessageHelper.Create((short) OperationCode.QuerySearchStatus),
                        (status, response) =>
                        {
                            if (status != ResponseStatus.Success && mState == State.SearchingGame)
                            {
                                mState = State.Idle;
                                if (OnSearchStopped != null)
                                {
                                    OnSearchStopped.Invoke();
                                }
                            }
                        });
                    yield return new WaitForSecondsRealtime(3);
                }
                else if (mState == State.SearchingGame)
                {
                    mState = State.Idle;
                    if (OnSearchStopped != null)
                    {
                        OnSearchStopped.Invoke();
                    }
                }
                yield return null;
            }
        }

        private enum State
        {
            Idle,
            SearchingGame,
            PlayingMultiplayer,
            PlayingSingleplayer,
        }
    }
}
