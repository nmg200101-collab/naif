#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using RealDrivingAcademy.Vehicle;
using RealDrivingAcademy.Cockpit;

namespace RealDrivingAcademy.EditorTools
{
    public static class T4RealismBuilder
    {
        const string ArtRoot = "Assets/RealDrivingAcademy/Art";
        const string VehicleRoot = ArtRoot + "/Vehicles";
        const string SedanRoot = VehicleRoot + "/Sedan";
        const string MeshRoot = SedanRoot + "/Meshes";
        const string MaterialRoot = SedanRoot + "/Materials";
        const int DriverHiddenLayer = 2;

        static Material paint;
        static Material glass;
        static Material rubber;
        static Material chrome;
        static Material leather;
        static Material headlamp;
        static Material rearLamp;
        static Material indicator;
        static Material road;
        static Material curb;
        static Material sidewalk;
        static Material pole;

        struct BodySection
        {
            public float z;
            public float halfWidth;
            public float lowerY;
            public float shoulderY;
            public float topY;

            public BodySection(float z, float halfWidth, float lowerY, float shoulderY, float topY)
            {
                this.z = z;
                this.halfWidth = halfWidth;
                this.lowerY = lowerY;
                this.shoulderY = shoulderY;
                this.topY = topY;
            }
        }

        public static void ApplyEnvironment()
        {
            EnsureFolders();
            CreateMaterials();

            PlayerSettings.colorSpace = ColorSpace.Linear;
            QualitySettings.antiAliasing = 4;
            QualitySettings.shadowDistance = 120f;

            foreach (var mountain in GameObject.FindObjectsOfType<GameObject>()
                         .Where(x => x.name == "MountainPlaceholder")
                         .ToArray())
            {
                Object.DestroyImmediate(mountain);
            }

            var trainingRoad = GameObject.Find("TrainingRoad");
            if (trainingRoad != null)
                trainingRoad.GetComponent<Renderer>().sharedMaterial = road;

            var crossRoad = GameObject.Find("CrossRoad");
            if (crossRoad != null)
                crossRoad.GetComponent<Renderer>().sharedMaterial = road;

            var ground = GameObject.Find("Ground");
            if (ground != null)
            {
                var r = ground.GetComponent<Renderer>();
                if (r != null)
                    r.sharedMaterial = CreatePbr(
                        "T4_Ground",
                        new Color(0.12f, 0.18f, 0.10f),
                        0f,
                        0.08f);
            }

            CreateRoadside();
            ConfigureLightingAndAtmosphere();
        }

        public static void Apply(GameObject carRoot)
        {
            if (carRoot == null)
                return;

            EnsureFolders();
            CreateMaterials();

            Transform existing = carRoot.transform.Find("Sedan_Realistic");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            Transform existingOld = carRoot.transform.Find("VisualModel_Old");
            if (existingOld != null)
                Object.DestroyImmediate(existingOld.gameObject);

            var oldTopLevel = CollectPrototypeVisualRoots(carRoot.transform);

            var oldRoot = new GameObject("VisualModel_Old");
            oldRoot.transform.SetParent(carRoot.transform, false);

            foreach (var top in oldTopLevel)
            {
                if (top == null || top == carRoot.transform)
                    continue;

                top.SetParent(oldRoot.transform, false);
            }

            oldRoot.SetActive(false);

            var realisticRoot = new GameObject("Sedan_Realistic");
            realisticRoot.transform.SetParent(carRoot.transform, false);

            var exteriorRoot = new GameObject("Exterior_DriverHidden");
            exteriorRoot.transform.SetParent(realisticRoot.transform, false);

            var body = CreateMeshObject(
                exteriorRoot.transform,
                "SedanBody",
                BuildSedanBodyMesh(),
                paint);

            body.transform.localPosition = Vector3.zero;

            CreateGlass(exteriorRoot.transform);
            CreateExteriorTrim(exteriorRoot.transform);
            ReplaceWheelVisuals(exteriorRoot.transform, carRoot);
            RewireVehicleLights(exteriorRoot.transform, carRoot);
            SetLayerRecursively(exteriorRoot.transform, DriverHiddenLayer);

            CreateInterior(realisticRoot.transform, carRoot);
            RecreateMirrorDisplays(realisticRoot.transform, carRoot);
            RepositionCameras(carRoot);

            var layer = carRoot.GetComponent<VehicleVisualLayer>();
            if (layer == null)
                layer = carRoot.AddComponent<VehicleVisualLayer>();

            layer.prototypeVisualRoot = oldRoot;
            layer.realisticVisualRoot = realisticRoot;
            layer.useRealisticVisuals = true;
            layer.Apply();

            var steeringVisual = carRoot.GetComponent<SteeringWheelVisual>();
            if (steeringVisual != null)
                layer.steeringWheel = steeringVisual.steeringWheel;

            var car = carRoot.GetComponent<RealisticCarController>();
            if (car != null)
            {
                layer.wheelVisuals = new[]
                {
                    car.frontAxle != null ? car.frontAxle.leftMesh : null,
                    car.frontAxle != null ? car.frontAxle.rightMesh : null,
                    car.rearAxle != null ? car.rearAxle.leftMesh : null,
                    car.rearAxle != null ? car.rearAxle.rightMesh : null
                };
            }
        }

        static List<Transform> CollectPrototypeVisualRoots(Transform carRoot)
        {
            var result = new HashSet<Transform>();

            foreach (var renderer in carRoot.GetComponentsInChildren<Renderer>(true))
            {
                Transform top = renderer.transform;

                while (top.parent != null && top.parent != carRoot)
                    top = top.parent;

                if (top.parent == carRoot)
                    result.Add(top);
            }

            return result.ToList();
        }

        static Mesh BuildSedanBodyMesh()
        {
            var sections = new[]
            {
                new BodySection(-2.18f, .76f, .38f, .68f, .78f),
                new BodySection(-1.78f, .91f, .38f, .80f, .92f),
                new BodySection(-1.18f, .94f, .38f, .88f, 1.32f),
                new BodySection(-.48f, .96f, .38f, .92f, 1.52f),
                new BodySection(.28f, .96f, .38f, .92f, 1.50f),
                new BodySection(.92f, .93f, .38f, .87f, 1.28f),
                new BodySection(1.48f, .90f, .40f, .79f, .93f),
                new BodySection(2.08f, .80f, .42f, .70f, .77f)
            };

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            const int ringCount = 10;

            for (int s = 0; s < sections.Length; s++)
            {
                BodySection section = sections[s];
                float w = section.halfWidth;
                float midY = Mathf.Lerp(section.lowerY, section.shoulderY, .55f);

                vertices.Add(new Vector3(-w * .82f, section.lowerY, section.z));
                vertices.Add(new Vector3(-w, midY, section.z));
                vertices.Add(new Vector3(-w * .96f, section.shoulderY, section.z));
                vertices.Add(new Vector3(-w * .72f, Mathf.Lerp(section.shoulderY, section.topY, .58f), section.z));
                vertices.Add(new Vector3(-w * .43f, section.topY, section.z));
                vertices.Add(new Vector3(w * .43f, section.topY, section.z));
                vertices.Add(new Vector3(w * .72f, Mathf.Lerp(section.shoulderY, section.topY, .58f), section.z));
                vertices.Add(new Vector3(w * .96f, section.shoulderY, section.z));
                vertices.Add(new Vector3(w, midY, section.z));
                vertices.Add(new Vector3(w * .82f, section.lowerY, section.z));
            }

            for (int s = 0; s < sections.Length - 1; s++)
            {
                int a = s * ringCount;
                int b = (s + 1) * ringCount;

                for (int i = 0; i < ringCount; i++)
                {
                    int next = (i + 1) % ringCount;
                    AddQuad(triangles, a + i, a + next, b + next, b + i);
                }
            }

            int rearCenter = vertices.Count;
            BodySection rear = sections[0];
            vertices.Add(new Vector3(0f, (rear.lowerY + rear.topY) * .5f, rear.z));

            int frontCenter = vertices.Count;
            BodySection front = sections[sections.Length - 1];
            vertices.Add(new Vector3(0f, (front.lowerY + front.topY) * .5f, front.z));

            for (int i = 0; i < ringCount; i++)
            {
                int next = (i + 1) % ringCount;

                triangles.Add(rearCenter);
                triangles.Add(next);
                triangles.Add(i);

                int start = (sections.Length - 1) * ringCount;
                triangles.Add(frontCenter);
                triangles.Add(start + i);
                triangles.Add(start + next);
            }

            var mesh = new Mesh { name = "T4_SedanBodyMesh" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return SaveMeshAsset(mesh, MeshRoot + "/T4_SedanBody.asset");
        }

        static Mesh BuildTorusMesh()
        {
            const int majorSegments = 32;
            const int minorSegments = 10;
            const float majorRadius = .225f;
            const float minorRadius = .026f;

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uv = new List<Vector2>();
            var triangles = new List<int>();

            for (int i = 0; i < majorSegments; i++)
            {
                float u = i / (float)majorSegments * Mathf.PI * 2f;
                Vector3 center = new Vector3(Mathf.Cos(u) * majorRadius, Mathf.Sin(u) * majorRadius, 0f);

                for (int j = 0; j < minorSegments; j++)
                {
                    float v = j / (float)minorSegments * Mathf.PI * 2f;
                    Vector3 normal = new Vector3(
                        Mathf.Cos(u) * Mathf.Cos(v),
                        Mathf.Sin(u) * Mathf.Cos(v),
                        Mathf.Sin(v));

                    vertices.Add(center + normal * minorRadius);
                    normals.Add(normal.normalized);
                    uv.Add(new Vector2(i / (float)majorSegments, j / (float)minorSegments));
                }
            }

            for (int i = 0; i < majorSegments; i++)
            {
                int ni = (i + 1) % majorSegments;

                for (int j = 0; j < minorSegments; j++)
                {
                    int nj = (j + 1) % minorSegments;

                    int a = i * minorSegments + j;
                    int b = ni * minorSegments + j;
                    int c = ni * minorSegments + nj;
                    int d = i * minorSegments + nj;

                    AddQuad(triangles, a, b, c, d);
                }
            }

            var mesh = new Mesh { name = "T4_SteeringWheelMesh" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            return SaveMeshAsset(mesh, MeshRoot + "/T4_SteeringWheel.asset");
        }

        static void CreateGlass(Transform parent)
        {
            CreateGlassPanel(parent, "Windshield", new Vector3(0f, 1.28f, .84f), new Vector3(1.50f, .58f, .025f), Quaternion.Euler(-24f, 0f, 0f));
            CreateGlassPanel(parent, "RearGlass", new Vector3(0f, 1.31f, -.94f), new Vector3(1.46f, .52f, .025f), Quaternion.Euler(24f, 0f, 0f));

            CreateGlassPanel(parent, "LeftFrontWindow", new Vector3(-.89f, 1.28f, .33f), new Vector3(.025f, .48f, .83f), Quaternion.Euler(0f, 0f, -5f));
            CreateGlassPanel(parent, "LeftRearWindow", new Vector3(-.89f, 1.28f, -.55f), new Vector3(.025f, .48f, .72f), Quaternion.Euler(0f, 0f, -5f));
            CreateGlassPanel(parent, "RightFrontWindow", new Vector3(.89f, 1.28f, .33f), new Vector3(.025f, .48f, .83f), Quaternion.Euler(0f, 0f, 5f));
            CreateGlassPanel(parent, "RightRearWindow", new Vector3(.89f, 1.28f, -.55f), new Vector3(.025f, .48f, .72f), Quaternion.Euler(0f, 0f, 5f));
        }

        static void CreateExteriorTrim(Transform parent)
        {
            CreateBox(parent, "FrontGrille", new Vector3(0f, .62f, 2.105f), new Vector3(1.05f, .23f, .035f), chrome);
            CreateBox(parent, "FrontLowerIntake", new Vector3(0f, .47f, 2.11f), new Vector3(1.40f, .12f, .03f), rubber);
            CreateBox(parent, "RearTrim", new Vector3(0f, .58f, -2.195f), new Vector3(1.20f, .12f, .03f), chrome);

            CreateBox(parent, "DoorTrimLeft", new Vector3(-.952f, .74f, -.10f), new Vector3(.025f, .055f, 1.55f), chrome);
            CreateBox(parent, "DoorTrimRight", new Vector3(.952f, .74f, -.10f), new Vector3(.025f, .055f, 1.55f), chrome);

            CreateBox(parent, "BumperFront", new Vector3(0f, .45f, 2.14f), new Vector3(1.67f, .11f, .07f), chrome);
            CreateBox(parent, "BumperRear", new Vector3(0f, .46f, -2.21f), new Vector3(1.67f, .11f, .07f), chrome);
        }

        static void CreateInterior(Transform parent, GameObject carRoot)
        {
            var interiorRoot = new GameObject("Interior");
            interiorRoot.transform.SetParent(parent, false);

            CreateBox(interiorRoot.transform, "Dashboard", new Vector3(0f, 1.00f, .55f), new Vector3(1.62f, .18f, .40f), leather);
            CreateBox(interiorRoot.transform, "DashboardTop", new Vector3(0f, 1.12f, .61f), new Vector3(1.57f, .065f, .32f), leather);
            CreateBox(interiorRoot.transform, "CenterConsole", new Vector3(0f, .69f, -.05f), new Vector3(.34f, .28f, 1.20f), leather);

            CreateSeat(interiorRoot.transform, "DriverSeat", new Vector3(-.46f, .69f, -.52f));
            CreateSeat(interiorRoot.transform, "PassengerSeat", new Vector3(.46f, .69f, -.52f));

            var steeringRoot = new GameObject("SteeringWheel_Realistic");
            steeringRoot.transform.SetParent(interiorRoot.transform, false);
            steeringRoot.transform.localPosition = new Vector3(-.47f, 1.06f, .22f);
            steeringRoot.transform.localRotation = Quaternion.Euler(68f, 0f, 0f);

            var mf = steeringRoot.AddComponent<MeshFilter>();
            mf.sharedMesh = BuildTorusMesh();
            steeringRoot.AddComponent<MeshRenderer>().sharedMaterial = leather;

            CreateBox(steeringRoot.transform, "SpokeHorizontal", Vector3.zero, new Vector3(.30f, .035f, .035f), chrome);
            CreateBox(steeringRoot.transform, "SpokeVertical", new Vector3(0f, -.09f, 0f), new Vector3(.04f, .19f, .035f), chrome);
            CreateBox(steeringRoot.transform, "Hub", Vector3.zero, new Vector3(.14f, .11f, .055f), leather);

            var steeringVisual = carRoot.GetComponent<SteeringWheelVisual>();
            if (steeringVisual != null)
                steeringVisual.steeringWheel = steeringRoot.transform;

            CreateBox(interiorRoot.transform, "InstrumentCluster", new Vector3(-.47f, 1.10f, .39f), new Vector3(.44f, .18f, .06f), rubber);
            CreateBox(interiorRoot.transform, "Infotainment", new Vector3(.22f, 1.08f, .47f), new Vector3(.42f, .20f, .055f), rubber);

            var seatAnchor = new GameObject("DriverSeatAnchor_T4").transform;
            seatAnchor.SetParent(interiorRoot.transform, false);
            seatAnchor.localPosition = new Vector3(-.46f, .82f, -.52f);

            var leftHand = new GameObject("LeftHandTarget").transform;
            leftHand.SetParent(steeringRoot.transform, false);
            leftHand.localPosition = new Vector3(-.17f, .12f, -.02f);
            leftHand.localRotation = Quaternion.Euler(0f, 0f, 18f);

            var rightHand = new GameObject("RightHandTarget").transform;
            rightHand.SetParent(steeringRoot.transform, false);
            rightHand.localPosition = new Vector3(.17f, .12f, -.02f);
            rightHand.localRotation = Quaternion.Euler(0f, 0f, -18f);

            var leftFoot = new GameObject("LeftFootTarget").transform;
            leftFoot.SetParent(interiorRoot.transform, false);
            leftFoot.localPosition = new Vector3(-.55f, .32f, .62f);

            var rightFoot = new GameObject("RightFootTarget").transform;
            rightFoot.SetParent(interiorRoot.transform, false);
            rightFoot.localPosition = new Vector3(-.24f, .32f, .62f);

            var rigAnchors = carRoot.GetComponent<DriverRigAnchors>();
            if (rigAnchors == null)
                rigAnchors = carRoot.AddComponent<DriverRigAnchors>();

            rigAnchors.seatAnchor = seatAnchor;
            rigAnchors.headAnchor = carRoot.transform.Find("DriverHeadAnchor");
            rigAnchors.leftHandTarget = leftHand;
            rigAnchors.rightHandTarget = rightHand;
            rigAnchors.leftFootTarget = leftFoot;
            rigAnchors.rightFootTarget = rightFoot;
        }

        static void CreateSeat(Transform parent, string name, Vector3 localPosition)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            var baseObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseObject.name = "SeatCushion";
            baseObject.transform.SetParent(root.transform, false);
            baseObject.transform.localScale = new Vector3(.56f, .18f, .68f);
            baseObject.GetComponent<Renderer>().sharedMaterial = leather;
            Object.DestroyImmediate(baseObject.GetComponent<Collider>());

            var back = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            back.name = "SeatBack";
            back.transform.SetParent(root.transform, false);
            back.transform.localPosition = new Vector3(0f, .48f, -.23f);
            back.transform.localRotation = Quaternion.Euler(82f, 0f, 0f);
            back.transform.localScale = new Vector3(.30f, .36f, .42f);
            back.GetComponent<Renderer>().sharedMaterial = leather;
            Object.DestroyImmediate(back.GetComponent<Collider>());

            var headrest = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            headrest.name = "Headrest";
            headrest.transform.SetParent(root.transform, false);
            headrest.transform.localPosition = new Vector3(0f, .88f, -.30f);
            headrest.transform.localScale = new Vector3(.20f, .18f, .15f);
            headrest.GetComponent<Renderer>().sharedMaterial = leather;
            Object.DestroyImmediate(headrest.GetComponent<Collider>());
        }

        static void ReplaceWheelVisuals(Transform realisticRoot, GameObject carRoot)
        {
            var car = carRoot.GetComponent<RealisticCarController>();
            if (car == null)
                return;

            if (car.frontAxle != null)
            {
                car.frontAxle.leftMesh = CreateWheelVisual(realisticRoot, "Wheel_FL_Realistic", car.frontAxle.leftCollider);
                car.frontAxle.rightMesh = CreateWheelVisual(realisticRoot, "Wheel_FR_Realistic", car.frontAxle.rightCollider);
            }

            if (car.rearAxle != null)
            {
                car.rearAxle.leftMesh = CreateWheelVisual(realisticRoot, "Wheel_RL_Realistic", car.rearAxle.leftCollider);
                car.rearAxle.rightMesh = CreateWheelVisual(realisticRoot, "Wheel_RR_Realistic", car.rearAxle.rightCollider);
            }
        }

        static Transform CreateWheelVisual(Transform parent, string name, WheelCollider collider)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);

            if (collider != null)
                root.transform.localPosition = collider.transform.localPosition;

            var tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tire.name = "Tire";
            tire.transform.SetParent(root.transform, false);
            tire.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            tire.transform.localScale = new Vector3(.34f, .115f, .34f);
            tire.GetComponent<Renderer>().sharedMaterial = rubber;
            Object.DestroyImmediate(tire.GetComponent<Collider>());

            var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Rim";
            rim.transform.SetParent(root.transform, false);
            rim.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            rim.transform.localScale = new Vector3(.21f, .12f, .21f);
            rim.GetComponent<Renderer>().sharedMaterial = chrome;
            Object.DestroyImmediate(rim.GetComponent<Collider>());

            for (int i = 0; i < 5; i++)
            {
                var spoke = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spoke.name = "Spoke";
                spoke.transform.SetParent(root.transform, false);
                spoke.transform.localRotation = Quaternion.Euler(i * 72f, 0f, 90f);
                spoke.transform.localScale = new Vector3(.035f, .12f, .19f);
                spoke.GetComponent<Renderer>().sharedMaterial = chrome;
                Object.DestroyImmediate(spoke.GetComponent<Collider>());
            }

            return root.transform;
        }

        static void RewireVehicleLights(Transform realisticRoot, GameObject carRoot)
        {
            var visual = carRoot.GetComponent<VehicleLightVisuals>();
            if (visual == null)
                return;

            Renderer leftHead = CreateLamp(realisticRoot, "Headlamp_L", new Vector3(-.58f, .78f, 2.11f), new Vector3(.42f, .15f, .035f), headlamp);
            Renderer rightHead = CreateLamp(realisticRoot, "Headlamp_R", new Vector3(.58f, .78f, 2.11f), new Vector3(.42f, .15f, .035f), headlamp);

            Renderer leftBrake = CreateLamp(realisticRoot, "BrakeLamp_L", new Vector3(-.61f, .79f, -2.19f), new Vector3(.38f, .16f, .035f), rearLamp);
            Renderer rightBrake = CreateLamp(realisticRoot, "BrakeLamp_R", new Vector3(.61f, .79f, -2.19f), new Vector3(.38f, .16f, .035f), rearLamp);

            Renderer leftFrontIndicator = CreateLamp(realisticRoot, "Indicator_FL", new Vector3(-.85f, .73f, 2.10f), new Vector3(.13f, .11f, .035f), indicator);
            Renderer rightFrontIndicator = CreateLamp(realisticRoot, "Indicator_FR", new Vector3(.85f, .73f, 2.10f), new Vector3(.13f, .11f, .035f), indicator);
            Renderer leftRearIndicator = CreateLamp(realisticRoot, "Indicator_RL", new Vector3(-.86f, .75f, -2.19f), new Vector3(.13f, .11f, .035f), indicator);
            Renderer rightRearIndicator = CreateLamp(realisticRoot, "Indicator_RR", new Vector3(.86f, .75f, -2.19f), new Vector3(.13f, .11f, .035f), indicator);

            Renderer leftReverse = CreateLamp(realisticRoot, "Reverse_L", new Vector3(-.33f, .74f, -2.20f), new Vector3(.17f, .10f, .035f), headlamp);
            Renderer rightReverse = CreateLamp(realisticRoot, "Reverse_R", new Vector3(.33f, .74f, -2.20f), new Vector3(.17f, .10f, .035f), headlamp);

            visual.headlamps = new[] { leftHead, rightHead };
            visual.brakeLamps = new[] { leftBrake, rightBrake };
            visual.reverseLamps = new[] { leftReverse, rightReverse };
            visual.leftIndicators = new[] { leftFrontIndicator, leftRearIndicator };
            visual.rightIndicators = new[] { rightFrontIndicator, rightRearIndicator };
        }

        static void RecreateMirrorDisplays(Transform realisticRoot, GameObject carRoot)
        {
            CreateMirrorDisplay(realisticRoot, carRoot, "LeftMirror", new Vector3(-.94f, 1.30f, .34f), new Vector3(.28f, .16f, 1f), Quaternion.Euler(0f, 18f, 0f));
            CreateMirrorDisplay(realisticRoot, carRoot, "RightMirror", new Vector3(.94f, 1.30f, .34f), new Vector3(.28f, .16f, 1f), Quaternion.Euler(0f, -18f, 0f));
            CreateMirrorDisplay(realisticRoot, carRoot, "CenterMirror", new Vector3(0f, 1.51f, .26f), new Vector3(.42f, .13f, 1f), Quaternion.identity);
        }

        static void CreateMirrorDisplay(Transform parent, GameObject carRoot, string name, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            var cameraObject = carRoot.transform.Find(name + "Camera");
            if (cameraObject == null)
                return;

            var camera = cameraObject.GetComponent<Camera>();
            if (camera == null || camera.targetTexture == null)
                return;

            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name + "_RealisticDisplay";
            q.transform.SetParent(parent, false);
            q.transform.localPosition = position;
            q.transform.localRotation = rotation;
            q.transform.localScale = scale;
            Object.DestroyImmediate(q.GetComponent<Collider>());

            string materialPath = MaterialRoot + "/" + name + "_Realistic.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                var shader = Shader.Find("Unlit/Texture");
                if (shader == null)
                    shader = Shader.Find("Standard");

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.mainTexture = camera.targetTexture;
            q.GetComponent<Renderer>().sharedMaterial = material;
        }

        static void RepositionCameras(GameObject carRoot)
        {
            Transform driver = carRoot.transform.Find("DriverCamera");
            if (driver != null)
            {
                driver.localPosition = new Vector3(-.47f, 1.38f, -.58f);

                var camera = driver.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.fieldOfView = 70f;
                    camera.nearClipPlane = .07f;
                    camera.allowHDR = true;
                    camera.cullingMask &= ~(1 << DriverHiddenLayer);
                }
            }

            Transform head = carRoot.transform.Find("DriverHeadAnchor");
            if (head != null)
                head.localPosition = new Vector3(-.47f, 1.38f, -.58f);

            Transform chase = carRoot.transform.Find("ChaseCamera");
            if (chase != null)
            {
                chase.localPosition = new Vector3(0f, 2.15f, -5.4f);

                var camera = chase.GetComponent<Camera>();
                if (camera != null)
                {
                    camera.fieldOfView = 62f;
                    camera.allowHDR = true;
                    camera.cullingMask |= 1 << DriverHiddenLayer;
                }
            }
        }

        static void CreateRoadside()
        {
            CreateSceneBox("Sidewalk_Left", new Vector3(-8.35f, .22f, 30f), new Vector3(1.35f, .20f, 110f), sidewalk);
            CreateSceneBox("Sidewalk_Right", new Vector3(8.35f, .22f, 30f), new Vector3(1.35f, .20f, 110f), sidewalk);

            CreateSceneBox("Curb_Left", new Vector3(-7.62f, .31f, 30f), new Vector3(.16f, .20f, 110f), curb);
            CreateSceneBox("Curb_Right", new Vector3(7.62f, .31f, 30f), new Vector3(.16f, .20f, 110f), curb);

            for (int z = -10; z <= 80; z += 18)
            {
                CreateStreetLight(new Vector3(-9.25f, .15f, z), false);
                CreateStreetLight(new Vector3(9.25f, .15f, z), true);
            }

            CreateSceneBox("DistantWall_Left", new Vector3(-25f, 1.2f, 35f), new Vector3(.35f, 2.4f, 120f), curb);
            CreateSceneBox("DistantWall_Right", new Vector3(25f, 1.2f, 35f), new Vector3(.35f, 2.4f, 120f), curb);

            var probeObject = new GameObject("T4_ReflectionProbe");
            probeObject.transform.position = new Vector3(0f, 2.4f, 30f);
            var probe = probeObject.AddComponent<ReflectionProbe>();
            probe.size = new Vector3(55f, 14f, 120f);
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.intensity = 1f;
            probe.boxProjection = true;
        }

        static void CreateStreetLight(Vector3 position, bool faceLeft)
        {
            var root = new GameObject("StreetLight_T4");
            root.transform.position = position;

            var poleObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleObject.name = "Pole";
            poleObject.transform.SetParent(root.transform, false);
            poleObject.transform.localPosition = new Vector3(0f, 2.8f, 0f);
            poleObject.transform.localScale = new Vector3(.055f, 2.8f, .055f);
            poleObject.GetComponent<Renderer>().sharedMaterial = pole;
            Object.DestroyImmediate(poleObject.GetComponent<Collider>());

            float direction = faceLeft ? -1f : 1f;
            var arm = CreateBox(root.transform, "Arm", new Vector3(direction * .55f, 5.35f, 0f), new Vector3(1.1f, .06f, .06f), pole);
            arm.transform.localRotation = Quaternion.identity;

            var lightObject = new GameObject("Lamp");
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = new Vector3(direction * 1.03f, 5.25f, 0f);
            lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.range = 18f;
            light.spotAngle = 68f;
            light.intensity = .75f;
            light.color = new Color(1f, .88f, .70f);
            light.shadows = LightShadows.Soft;
        }

        static void ConfigureLightingAndAtmosphere()
        {
            var sun = GameObject.Find("Sun");
            if (sun != null)
            {
                var light = sun.GetComponent<Light>();
                if (light != null)
                {
                    light.intensity = 1.05f;
                    light.color = new Color(1f, .94f, .86f);
                    light.shadows = LightShadows.Soft;
                    light.shadowStrength = .82f;
                }

                sun.transform.rotation = Quaternion.Euler(34f, -28f, 0f);
            }

            string skyboxPath = MaterialRoot + "/T4_ProceduralSky.mat";
            var skybox = AssetDatabase.LoadAssetAtPath<Material>(skyboxPath);

            if (skybox == null)
            {
                var shader = Shader.Find("Skybox/Procedural");
                if (shader != null)
                {
                    skybox = new Material(shader);
                    skybox.SetFloat("_SunSize", .035f);
                    skybox.SetFloat("_AtmosphereThickness", 1.0f);
                    skybox.SetColor("_SkyTint", new Color(.42f, .53f, .68f));
                    skybox.SetColor("_GroundColor", new Color(.28f, .30f, .33f));
                    AssetDatabase.CreateAsset(skybox, skyboxPath);
                }
            }

            if (skybox != null)
                RenderSettings.skybox = skybox;

            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = .85f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.64f, .72f, .80f);
            RenderSettings.fogStartDistance = 85f;
            RenderSettings.fogEndDistance = 210f;
        }

        static void SetLayerRecursively(Transform root, int layer)
        {
            if (root == null)
                return;

            root.gameObject.layer = layer;

            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i), layer);
        }

        static Renderer CreateLamp(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var g = CreateBox(parent, name, position, scale, material);
            return g.GetComponent<Renderer>();
        }

        static GameObject CreateGlassPanel(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation)
        {
            var g = CreateBox(parent, name, position, scale, glass);
            g.transform.localRotation = rotation;
            return g;
        }

        static GameObject CreateBox(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = position;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        static GameObject CreateMeshObject(Transform parent, string name, Mesh mesh, Material material)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            g.AddComponent<MeshFilter>().sharedMesh = mesh;
            g.AddComponent<MeshRenderer>().sharedMaterial = material;
            return g;
        }

        static GameObject CreateSceneBox(string name, Vector3 position, Vector3 scale, Material material)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.position = position;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = material;
            return g;
        }

        static Mesh SaveMeshAsset(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(mesh, path);
            return AssetDatabase.LoadAssetAtPath<Mesh>(path);
        }

        static void AddQuad(List<int> triangles, int a, int b, int c, int d)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);

            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }

        static void EnsureFolders()
        {
            EnsureFolder("Assets/RealDrivingAcademy", "Art");
            EnsureFolder(ArtRoot, "Vehicles");
            EnsureFolder(VehicleRoot, "Sedan");
            EnsureFolder(SedanRoot, "Meshes");
            EnsureFolder(SedanRoot, "Materials");
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        static void CreateMaterials()
        {
            paint = CreatePbr("T4_CarPaint", new Color(.035f, .11f, .22f), .86f, .80f);
            rubber = CreatePbr("T4_Rubber", new Color(.018f, .02f, .022f), 0f, .20f);
            chrome = CreatePbr("T4_Chrome", new Color(.54f, .57f, .60f), 1f, .84f);
            leather = CreatePbr("T4_Leather", new Color(.025f, .028f, .032f), 0f, .34f);
            headlamp = CreatePbr("T4_Headlamp", new Color(.84f, .88f, .92f), .12f, .88f);
            rearLamp = CreatePbr("T4_RearLamp", new Color(.40f, .012f, .008f), .05f, .72f);
            indicator = CreatePbr("T4_Indicator", new Color(.78f, .25f, .008f), .05f, .70f);

            road = CreatePbr("T4_Asphalt", new Color(.055f, .060f, .065f), 0f, .12f);
            curb = CreatePbr("T4_Curb", new Color(.42f, .43f, .44f), 0f, .16f);
            sidewalk = CreatePbr("T4_Sidewalk", new Color(.32f, .33f, .34f), 0f, .18f);
            pole = CreatePbr("T4_Pole", new Color(.12f, .13f, .14f), .70f, .42f);

            glass = CreateTransparentMaterial(
                "T4_Glass",
                new Color(.09f, .13f, .17f, .46f),
                .92f);
        }

        static Material CreatePbr(string name, Color color, float metallic, float smoothness)
        {
            string path = MaterialRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                var shader = Shader.Find("Standard");
                if (shader == null)
                    shader = Shader.Find("Universal Render Pipeline/Lit");

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;

            if (material.HasProperty("_Metallic"))
                material.SetFloat("_Metallic", metallic);

            if (material.HasProperty("_Glossiness"))
                material.SetFloat("_Glossiness", smoothness);

            if (material.HasProperty("_Smoothness"))
                material.SetFloat("_Smoothness", smoothness);

            return material;
        }

        static Material CreateTransparentMaterial(string name, Color color, float smoothness)
        {
            Material material = CreatePbr(name, color, 0f, smoothness);

            if (material.HasProperty("_Mode"))
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }

            return material;
        }
    }
}
#endif
