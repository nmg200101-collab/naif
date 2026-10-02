#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RealDrivingAcademy.Core;
using RealDrivingAcademy.UI;

namespace RealDrivingAcademy.EditorTools
{
    public static class T1FoundationBuilder
    {
        const string ScenesDir = "Assets/RealDrivingAcademy/Scenes";

        [MenuItem("Real Driving Academy/T1/Build Foundation Scenes")]
        public static void BuildAllScenes()
        {
            EnsureFolder();
            Stage2PrototypeBuilder.BuildScene();

            string welcome = BuildWelcome();
            string main = BuildMainMenu();
            string training = BuildTrainingMenu();
            string test = BuildPlaceholder(RdaSceneNames.TestMenu, "Driving Test", "Theory and practical test modules will be expanded in T9.", GameFlowState.DrivingTest);
            string garage = BuildPlaceholder(RdaSceneNames.Garage, "Garage", "Vehicle selection will be expanded in T10.", GameFlowState.Garage);
            string progress = BuildPlaceholder(RdaSceneNames.Progress, "Progress", "Learner progress and history will be expanded in T10.", GameFlowState.Progress);
            string settings = BuildSettings();

            string trainingGround = ScenesDir + "/" + RdaSceneNames.TrainingGround + ".unity";
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(welcome, true),
                new EditorBuildSettingsScene(main, true),
                new EditorBuildSettingsScene(training, true),
                new EditorBuildSettingsScene(test, true),
                new EditorBuildSettingsScene(garage, true),
                new EditorBuildSettingsScene(progress, true),
                new EditorBuildSettingsScene(settings, true),
                new EditorBuildSettingsScene(trainingGround, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(welcome);
            Debug.Log("RDA T1 foundation scenes generated and registered. Welcome is the startup scene.");
        }

        static string BuildWelcome()
        {
            var context = NewUiScene(RdaSceneNames.Welcome, "Real Driving Academy", GameFlowState.Boot);
            CreateInfo(context.content, "T1 Foundation - local prototype", 110f);
            CreateButton(context.content, "Continue as Guest", 10f, context.actions.ContinueAsGuest);
            CreateButton(context.content, "Local Learner Profile", -80f, context.actions.ContinueWithLocalProfile);
            return Save(context.scene, RdaSceneNames.Welcome);
        }

        static string BuildMainMenu()
        {
            var context = NewUiScene(RdaSceneNames.MainMenu, "Main Menu", GameFlowState.MainMenu);
            float y = 145f;
            CreateButton(context.content, "Training", y, context.actions.OpenTrainingMenu); y -= 78f;
            CreateButton(context.content, "Driving Test", y, context.actions.OpenTestMenu); y -= 78f;
            CreateButton(context.content, "Garage", y, context.actions.OpenGarage); y -= 78f;
            CreateButton(context.content, "Progress", y, context.actions.OpenProgress); y -= 78f;
            CreateButton(context.content, "Settings", y, context.actions.OpenSettings); y -= 78f;
            CreateButton(context.content, "End Session", y, context.actions.EndSession);
            CreateButton(context.content, "Quit", -345f, context.actions.QuitApplication, new Vector2(240f, 58f));
            return Save(context.scene, RdaSceneNames.MainMenu);
        }

        static string BuildTrainingMenu()
        {
            var context = NewUiScene(RdaSceneNames.TrainingMenu, "Training", GameFlowState.Training);
            CreateInfo(context.content, "Stage 2 driving ground is connected to the T1 navigation flow.", 120f);
            CreateButton(context.content, "Start Training Drive", 5f, context.actions.StartTrainingDrive);
            CreateButton(context.content, "Back to Main Menu", -90f, context.actions.BackToMainMenu);
            return Save(context.scene, RdaSceneNames.TrainingMenu);
        }

        static string BuildPlaceholder(string sceneName, string title, string message, GameFlowState state)
        {
            var context = NewUiScene(sceneName, title, state);
            CreateInfo(context.content, message, 100f);
            CreateButton(context.content, "Back to Main Menu", -60f, context.actions.BackToMainMenu);
            return Save(context.scene, sceneName);
        }

        static string BuildSettings()
        {
            var context = NewUiScene(RdaSceneNames.Settings, "Settings", GameFlowState.Settings);
            var settings = context.root.AddComponent<RdaSettingsPanel>();

            settings.masterVolume = CreateSlider(context.content, "Master Volume", 130f, 0f, 1f, settings.SetMasterVolume);
            settings.steeringSensitivity = CreateSlider(context.content, "Steering Sensitivity", 35f, 0.25f, 2f, settings.SetSteeringSensitivity);
            settings.cameraSensitivity = CreateSlider(context.content, "Camera Sensitivity", -60f, 0.25f, 2f, settings.SetCameraSensitivity);
            settings.haptics = CreateToggle(context.content, "Haptics", -150f, settings.SetHaptics);

            CreateButton(context.content, "Reset Settings", -245f, settings.ResetSettings, new Vector2(300f, 62f));
            CreateButton(context.content, "Back to Main Menu", -330f, context.actions.BackToMainMenu, new Vector2(300f, 62f));

            return Save(context.scene, RdaSceneNames.Settings);
        }

        static SceneContext NewUiScene(string sceneName, string title, GameFlowState flowState)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var root = new GameObject("RDA_UI_Root");
            var actions = root.AddComponent<RdaMenuActions>();
            actions.flowStateOnStart = flowState;

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(root.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(canvasObject.transform, false);
            var bgRect = background.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background.GetComponent<Image>().color = new Color(0.045f, 0.06f, 0.08f, 1f);

            var titleText = CreateText(canvasObject.transform, title, 46, TextAnchor.MiddleCenter);
            var titleRect = titleText.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(1200f, 90f);
            titleRect.anchoredPosition = new Vector2(0f, -65f);

            var contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(canvasObject.transform, false);
            var content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0.5f, 0.5f);
            content.anchorMax = new Vector2(0.5f, 0.5f);
            content.pivot = new Vector2(0.5f, 0.5f);
            content.sizeDelta = new Vector2(1000f, 760f);
            content.anchoredPosition = new Vector2(0f, -20f);

            return new SceneContext(scene, root, content, actions);
        }

        static Button CreateButton(RectTransform parent, string label, float y, UnityEngine.Events.UnityAction action, Vector2? size = null)
        {
            var go = new GameObject(label.Replace(" ", "_") + "_Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size ?? new Vector2(420f, 66f);
            rect.anchoredPosition = new Vector2(0f, y);

            go.GetComponent<Image>().color = new Color(0.12f, 0.32f, 0.52f, 1f);
            var button = go.GetComponent<Button>();
            UnityEventTools.AddPersistentListener(button.onClick, action);

            var text = CreateText(go.transform, label, 25, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        static Slider CreateSlider(RectTransform parent, string label, float y, float min, float max, UnityEngine.Events.UnityAction<float> action)
        {
            var labelText = CreateText(parent, label, 22, TextAnchor.MiddleLeft);
            labelText.rectTransform.anchorMin = labelText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            labelText.rectTransform.sizeDelta = new Vector2(350f, 50f);
            labelText.rectTransform.anchoredPosition = new Vector2(-250f, y);

            var root = new GameObject(label.Replace(" ", "_") + "_Slider", typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 44f);
            rect.anchoredPosition = new Vector2(170f, y);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(root.transform, false);
            Stretch(bg.GetComponent<RectTransform>());
            bg.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.23f, 1f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            Stretch(fillArea.GetComponent<RectTransform>());
            fillArea.GetComponent<RectTransform>().offsetMin = new Vector2(8f, 8f);
            fillArea.GetComponent<RectTransform>().offsetMax = new Vector2(-8f, -8f);

            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            Stretch(fill.GetComponent<RectTransform>());
            fill.GetComponent<Image>().color = new Color(0.18f, 0.55f, 0.85f, 1f);

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            Stretch(handleArea.GetComponent<RectTransform>());
            handleArea.GetComponent<RectTransform>().offsetMin = new Vector2(12f, 0f);
            handleArea.GetComponent<RectTransform>().offsetMax = new Vector2(-12f, 0f);

            var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            handle.GetComponent<RectTransform>().sizeDelta = new Vector2(28f, 48f);
            handle.GetComponent<Image>().color = Color.white;

            var slider = root.GetComponent<Slider>();
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = min;
            UnityEventTools.AddPersistentListener(slider.onValueChanged, action);
            return slider;
        }

        static Toggle CreateToggle(RectTransform parent, string label, float y, UnityEngine.Events.UnityAction<bool> action)
        {
            var root = new GameObject(label.Replace(" ", "_") + "_Toggle", typeof(RectTransform), typeof(Toggle));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(420f, 54f);
            rect.anchoredPosition = new Vector2(0f, y);

            var box = new GameObject("Background", typeof(RectTransform), typeof(Image));
            box.transform.SetParent(root.transform, false);
            var boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.sizeDelta = new Vector2(44f, 44f);
            boxRect.anchoredPosition = new Vector2(22f, 0f);
            box.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.23f, 1f);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(box.transform, false);
            Stretch(check.GetComponent<RectTransform>());
            check.GetComponent<RectTransform>().offsetMin = new Vector2(8f, 8f);
            check.GetComponent<RectTransform>().offsetMax = new Vector2(-8f, -8f);
            check.GetComponent<Image>().color = new Color(0.18f, 0.75f, 0.38f, 1f);

            var labelText = CreateText(root.transform, label, 23, TextAnchor.MiddleLeft);
            labelText.rectTransform.anchorMin = new Vector2(0f, 0f);
            labelText.rectTransform.anchorMax = new Vector2(1f, 1f);
            labelText.rectTransform.offsetMin = new Vector2(65f, 0f);
            labelText.rectTransform.offsetMax = Vector2.zero;

            var toggle = root.GetComponent<Toggle>();
            toggle.targetGraphic = box.GetComponent<Image>();
            toggle.graphic = check.GetComponent<Image>();
            toggle.isOn = true;
            UnityEventTools.AddPersistentListener(toggle.onValueChanged, action);
            return toggle;
        }

        static Text CreateInfo(RectTransform parent, string message, float y)
        {
            var text = CreateText(parent, message, 22, TextAnchor.MiddleCenter);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            text.rectTransform.sizeDelta = new Vector2(850f, 100f);
            text.rectTransform.anchoredPosition = new Vector2(0f, y);
            return text;
        }

        static Text CreateText(Transform parent, string value, int size, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.font = GetBuiltinUiFont();
            return text;
        }

        static Font GetBuiltinUiFont()
        {
            Font font = null;
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { }

            if (font == null)
            {
                try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { }
            }

            return font;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static string Save(Scene scene, string sceneName)
        {
            string path = ScenesDir + "/" + sceneName + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        static void EnsureFolder()
        {
            const string root = "Assets/RealDrivingAcademy";
            if (!AssetDatabase.IsValidFolder(ScenesDir))
                AssetDatabase.CreateFolder(root, "Scenes");
        }

        readonly struct SceneContext
        {
            public readonly Scene scene;
            public readonly GameObject root;
            public readonly RectTransform content;
            public readonly RdaMenuActions actions;

            public SceneContext(Scene scene, GameObject root, RectTransform content, RdaMenuActions actions)
            {
                this.scene = scene;
                this.root = root;
                this.content = content;
                this.actions = actions;
            }
        }
    }
}
#endif
