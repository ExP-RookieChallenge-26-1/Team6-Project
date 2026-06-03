#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Project2048.EditorTools
{
    public static class SkillVfxShowcaseBuild
    {
        private const string ScenePath = "Assets/Scenes/AttackEffectShowcase.unity";
        private const string OutputPath = "Builds/SkillVfxShowcase/SkillVfxShowcase.exe";

        [MenuItem("Project2048/Build Skill VFX Showcase (Windows)")]
        public static void BuildWindows()
        {
            AttackEffectShowcaseSceneBuilder.Generate();

            var absoluteOutputPath = Path.GetFullPath(OutputPath);
            var outputDirectory = Path.GetDirectoryName(absoluteOutputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Skill VFX showcase build failed: {report.summary.result}");
            }

            Debug.Log($"Built Skill VFX showcase: {absoluteOutputPath}");
        }
    }
}
#endif
