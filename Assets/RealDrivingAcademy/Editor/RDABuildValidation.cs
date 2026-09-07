#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RDA.V50.EditorTools
{
    public static class RDABuildValidation
    {
        [MenuItem("RDA/Build/Android Development Validation")]
        public static void BuildAndroidDevelopment()
        {
            var scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            if (scenes.Length < 3) throw new InvalidOperationException("RDA V50 requires Boot, Login and MainMenu scenes before Android validation.");
            Directory.CreateDirectory("Builds/Android");
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/Android/RDA-V50-FOUNDATION-VALIDATION.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"RDA Android validation failed: {report.summary.result}, errors={report.summary.totalErrors}");
            Debug.Log($"[RDA] Android validation PASSED. Size={report.summary.totalSize} bytes");
        }
    }
}
#endif