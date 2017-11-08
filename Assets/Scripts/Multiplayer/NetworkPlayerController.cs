using System.Collections.Generic;
using System.Linq;

using Assets.Scripts.App;
using Assets.Scripts.Msf;

using UnityEngine.Assertions;
using UnityEngine.Networking;

namespace Assets.Scripts.Multiplayer
{
    public class NetworkPlayerController : NetworkBehaviour
    {
        public ServerController.PlayerType Type { get; private set; }
        public string Username { get; private set; }

        private const int MaxFrameDiff = 30;

        private bool mConnected;
        private int mFrameCount;
        private MultiplayerGameController mGameController;
        private readonly List<PlayerEvent> mPlayerEvents = new List<PlayerEvent>();

        public void Start()
        {
            if (isClient)
            {
                StartClient();
            }
            if (isLocalPlayer)
            {
                StartLocal();
            }
        }

        public void FixedUpdate()
        {
            if (isLocalPlayer)
            {
                FixedUpdateLocal();
            }
        }

        public void OnDestroy()
        {
            if (isClient)
            {
                OnDestoryClient();
            }
        }

        [Client]
        private void StartClient()
        {
            mGameController = MultiplayerGameController.Instance;
            mGameController.Players.Add(this);
        }

        [Client]
        private void StartLocal()
        {
            Type = mGameController.LocalPlayerType;
            Username = MsfContext.Client.Auth.AccountInfo.Username;
            var inputController = gameObject.AddComponent<InputController>();
            inputController.ButtonDown += button =>
                mPlayerEvents.Add(new PlayerEvent
                {
                    Type = PlayerEvent.EventType.ButtonDown,
                    Data = (int) button,
                });
            inputController.ButtonUp += button =>
                mPlayerEvents.Add(new PlayerEvent
                {
                    Type = PlayerEvent.EventType.ButtonUp,
                    Data = (int) button,
                });
            CmdRegisterPlayer(new ServerController.PlayerInfo {Type = Type, Username = Username});
        }

        [Client]
        private void FixedUpdateLocal()
        {
            if (mConnected)
            {
                if (mFrameCount - mGameController.Players.Min(player => player.mFrameCount) >
                    MaxFrameDiff)
                {
                    return;
                }
                ++mFrameCount;
                CmdUpdateFrame(mFrameCount, mPlayerEvents.ToArray());
                mGameController.OnLocalUpdateFrame(mPlayerEvents);
            }
            mPlayerEvents.Clear();
        }

        [Client]
        private void OnDestoryClient()
        {
            mGameController.Players.Remove(this);
        }

        [Command]
        private void CmdRegisterPlayer(ServerController.PlayerInfo info)
        {
            Type = info.Type;
            Username = info.Username;
            RpcSetPlayerType(Type);
            RpcSetUsername(Username);
            ServerController.Instance.RegisterPlayer(this, info);
        }

        [Command]
        private void CmdUpdateFrame(int frameCount, PlayerEvent[] events)
        {
            RpcOnFrameUpdate(frameCount, events);
        }

        [ClientRpc]
        public void RpcOnRegisterComplete(ServerController.GameInfo info)
        {
            if (isLocalPlayer)
            {
                mGameController.OnGameStart(info);
            }
            mConnected = true;
        }

        [ClientRpc]
        private void RpcSetPlayerType(ServerController.PlayerType type)
        {
            Type = type;
        }

        [ClientRpc]
        private void RpcSetUsername(string username)
        {
            Username = username;
        }

        [ClientRpc]
        private void RpcOnFrameUpdate(int frameCount, PlayerEvent[] events)
        {
            if (isLocalPlayer)
            {
                return;
            }
            Assert.IsTrue(frameCount == mFrameCount + 1);
            mFrameCount = frameCount;
            mGameController.OnRemoteUpdateFrame(events);
        }

        public struct PlayerEvent
        {
            public EventType Type;
            public int Data;

            public enum EventType
            {
                ButtonDown,
                ButtonUp,
            }
        }
    }
}
