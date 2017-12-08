using System;
using System.Collections;

using App;

using Barebones.MasterServer;
using Barebones.Networking;

using Singleplayer.Packets;

using UnityEngine;

namespace MsfWrapper.Modules.Profile
{
    public class ProfileModule : ProfilesModule
    {
        public override void Initialize(IServer server)
        {
            base.Initialize(server);
            server.SetHandler((short) OperationCode.UploadSingleplayerResult,
                OnUploadSingleplayerResult);
        }

        private void OnUploadSingleplayerResult(IIncommingMessage message)
        {
            int peerId = message.Peer.Id;
            var peer = Server.GetPeer(peerId);
            if (peer == null)
            {
                message.Respond("Peer not valid.", ResponseStatus.NotConnected);
                return;
            }

            var user = peer.GetExtension<IUserExtension>();
            if (user == null)
            {
                message.Respond("User not logged in.", ResponseStatus.Unauthorized);
                return;
            }

            var username = user.Username;
            var packet = message.Deserialize(new SingleplayerResultPacket());
            StartCoroutine(SaveSingleplayerResult(username, packet));
        }

        private IEnumerator SaveSingleplayerResult(string username, SingleplayerResultPacket packet)
        {
            while (!MsfContext.Connection.IsConnected)
            {
                yield return null;
            }

            var profile = new ObservableServerProfile(username)
            {
                new ObservableInt(ProfileKey.SingleplayerGamesPlayed),
                new ObservableInt(ProfileKey.SingleplayerBestGrade),
            };
            MsfContext.Server.Profiles.FillProfileValues(profile, (successful, error) =>
            {
                if (successful)
                {
                    profile.GetProperty<ObservableInt>(ProfileKey.SingleplayerGamesPlayed).Add(1);

                    var bestGrade =
                        profile.GetProperty<ObservableInt>(ProfileKey.SingleplayerBestGrade);
                    bestGrade.Set(Math.Max(bestGrade.Value, packet.Grade));
                }
                else
                {
                    Debug.Log(error);
                    StartCoroutine(SaveSingleplayerResult(username, packet));
                }
            });
        }
    }
}
