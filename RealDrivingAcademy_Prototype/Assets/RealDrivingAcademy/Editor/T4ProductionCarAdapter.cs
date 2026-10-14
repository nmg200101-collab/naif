#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using RealDrivingAcademy.Vehicle;
using RealDrivingAcademy.Cockpit;

namespace RealDrivingAcademy.EditorTools
{
    public static class T4ProductionCarAdapter
    {
        const string ProductionRootName = "ProductionCar_Real";
        const float SourceWheelbaseMeters = 2.85f;
        const float PhysicsWheelbaseMeters = 2.60f;
        const float SourceWheelCenterHeight = 0.53f;
        const float PhysicsWheelCenterHeight = 0.52f;
        const float ModelScale = PhysicsWheelbaseMeters / SourceWheelbaseMeters;
        const float ModelRotationY = 90f;

        public static bool TryApply(GameObject carRoot)
        {
            if (carRoot == null)
                return false;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                T4ProductionAssetInstaller.ModelAssetPath);

            if (prefab == null)
                return false;

            Transform oldProduction = carRoot.transform.Find(ProductionRootName);
            if (oldProduction != null)
                UnityEngine.Object.DestroyImmediate(oldProduction.gameObject);

            GameObject instance = UnityEngine.Object.Instantiate(prefab, carRoot.transform);
            instance.name = ProductionRootName;
            instance.transform.localPosition =
                new Vector3(
                    0f,
                    PhysicsWheelCenterHeight - SourceWheelCenterHeight * ModelScale,
                    0f);
            // glTF handedness conversion flips the authored +X front axis.
            // +90 degrees maps the imported production model back to RDA +Z forward.
            instance.transform.localRotation =
                Quaternion.Euler(0f, ModelRotationY, 0f);
            instance.transform.localScale = Vector3.one * ModelScale;

            foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
                animator.enabled = false;

            // Keep the older procedural pass only as a hidden diagnostics fallback.
            Transform procedural = carRoot.transform.Find("Sedan_Realistic");
            if (procedural != null)
                procedural.gameObject.SetActive(false);

            Transform legacy = carRoot.transform.Find("VisualModel_Old");
            if (legacy != null)
                legacy.gameObject.SetActive(false);

            var visualLayer = carRoot.GetComponent<VehicleVisualLayer>();
            if (visualLayer == null)
                visualLayer = carRoot.AddComponent<VehicleVisualLayer>();

            visualLayer.prototypeVisualRoot =
                legacy != null ? legacy.gameObject :
                procedural != null ? procedural.gameObject : null;

            visualLayer.realisticVisualRoot = instance;
            visualLayer.useRealisticVisuals = true;
            visualLayer.Apply();

            var car = carRoot.GetComponent<RealisticCarController>();
            if (car == null)
                return false;

            BindProductionWheels(carRoot, instance.transform, car);
            BindProductionSteering(carRoot, instance.transform);
            BindProductionLights(carRoot, instance.transform);
            BindDriverRig(carRoot, instance.transform);
            ConfigureProductionCameras(carRoot);

            Debug.Log(
                "RDA T4 production GLB applied: Spectral GT RS (CC0), " +
                "physics rig preserved.");

            return true;
        }

        static void BindProductionWheels(
            GameObject carRoot,
            Transform modelRoot,
            RealisticCarController car)
        {
            BindWheelGroup(
                carRoot.transform,
                modelRoot,
                "Front_Left_",
                "ProductionWheel_FL",
                car.frontAxle != null ? car.frontAxle.leftCollider : null,
                t => { if (car.frontAxle != null) car.frontAxle.leftMesh = t; });

            BindWheelGroup(
                carRoot.transform,
                modelRoot,
                "Front_Right_",
                "ProductionWheel_FR",
                car.frontAxle != null ? car.frontAxle.rightCollider : null,
                t => { if (car.frontAxle != null) car.frontAxle.rightMesh = t; });

            BindWheelGroup(
                carRoot.transform,
                modelRoot,
                "Rear_Left_",
                "ProductionWheel_RL",
                car.rearAxle != null ? car.rearAxle.leftCollider : null,
                t => { if (car.rearAxle != null) car.rearAxle.leftMesh = t; });

            BindWheelGroup(
                carRoot.transform,
                modelRoot,
                "Rear_Right_",
                "ProductionWheel_RR",
                car.rearAxle != null ? car.rearAxle.rightCollider : null,
                t => { if (car.rearAxle != null) car.rearAxle.rightMesh = t; });
        }

        static void BindWheelGroup(
            Transform carRoot,
            Transform modelRoot,
            string sourcePrefix,
            string pivotName,
            WheelCollider collider,
            Action<Transform> assign)
        {
            if (collider == null)
                return;

            Transform old = carRoot.Find(pivotName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);

            var matching = modelRoot
                .GetComponentsInChildren<Transform>(true)
                .Where(t =>
                    t != modelRoot &&
                    t.name.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matching.Count == 0)
            {
                Debug.LogWarning(
                    "RDA T4: no GLB wheel parts found for prefix " +
                    sourcePrefix);
                return;
            }

            // Only reparent top-level matches so nested pieces are not moved twice.
            var topLevel = matching
                .Where(t => !matching.Any(other =>
                    other != t && t.IsChildOf(other)))
                .ToList();

            Vector3 sourceCenter = CalculateVisualCenter(topLevel);

            var pivot = new GameObject(pivotName).transform;
            pivot.SetParent(carRoot, true);
            pivot.position = sourceCenter;
            pivot.rotation = collider.transform.rotation;

            foreach (Transform part in topLevel)
                part.SetParent(pivot, true);

            // Move the whole production wheel group onto the existing physics wheel.
            pivot.position = collider.transform.position;

            assign(pivot);
        }

        static Vector3 CalculateVisualCenter(List<Transform> roots)
        {
            bool hasBounds = false;
            Bounds bounds = default;

            foreach (Transform root in roots)
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            if (hasBounds)
                return bounds.center;

            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (Transform root in roots)
            {
                sum += root.position;
                count++;
            }

            return count > 0 ? sum / count : Vector3.zero;
        }

        static void BindProductionSteering(
            GameObject carRoot,
            Transform modelRoot)
        {
            Transform steeringControl =
                FindDeep(modelRoot, "STEERING_INPUT_CTRL") ??
                FindDeep(modelRoot, "Steering_Wheel");

            if (steeringControl == null)
            {
                Debug.LogWarning(
                    "RDA T4: production steering control not found in GLB.");
                return;
            }

            var steeringVisual = carRoot.GetComponent<SteeringWheelVisual>();
            if (steeringVisual == null)
                return;

            steeringVisual.steeringWheel = steeringControl;
            steeringVisual.localRotationAxis = Vector3.up;
            steeringVisual.steeringWheelDegrees = 720f;
        }

        static void BindProductionLights(
            GameObject carRoot,
            Transform modelRoot)
        {
            var visuals = carRoot.GetComponent<VehicleLightVisuals>();
            if (visuals == null)
                return;

            visuals.headlamps = FindRenderers(
                modelRoot,
                "Headlight_Main_-1",
                "Headlight_Main_1");

            visuals.brakeLamps = FindRenderers(
                modelRoot,
                "Tail_Light_Bar_-1",
                "Tail_Light_Bar_1");

            visuals.leftIndicators = FindRenderers(
                modelRoot,
                "Tail_Light_Accent_-1");

            visuals.rightIndicators = FindRenderers(
                modelRoot,
                "Tail_Light_Accent_1");

            // The source model has no dedicated reverse-lamp objects.
            // Create subtle white lamps integrated into the production rear fascia.
            visuals.reverseLamps = CreateReverseLamps(carRoot.transform);
        }

        static Renderer[] CreateReverseLamps(Transform carRoot)
        {
            var result = new List<Renderer>();
            Material reverseMat = GetOrCreateReverseLampMaterial();

            foreach (float x in new[] { -0.30f, 0.30f })
            {
                var lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lamp.name = x < 0f
                    ? "ProductionReverseLamp_L"
                    : "ProductionReverseLamp_R";

                lamp.transform.SetParent(carRoot, false);
                lamp.transform.localPosition =
                    new Vector3(x, .73f, -2.19f);
                lamp.transform.localScale =
                    new Vector3(.14f, .07f, .025f);

                UnityEngine.Object.DestroyImmediate(
                    lamp.GetComponent<Collider>());

                var renderer = lamp.GetComponent<Renderer>();
                renderer.sharedMaterial = reverseMat;
                result.Add(renderer);
            }

            return result.ToArray();
        }

        static Material GetOrCreateReverseLampMaterial()
        {
            const string path =
                "Assets/RealDrivingAcademy/Art/Vehicles/Sedan/Materials/" +
                "T4_ProductionReverseLamp.mat";

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
                return material;

            Shader shader = Shader.Find("Standard");
            material = new Material(shader);
            material.color = new Color(.88f, .93f, 1f);
            material.SetFloat("_Metallic", .08f);
            material.SetFloat("_Glossiness", .82f);

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void BindDriverRig(
            GameObject carRoot,
            Transform modelRoot)
        {
            var anchors = carRoot.GetComponent<DriverRigAnchors>();
            if (anchors == null)
                anchors = carRoot.AddComponent<DriverRigAnchors>();

            Transform steering =
                FindDeep(modelRoot, "STEERING_INPUT_CTRL") ??
                FindDeep(modelRoot, "Steering_Wheel");

            if (steering != null)
            {
                anchors.leftHandTarget =
                    CreateOrReplaceAnchor(
                        steering,
                        "ProductionLeftHandTarget",
                        new Vector3(-.16f, .10f, 0f));

                anchors.rightHandTarget =
                    CreateOrReplaceAnchor(
                        steering,
                        "ProductionRightHandTarget",
                        new Vector3(.16f, .10f, 0f));
            }

            Transform head = carRoot.transform.Find("DriverHeadAnchor");
            anchors.headAnchor = head;

            Transform seat =
                FindDeep(modelRoot, "Driver_Seat_Base") ??
                FindDeep(modelRoot, "DriverSeat") ??
                FindDeep(modelRoot, "Driver_Seat_Back");

            if (seat != null)
                anchors.seatAnchor = seat;
        }

        static Transform CreateOrReplaceAnchor(
            Transform parent,
            string name,
            Vector3 localPosition)
        {
            Transform old = parent.Find(name);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);

            var anchor = new GameObject(name).transform;
            anchor.SetParent(parent, false);
            anchor.localPosition = localPosition;
            anchor.localRotation = Quaternion.identity;
            return anchor;
        }

        static void ConfigureProductionCameras(GameObject carRoot)
        {
            var rig = carRoot.GetComponent<VehicleCameraRig>();
            if (rig == null)
                return;

            if (rig.driverCamera != null)
            {
                rig.driverCamera.transform.localPosition =
                    new Vector3(-.44f, 1.34f, -.42f);

                rig.driverCamera.fieldOfView = 68f;
                rig.driverCamera.nearClipPlane = .045f;

                // Production GLB stays on Default layer and is visible from cockpit.
                rig.driverCamera.cullingMask |= 1 << 0;
            }

            if (rig.chaseCamera != null)
            {
                rig.chaseCamera.transform.localPosition =
                    new Vector3(0f, 2.25f, -5.8f);

                rig.chaseCamera.fieldOfView = 60f;
                rig.chaseCamera.cullingMask |= 1 << 0;
            }
        }

        static Renderer[] FindRenderers(
            Transform root,
            params string[] names)
        {
            var result = new List<Renderer>();

            foreach (string name in names)
            {
                Transform t = FindDeep(root, name);
                if (t == null)
                    continue;

                var renderer = t.GetComponent<Renderer>();
                if (renderer != null)
                    result.Add(renderer);
            }

            return result.ToArray();
        }

        static Transform FindDeep(Transform root, string exactName)
        {
            if (root == null)
                return null;

            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(
                    t.name,
                    exactName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    return t;
                }
            }

            return null;
        }
    }
}
#endif
