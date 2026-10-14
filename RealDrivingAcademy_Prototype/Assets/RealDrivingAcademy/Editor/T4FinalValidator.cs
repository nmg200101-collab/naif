#if UNITY_EDITOR
using System;
using System.Collections.Generic;
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
                        failures.Add("Sedan_Realistic reference is missing.");
                    else if (!visualLayer.realisticVisualRoot.activeSelf)
                        failures.Add("Sedan_Realistic must be active.");
                }

                Transform realistic = car.transform.Find("Sedan_Realistic");
                if (realistic == null)
                {
                    failures.Add("Sedan_Realistic hierarchy not found.");
                }
                else
                {
                    var body = realistic.Find("SedanBody");
                    if (body == null)
                    {
                        failures.Add("SedanBody not found.");
                    }
                    else
                    {
                        var filter = body.GetComponent<MeshFilter>();
                        var renderer = body.GetComponent<MeshRenderer>();

                        if (filter == null || filter.sharedMesh == null)
                            failures.Add("SedanBody mesh is missing.");

                        if (renderer == null || renderer.sharedMaterial == null)
                            failures.Add("SedanBody material is missing.");
                    }

                    RequireChild(realistic, "Interior", failures);
                    RequireChild(realistic, "Windshield", failures);
                    RequireChild(realistic, "Wheel_FL_Realistic", failures);
                    RequireChild(realistic, "Wheel_FR_Realistic", failures);
                    RequireChild(realistic, "Wheel_RL_Realistic", failures);
                    RequireChild(realistic, "Wheel_RR_Realistic", failures);
                }

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
                    failures.Add("Realistic steering visual reference is missing.");
                else if (!steering.steeringWheel.name.Contains("Realistic"))
                    failures.Add("SteeringWheelVisual is still using the prototype steering wheel.");

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
                        failures.Add("Driver camera HDR is not enabled.");
                    if (cameraRig.chaseCamera == null || !cameraRig.chaseCamera.allowHDR)
                        failures.Add("Chase camera HDR is not enabled.");
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
                    "RDA T4 EDITOR VALIDATION PASSED: realistic sedan visual layer, PBR materials, cockpit anchors, wheels, cameras and environment pass are valid.";

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

            if (!wheel.name.Contains("Realistic"))
                failures.Add(label + " wheel is still using prototype geometry.");
        }
    }
}
#endif
