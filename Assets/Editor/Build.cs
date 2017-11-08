using UnityEditor;

namespace Assets.Editor
{
    public class Build
    {
        [MenuItem("Build/Build Debug", false, 100)]
        public static void BuildDebug()
        {
            ApplySettings();
            InternalBuildDebugWindows();
            InternalBuildDebugLinux();
        }

        [MenuItem("Build/Build Debug Windows", false, 110)]
        public static void BuildDebugWindows()
        {
            ApplySettings();
            InternalBuildDebugWindows();
        }

        [MenuItem("Build/Build Debug Linux", false, 120)]
        public static void BuildDebugLinux()
        {
            ApplySettings();
            InternalBuildDebugLinux();
        }

        [MenuItem("Build/Build Release", false, 200)]
        public static void BuildRelease()
        {
            ApplySettings();
            InternalBuildReleaseWindows();
            InternalBuildReleaseLinux();
        }

        [MenuItem("Build/Build Release Windows", false, 210)]
        public static void BuildReleaseWindows()
        {
            ApplySettings();
            InternalBuildReleaseWindows();
        }

        [MenuItem("Build/Build Release Linux", false, 220)]
        public static void BuildReleaseLinux()
        {
            ApplySettings();
            InternalBuildReleaseLinux();
        }

        [MenuItem("Build/Build Android", false, 300)]
        public static void BuildAndroid()
        {
            ApplySettings();
            FileUtil.DeleteFileOrDirectory(AndroidBuildPath(false));
            BuildClient(
                AndroidBuildPath(false) + "client.apk",
                BuildTarget.Android,
                BuildOptions.None);
        }

        private static void ApplySettings()
        {
            PlayerSettings.runInBackground = true;
            PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.HiddenByDefault;
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

        private static void BuildMasterServer(string path, BuildTarget target, BuildOptions options)
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

        private static void BuildGameServer(string path, BuildTarget target, BuildOptions options)
        {
            PlayerSettings.productName = "TetrisGameServer";
            string[] scenes =
            {
                SceneRoot() + "GameServer.unity",
            };
            BuildPipeline.BuildPlayer(scenes, path, target, options);
        }

        private static void BuildClient(string path, BuildTarget target, BuildOptions options)
        {
            PlayerSettings.productName = "Tetris";
            string[] scenes =
            {
                SceneRoot() + "Login.unity",
                SceneRoot() + "MainMenu.unity",
                SceneRoot() + "MultiplayerGame.unity",
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
    }
}
