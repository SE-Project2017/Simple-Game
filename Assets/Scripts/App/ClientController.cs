using Assets.Scripts.Msf;
using Assets.Scripts.Multiplayer;
using Assets.Scripts.Utils;

using Barebones.Networking;

using UnityEngine.Assertions;

namespace Assets.Scripts.App
{
    public class ClientController : Singleton<ClientController>
    {
        public GameFoundPacket GameInfo;

        private State mState = State.Idle;

        public void Start()
        {
            MsfContext.Connection.SetHandler((short) OperationCode.GameFound, OnGameFound);
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

        private enum State
        {
            Idle,
            SearchingGame,
            PlayingMultiplayer,
        }
    }
}
