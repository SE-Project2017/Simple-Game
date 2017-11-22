﻿using System.Collections.Generic;

using Assets.Scripts.Utils;

using Barebones.MasterServer;
using Barebones.Networking;

namespace Assets.Scripts.Msf.Modules.Auth
{
    public class AuthClient : MsfAuthClient
    {
        public AuthClient(IClientSocket connection) : base(connection) { }

        public void Register(string username, string password, SuccessCallback callback)
        {
            Register(
                new Dictionary<string, string>
                {
                    {AuthModule.Username, username},
                    {AuthModule.Password, password},
                    {AuthModule.Email, username}
                },
                callback);
        }

        public void Login(string username, string password, LoginCallback callback)
        {
            LogIn(
                new Dictionary<string, string>
                {
                    {AuthModule.Username, username},
                    {AuthModule.Password, password},
                    {AuthModule.Version, Utilities.VersionCode.ToString()}
                }, callback);
        }
    }
}
