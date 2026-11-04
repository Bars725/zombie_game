using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Reproducible player builds, from the menu or the command line:
    ///   Unity.exe -batchmode -quit -projectPath Zombie_game
    ///     -executeMethod Game.EditorTools.BuildScript.BuildWindows
    ///     [-buildOutput "C:\path\Game.exe"] [-developmentBuild] -logFile build.log
    /// Uses the scenes enabled in Build Profiles / Build Settings.
    /// Exit code in batchmode: 0 = success, 1 = failure.
    /// </summary>
    public static class BuildScript
    {
        private const string ExeName = "ZombieGame.exe";

        [MenuItem("Tools/Build/Windows (Release)")]
        public static void BuildWindowsFromMenu() => Run(DefaultOutput(), development: false);

        [MenuItem("Tools/Build/Windows (Development)")]
        public static void BuildWindowsDevFromMenu() => Run(DefaultOutput(), development: true);

        /// <summary>Entry point for -executeMethod.</summary>
        public static void BuildWindows()
        {
            string output = GetArg("-buildOutput") ?? DefaultOutput();
            bool development = HasArg("-developmentBuild");
            Run(output, development);
        }

        private static void Run(string outputPath, bool development)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path))
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                Fail("No enabled scenes in Build Settings.");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            Debug.Log($"[BuildScript] Building {scenes.Length} scene(s) to {outputPath} (development: {development})");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[BuildScript] SUCCEEDED in {summary.totalTime:mm\\:ss}, size {summary.totalSize / (1024f * 1024f):0.0} MB");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            else
            {
                Fail($"Build {summary.result}: {summary.totalErrors} error(s). See the log above.");
            }
        }

        /// <summary>Repo root is one level above the Unity project: &lt;repo&gt;/_out/Builds/Windows.</summary>
        private static string DefaultOutput()
        {
            string projectDir = Directory.GetParent(Application.dataPath).FullName;
            string repoRoot = Directory.GetParent(projectDir).FullName;
            return Path.Combine(repoRoot, "_out", "Builds", "Windows", ExeName);
        }

        private static void Fail(string message)
        {
            Debug.LogError("[BuildScript] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }

        private static string GetArg(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            }
            return null;
        }

        private static bool HasArg(string name) =>
            Environment.GetCommandLineArgs().Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
    }
}
