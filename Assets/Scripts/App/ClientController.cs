using System;
using System.Collections;

using Barebones.MasterServer;
using Barebones.Networking;

using MsfWrapper;

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

        public int MultiplayerWins
        {
            get { return mMultiplayerWins; }
            private set
            {
                mMultiplayerWins = value;
                if (OnMultiplayerWinsChange != null)
                {
                    OnMultiplayerWinsChange.Invoke(value);
                }
            }
        }

        public int MultiplayerLosses
        {
            get { return mMultiplayerLosses; }
            private set
            {
                mMultiplayerLosses = value;
                if (OnMultiplayerLossesChange != null)
                {
                    OnMultiplayerLossesChange.Invoke(value);
                }
            }
        }

        public int MultiplayerGamesPlayed
        {
            get { return mMultiplayerGamesPlayed; }
            private set
            {
                mMultiplayerGamesPlayed = value;
                if (OnMultiplayerGamesPlayedChange != null)
                {
                    OnMultiplayerGamesPlayedChange.Invoke(value);
                }
            }
        }

        public int SingleplayerGamesPlayed
        {
            get { return mSingleplayerGamesPlayed; }
            private set
            {
                mSingleplayerGamesPlayed = value;
                if (OnSingleplayerGameCountChange != null)
                {
                    OnSingleplayerGameCountChange.Invoke(value);
                }
            }
        }

        public bool IsOfflineMode
        {
            get { return mIsOfflineMode; }
            set
            {
                mIsOfflineMode = value;
                if (!value)
                {
                    MainMenuTab = MainMenuUI.Tab.Single;
                }
            }
        }

        public MainMenuUI.Tab MainMenuTab = MainMenuUI.Tab.Versus;
        public GameFoundPacket GameInfo;

        public event Action<string> OnPlayerNameChange;

        public event Action<int> OnMultiplayerWinsChange;
        public event Action<int> OnMultiplayerLossesChange;
        public event Action<int> OnMultiplayerGamesPlayedChange;

        public event Action<int> OnSingleplayerGameCountChange;

        public event Action OnSearchStarted;
        public event Action OnSearchStopped;

        private string mUsername;
        private string mPassword;
        private State mState = State.Idle;

        private string mPlayerName;

        private int mMultiplayerWins;
        private int mMultiplayerLosses;
        private int mMultiplayerGamesPlayed;

        private int mSingleplayerGamesPlayed;

        private bool mIsOfflineMode;

        public void Start()
        {
            MsfContext.Connection.SetHandler((short) OperationCode.GameFound, OnGameFound);
            MsfContext.Connection.Disconnected += () => { StartCoroutine(OnDisconnected()); };
            MsfContext.Client.Auth.LoggedIn += () =>
            {
                var profile = new ObservableProfile
                {
                    new ObservableString(ProfileKey.Name),
                    new ObservableInt(ProfileKey.MultiplayerWins),
                    new ObservableInt(ProfileKey.MultiplayerLosses),
                    new ObservableInt(ProfileKey.MultiplayerGamesPlayed),
                    new ObservableInt(ProfileKey.SingleplayerGamesPlayed),
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

        public void OnCancelSearch()
        {
            Assert.IsTrue(mState == State.SearchingGame);
            mState = State.Idle;
            MsfContext.Connection.Peer.SendMessage(
                MessageHelper.Create((short) OperationCode.CancelSearch));
            if (OnSearchStopped != null)
            {
                OnSearchStopped.Invoke();
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
            if (IsOfflineMode)
            {
                yield break;
            }
            while (SceneManager.GetActiveScene().name == "MultiplayerGame" ||
                SceneManager.GetActiveScene().name == "SingleplayerGame")
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

                var multiplayerWinsProp =
                    profile.GetProperty<ObservableInt>(ProfileKey.MultiplayerWins);
                MultiplayerWins = multiplayerWinsProp.Value;
                multiplayerWinsProp.OnDirty +=
                    property => MultiplayerWins = multiplayerWinsProp.Value;

                var multiplayerLossesProp =
                    profile.GetProperty<ObservableInt>(ProfileKey.MultiplayerLosses);
                MultiplayerLosses = multiplayerLossesProp.Value;
                multiplayerLossesProp.OnDirty += property =>
                    MultiplayerLosses = multiplayerLossesProp.Value;

                var multiplayerGamesPlayedProp =
                    profile.GetProperty<ObservableInt>(ProfileKey.MultiplayerGamesPlayed);
                MultiplayerGamesPlayed = multiplayerGamesPlayedProp.Value;
                multiplayerGamesPlayedProp.OnDirty += property =>
                    MultiplayerGamesPlayed = multiplayerGamesPlayedProp.Value;

                var singleplayerGamesPlayedProp =
                    profile.GetProperty<ObservableInt>(ProfileKey.SingleplayerGamesPlayed);
                SingleplayerGamesPlayed = singleplayerGamesPlayedProp.Value;
                singleplayerGamesPlayedProp.OnDirty += property =>
                    SingleplayerGamesPlayed = singleplayerGamesPlayedProp.Value;
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
