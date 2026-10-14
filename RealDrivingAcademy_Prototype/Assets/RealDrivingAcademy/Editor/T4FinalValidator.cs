#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RealDrivingAcademy.Vehicle;
using RealDrivingAcademy.Cockpit;

namespace RealDrivingAcademy.EditorTools
{
    public static class T4FinalValidator
    {
        const string TrainingScenePath = "Assets/RealDrivingAcademy/Scenes/Stage2_TrainingGround.unity";

        [MenuItem("Real Driving Academy/T4/Build & Validate Realism Stage")]
        public static void BuildAndValidate()
        {
            if (!T4ProductionAssetInstaller.EnsureInstalled())
            {
                EditorUtility.DisplayDialog(
                    "RDA T4",
                    "Production car could not be downloaded/imported. T4 validation stopped.",
                    "OK");
                return;
            }

            T1FoundationBuilder.BuildAllScenes();
            EditorSceneManager.OpenScene(TrainingScenePath);
            ValidateRealismStage();
        }

        [MenuItem("Real Driving Academy/T4/Validate Realism Stage")]
        public static void ValidateRealismStage()
        {
            var failures = new List<string>();

            var car = UnityEngine.Object.FindObjectOfType<RealisticCarController>();
            if (car == null)
            {
                failures.Add("RealisticCarController not found.");
            }
            else
            {
                var visualLayer = car.GetComponent<VehicleVisualLayer>();
                if (visualLayer == null)
                {
                    failures.Add("VehicleVisualLayer is missing.");
                }
                else
                {
                    if (visualLayer.prototypeVisualRoot == null)
                        failures.Add("VisualModel_Old reference is missing.");
                    else if (visualLayer.prototypeVisualRoot.activeSelf)
                        failures.Add("VisualModel_Old must be hidden.");

                    if (visualLayer.realisticVisualRoot == null)
                        failures.Add("Production visual root reference is missing.");
                    else if (!visualLayer.realisticVisualRoot.activeSelf)
                        failures.Add("Production visual root must be active.");
                    else if (visualLayer.realisticVisualRoot.name != "ProductionCar_Real")
                        failures.Add("VehicleVisualLayer is not pointing to ProductionCar_Real.");
                }

                Transform production = car.transform.Find("ProductionCar_Real");
                if (production == null)
                {
                    failures.Add(
                        "ProductionCar_Real hierarchy not found. " +
                        "The procedural fallback is not accepted as the final T4 vehicle.");
                }
                else
                {
                    int rendererCount = production.GetComponentsInChildren<Renderer>(true).Length;
                    if (rendererCount < 80)
                        failures.Add(
                            "Production car imported with too few renderers: " +
                            rendererCount);

                    if (production.Find("SPECTRAL_GT_ROOT") == null &&
                        production.GetComponentsInChildren<Transform>(true)
                            .All(t => t.name != "SPECTRAL_GT_ROOT"))
                    {
                        failures.Add("Spectral GT production root not found in imported GLB.");
                    }
                }

                Transform procedural = car.transform.Find("Sedan_Realistic");
                if (procedural != null && procedural.gameObject.activeSelf)
                    failures.Add("Procedural T4 fallback must be hidden when production GLB is installed.");

                if (car.frontAxle == null || car.rearAxle == null)
                {
                    failures.Add("Vehicle axles are missing.");
                }
                else
                {
                    ValidateWheelVisual(car.frontAxle.leftMesh, "front-left", failures);
                    ValidateWheelVisual(car.frontAxle.rightMesh, "front-right", failures);
                    ValidateWheelVisual(car.rearAxle.leftMesh, "rear-left", failures);
                    ValidateWheelVisual(car.rearAxle.rightMesh, "rear-right", failures);
                }

                var steering = car.GetComponent<SteeringWheelVisual>();
                if (steering == null || steering.steeringWheel == null)
                    failures.Add("Production steering visual reference is missing.");
                else if (steering.steeringWheel.name != "STEERING_INPUT_CTRL" &&
                         steering.steeringWheel.name != "Steering_Wheel")
                    failures.Add("SteeringWheelVisual is not bound to the production GLB steering.");

                var anchors = car.GetComponent<DriverRigAnchors>();
                if (anchors == null || !anchors.IsReady)
                    failures.Add("Driver hand/head rig anchors are incomplete.");

                var cameraRig = car.GetComponent<VehicleCameraRig>();
                if (cameraRig == null)
                {
                    failures.Add("VehicleCameraRig is missing.");
                }
                else
                {
                    if (cameraRig.driverCamera == null || !cameraRig.driverCamera.allowHDR)
                    {
                        failures.Add("Driver camera HDR is not enabled.");
                    }
                    else if ((cameraRig.driverCamera.cullingMask & (1 << 2)) != 0)
                    {
                        failures.Add("Driver camera must hide the exterior visual layer.");
                    }

                    if (cameraRig.chaseCamera == null || !cameraRig.chaseCamera.allowHDR)
                    {
                        failures.Add("Chase camera HDR is not enabled.");
                    }
                    else if ((cameraRig.chaseCamera.cullingMask & (1 << 2)) == 0)
                    {
                        failures.Add("Chase camera must render the exterior visual layer.");
                    }
                }
            }

            if (GameObject.Find("T4_ReflectionProbe") == null)
                failures.Add("T4 reflection probe is missing.");

            if (GameObject.Find("Sidewalk_Left") == null || GameObject.Find("Sidewalk_Right") == null)
                failures.Add("T4 sidewalks are missing.");

            if (GameObject.Find("Curb_Left") == null || GameObject.Find("Curb_Right") == null)
                failures.Add("T4 curbs are missing.");

            if (GameObject.Find("MountainPlaceholder") != null)
                failures.Add("Prototype mountain placeholders are still active.");

            if (RenderSettings.skybox == null)
                failures.Add("Procedural skybox is missing.");

            if (!RenderSettings.fog)
                failures.Add("Atmospheric fog is not enabled.");

            if (PlayerSettings.colorSpace != ColorSpace.Linear)
                failures.Add("Project color space must be Linear for the realism pass.");

            if (failures.Count == 0)
            {
                const string pass =
                    "RDA T4 EDITOR VALIDATION PASSED: production GLB car, wheel/steering bindings, cockpit, cameras and environment are valid.";

                Debug.Log(pass);
                EditorUtility.DisplayDialog("RDA T4", pass + "\n\nProceed to the visual runtime test.", "OK");
                return;
            }

            string message = "RDA T4 EDITOR VALIDATION FAILED:\n- " + string.Join("\n- ", failures);
            Debug.LogError(message);
            EditorUtility.DisplayDialog("RDA T4", message, "OK");
            throw new InvalidOperationException(message);
        }

        static void RequireChild(Transform root, string childName, List<string> failures)
        {
            if (root.Find(childName) == null)
                failures.Add("Required T4 visual object missing: " + childName);
        }

        static void ValidateWheelVisual(Transform wheel, string label, List<string> failures)
        {
            if (wheel == null)
            {
                failures.Add("Missing " + label + " wheel visual.");
                return;
            }

            if (!wheel.name.StartsWith("ProductionWheel_", StringComparison.Ordinal))
                failures.Add(label + " wheel is not bound to the production GLB wheel group.");
        }
    }
}
#endif
