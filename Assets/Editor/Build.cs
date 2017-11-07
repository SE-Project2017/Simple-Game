using UnityEditor;

namespace Assets.Editor
{
    public class Build
    {
        [MenuItem("Build/Build Debug", false, 100)]
        public static void BuildDebug()
        {
            PlayerSettings.runInBackground = true;
            PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;
            FileUtil.DeleteFileOrDirectory(WindowsBuildPath(true));
            FileUtil.DeleteFileOrDirectory(LinuxBuildPath(true));

            BuildMasterServer(
                WindowsBuildPath(true) + "MasterServer.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
            BuildClient(
                WindowsBuildPath(true) + "Client.exe",
                BuildTarget.StandaloneWindows64,
                BuildOptions.Development | BuildOptions.AllowDebugging);
        }

        private static void BuildMasterServer(string path, BuildTarget target, BuildOptions options)
        {
            string[] scenes =
            {
                SceneRoot() + "MasterServer.unity",
            };
            BuildPipeline.BuildPlayer(scenes, path, target, options);
        }

        private static void BuildClient(string path, BuildTarget target, BuildOptions options)
        {
            string[] scenes =
            {
                SceneRoot() + "Login.unity",
                SceneRoot() + "MainMenu.unity",
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
