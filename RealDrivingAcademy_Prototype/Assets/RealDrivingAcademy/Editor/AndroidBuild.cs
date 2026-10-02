#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System;
using UnityEngine;

namespace RealDrivingAcademy.EditorTools
{
    public static class AndroidBuild
    {
        [MenuItem("Real Driving Academy/Build Android APK")]
        public static void BuildApkMenu() => BuildApk();

        public static void BuildApk()
        {
            Stage2PrototypeBuilder.BuildScene();
            PlayerSettings.productName = "Real Driving Academy";
            PlayerSettings.companyName = "RDA Studio";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.rda.realdrivingacademy");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            EditorUserBuildSettings.buildAppBundle = false;
            string[] scenes = EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes were generated for the Android build.");

            string outputPath = "Builds/Android/RealDrivingAcademy_Stage2.apk";
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("Android build result: " + report.summary.result + " | " + report.summary.outputPath);
            if (report.summary.result != BuildResult.Succeeded || !File.Exists(outputPath))
                throw new BuildFailedException("Android APK build failed. See the Unity Editor log above for the exact cause.");
        }
    }
}
#endif
