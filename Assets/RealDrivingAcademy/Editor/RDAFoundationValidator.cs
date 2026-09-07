#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace RDA.V50.EditorTools
{
    public static class RDAFoundationValidator
    {
        [MenuItem("RDA/Validate/Foundation")]
        public static void Validate()
        {
            int errors = 0;
            if (Application.unityVersion != "2022.3.62f2") { Debug.LogError($"[RDA] Expected Unity 2022.3.62f2, got {Application.unityVersion}"); errors++; }
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToArray();
            foreach (var required in new[] { "RDA_Boot", "RDA_Login", "RDA_MainMenu" })
                if (!scenes.Contains(required)) { Debug.LogError($"[RDA] Build Settings missing scene: {required}"); errors++; }

            foreach (var scene in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                var opened = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                foreach (var root in opened.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    if (component == null) { Debug.LogError($"[RDA] Missing script reference in {scene.path}"); errors++; }
            }
            if (errors == 0) Debug.Log("[RDA] FOUNDATION VALIDATION PASSED.");
            else Debug.LogError($"[RDA] FOUNDATION VALIDATION FAILED: {errors} issue(s).");
        }
    }
}
#endif