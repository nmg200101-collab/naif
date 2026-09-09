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
            RDAProjectFoundationInstaller.Install();
            RDAFoundationValidator.ValidateOrThrow(false);

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Unable to switch to Android. Install Android Build Support for this Unity editor first.");

            var scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray();
            Directory.CreateDirectory("Builds/Android");
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/Android/RDA-V50-FOUNDATION-LOCAL.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.StrictMode
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"RDA Android validation failed: {report.summary.result}, errors={report.summary.totalErrors}");
            Debug.Log($"[RDA] Android local validation PASSED. Size={report.summary.totalSize} bytes");
        }
    }
}
#endif
