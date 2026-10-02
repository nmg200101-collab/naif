#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RealDrivingAcademy.Core;

namespace RealDrivingAcademy.EditorTools
{
    public static class T1GateValidator
    {
        static readonly string[] RequiredScenes =
        {
            RdaSceneNames.Welcome,
            RdaSceneNames.MainMenu,
            RdaSceneNames.TrainingMenu,
            RdaSceneNames.TestMenu,
            RdaSceneNames.Garage,
            RdaSceneNames.Progress,
            RdaSceneNames.Settings,
            RdaSceneNames.TrainingGround
        };

        [MenuItem("Real Driving Academy/T1/Validate Foundation")]
        public static void ValidateFoundation()
        {
            var failures = new List<string>();

            if (!Application.unityVersion.StartsWith("2022.3.62"))
                failures.Add("Unexpected Unity version: " + Application.unityVersion);

            foreach (string sceneName in RequiredScenes)
            {
                string path = "Assets/RealDrivingAcademy/Scenes/" + sceneName + ".unity";
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
                    failures.Add("Missing generated scene: " + path);
            }

            var enabledSceneNames = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))
                .ToArray();

            foreach (string sceneName in RequiredScenes)
            {
                if (!enabledSceneNames.Contains(sceneName))
                    failures.Add("Scene not enabled in Build Settings: " + sceneName);
            }

            if (EditorBuildSettings.scenes.Length == 0 ||
                System.IO.Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[0].path) != RdaSceneNames.Welcome)
            {
                failures.Add("Welcome must be the first Build Settings scene.");
            }

            if (failures.Count == 0)
            {
                Debug.Log("RDA T1 EDITOR VALIDATION PASSED: all required scenes exist and Build Settings are correct.");
                EditorUtility.DisplayDialog("RDA T1", "Editor validation PASSED. Proceed to the device test checklist.", "OK");
                return;
            }

            string message = "RDA T1 EDITOR VALIDATION FAILED:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            EditorUtility.DisplayDialog("RDA T1", message, "OK");
            throw new InvalidOperationException(message);
        }
    }
}
#endif
