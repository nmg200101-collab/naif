#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RealDrivingAcademy.Vehicle;
using RealDrivingAcademy.UI;

namespace RealDrivingAcademy.EditorTools
{
    public static class T2FinalValidator
    {
        const string TrainingScenePath = "Assets/RealDrivingAcademy/Scenes/Stage2_TrainingGround.unity";

        [MenuItem("Real Driving Academy/T2/Build & Validate Vehicle Stage")]
        public static void BuildAndValidate()
        {
            T1FoundationBuilder.BuildAllScenes();
            EditorSceneManager.OpenScene(TrainingScenePath);
            ValidateVehicleStage();
        }

        [MenuItem("Real Driving Academy/T2/Validate Vehicle Stage")]
        public static void ValidateVehicleStage()
        {
            var failures = new List<string>();

            if (!Application.unityVersion.StartsWith("2022.3.62"))
                failures.Add("Unexpected Unity version: " + Application.unityVersion);

            var car = UnityEngine.Object.FindObjectOfType<RealisticCarController>();
            if (car == null)
            {
                failures.Add("TrainingSedan / RealisticCarController not found.");
            }
            else
            {
                if (car.config == null)
                    failures.Add("VehicleConfig is not assigned.");

                if (car.selectedGear != 0)
                    failures.Add("Vehicle must start in Neutral.");

                CheckAxle(car.frontAxle, "Front axle", failures);
                CheckAxle(car.rearAxle, "Rear axle", failures);

                var rb = car.GetComponent<Rigidbody>();
                if (rb == null)
                    failures.Add("Rigidbody is missing.");
                else if (rb.mass < 1000f)
                    failures.Add("Unexpected vehicle mass: " + rb.mass);

                var input = car.GetComponent<CarInputState>();
                if (input == null)
                    failures.Add("CarInputState is missing.");
                else if (!input.handbrake)
                    failures.Add("Vehicle must start with handbrake ON.");

                if (car.GetComponent<CarSystems>() == null)
                    failures.Add("CarSystems is missing.");

                var safety = car.GetComponent<VehicleSafetyAssist>();
                if (safety == null)
                    failures.Add("VehicleSafetyAssist is missing.");

                var cameraRig = car.GetComponent<VehicleCameraRig>();
                if (cameraRig == null)
                {
                    failures.Add("VehicleCameraRig is missing.");
                }
                else
                {
                    if (cameraRig.driverCamera == null)
                        failures.Add("Driver camera is missing.");
                    if (cameraRig.chaseCamera == null)
                        failures.Add("Chase camera is missing.");
                }

                var lightVisuals = car.GetComponent<VehicleLightVisuals>();
                if (lightVisuals == null)
                {
                    failures.Add("VehicleLightVisuals is missing.");
                }
                else
                {
                    CheckRendererArray(lightVisuals.headlamps, 2, "Headlamps", failures);
                    CheckRendererArray(lightVisuals.brakeLamps, 2, "Brake lamps", failures);
                    CheckRendererArray(lightVisuals.reverseLamps, 2, "Reverse lamps", failures);
                    CheckRendererArray(lightVisuals.leftIndicators, 2, "Left indicators", failures);
                    CheckRendererArray(lightVisuals.rightIndicators, 2, "Right indicators", failures);
                }
            }

            var canvas = UnityEngine.Object.FindObjectOfType<Canvas>();
            if (canvas == null)
                failures.Add("Training HUD Canvas is missing.");

            if (UnityEngine.Object.FindObjectOfType<PrototypeHUD>() == null)
                failures.Add("PrototypeHUD is missing.");

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
                failures.Add("EventSystem is missing.");

            RequireGameObject("ResetVehicle", failures);
            RequireGameObject("Camera", failures);
            RequireGameObject("Handbrake", failures);
            RequireGameObject("GearR", failures);
            RequireGameObject("GearN", failures);
            RequireGameObject("GearD", failures);
            RequireGameObject("HAZ", failures);
            RequireGameObject("LIGHT", failures);
            RequireGameObject("LEFT", failures);
            RequireGameObject("RIGHT", failures);

            int enabledListeners = UnityEngine.Object.FindObjectsOfType<AudioListener>(true).Count(x => x.enabled);
            if (enabledListeners != 1)
                failures.Add("Exactly one AudioListener must be enabled in the training scene. Found: " + enabledListeners);

            bool sceneRegistered = EditorBuildSettings.scenes.Any(s =>
                s.enabled && string.Equals(s.path, TrainingScenePath, StringComparison.OrdinalIgnoreCase));

            if (!sceneRegistered)
                failures.Add("Stage2_TrainingGround is not enabled in Build Settings.");

            if (failures.Count == 0)
            {
                const string pass = "RDA T2 EDITOR VALIDATION PASSED: vehicle, physics, controls, cameras, lights, recovery and HUD structure are valid.";
                Debug.Log(pass);
                EditorUtility.DisplayDialog("RDA T2", pass + "\n\nProceed to the final runtime checklist.", "OK");
                return;
            }

            string message = "RDA T2 EDITOR VALIDATION FAILED:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            EditorUtility.DisplayDialog("RDA T2", message, "OK");
            throw new InvalidOperationException(message);
        }

        static void CheckAxle(RealisticCarController.Axle axle, string label, List<string> failures)
        {
            if (axle == null)
            {
                failures.Add(label + " is missing.");
                return;
            }

            if (axle.leftCollider == null || axle.rightCollider == null)
                failures.Add(label + " WheelColliders are incomplete.");
            if (axle.leftMesh == null || axle.rightMesh == null)
                failures.Add(label + " visual wheels are incomplete.");
        }

        static void CheckRendererArray(Renderer[] items, int minimum, string label, List<string> failures)
        {
            if (items == null || items.Count(x => x != null) < minimum)
                failures.Add(label + " are incomplete.");
        }

        static void RequireGameObject(string name, List<string> failures)
        {
            if (GameObject.Find(name) == null)
                failures.Add("Required UI object missing: " + name);
        }
    }
}
#endif
