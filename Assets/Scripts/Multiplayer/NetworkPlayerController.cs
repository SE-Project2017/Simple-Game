using System.Collections.Generic;

using Assets.Scripts.App;
using Assets.Scripts.Msf;

using UnityEngine.Networking;

namespace Assets.Scripts.Multiplayer
{
    public class NetworkPlayerController : NetworkBehaviour
    {
        public ServerController.PlayerType Type { get; private set; }
        public string Username { get; private set; }

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
            MultiplayerGameController.Instance.Players.Add(this);
        }

        [Client]
        private void StartLocal()
        {
            Type = MultiplayerGameController.Instance.LocalPlayerType;
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
            CmdRegisterPlayer(new ServerController.PlayerInfo{Type = Type, Username = Username});
        }

        [Client]
        private void OnDestoryClient()
        {
            MultiplayerGameController.Instance.Players.Remove(this);
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

        [ClientRpc]
        public void RpcOnRegisterComplete(ServerController.GameInfo info)
        {
            if (isLocalPlayer)
            {
                MultiplayerGameController.Instance.OnGameStart(info);
            }
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
