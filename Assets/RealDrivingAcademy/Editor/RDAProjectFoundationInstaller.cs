#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RDA.V50.EditorTools
{
    public static class RDAProjectFoundationInstaller
    {
        private const string SceneRoot = "Assets/RealDrivingAcademy/Scenes";

        [MenuItem("RDA/Foundation/Install V50 Baseline")]
        public static void Install()
        {
            EnsureFolder("Assets/RealDrivingAcademy/Art");
            EnsureFolder("Assets/RealDrivingAcademy/Audio");
            EnsureFolder("Assets/RealDrivingAcademy/Materials");
            EnsureFolder("Assets/RealDrivingAcademy/Prefabs");
            EnsureFolder(SceneRoot);
            EnsureFolder(SceneRoot + "/Boot");
            EnsureFolder(SceneRoot + "/UI");
            EnsureFolder(SceneRoot + "/Training");
            EnsureFolder(SceneRoot + "/City");
            EnsureFolder(SceneRoot + "/Highway");
            EnsureFolder(SceneRoot + "/Mountain");
            EnsureFolder("Assets/RealDrivingAcademy/Settings");
            EnsureFolder("Assets/RealDrivingAcademy/Tests");

            CreateScene(SceneRoot + "/Boot/RDA_Boot.unity", true);
            CreateScene(SceneRoot + "/UI/RDA_Login.unity", false);
            CreateScene(SceneRoot + "/UI/RDA_MainMenu.unity", false);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(SceneRoot + "/Boot/RDA_Boot.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/UI/RDA_Login.unity", true),
                new EditorBuildSettingsScene(SceneRoot + "/UI/RDA_MainMenu.unity", true)
            };

            PlayerSettings.companyName = "Real Driving Academy";
            PlayerSettings.productName = "Real Driving Academy";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.realdrivingacademy.rda");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[RDA] V50 baseline installed. Run RDA/Validate/Foundation next.");
        }

        private static void CreateScene(string path, bool boot)
        {
            if (File.Exists(path)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera");
            camera.tag = "MainCamera";
            camera.AddComponent<Camera>();
            if (boot) new GameObject("RDA Bootstrap").AddComponent<RDA.V50.Core.RDABootstrap>();
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif