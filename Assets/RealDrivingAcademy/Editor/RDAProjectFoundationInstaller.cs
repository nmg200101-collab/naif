#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RDA.V50.Core;
using RDA.V50.UI;

namespace RDA.V50.EditorTools
{
    public static class RDAProjectFoundationInstaller
    {
        private const string Root = "Assets/RealDrivingAcademy";
        private const string SceneRoot = Root + "/Scenes";
        private const string BootScene = SceneRoot + "/Boot/RDA_Boot.unity";
        private const string LoginScene = SceneRoot + "/UI/RDA_Login.unity";
        private const string MainMenuScene = SceneRoot + "/UI/RDA_MainMenu.unity";

        [MenuItem("RDA/Foundation/Install or Refresh V50 Baseline")]
        public static void Install()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFoundationFolders();
            ApplyProjectSerializationBaseline();
            CreateBootScene();
            CreateLoginScene();
            CreateMainMenuScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(BootScene, true),
                new EditorBuildSettingsScene(LoginScene, true),
                new EditorBuildSettingsScene(MainMenuScene, true)
            };

            ApplyAndroidBaseline();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(BootScene, OpenSceneMode.Single);
            Debug.Log("[RDA] V50 baseline installed/refreshed. Run RDA/Validate/Local Foundation next.");
        }

        private static void EnsureFoundationFolders()
        {
            foreach (var path in new[]
            {
                Root + "/Art", Root + "/Art/Vehicles", Root + "/Art/Environment", Root + "/Art/UI",
                Root + "/Audio", Root + "/Materials",
                Root + "/Prefabs", Root + "/Prefabs/Vehicles", Root + "/Prefabs/World", Root + "/Prefabs/UI",
                SceneRoot, SceneRoot + "/Boot", SceneRoot + "/UI", SceneRoot + "/Training", SceneRoot + "/City", SceneRoot + "/Highway", SceneRoot + "/Mountain",
                Root + "/Scripts", Root + "/Scripts/Core", Root + "/Scripts/Input", Root + "/Scripts/Vehicle", Root + "/Scripts/Camera",
                Root + "/Scripts/Academy", Root + "/Scripts/Traffic", Root + "/Scripts/World", Root + "/Scripts/UI", Root + "/Scripts/Save", Root + "/Scripts/Audio",
                Root + "/Settings", Root + "/Tests", Root + "/Tests/Editor"
            }) EnsureFolder(path);
        }

        private static void ApplyProjectSerializationBaseline()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.externalVersionControl = "Visible Meta Files";
        }

        private static void CreateBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color32(10, 14, 20, 255));
            new GameObject("RDA Bootstrap").AddComponent<RDABootstrap>();
            EditorSceneManager.SaveScene(scene, BootScene);
        }

        private static void CreateLoginScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color32(12, 18, 26, 255));
            CreateEventSystem();
            var canvas = CreateCanvas();
            CreateBackground(canvas.transform, new Color32(19, 28, 39, 255));
            CreateText(canvas.transform, "Title", "REAL DRIVING ACADEMY", 42, new Vector2(0f, 145f), new Vector2(920f, 80f));
            CreateText(canvas.transform, "Subtitle", "V50 FOUNDATION", 20, new Vector2(0f, 90f), new Vector2(500f, 50f));
            var guest = CreateButton(canvas.transform, "ContinueAsGuest", "CONTINUE AS GUEST", new Vector2(0f, -10f), new Vector2(520f, 82f));
            var exit = CreateButton(canvas.transform, "Exit", "EXIT", new Vector2(0f, -115f), new Vector2(300f, 68f));
            var controller = new GameObject("RDA Login Controller").AddComponent<RDALoginController>();
            controller.Configure(guest, exit);
            EditorSceneManager.SaveScene(scene, LoginScene);
        }

        private static void CreateMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(new Color32(10, 16, 24, 255));
            CreateEventSystem();
            var canvas = CreateCanvas();
            CreateBackground(canvas.transform, new Color32(17, 25, 35, 255));
            CreateText(canvas.transform, "Title", "REAL DRIVING ACADEMY", 38, new Vector2(0f, 185f), new Vector2(900f, 70f));
            CreateText(canvas.transform, "FoundationStatus", "V50 FOUNDATION READY FOR LOCAL VALIDATION", 22, new Vector2(0f, 112f), new Vector2(850f, 60f));
            CreateText(canvas.transform, "Sections", "LAWS   |   PRACTICE   |   EXAM   |   FREE DRIVE   |   GARAGE   |   PROGRESS   |   SETTINGS", 18, new Vector2(0f, 15f), new Vector2(1450f, 80f));
            var back = CreateButton(canvas.transform, "BackToLogin", "BACK TO LOGIN", new Vector2(0f, -125f), new Vector2(360f, 70f));
            var controller = new GameObject("RDA Main Menu Controller").AddComponent<RDAMainMenuController>();
            controller.Configure(back);
            EditorSceneManager.SaveScene(scene, MainMenuScene);
        }

        private static void ApplyAndroidBaseline()
        {
            PlayerSettings.companyName = "Real Driving Academy";
            PlayerSettings.productName = "Real Driving Academy";
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.realdrivingacademy.rda");
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        }

        private static void CreateCamera(Color background)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static void CreateEventSystem()
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private static void CreateBackground(Transform parent, Color color)
        {
            var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
        }

        private static Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = fontSize;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = new Color32(44, 67, 91, 255);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var labelText = CreateText(go.transform, "Label", label, 24, Vector2.zero, size);
            var labelRect = (RectTransform)labelText.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta = Vector2.zero;
            return button;
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
