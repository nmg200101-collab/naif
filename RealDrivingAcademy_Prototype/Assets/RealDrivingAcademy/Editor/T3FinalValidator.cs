#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RealDrivingAcademy.Core;
using RealDrivingAcademy.Training;
using RealDrivingAcademy.UI;
using RealDrivingAcademy.Vehicle;

namespace RealDrivingAcademy.EditorTools
{
    public static class T3FinalValidator
    {
        const string ScenesDir = "Assets/RealDrivingAcademy/Scenes";
        const string TrainingGroundPath = ScenesDir + "/Stage2_TrainingGround.unity";
        const string TrainingMenuPath = ScenesDir + "/TrainingMenu.unity";
        const string ProgressPath = ScenesDir + "/Progress.unity";

        [MenuItem("Real Driving Academy/T3/Build & Validate Training Stage")]
        public static void BuildAndValidate()
        {
            T1FoundationBuilder.BuildAllScenes();
            ValidateTrainingStage();
        }

        [MenuItem("Real Driving Academy/T3/Validate Training Stage")]
        public static void ValidateTrainingStage()
        {
            var failures = new List<string>();

            ValidateTrainingGround(failures);
            ValidateTrainingMenu(failures);
            ValidateProgressScene(failures);
            ValidateBuildSettings(failures);

            EditorSceneManager.OpenScene(TrainingGroundPath);

            if (failures.Count == 0)
            {
                const string pass = "RDA T3 EDITOR VALIDATION PASSED: guided lesson, checkpoints, scoring, results, persistence hooks and progress UI are valid.";
                Debug.Log(pass);
                EditorUtility.DisplayDialog("RDA T3", pass + "\n\nProceed to the final runtime lesson test.", "OK");
                return;
            }

            string message = "RDA T3 EDITOR VALIDATION FAILED:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            EditorUtility.DisplayDialog("RDA T3", message, "OK");
            throw new InvalidOperationException(message);
        }

        static void ValidateTrainingGround(List<string> failures)
        {
            EditorSceneManager.OpenScene(TrainingGroundPath);

            var car = UnityEngine.Object.FindObjectOfType<RealisticCarController>();
            var lesson = UnityEngine.Object.FindObjectOfType<DrivingLessonManager>();
            var violations = UnityEngine.Object.FindObjectOfType<DrivingViolationSystem>();
            var checkpoint = UnityEngine.Object.FindObjectOfType<LessonCheckpointZone>();
            var stopZone = UnityEngine.Object.FindObjectOfType<LessonStopZone>();
            var collision = UnityEngine.Object.FindObjectOfType<VehicleCollisionPenalty>();
            var result = UnityEngine.Object.FindObjectOfType<TrainingResultController>();
            var hud = UnityEngine.Object.FindObjectOfType<PrototypeHUD>();

            if (car == null) failures.Add("Training car is missing.");
            if (lesson == null) failures.Add("DrivingLessonManager is missing.");
            if (violations == null) failures.Add("DrivingViolationSystem is missing.");
            if (checkpoint == null) failures.Add("LessonCheckpointZone is missing.");
            if (stopZone == null) failures.Add("LessonStopZone is missing.");
            if (collision == null) failures.Add("VehicleCollisionPenalty is missing.");
            if (result == null) failures.Add("TrainingResultController is missing.");
            if (hud == null) failures.Add("PrototypeHUD is missing.");

            if (lesson != null)
            {
                if (lesson.car == null) failures.Add("Lesson manager car reference is missing.");
                if (lesson.input == null) failures.Add("Lesson manager input reference is missing.");
                if (lesson.systems == null) failures.Add("Lesson manager systems reference is missing.");
            }

            if (checkpoint != null)
            {
                if (checkpoint.lesson == null) failures.Add("Checkpoint lesson reference is missing.");
                if (checkpoint.violations == null) failures.Add("Checkpoint violation reference is missing.");
                if (string.IsNullOrWhiteSpace(checkpoint.checkpointId)) failures.Add("Checkpoint id is empty.");
            }

            if (stopZone != null && stopZone.lesson == null)
                failures.Add("Stop zone lesson reference is missing.");

            if (collision != null && collision.violations == null)
                failures.Add("Collision penalty violation reference is missing.");

            if (result != null)
            {
                if (result.lesson == null) failures.Add("Result controller lesson reference is missing.");
                if (result.violations == null) failures.Add("Result controller violation reference is missing.");
                if (result.resultText == null) failures.Add("Result controller result Text is missing.");
                if (result.passingScore != 70) failures.Add("Expected passing score 70.");
                if (result.lessonId != "training_basic_01") failures.Add("Unexpected basic lesson id.");
            }

            if (hud != null)
            {
                if (hud.lesson == null) failures.Add("HUD lesson reference is missing.");
                if (hud.lessonProgressText == null) failures.Add("HUD lesson progress Text is missing.");
                if (hud.scoreText == null) failures.Add("HUD score Text is missing.");
            }

            RequireGameObject("LessonCheckpointZone", failures);
            RequireGameObject("LessonStopZone", failures);
            RequireGameObject("LessonStopTarget", failures);
            RequireGameObject("LessonProgress", failures);
            RequireGameObject("LessonResult", failures);
        }

        static void ValidateTrainingMenu(List<string> failures)
        {
            EditorSceneManager.OpenScene(TrainingMenuPath);

            var panel = UnityEngine.Object.FindObjectOfType<RdaTrainingMenuPanel>();
            if (panel == null)
                failures.Add("RdaTrainingMenuPanel is missing from TrainingMenu.");
            else if (panel.statusText == null)
                failures.Add("Training menu status Text is missing.");

            RequireGameObject("Start_Basic_Driving_Lesson_Button", failures);
        }

        static void ValidateProgressScene(List<string> failures)
        {
            EditorSceneManager.OpenScene(ProgressPath);

            var panel = UnityEngine.Object.FindObjectOfType<RdaProgressPanel>();
            if (panel == null)
                failures.Add("RdaProgressPanel is missing from Progress scene.");
            else if (panel.summaryText == null)
                failures.Add("Progress summary Text is missing.");
        }

        static void ValidateBuildSettings(List<string> failures)
        {
            string[] required =
            {
                TrainingGroundPath,
                TrainingMenuPath,
                ProgressPath
            };

            foreach (string path in required)
            {
                bool found = EditorBuildSettings.scenes.Any(s =>
                    s.enabled && string.Equals(s.path, path, StringComparison.OrdinalIgnoreCase));

                if (!found)
                    failures.Add("Required scene is not enabled in Build Settings: " + path);
            }
        }

        static void RequireGameObject(string name, List<string> failures)
        {
            if (GameObject.Find(name) == null)
                failures.Add("Required object missing: " + name);
        }
    }
}
#endif
