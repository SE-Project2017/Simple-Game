using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Editor
{
    public class Build
    {
        private static string AndroidKeystorePath
        {
            get
            {
                var value = Environment.GetEnvironmentVariable(
                    "UNITY_ANDROID_KEYSTORE");
                return value ?? PlayerSettings.Android.keystoreName;
            }
        }

        private static string AndroidKeystorePassword
        {
            get
            {
                var value = Environment.GetEnvironmentVariable(
                    "UNITY_ANDROID_KEYSTORE_PASSWORD");
                return value ?? PlayerSettings.Android.keystorePass;
            }
        }

        private static string AndroidKeyalias
        {
            get
            {
                var value = Environment.GetEnvironmentVariable(
                    "UNITY_ANDROID_KEYALIAS");
                return value ?? PlayerSettings.Android.keyaliasName;
            }
        }

        private static string AndroidKeyaliasPassword
        {
            get
            {
                var value = Environment.GetEnvironmentVariable(
                    "UNITY_ANDROID_KEYALIAS_PASSWORD");
                return value ?? PlayerSettings.Android.keyaliasPass;
            }
        }

        [MenuItem("Build/Build Debug", false, 100)]
        public static void BuildDebug()
        {
            var state = Configure();
            InternalBuildDebugWindows();
            InternalBuildDebugLinux();
            InternalBuildDebugAndroid();
            InternalBuildDebugWebGL();
            CleanUp(state);
        }

        [MenuItem("Build/Build Debug Windows", false, 110)]
        public static void BuildDebugWindows()
        {
            var state = Configure();
            InternalBuildDebugWindows();
            CleanUp(state);
        }

        [MenuItem("Build/Build Debug Linux", false, 120)]
        public static void BuildDebugLinux()
        {
            var state = Configure();
            InternalBuildDebugLinux();
            CleanUp(state);
        }

        [MenuItem("Build/Build Debug Android", false, 130)]
        public static void BuildDebugAndroid()
        {
            var state = Configure();
            InternalBuildDebugAndroid();
            CleanUp(state);
        }

        [MenuItem("Build/Build Debug WebGL", false, 140)]
        public static void BuildDebugWebGL()
        {
            var state = Configure();
            InternalBuildDebugWebGL();
            CleanUp(state);
        }

        [MenuItem("Build/Build Release", false, 200)]
        public static void BuildRelease()
        {
            var state = Configure();
            InternalBuildReleaseWindows();
            InternalBuildReleaseLinux();
            InternalBuildReleaseAndroid();
            InternalBuildReleaseWebGL();
            CleanUp(state);
        }

        [MenuItem("Build/Build Release Windows", false, 210)]
        public static void BuildReleaseWindows()
        {
            var state = Configure();
            InternalBuildReleaseWindows();
            CleanUp(state);
        }

        [MenuItem("Build/Build Release Linux", false, 220)]
        public static void BuildReleaseLinux()
        {
            var state = Configure();
            InternalBuildReleaseLinux();
            CleanUp(state);
        }

        [MenuItem("Build/Build Release Android", false, 230)]
        public static void BuildReleaseAndroid()
        {
            var state = Configure();
            InternalBuildReleaseAndroid();
            CleanUp(state);
        }

        [MenuItem("Build/Build Release WebGL", false, 240)]
        public static void BuildReleaseWebGL()
        {
            var state = Configure();
            InternalBuildReleaseWebGL();
            CleanUp(state);
        }

        [MenuItem("Build/Build All", false, 400)]
        public static void BuildAll()
        {
            var state = Configure();
            InternalBuildReleaseAndroid();
            InternalBuildReleaseLinux();
            InternalBuildReleaseWindows();
            InternalBuildReleaseWebGL();
            InternalBuildDebugAndroid();
            InternalBuildDebugLinux();
            InternalBuildDebugWindows();
            InternalBuildDebugWebGL();
            CleanUp(state);
        }

        [MenuItem("Build/Enable Local Server", false, 500)]
        public static void EnableLocalServer()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone,
                "LOCAL_SERVER");
            EditorSceneManager.SaveOpenScenes();
        }

        [MenuItem("Build/Disable Local Server", false, 510)]
        public static void DisableLocalServer()
        {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(
                BuildTargetGroup.Standalone, "");
            EditorSceneManager.SaveOpenScenes();
        }

        public static void BuildSilently()
        {
            var state = Configure(false);
            InternalBuildReleaseAndroid();
            InternalBuildReleaseLinux();
            InternalBuildReleaseWindows();
            InternalBuildReleaseWebGL();
            InternalBuildDebugAndroid();
            InternalBuildDebugLinux();
            InternalBuildDebugWindows();
            InternalBuildDebugWebGL();
            CleanUp(state);
        }

        private static BuildState Configure(bool bumpVersion = true)
        {
            if (bumpVersion)
            {
                var version = BumpVersion();
                PlayerSettings.Android.bundleVersionCode = version.VersionCode;
                PlayerSettings.bundleVersion = version.VersionName;
            }

            var state = new BuildState
            {
                PrevAndroidKeystorePath = PlayerSettings.Android.keystoreName,
                PrevAndroidKeystorePassword = PlayerSettings.Android.keystorePass,
                PrevAndroidKeyalias = PlayerSettings.Android.keyaliasName,
                PrevAndroidKeyaliasPassword = PlayerSettings.Android.keyaliasPass,
                PrevAndroidUseCustomKeystore = PlayerSettings.Android.useCustomKeystore
            };
            PlayerSettings.Android.keystoreName = AndroidKeystorePath;
            PlayerSettings.Android.keystorePass = AndroidKeystorePassword;
            PlayerSettings.Android.keyaliasName = AndroidKeyalias;
            PlayerSettings.Android.keyaliasPass = AndroidKeyaliasPassword;
            PlayerSettings.Android.useCustomKeystore = true;

            PlayerSettings.runInBackground = true;
            EditorSceneManager.SaveOpenScenes();

            return state;
        }

        private static void CleanUp(BuildState state)
        {
            PlayerSettings.Android.useCustomKeystore = state.PrevAndroidUseCustomKeystore;
            PlayerSettings.Android.keystoreName = state.PrevAndroidKeystorePath;
            PlayerSettings.Android.keystorePass = state.PrevAndroidKeystorePassword;
            PlayerSettings.Android.keyaliasName = state.PrevAndroidKeyalias;
            PlayerSettings.Android.keyaliasPass = state.PrevAndroidKeyaliasPassword;
            EditorSceneManager.SaveOpenScenes();
        }

        private static VersionNumber BumpVersion()
        {
            const string path = "Assets/Scripts/Utils/Utilities.cs";
            var text = File.ReadAllText(path);
            Regex regex = new Regex(
                @"^\s*public\s+const\s+int\s+VersionCode\s*=\s*(\d+);$",
                RegexOptions.Multiline);
            var group = regex.Match(text).Groups[1];
            int versionCode = int.Parse(group.Value);
            ++versionCode;
            text = text.Remove(group.Index, group.Length);
            text = text.Insert(group.Index, versionCode.ToString());
            regex = new Regex(
                @"^\s*public\s+const\s+string\s+VersionName\s*=\s*\"".*\.(\d+)\"";$",
                RegexOptions.Multiline);
            group = regex.Match(text).Groups[1];
            text = text.Remove(group.Index, group.Length);
            text = text.Insert(group.Index, versionCode.ToString());
            File.WriteAllText(path, text);
            AssetDatabase.Refresh();
            regex = new Regex(
                @"^\s*public\s+const\s+string\s+VersionName\s*=\s*\""(.*)\"";$",
                RegexOptions.Multiline);
            return new VersionNumber
            {
                VersionCode = versionCode,
                VersionName = regex.Match(text).Groups[1].Value
            };
        }

        private static void InternalBuildDebugWindows()
        {
            FileUtil.DeleteFileOrDirectory(WindowsBuildPath(true));
            BuildMasterServer(
                WindowsBuildPath(true) + "master/MasterServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildSpawnerServer(
                WindowsBuildPath(true) + "spawner/SpawnerServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildGameServer(
                WindowsBuildPath(true) + "server/GameServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildClient(
                WindowsBuildPath(true) + "client/Client.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        private static void InternalBuildDebugLinux()
        {
            FileUtil.DeleteFileOrDirectory(LinuxBuildPath(true));
            BuildMasterServer(
                LinuxBuildPath(true) + "master/master",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildSpawnerServer(
                LinuxBuildPath(true) + "spawner/spawner",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildGameServer(
                LinuxBuildPath(true) + "server/server",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildClient(
                LinuxBuildPath(true) + "client/client",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        private static void InternalBuildDebugAndroid()
        {
            FileUtil.DeleteFileOrDirectory(AndroidBuildPath(true));
            BuildClient(
                AndroidBuildPath(true) + "client.apk",
                BuildTarget.Android,
                BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        private static void InternalBuildDebugWebGL()
        {
            FileUtil.DeleteFileOrDirectory(WebGLBuildPath(true));
            BuildClient(WebGLBuildPath(true) + "client", BuildTarget.WebGL,
                BuildOptions.Development);
        }

        private static void InternalBuildReleaseWindows()
        {
            FileUtil.DeleteFileOrDirectory(WindowsBuildPath(false));
            BuildMasterServer(
                WindowsBuildPath(false) + "master/MasterServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            BuildSpawnerServer(
                WindowsBuildPath(false) + "spawner/SpawnerServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            BuildGameServer(
                WindowsBuildPath(false) + "server/GameServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            BuildClient(
                WindowsBuildPath(false) + "client/Client.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
        }

        private static void InternalBuildReleaseLinux()
        {
            FileUtil.DeleteFileOrDirectory(LinuxBuildPath(false));
            BuildMasterServer(
                LinuxBuildPath(false) + "master/master",
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode);
            BuildSpawnerServer(
                LinuxBuildPath(false) + "spawner/spawner",
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode);
            BuildGameServer(
                LinuxBuildPath(false) + "server/server",
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode);
            BuildClient(
                LinuxBuildPath(false) + "client/client",
                BuildTarget.StandaloneLinux64,
                BuildOptions.None);
        }

        private static void InternalBuildReleaseAndroid()
        {
            FileUtil.DeleteFileOrDirectory(AndroidBuildPath(false));

            EditorUserBuildSettings.buildAppBundle = true;
            BuildClient(
                AndroidBuildPath(false) + "client.aab",
                BuildTarget.Android,
                BuildOptions.None);

            EditorUserBuildSettings.buildAppBundle = false;
            BuildClient(
                AndroidBuildPath(false) + "client.apk",
                BuildTarget.Android,
                BuildOptions.None);
        }

        private static void InternalBuildReleaseWebGL()
        {
            FileUtil.DeleteFileOrDirectory(WebGLBuildPath(false));
            BuildClient(WebGLBuildPath(false) + "client", BuildTarget.WebGL, BuildOptions.None);
        }

        private static void BuildMasterServer(string path, BuildTarget target,
            BuildOptions options)
        {
            PlayerSettings.productName = "TetrisMasterServer";
            string[] scenes =
            {
                SceneRoot() + "MasterServer.unity",
            };
            BuildPipeline.BuildPlayer(scenes, path, target, options);
        }

        private static void BuildSpawnerServer(string path, BuildTarget target,
            BuildOptions options)
        {
            PlayerSettings.productName = "TetrisSpawnerServer";
            string[] scenes =
            {
                SceneRoot() + "SpawnerServer.unity",
            };
            BuildPipeline.BuildPlayer(scenes, path, target, options);
        }

        private static void BuildGameServer(string path, BuildTarget target,
            BuildOptions options)
        {
            PlayerSettings.productName = "TetrisGameServer";
            string[] scenes =
            {
                SceneRoot() + "GameServer.unity",
            };
            BuildPipeline.BuildPlayer(scenes, path, target, options);
        }

        private static void BuildClient(string path, BuildTarget target,
            BuildOptions options)
        {
            PlayerSettings.productName = "Tetris";
            string[] scenes =
            {
                SceneRoot() + "Login.unity",
                SceneRoot() + "MainMenu.unity",
                SceneRoot() + "MultiplayerGame.unity",
                SceneRoot() + "SingleplayerGame.unity",
            };
            BuildPipeline.BuildPlayer(scenes, path, target, options);
        }

        private static string WindowsBuildPath(bool isDebug)
        {
            return BaseBuildPath(isDebug) + "Windows/";
        }

        private static string LinuxBuildPath(bool isDebug)
        {
            return BaseBuildPath(isDebug) + "Linux/";
        }

        private static string AndroidBuildPath(bool isDebug)
        {
            return BaseBuildPath(isDebug) + "Android/";
        }

        private static string WebGLBuildPath(bool isDebug)
        {
            return BaseBuildPath(isDebug) + "WebGL/";
        }

        private static string BaseBuildPath(bool isDebug)
        {
            return "Build/" + (isDebug ? "Debug/" : "Release/");
        }

        private static string SceneRoot()
        {
            return "Assets/Scenes/";
        }

        private struct VersionNumber
        {
            public int VersionCode;
            public string VersionName;
        }

        private struct BuildState
        {
            public string PrevAndroidKeystorePath;
            public string PrevAndroidKeystorePassword;
            public string PrevAndroidKeyalias;
            public string PrevAndroidKeyaliasPassword;
            public bool PrevAndroidUseCustomKeystore;
        }
    }
}
