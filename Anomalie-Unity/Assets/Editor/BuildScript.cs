using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void Build()
    {
        SceneBuilder.BuildAll();

        PlayerSettings.companyName = "Papulovka";
        PlayerSettings.productName = "Anomalie";
        PlayerSettings.bundleVersion = "1.0.0";
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.runInBackground = true;

        string[] scenes =
        {
            "Assets/Scenes/Menu.unity",
            "Assets/Scenes/Game.unity",
            "Assets/Scenes/Win.unity",
            "Assets/Scenes/Credits.unity"
        };
        EditorBuildSettings.scenes = Array.ConvertAll(scenes, path => new EditorBuildSettingsScene(path, true));
        AssetDatabase.SaveAssets();

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "/tmp/opencode/anomaly-build/Anomalie.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("BUILD RESULT: " + report.summary.result + " / errors: " + report.summary.totalErrors);
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Windows build failed: " + report.summary.result);
    }
}
