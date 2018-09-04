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
            Configure();
            InternalBuildDebugWindows();
            InternalBuildDebugLinux();
            InternalBuildDebugAndroid();
            CleanUp();
        }

        [MenuItem("Build/Build Debug Windows", false, 110)]
        public static void BuildDebugWindows()
        {
            Configure();
            InternalBuildDebugWindows();
            CleanUp();
        }

        [MenuItem("Build/Build Debug Linux", false, 120)]
        public static void BuildDebugLinux()
        {
            Configure();
            InternalBuildDebugLinux();
            CleanUp();
        }

        [MenuItem("Build/Build Debug Android", false, 130)]
        public static void BuildDebugAndroid()
        {
            Configure();
            InternalBuildDebugAndroid();
            CleanUp();
        }

        [MenuItem("Build/Build Release", false, 200)]
        public static void BuildRelease()
        {
            Configure();
            InternalBuildReleaseWindows();
            InternalBuildReleaseLinux();
            InternalBuildReleaseAndroid();
            CleanUp();
        }

        [MenuItem("Build/Build Release Windows", false, 210)]
        public static void BuildReleaseWindows()
        {
            Configure();
            InternalBuildReleaseWindows();
            CleanUp();
        }

        [MenuItem("Build/Build Release Linux", false, 220)]
        public static void BuildReleaseLinux()
        {
            Configure();
            InternalBuildReleaseLinux();
            CleanUp();
        }

        [MenuItem("Build/Build Release Android", false, 230)]
        public static void BuildReleaseAndroid()
        {
            Configure();
            InternalBuildReleaseAndroid();
            CleanUp();
        }

        [MenuItem("Build/Build All", false, 400)]
        public static void BuildAll()
        {
            Configure();
            InternalBuildReleaseAndroid();
            InternalBuildReleaseLinux();
            InternalBuildReleaseWindows();
            InternalBuildDebugAndroid();
            InternalBuildDebugLinux();
            InternalBuildDebugWindows();
            CleanUp();
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
            Configure(false);
            InternalBuildReleaseAndroid();
            InternalBuildReleaseLinux();
            InternalBuildReleaseWindows();
            InternalBuildDebugAndroid();
            InternalBuildDebugLinux();
            InternalBuildDebugWindows();
            CleanUp();
        }

        private static void Configure(bool bumpVersion = true)
        {
            if (bumpVersion)
            {
                var version = BumpVersion();
                PlayerSettings.Android.bundleVersionCode = version.VersionCode;
                PlayerSettings.bundleVersion = version.VersionName;
            }
            PlayerSettings.Android.keystoreName = AndroidKeystorePath;
            PlayerSettings.Android.keystorePass = AndroidKeystorePassword;
            PlayerSettings.Android.keyaliasName = AndroidKeyalias;
            PlayerSettings.Android.keyaliasPass = AndroidKeyaliasPassword;
            PlayerSettings.runInBackground = true;
            PlayerSettings.displayResolutionDialog =
                ResolutionDialogSetting.HiddenByDefault;
            EditorSceneManager.SaveOpenScenes();
        }

        private static void CleanUp()
        {
            PlayerSettings.Android.keystoreName = string.Empty;
            PlayerSettings.Android.keystorePass = string.Empty;
            PlayerSettings.Android.keyaliasName = string.Empty;
            PlayerSettings.Android.keyaliasPass = string.Empty;
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
                WindowsBuildPath(true) + "MasterServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildSpawnerServer(
                WindowsBuildPath(true) + "SpawnerServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildGameServer(
                WindowsBuildPath(true) + "GameServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildClient(
                WindowsBuildPath(true) + "Client.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        private static void InternalBuildDebugLinux()
        {
            FileUtil.DeleteFileOrDirectory(LinuxBuildPath(true));
            BuildMasterServer(
                LinuxBuildPath(true) + "master",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildSpawnerServer(
                LinuxBuildPath(true) + "spawner",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildGameServer(
                LinuxBuildPath(true) + "server",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildClient(
                LinuxBuildPath(true) + "client",
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

        private static void InternalBuildReleaseWindows()
        {
            FileUtil.DeleteFileOrDirectory(WindowsBuildPath(false));
            BuildMasterServer(
                WindowsBuildPath(false) + "MasterServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            BuildSpawnerServer(
                WindowsBuildPath(false) + "SpawnerServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            BuildGameServer(
                WindowsBuildPath(false) + "GameServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
            BuildClient(
                WindowsBuildPath(false) + "Client.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.None);
        }

        private static void InternalBuildReleaseLinux()
        {
            FileUtil.DeleteFileOrDirectory(LinuxBuildPath(false));
            BuildMasterServer(
                LinuxBuildPath(false) + "master",
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode);
            BuildSpawnerServer(
                LinuxBuildPath(false) + "spawner",
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode);
            BuildGameServer(
                LinuxBuildPath(false) + "server",
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode);
            BuildClient(
                LinuxBuildPath(false) + "client",
                BuildTarget.StandaloneLinux64,
                BuildOptions.None);
        }

        private static void InternalBuildReleaseAndroid()
        {
            FileUtil.DeleteFileOrDirectory(AndroidBuildPath(false));
            BuildClient(
                AndroidBuildPath(false) + "client.apk",
                BuildTarget.Android,
                BuildOptions.None);
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
    }
}
