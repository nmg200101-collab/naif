#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RDA.V50.EditorTools
{
    public static class RDACIBuild
    {
        public static void BuildAndroid()
        {
            RDAProjectFoundationInstaller.Install();
            RDAFoundationValidator.ValidateOrThrow(true);

            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("CI could not switch the active build target to Android.");

            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length != 3) throw new InvalidOperationException("RDA V50 CI requires exactly the three foundation scenes.");

            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Builds/Android/RDA-V50-FOUNDATION-VALIDATION.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development | BuildOptions.StrictMode
            });

            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"Android build failed: {report.summary.result}; errors={report.summary.totalErrors}");
            Debug.Log("[RDA] CI Android foundation build passed.");
        }
    }
}
#endif
