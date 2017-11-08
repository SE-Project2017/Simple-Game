using UnityEditor;

namespace Assets.Test.Editor
{
    public class Build
    {
        [MenuItem("Build/Build Debug", false, 100)]
        public static void BuildDebug()
        {
            PlayerSettings.runInBackground = true;
            PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.HiddenByDefault;
            FileUtil.DeleteFileOrDirectory(GetWindowsBuildPath(true));
            FileUtil.DeleteFileOrDirectory(GetLinuxBuildPath(true));

            BuildMasterServer(
                GetWindowsBuildPath(true) + "master.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildClient(
                GetWindowsBuildPath(true) + "client.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildSpawner(
                GetWindowsBuildPath(true) + "spawner.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildServer(
                GetWindowsBuildPath(true) + "server.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);

            BuildMasterServer(
                GetLinuxBuildPath(true) + "master",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildClient(
                GetLinuxBuildPath(true) + "client",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildSpawner(
                GetLinuxBuildPath(true) + "spawner",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildServer(
                GetLinuxBuildPath(true) + "server",
                BuildTarget.StandaloneLinux64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        [MenuItem("Build/Build Release", false, 101)]
        public static void BuildRelease()
        {
            PlayerSettings.runInBackground = true;
            PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.HiddenByDefault;
            FileUtil.DeleteFileOrDirectory(GetWindowsBuildPath(false));
            FileUtil.DeleteFileOrDirectory(GetLinuxBuildPath(false));

            BuildMasterServer(
                GetWindowsBuildPath(false) + "master.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.EnableHeadlessMode);
            BuildClient(
                GetWindowsBuildPath(false) + "client.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.EnableHeadlessMode);
            BuildSpawner(
                GetWindowsBuildPath(false) + "spawner.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.EnableHeadlessMode);
            BuildServer(
                GetWindowsBuildPath(false) + "server.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.EnableHeadlessMode);

            BuildMasterServer(
                GetLinuxBuildPath(false) + "master",
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode);
            BuildClient(
                GetLinuxBuildPath(false) + "client",
                BuildTarget.StandaloneLinux64,
                BuildOptions.None);
            BuildSpawner(
                GetLinuxBuildPath(false) + "spawner",
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode);
            BuildServer(
                GetLinuxBuildPath(false) + "server",
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode);
        }

        [MenuItem("Build/Build Android", false, 110)]
        public static void BuildAndroid()
        {
            PlayerSettings.runInBackground = true;
            FileUtil.DeleteFileOrDirectory(GetAndroidBuildPath(false));
            BuildClient(GetAndroidBuildPath(false) + "client.apk", BuildTarget.Android,
                BuildOptions.None);
        }

        private static void BuildMasterServer(string path, BuildTarget target, BuildOptions options)
        {
            string[] scenes =
            {
                GetScenesRoot() + "TestMasterServer.unity",
            };
            BuildPipeline.BuildPlayer(scenes, path, target, options);
        }

        private static void BuildClient(string path, BuildTarget target, BuildOptions options)
        {
            string[] scenes =
            {
                GetScenesRoot() + "TestClient.unity",
                GetScenesRoot() + "ClientMenu.unity",
                GetScenesRoot() + "TestGame.unity",
            };
            BuildPipeline.BuildPlayer(scenes, path, target, options);
        }

        private static void BuildSpawner(string path, BuildTarget target, BuildOptions options)
        {
            string[] scenes =
            {
                GetScenesRoot() + "TestSpawner.unity",
            };
            BuildPipeline.BuildPlayer(scenes, path, target, options);
        }

        private static void BuildServer(string path, BuildTarget target, BuildOptions options)
        {
            string[] scenes =
            {
                GetScenesRoot() + "TestServer.unity",
                GetScenesRoot() + "TestGame.unity",
            };
            BuildPipeline.BuildPlayer(scenes, path, target, options);
        }

        private static string GetBaseBuildPath(bool isDebug)
        {
            return "build/" + (isDebug ? "debug/" : "release/");
        }

        private static string GetWindowsBuildPath(bool isDebug)
        {
            return GetBaseBuildPath(isDebug) + "windows/";
        }

        private static string GetLinuxBuildPath(bool isDebug)
        {
            return GetBaseBuildPath(isDebug) + "linux/";
        }

        private static string GetAndroidBuildPath(bool isDebug)
        {
            return GetBaseBuildPath(isDebug) + "android/";
        }

        private static string GetScenesRoot()
        {
            return "Assets/Test/Scenes/";
        }
    }
}
