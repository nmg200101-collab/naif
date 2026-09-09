#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RDA.V50.Core;
using RDA.V50.UI;

namespace RDA.V50.EditorTools
{
    public static class RDAFoundationValidator
    {
        private const string ExpectedVersion = "2022.3.62f2";
        private const string CompatibleLocalVersion = "2022.3.62f1";
        private static readonly string[] RequiredScenePaths =
        {
            "Assets/RealDrivingAcademy/Scenes/Boot/RDA_Boot.unity",
            "Assets/RealDrivingAcademy/Scenes/UI/RDA_Login.unity",
            "Assets/RealDrivingAcademy/Scenes/UI/RDA_MainMenu.unity"
        };

        [MenuItem("RDA/Validate/Local Foundation")]
        public static void ValidateLocalMenu() => ValidateOrThrow(false);

        [MenuItem("RDA/Validate/Strict Foundation (2022.3.62f2)")]
        public static void ValidateStrictMenu() => ValidateOrThrow(true);

        public static void ValidateOrThrow(bool strictVersion)
        {
            int errors = Validate(strictVersion);
            if (errors > 0) throw new InvalidOperationException($"RDA V50 foundation validation failed with {errors} issue(s). See Console.");
        }

        public static int Validate(bool strictVersion)
        {
            int errors = 0;
            string version = Application.unityVersion;
            if (strictVersion)
            {
                if (version != ExpectedVersion)
                {
                    Debug.LogError($"[RDA] Strict validation requires Unity {ExpectedVersion}; current={version}");
                    errors++;
                }
            }
            else if (version != ExpectedVersion && version != CompatibleLocalVersion)
            {
                Debug.LogError($"[RDA] Local validation supports {CompatibleLocalVersion} or {ExpectedVersion}; current={version}");
                errors++;
            }
            else if (version == CompatibleLocalVersion)
            {
                Debug.LogWarning($"[RDA] Local compatibility validation is running on {CompatibleLocalVersion}. Final V50 freeze still requires {ExpectedVersion}.");
            }

            var enabledScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (!enabledScenes.SequenceEqual(RequiredScenePaths))
            {
                Debug.LogError("[RDA] Build Settings must contain exactly Boot, Login and MainMenu in the required order.");
                errors++;
            }

            foreach (string path in RequiredScenePaths)
            {
                if (!File.Exists(path))
                {
                    Debug.LogError($"[RDA] Required scene is missing: {path}");
                    errors++;
                    continue;
                }

                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                errors += CountMissingScripts(scene, path);

                string sceneName = Path.GetFileNameWithoutExtension(path);
                if (sceneName == RDASceneFlow.Boot && !Contains<RDABootstrap>(scene))
                {
                    Debug.LogError("[RDA] RDA_Boot is missing RDABootstrap.");
                    errors++;
                }
                else if (sceneName == RDASceneFlow.Login && !Contains<RDALoginController>(scene))
                {
                    Debug.LogError("[RDA] RDA_Login is missing RDALoginController.");
                    errors++;
                }
                else if (sceneName == RDASceneFlow.MainMenu && !Contains<RDAMainMenuController>(scene))
                {
                    Debug.LogError("[RDA] RDA_MainMenu is missing RDAMainMenuController.");
                    errors++;
                }
            }

            string identifier = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            if (identifier != "com.realdrivingacademy.rda")
            {
                Debug.LogError($"[RDA] Android application identifier mismatch: {identifier}");
                errors++;
            }
            if (PlayerSettings.Android.minSdkVersion != AndroidSdkVersions.AndroidApiLevel26)
            {
                Debug.LogError("[RDA] Android minimum API must be 26 for V50 baseline.");
                errors++;
            }
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0)
            {
                Debug.LogError("[RDA] Android ARM64 must be enabled.");
                errors++;
            }

            if (errors == 0) Debug.Log(strictVersion ? "[RDA] STRICT FOUNDATION VALIDATION PASSED." : "[RDA] LOCAL FOUNDATION VALIDATION PASSED.");
            else Debug.LogError($"[RDA] FOUNDATION VALIDATION FAILED: {errors} issue(s).");
            return errors;
        }

        private static int CountMissingScripts(Scene scene, string path)
        {
            int errors = 0;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var component in root.GetComponentsInChildren<Component>(true))
                if (component == null)
                {
                    Debug.LogError($"[RDA] Missing script reference in {path}");
                    errors++;
                }
            return errors;
        }

        private static bool Contains<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<T>(true) != null) return true;
            return false;
        }
    }
}
#endif
