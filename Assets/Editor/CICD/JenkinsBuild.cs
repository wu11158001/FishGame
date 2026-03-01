using UnityEditor;
using System.Linq;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class JenkinsBuild
{
    public static void BuildProject()
    {
        // 1. 自動取得 Build Settings 中勾選的場景
        string[] scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        // 2. 建議改用相對路徑，這樣換電腦也能跑
        string buildPath = "Builds/WebGL";

        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.WebGL;
        buildPlayerOptions.options = BuildOptions.None;

        // 執行打包
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build Succeeded: " + summary.totalSize + " bytes");
            EditorApplication.Exit(0); // 成功結束
        }
        else
        {
            Debug.LogError("Build Failed!");
            EditorApplication.Exit(1); // 失敗結束，讓 Jenkins 變紅燈
        }
    }
}