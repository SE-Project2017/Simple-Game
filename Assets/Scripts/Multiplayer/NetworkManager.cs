using System;
using System.Collections;
using System.Collections.Generic;

using App;

using Barebones.MasterServer;
using Barebones.Networking;

using Multiplayer.Packets;

using UnityEngine;
using UnityEngine.Serialization;

using Utils;

namespace Multiplayer
{
    public class NetworkManager : Singleton<NetworkManager>
    {
        public bool IsClientConnected => mClientSocket != null && mClientSocket.IsConnected;

        public int RttMs { get; private set; }

        [FormerlySerializedAs("m_PlayerPrefab")]
        [SerializeField]
        private GameObject mPlayerPrefab = null;

        private const float PingIntervalSeconds = 1.0f;

        private ServerController mServerController;
        private ClientController mClientController;

        private IServerSocket mServerSocket;
        private readonly Dictionary<int, NetworkPlayer> mServerPlayersByPeerId =
            new Dictionary<int, NetworkPlayer>();
        private readonly HashSet<ServerController.PlayerType> mRegisteredPlayerTypes =
            new HashSet<ServerController.PlayerType>();

        private IClientSocket mClientSocket;
        private NetworkPlayer mLocalClientPlayer;
        private Coroutine mPingCoroutine;

        public override void Awake()
        {
            base.Awake();
            Application.runInBackground = true;
        }

        public override void OnDestroy()
        {
            if (Instance == this)
            {
                StopClient();
                StopServer();
            }
            base.OnDestroy();
        }

        public void OnApplicationQuit()
        {
            StopClient();
            StopServer();
        }

        public void StartServer(int port)
        {
            StopServer();

            mServerController = ServerController.Instance;
            mServerSocket = Msf.Create.ServerSocket();
            mServerSocket.Connected += OnServerPeerConnected;
            mServerSocket.Disconnected += OnServerPeerDisconnected;
            mServerSocket.Listen(port);
        }

        public void StopServer()
        {
            if (mServerSocket != null)
            {
                mServerSocket.Connected -= OnServerPeerConnected;
                mServerSocket.Disconnected -= OnServerPeerDisconnected;
                mServerSocket.Stop();
                mServerSocket = null;
            }

            foreach (var player in mServerPlayersByPeerId.Values)
            {
                if (player != null)
                {
                    Destroy(player.gameObject);
                }
            }
            mServerPlayersByPeerId.Clear();
            mRegisteredPlayerTypes.Clear();
        }

        public void RelayFrameUpdate(NetworkPlayer sender, FrameUpdatePacket packet)
        {
            foreach (var player in mServerPlayersByPeerId.Values)
            {
                if (player != null && player != sender)
                {
                    player.SendFrameUpdateToClient(packet);
                }
            }
        }

        public void StartClient(string address, int port)
        {
            StopClient();

            mClientController = ClientController.Instance;
            mClientSocket = Msf.Advanced.ClientSocketFactory();
            mClientSocket.SetHandler((short) GameOperationCode.RegisterComplete,
                OnClientRegisterComplete);
            mClientSocket.SetHandler((short) GameOperationCode.FrameUpdate,
                OnClientFrameUpdate);
            mClientSocket.SetHandler((short) GameOperationCode.GameResult,
                OnClientGameResult);
            mClientSocket.Connected += OnClientSocketConnected;
            mClientSocket.Disconnected += OnClientSocketDisconnected;

            Uri serverUri = BuildServerUri(address, port);
            Debug.Log("Connecting to game server at: " + serverUri);
            mClientSocket.Connect(serverUri);
        }

        public void StopClient()
        {
            if (mPingCoroutine != null)
            {
                StopCoroutine(mPingCoroutine);
                mPingCoroutine = null;
            }

            if (mClientSocket != null)
            {
                mClientSocket.Connected -= OnClientSocketConnected;
                mClientSocket.Disconnected -= OnClientSocketDisconnected;
                if (mClientSocket.IsConnected)
                {
                    mClientSocket.Disconnect();
                }
                mClientSocket = null;
            }

            if (mLocalClientPlayer != null)
            {
                Destroy(mLocalClientPlayer.gameObject);
                mLocalClientPlayer = null;
            }

            RttMs = 0;
        }

        private static Uri BuildServerUri(string address, int port)
        {
#if LOCAL_SERVER
            return new Uri(string.Format("ws://{0}:{1}/w", address, port));
#else
            // .dev domains enforce HTTPS (HSTS) and Cloudflare only proxies WebSockets
            // on standard HTTPS ports, so production match servers are routed via
            // Nginx path rules (/game/<port> -> 127.0.0.1:<port>/w) over wss://:443.
            return new Uri(string.Format("wss://{0}/game/{1}", address, port));
#endif
        }

        private void OnServerPeerConnected(IPeer peer)
        {
            peer.MessageReceived += OnServerPeerMessage;
        }

        private void OnServerPeerDisconnected(IPeer peer)
        {
            peer.MessageReceived -= OnServerPeerMessage;

            NetworkPlayer player;
            if (mServerPlayersByPeerId.TryGetValue(peer.Id, out player))
            {
                mServerPlayersByPeerId.Remove(peer.Id);
                if (player != null)
                {
                    player.OnServerPeerDisconnected();
                    Destroy(player.gameObject);
                }
            }
        }

        private void OnServerPeerMessage(IIncommingMessage message)
        {
            var opCode = (GameOperationCode) message.OpCode;
            switch (opCode)
            {
                case GameOperationCode.Ping:
                    message.Respond(ResponseStatus.Success);
                    break;
                case GameOperationCode.JoinGame:
                    OnServerJoinGame(message);
                    break;
                case GameOperationCode.FrameUpdate:
                {
                    NetworkPlayer player;
                    if (mServerPlayersByPeerId.TryGetValue(message.Peer.Id, out player))
                    {
                        var packet = message.Deserialize(new FrameUpdatePacket());
                        player.OnServerFrameUpdate(packet);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Ignoring FrameUpdate from unregistered peer {message.Peer.Id}.");
                    }
                    break;
                }
                case GameOperationCode.PlayerEnded:
                {
                    NetworkPlayer player;
                    if (mServerPlayersByPeerId.TryGetValue(message.Peer.Id, out player))
                    {
                        var packet = message.Deserialize(new PlayerEndedPacket());
                        player.OnServerPlayerEnded(packet.Result);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"Ignoring PlayerEnded from unregistered peer {message.Peer.Id}.");
                    }
                    break;
                }
                default:
                    Debug.LogWarning(
                        $"Unhandled game operation code {message.OpCode} from peer {message.Peer.Id}.");
                    break;
            }
        }

        private void OnServerJoinGame(IIncommingMessage message)
        {
            if (mServerPlayersByPeerId.ContainsKey(message.Peer.Id))
            {
                Debug.LogWarning(
                    $"Ignoring duplicate JoinGame from registered peer {message.Peer.Id}.");
                return;
            }

            var packet = message.Deserialize(new JoinGamePacket());
            ServerController.PlayerType playerType;
            if (packet.Token == mServerController.PlayerAToken)
            {
                playerType = ServerController.PlayerType.PlayerA;
            }
            else if (packet.Token == mServerController.PlayerBToken)
            {
                playerType = ServerController.PlayerType.PlayerB;
            }
            else
            {
                Debug.LogWarning(
                    $"Rejected JoinGame with invalid token from peer {message.Peer.Id}.");
                return;
            }

            if (!mRegisteredPlayerTypes.Add(playerType))
            {
                Debug.LogWarning(
                    $"Rejected duplicate JoinGame for {playerType} from peer {message.Peer.Id}.");
                return;
            }

            var playerObj = Instantiate(mPlayerPrefab);
            var player = playerObj.GetComponent<NetworkPlayer>();
            player.InitializeServer(message.Peer);
            mServerPlayersByPeerId[message.Peer.Id] = player;
            mServerController.RegisterPlayer(player, playerType);
        }

        private void OnClientSocketConnected()
        {
            var playerObj = Instantiate(mPlayerPrefab);
            mLocalClientPlayer = playerObj.GetComponent<NetworkPlayer>();
            mLocalClientPlayer.InitializeLocalClient(mClientSocket);

            mClientSocket.SendMessage(Msf.Create.Message(
                (short) GameOperationCode.JoinGame,
                new JoinGamePacket
                {
                    Token = mClientController.GameInfo.Token,
                }));

            mPingCoroutine = StartCoroutine(UpdateRttCoroutine());
        }

        private void OnClientSocketDisconnected()
        {
            var player = mLocalClientPlayer;
            mLocalClientPlayer = null;

            StopClient();

            if (player != null)
            {
                player.OnClientSocketDisconnected();
                Destroy(player.gameObject);
            }
        }

        private void OnClientRegisterComplete(IIncommingMessage message)
        {
            if (mLocalClientPlayer == null)
            {
                Debug.LogWarning(
                    "Ignoring RegisterComplete because local client player is null.");
                return;
            }
            var packet = message.Deserialize(new RegisterCompletePacket());
            mLocalClientPlayer.OnClientRegisterComplete(packet);
        }

        private void OnClientFrameUpdate(IIncommingMessage message)
        {
            if (mLocalClientPlayer == null)
            {
                Debug.LogWarning(
                    "Ignoring FrameUpdate because local client player is null.");
                return;
            }
            var packet = message.Deserialize(new FrameUpdatePacket());
            mLocalClientPlayer.OnClientRemoteFrameUpdate(packet);
        }

        private void OnClientGameResult(IIncommingMessage message)
        {
            if (mLocalClientPlayer == null)
            {
                Debug.LogWarning(
                    "Ignoring GameResult because local client player is null.");
                return;
            }
            var packet = message.Deserialize(new GameResultPacket());
            mLocalClientPlayer.OnClientGameResult(packet.Outcome);
        }

        private IEnumerator UpdateRttCoroutine()
        {
            while (IsClientConnected)
            {
                float startTime = Time.realtimeSinceStartup;
                bool done = false;
                mClientSocket.SendMessage((short) GameOperationCode.Ping, (status, response) =>
                {
                    if (status == ResponseStatus.Success)
                    {
                        RttMs = Mathf.RoundToInt((Time.realtimeSinceStartup - startTime) * 1000f);
                    }
                    done = true;
                });

                float elapsed = 0f;
                while (IsClientConnected && (!done || elapsed < PingIntervalSeconds))
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
        }
    }
}
