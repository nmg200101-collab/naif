#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RealDrivingAcademy.Vehicle;
using RealDrivingAcademy.Cockpit;
using RealDrivingAcademy.Mobile;
using RealDrivingAcademy.Training;
using RealDrivingAcademy.UI;
using RealDrivingAcademy.Audio;
using RealDrivingAcademy.World;

namespace RealDrivingAcademy.EditorTools
{
    public static class Stage2PrototypeBuilder
    {
        const string ScenePath = "Assets/RealDrivingAcademy/Scenes/Stage2_TrainingGround.unity";
        static Material roadMat, grassMat, bodyMat, darkMat, glassMat, lineMat, redMat, greenMat, yellowMat;

        [MenuItem("Real Driving Academy/Build Stage 2 Prototype Scene")]
        public static void BuildScene()
        {
            const string scenesDir = "Assets/RealDrivingAcademy/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir)) AssetDatabase.CreateFolder("Assets/RealDrivingAcademy", "Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateMaterials();
            CreateLighting();
            CreateWorld();
            var car = CreateTrainingCar();
            CreateTrainingSystems(car);
            CreateUI(car);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = car.gameObject;
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
            Debug.Log("Stage 2 prototype generated. Open the scene and press Play.");
        }

        static void CreateMaterials()
        {
            roadMat = Mat("RDA_Road", new Color(0.12f,0.13f,0.14f));
            grassMat = Mat("RDA_Ground", new Color(0.18f,0.28f,0.16f));
            bodyMat = Mat("RDA_CarBody", new Color(0.08f,0.20f,0.34f));
            darkMat = Mat("RDA_Dark", new Color(0.035f,0.04f,0.045f));
            glassMat = Mat("RDA_Glass", new Color(0.10f,0.18f,0.22f));
            lineMat = Mat("RDA_Line", new Color(0.85f,0.85f,0.80f));
            redMat = Mat("RDA_Red", new Color(0.8f,0.05f,0.03f));
            yellowMat = Mat("RDA_Yellow", new Color(0.95f,0.65f,0.03f));
            greenMat = Mat("RDA_Green", new Color(0.05f,0.65f,0.12f));
        }

        static Material Mat(string name, Color color)
        {
            string dir = "Assets/RealDrivingAcademy/Materials";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/RealDrivingAcademy", "Materials");
            string path = dir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("Standard");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
                m = new Material(shader) { color = color };
                AssetDatabase.CreateAsset(m, path);
            }
            else m.color = color;
            return m;
        }

        static void CreateLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.transform.rotation = Quaternion.Euler(38f, -35f, 0f);
            RenderSettings.ambientIntensity = 1.0f;
        }

        static void CreateWorld()
        {
            CreateCube("Ground", Vector3.zero, new Vector3(220f,0.2f,220f), grassMat, true);
            CreateCube("TrainingRoad", new Vector3(0f,0.12f,30f), new Vector3(15f,0.15f,110f), roadMat, true);
            CreateCube("CrossRoad", new Vector3(0f,0.13f,62f), new Vector3(70f,0.15f,16f), roadMat, true);
            // Lane markings
            for (int z = -15; z < 85; z += 8)
                CreateCube("LaneMark", new Vector3(0f,0.22f,z), new Vector3(0.18f,0.02f,3.5f), lineMat, false);
            CreateCube("StopLine", new Vector3(0f,0.23f,53f), new Vector3(13f,0.025f,0.35f), lineMat, false);
            // Parking bays
            for (int i = 0; i < 6; i++)
            {
                float x = -20f + i * 4f;
                CreateCube("ParkingLine", new Vector3(x,0.22f,17f), new Vector3(0.12f,0.02f,7f), lineMat, false);
            }
            CreateCube("ParkingBack", new Vector3(-10f,0.22f,13.5f), new Vector3(22f,0.02f,0.12f), lineMat, false);
            // Hill start ramp
            var ramp = CreateCube("HillStartRamp", new Vector3(24f,1.6f,20f), new Vector3(10f,0.4f,24f), roadMat, true);
            ramp.transform.rotation = Quaternion.Euler(-8f,0f,0f);
            // Slalom cones
            for (int i = 0; i < 8; i++) CreateCone(new Vector3((i%2==0?-2.4f:2.4f),0.55f,4f+i*7f));
            // Simple mountains / horizon
            for (int i=0;i<10;i++)
            {
                var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                m.name="MountainPlaceholder";
                m.transform.position = new Vector3(-80f+i*18f, 6f, 105f + Mathf.Sin(i)*8f);
                m.transform.localScale = new Vector3(14f+i%3*3f, 6f+i%4*2f, 14f+i%2*4f);
                m.GetComponent<Renderer>().sharedMaterial = grassMat;
                Object.DestroyImmediate(m.GetComponent<Collider>());
            }
            CreateTrafficLight(new Vector3(6.2f,0f,53f));
        }

        static void CreateCone(Vector3 p)
        {
            var cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cone.name = "TrainingCone";
            cone.transform.position = p;
            cone.transform.localScale = new Vector3(0.28f,0.55f,0.28f);
            cone.GetComponent<Renderer>().sharedMaterial = redMat;
        }

        static void CreateTrafficLight(Vector3 pos)
        {
            var root = new GameObject("TrafficLight"); root.transform.position = pos;
            CreateChildCube(root.transform,"Pole",new Vector3(0,2.2f,0),new Vector3(.18f,4.4f,.18f),darkMat);
            CreateChildCube(root.transform,"Box",new Vector3(0,4.2f,0),new Vector3(.75f,1.8f,.55f),darkMat);
            var r=CreateLamp(root.transform,"Red",new Vector3(0,4.75f,-.3f),redMat);
            var y=CreateLamp(root.transform,"Yellow",new Vector3(0,4.2f,-.3f),yellowMat);
            var g=CreateLamp(root.transform,"Green",new Vector3(0,3.65f,-.3f),greenMat);
            var c=root.AddComponent<TrafficLightController>(); c.redLamp=r; c.yellowLamp=y; c.greenLamp=g; c.redSeconds=8; c.greenSeconds=10;
            var zone=new GameObject("RedLightViolationZone"); zone.transform.position=new Vector3(0,1f,53f);
            var bc=zone.AddComponent<BoxCollider>(); bc.isTrigger=true; bc.size=new Vector3(13f,2f,1.0f);
            var rz=zone.AddComponent<RedLightViolationZone>(); rz.trafficLight=c;
        }

        static Renderer CreateLamp(Transform parent,string name,Vector3 worldPos,Material mat)
        {
            var s=GameObject.CreatePrimitive(PrimitiveType.Sphere); s.name=name; s.transform.SetParent(parent); s.transform.position=worldPos; s.transform.localScale=Vector3.one*.28f; s.GetComponent<Renderer>().sharedMaterial=mat; Object.DestroyImmediate(s.GetComponent<Collider>()); return s.GetComponent<Renderer>();
        }

        class CarRefs
        {
            public GameObject gameObject;
            public RealisticCarController car;
            public CarInputState input;
            public CarSystems systems;
            public DrivingViolationSystem violations;
            public Camera camera;
            public Transform steering;
            public Transform accelerator, brake, clutch, shifter;
            public Transform speedNeedle, rpmNeedle;
            public Transform leftWiper, rightWiper;
        }

        static CarRefs CreateTrainingCar()
        {
            var root = new GameObject("TrainingSedan");
            root.transform.position = new Vector3(0f,1.0f,-8f);
            var rb=root.AddComponent<Rigidbody>(); rb.mass=1450f; rb.interpolation=RigidbodyInterpolation.Interpolate; rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            var input=root.AddComponent<CarInputState>(); input.useKeyboardForPrototype=true; input.handbrake=true;
            var systems=root.AddComponent<CarSystems>();
            var car=root.AddComponent<RealisticCarController>();
            root.AddComponent<VehicleSafetyAssist>().car=car;
            var violations=root.AddComponent<DrivingViolationSystem>(); violations.car=car; violations.systems=systems;
            var cfg=ScriptableObject.CreateInstance<VehicleConfig>(); cfg.vehicleName="Training Sedan Stage 2"; cfg.mass=1450f; cfg.maxEngineTorque=245f; cfg.maxSteerAngle=34f; cfg.maxSpeedKph=165f; cfg.automaticTransmission=true;
            if (!AssetDatabase.IsValidFolder("Assets/RealDrivingAcademy/Configs")) AssetDatabase.CreateFolder("Assets/RealDrivingAcademy","Configs");
            const string cfgPath = "Assets/RealDrivingAcademy/Configs/TrainingSedan_Stage2.asset";
            if (AssetDatabase.LoadAssetAtPath<VehicleConfig>(cfgPath) != null) AssetDatabase.DeleteAsset(cfgPath);
            AssetDatabase.CreateAsset(cfg,cfgPath);
            car.config=cfg;

            // Body shell
            CreateChildCube(root.transform,"Floor",new Vector3(0,0.40f,0),new Vector3(1.9f,.22f,4.2f),bodyMat);
            CreateChildCube(root.transform,"Hood",new Vector3(0,0.80f,1.45f),new Vector3(1.85f,.35f,1.25f),bodyMat);
            CreateChildCube(root.transform,"Roof",new Vector3(0,1.65f,-.20f),new Vector3(1.75f,.14f,1.75f),bodyMat);
            CreateChildCube(root.transform,"RearBody",new Vector3(0,0.92f,-1.55f),new Vector3(1.85f,.50f,1.1f),bodyMat);
            CreateChildCube(root.transform,"Dash",new Vector3(0,1.05f,.50f),new Vector3(1.75f,.28f,.55f),darkMat);
            CreateChildCube(root.transform,"CenterConsole",new Vector3(0,0.72f,.05f),new Vector3(.42f,.38f,1.35f),darkMat);
            // Windshield / windows are visual panels (opaque placeholder until real model)
            var windshield=CreateChildCube(root.transform,"Windshield",new Vector3(0,1.45f,.88f),new Vector3(1.68f,.72f,.05f),glassMat); windshield.transform.rotation=Quaternion.Euler(-18f,0,0);
            CreateChildCube(root.transform,"LeftDoor",new Vector3(-.96f,1.02f,-.25f),new Vector3(.08f,.95f,1.70f),bodyMat);
            CreateChildCube(root.transform,"RightDoor",new Vector3(.96f,1.02f,-.25f),new Vector3(.08f,.95f,1.70f),bodyMat);
            CreateSeat(root.transform,new Vector3(-.46f,.77f,-.45f));
            CreateSeat(root.transform,new Vector3(.46f,.77f,-.45f));

            // Steering wheel
            var steerRoot=new GameObject("SteeringWheel").transform; steerRoot.SetParent(root.transform); steerRoot.localPosition=new Vector3(-.48f,1.05f,.22f); steerRoot.localRotation=Quaternion.Euler(68f,0,0);
            for(int i=0;i<12;i++){ var seg=GameObject.CreatePrimitive(PrimitiveType.Cube); seg.transform.SetParent(steerRoot); float a=i*30*Mathf.Deg2Rad; seg.transform.localPosition=new Vector3(Mathf.Cos(a)*.23f,Mathf.Sin(a)*.23f,0); seg.transform.localScale=new Vector3(.11f,.045f,.045f); seg.transform.localRotation=Quaternion.Euler(0,0,-i*30); seg.GetComponent<Renderer>().sharedMaterial=darkMat; Object.DestroyImmediate(seg.GetComponent<Collider>()); }
            CreateChildCube(steerRoot,"Hub",Vector3.zero,new Vector3(.18f,.18f,.08f),darkMat);
            var steerVisual=root.AddComponent<SteeringWheelVisual>(); steerVisual.input=input; steerVisual.steeringWheel=steerRoot; steerVisual.localRotationAxis=Vector3.forward; steerVisual.steeringWheelDegrees=900f;

            // Pedals
            var accel=CreateChildCube(root.transform,"Accelerator",new Vector3(-.20f,.39f,.62f),new Vector3(.12f,.30f,.06f),darkMat).transform;
            var brake=CreateChildCube(root.transform,"BrakePedal",new Vector3(-.42f,.39f,.62f),new Vector3(.15f,.25f,.06f),darkMat).transform;
            var clutch=CreateChildCube(root.transform,"ClutchPedal",new Vector3(-.66f,.39f,.62f),new Vector3(.15f,.25f,.06f),darkMat).transform;
            AddPedal(root,input,accel,PedalVisual.PedalType.Throttle); AddPedal(root,input,brake,PedalVisual.PedalType.Brake); AddPedal(root,input,clutch,PedalVisual.PedalType.Clutch);
            // Shifter
            var shifter=GameObject.CreatePrimitive(PrimitiveType.Cylinder); shifter.name="GearLever"; shifter.transform.SetParent(root.transform); shifter.transform.localPosition=new Vector3(.18f,.92f,-.05f); shifter.transform.localScale=new Vector3(.055f,.18f,.055f); shifter.GetComponent<Renderer>().sharedMaterial=darkMat; Object.DestroyImmediate(shifter.GetComponent<Collider>());
            var gearVis=root.AddComponent<GearSelectorVisual>(); gearVis.car=car; gearVis.lever=shifter.transform;

            // Wipers
            var lw=CreateChildCube(root.transform,"LeftWiper",new Vector3(-.43f,1.18f,.91f),new Vector3(.55f,.025f,.025f),darkMat).transform;
            var rw=CreateChildCube(root.transform,"RightWiper",new Vector3(.43f,1.18f,.91f),new Vector3(.55f,.025f,.025f),darkMat).transform;
            var w=root.AddComponent<WiperVisual>(); w.systems=systems; w.leftWiper=lw; w.rightWiper=rw;

            // Gauges
            var speedN=CreateGauge(root.transform,new Vector3(-.62f,1.08f,.18f),"SpeedNeedle");
            var rpmN=CreateGauge(root.transform,new Vector3(-.27f,1.08f,.18f),"RpmNeedle");
            var d1=root.AddComponent<DashboardNeedle>(); d1.car=car; d1.needle=speedN; d1.gauge=DashboardNeedle.Gauge.Speed; d1.maxValue=180f; d1.localAxis=Vector3.forward;
            var d2=root.AddComponent<DashboardNeedle>(); d2.car=car; d2.needle=rpmN; d2.gauge=DashboardNeedle.Gauge.Rpm; d2.maxValue=7000f; d2.localAxis=Vector3.forward;

            // Wheels
            var fl=Wheel(root.transform,"FL",new Vector3(-.86f,.52f,1.25f));
            var fr=Wheel(root.transform,"FR",new Vector3(.86f,.52f,1.25f));
            var rl=Wheel(root.transform,"RL",new Vector3(-.86f,.52f,-1.35f));
            var rr=Wheel(root.transform,"RR",new Vector3(.86f,.52f,-1.35f));
            car.frontAxle=new RealisticCarController.Axle{leftCollider=fl.collider,rightCollider=fr.collider,leftMesh=fl.mesh,rightMesh=fr.mesh,steering=true,powered=true,handbrake=false};
            car.rearAxle=new RealisticCarController.Axle{leftCollider=rl.collider,rightCollider=rr.collider,leftMesh=rl.mesh,rightMesh=rr.mesh,steering=false,powered=false,handbrake=true};

            CreateMirrors(root.transform);

            // Driver camera
            var camGO=new GameObject("DriverCamera"); camGO.transform.SetParent(root.transform); camGO.transform.localPosition=new Vector3(-.47f,1.46f,-.38f); camGO.transform.localRotation=Quaternion.identity;
            var cam=camGO.AddComponent<Camera>(); cam.fieldOfView=72f; cam.nearClipPlane=.04f; cam.tag="MainCamera";
            var cc=camGO.AddComponent<DriverCameraController>(); cc.driverSeatAnchor=camGO.transform; cc.yawLimit=105f; cc.pitchLimit=35f;
            // audio
            var aud=root.AddComponent<AudioSource>(); aud.spatialBlend=.6f;
            var pae=root.AddComponent<ProceduralEngineAudio>(); pae.car=car;

            systems.ignitionOn=false; systems.seatBeltFastened=false;
            return new CarRefs{gameObject=root,car=car,input=input,systems=systems,violations=violations,camera=cam,steering=steerRoot,accelerator=accel,brake=brake,clutch=clutch,shifter=shifter.transform,speedNeedle=speedN,rpmNeedle=rpmN,leftWiper=lw,rightWiper=rw};
        }

        static void AddPedal(GameObject root,CarInputState input,Transform t,PedalVisual.PedalType type)
        { var p=root.AddComponent<PedalVisual>(); p.input=input; p.pedal=t; p.pedalType=type; p.localAxis=Vector3.right; }

        struct WheelRef { public WheelCollider collider; public Transform mesh; }
        static WheelRef Wheel(Transform parent,string name,Vector3 lp)
        {
            var cgo=new GameObject(name+"Collider"); cgo.transform.SetParent(parent); cgo.transform.localPosition=lp; var wc=cgo.AddComponent<WheelCollider>(); wc.radius=.34f; wc.mass=22f; wc.suspensionDistance=.20f; var sp=wc.suspensionSpring; sp.spring=33000f; sp.damper=4600f; sp.targetPosition=.48f; wc.suspensionSpring=sp;
            var mesh=GameObject.CreatePrimitive(PrimitiveType.Cylinder); mesh.name=name+"Wheel"; mesh.transform.SetParent(parent); mesh.transform.localPosition=lp; mesh.transform.localRotation=Quaternion.Euler(0,0,90); mesh.transform.localScale=new Vector3(.34f,.11f,.34f); mesh.GetComponent<Renderer>().sharedMaterial=darkMat; Object.DestroyImmediate(mesh.GetComponent<Collider>());
            return new WheelRef{collider=wc,mesh=mesh.transform};
        }

        static Transform CreateGauge(Transform parent,Vector3 lp,string name)
        {
            var plate=GameObject.CreatePrimitive(PrimitiveType.Cylinder); plate.name=name+"Plate"; plate.transform.SetParent(parent); plate.transform.localPosition=lp; plate.transform.localRotation=Quaternion.Euler(90,0,0); plate.transform.localScale=new Vector3(.15f,.012f,.15f); plate.GetComponent<Renderer>().sharedMaterial=darkMat; Object.DestroyImmediate(plate.GetComponent<Collider>());
            var needle=CreateChildCube(plate.transform,name,new Vector3(0,.025f,0),new Vector3(.015f,.015f,.11f),redMat).transform; needle.localPosition=new Vector3(0,.03f,.04f); return needle;
        }

        static void CreateSeat(Transform parent,Vector3 lp)
        { CreateChildCube(parent,"SeatBase",lp,new Vector3(.60f,.20f,.70f),darkMat); var b=CreateChildCube(parent,"SeatBack",lp+new Vector3(0,.47f,-.25f),new Vector3(.60f,.80f,.18f),darkMat); b.transform.rotation=Quaternion.Euler(-8f,0,0); }

        static void CreateMirrors(Transform carRoot)
        {
            string dir = "Assets/RealDrivingAcademy/RenderTextures";
            if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/RealDrivingAcademy", "RenderTextures");
            CreateMirror(carRoot, "LeftMirror", new Vector3(-1.02f,1.28f,.38f), Quaternion.Euler(0,180f,0), new Vector3(-.88f,1.32f,.25f), Quaternion.Euler(0,15f,0), dir+"/LeftMirror.renderTexture");
            CreateMirror(carRoot, "RightMirror", new Vector3(1.02f,1.28f,.38f), Quaternion.Euler(0,180f,0), new Vector3(.88f,1.32f,.25f), Quaternion.Euler(0,-15f,0), dir+"/RightMirror.renderTexture");
            CreateMirror(carRoot, "CenterMirror", new Vector3(0f,1.53f,.22f), Quaternion.Euler(0,180f,0), new Vector3(0f,1.49f,.55f), Quaternion.identity, dir+"/CenterMirror.renderTexture");
        }

        static void CreateMirror(Transform carRoot,string name,Vector3 cameraLocalPos,Quaternion cameraLocalRot,Vector3 displayLocalPos,Quaternion displayLocalRot,string rtPath)
        {
            var rt=AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            if(rt==null){ rt=new RenderTexture(384,192,16,RenderTextureFormat.ARGB32){name=name+"RT"}; AssetDatabase.CreateAsset(rt,rtPath); }
            var cgo=new GameObject(name+"Camera"); cgo.transform.SetParent(carRoot); cgo.transform.localPosition=cameraLocalPos; cgo.transform.localRotation=cameraLocalRot;
            var cam=cgo.AddComponent<Camera>(); cam.fieldOfView=58f; cam.nearClipPlane=.08f; cam.farClipPlane=220f; cam.targetTexture=rt; cam.depth=-2;
            var q=GameObject.CreatePrimitive(PrimitiveType.Quad); q.name=name+"Display"; q.transform.SetParent(carRoot); q.transform.localPosition=displayLocalPos; q.transform.localRotation=displayLocalRot; q.transform.localScale=name=="CenterMirror"?new Vector3(.42f,.13f,1f):new Vector3(.27f,.18f,1f); Object.DestroyImmediate(q.GetComponent<Collider>());
            string matPath="Assets/RealDrivingAcademy/Materials/"+name+"_Material.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if(mat==null){ var shader=Shader.Find("Unlit/Texture"); if(shader==null) shader=Shader.Find("Standard"); mat=new Material(shader); AssetDatabase.CreateAsset(mat,matPath); }
            mat.mainTexture=rt; q.GetComponent<Renderer>().sharedMaterial=mat;
        }

        static void CreateTrainingSystems(CarRefs refs)
        {
            var lm=new GameObject("LessonManager").AddComponent<DrivingLessonManager>(); lm.car=refs.car; lm.input=refs.input; lm.systems=refs.systems;
            var speedZone=new GameObject("SpeedLimit50"); speedZone.transform.position=new Vector3(0,1f,0); var bc=speedZone.AddComponent<BoxCollider>(); bc.isTrigger=true; bc.size=new Vector3(14f,3f,12f); speedZone.AddComponent<SpeedLimitZone>().speedLimitKph=50f;
        }

        static void CreateUI(CarRefs refs)
        {
            var canvasGO=new GameObject("MobileCanvas"); var canvas=canvasGO.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; var scaler=canvasGO.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080); scaler.matchWidthOrHeight=.5f; canvasGO.AddComponent<GraphicRaycaster>();
            var es=new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<StandaloneInputModule>();

            // Top HUD
            var speed=Text(canvas.transform,"Speed",new Vector2(20,-20),new Vector2(280,72),32,TextAnchor.MiddleLeft,"0 km/h"); Anchor(speed.rectTransform,new Vector2(0,1),new Vector2(0,1));
            var rpm=Text(canvas.transform,"RPM",new Vector2(310,-20),new Vector2(280,72),28,TextAnchor.MiddleLeft,"850 RPM"); Anchor(rpm.rectTransform,new Vector2(0,1),new Vector2(0,1));
            var gear=Text(canvas.transform,"Gear",new Vector2(600,-20),new Vector2(120,72),34,TextAnchor.MiddleCenter,"N"); Anchor(gear.rectTransform,new Vector2(0,1),new Vector2(0,1));
            var score=Text(canvas.transform,"Score",new Vector2(-20,-20),new Vector2(240,72),30,TextAnchor.MiddleRight,"Score: 100"); Anchor(score.rectTransform,new Vector2(1,1),new Vector2(1,1));
            var lesson=Text(canvas.transform,"Lesson",new Vector2(0,-100),new Vector2(900,68),28,TextAnchor.MiddleCenter,"Driving lesson"); Anchor(lesson.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1));
            var warning=Text(canvas.transform,"Warning",new Vector2(0,-170),new Vector2(900,62),26,TextAnchor.MiddleCenter,""); Anchor(warning.rectTransform,new Vector2(.5f,1),new Vector2(.5f,1));
            var hud=canvasGO.AddComponent<PrototypeHUD>(); hud.car=refs.car; hud.violations=refs.violations; hud.lesson=Object.FindObjectOfType<DrivingLessonManager>(); hud.speedText=speed; hud.rpmText=rpm; hud.gearText=gear; hud.scoreText=score; hud.lessonText=lesson; hud.warningText=warning;

            // Steering wheel touch area
            var steering=Panel(canvas.transform,"Steering",new Vector2(60,50),new Vector2(420,420),new Color(0.08f,0.08f,0.08f,.55f)); Anchor(steering,new Vector2(0,0),new Vector2(0,0));
            var sw=steering.gameObject.AddComponent<TouchSteeringWheel>(); sw.input=refs.input; sw.wheelGraphic=steering;
            Text(steering,"SteerLabel",Vector2.zero,new Vector2(360,100),30,TextAnchor.MiddleCenter,"STEER");

            // Pedals
            CreatePedal(canvas.transform,"Brake",new Vector2(-380,55),new Vector2(210,300),"BRAKE",refs.input,TouchPedal.Pedal.Brake);
            CreatePedal(canvas.transform,"Throttle",new Vector2(-150,55),new Vector2(210,360),"GAS",refs.input,TouchPedal.Pedal.Throttle);
            // System buttons middle/right
            CreateSystemButton(canvas.transform,"IGN",new Vector2(-620,70),CockpitControl.ControlAction.Ignition,refs);
            CreateSystemButton(canvas.transform,"BELT",new Vector2(-620,160),CockpitControl.ControlAction.SeatBelt,refs);
            CreateSystemButton(canvas.transform,"LEFT",new Vector2(-620,250),CockpitControl.ControlAction.LeftIndicator,refs);
            CreateSystemButton(canvas.transform,"RIGHT",new Vector2(-480,250),CockpitControl.ControlAction.RightIndicator,refs);
            CreateSystemButton(canvas.transform,"LIGHT",new Vector2(-620,340),CockpitControl.ControlAction.LowBeams,refs);
            CreateSystemButton(canvas.transform,"WIPER",new Vector2(-480,340),CockpitControl.ControlAction.Wipers,refs);
            var hb=Button(canvas.transform,"Handbrake",new Vector2(-620,430),new Vector2(260,75),"HANDBRAKE"); Anchor(hb.GetComponent<RectTransform>(),new Vector2(1,0),new Vector2(1,0)); var hbc=hb.gameObject.AddComponent<HoldHandbrakeButton>(); hbc.input=refs.input; hbc.latchMode=true;

            // Gears
            CreateGear(canvas.transform,"R",new Vector2(-310,430),-1,refs.car);
            CreateGear(canvas.transform,"N",new Vector2(-210,430),0,refs.car);
            CreateGear(canvas.transform,"D",new Vector2(-110,430),1,refs.car);
        }

        static void CreatePedal(Transform parent,string name,Vector2 pos,Vector2 size,string label,CarInputState input,TouchPedal.Pedal pedal)
        { var b=Button(parent,name,pos,size,label); Anchor(b.GetComponent<RectTransform>(),new Vector2(1,0),new Vector2(1,0)); var p=b.gameObject.AddComponent<TouchPedal>(); p.input=input; p.pedal=pedal; }

        static void CreateSystemButton(Transform parent,string label,Vector2 pos,CockpitControl.ControlAction action,CarRefs refs)
        { var b=Button(parent,label,pos,new Vector2(125,72),label); Anchor(b.GetComponent<RectTransform>(),new Vector2(1,0),new Vector2(1,0)); var c=b.gameObject.AddComponent<CockpitControl>(); c.action=action; c.systems=refs.systems; c.car=refs.car; c.input=refs.input; UnityEventTools.AddPersistentListener(b.onClick,c.Activate); }

        static void CreateGear(Transform parent,string label,Vector2 pos,int gear,RealisticCarController car)
        { var b=Button(parent,"Gear"+label,pos,new Vector2(86,72),label); Anchor(b.GetComponent<RectTransform>(),new Vector2(1,0),new Vector2(1,0)); var g=b.gameObject.AddComponent<MobileGearButton>(); g.car=car; g.gear=gear; UnityEventTools.AddPersistentListener(b.onClick,g.Activate); }

        static Button Button(Transform parent,string name,Vector2 pos,Vector2 size,string label)
        { var rt=Panel(parent,name,pos,size,new Color(.08f,.10f,.12f,.78f)); var b=rt.gameObject.AddComponent<Button>(); Text(rt,label,Vector2.zero,size-Vector2.one*8,24,TextAnchor.MiddleCenter,label); return b; }

        static RectTransform Panel(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
        { var go=new GameObject(name); go.transform.SetParent(parent,false); var rt=go.AddComponent<RectTransform>(); rt.sizeDelta=size; rt.anchoredPosition=pos; var img=go.AddComponent<Image>(); img.color=color; return rt; }

        static Text Text(Transform parent,string name,Vector2 pos,Vector2 size,int fontSize,TextAnchor anchor,string content)
        { var go=new GameObject(name); go.transform.SetParent(parent,false); var rt=go.AddComponent<RectTransform>(); rt.sizeDelta=size; rt.anchoredPosition=pos; var t=go.AddComponent<Text>(); t.font=Resources.GetBuiltinResource<Font>("Arial.ttf"); t.fontSize=fontSize; t.alignment=anchor; t.color=Color.white; t.text=content; t.raycastTarget=false; return t; }

        static void Anchor(RectTransform rt,Vector2 min,Vector2 max){ rt.anchorMin=min; rt.anchorMax=max; rt.pivot=min; }

        static GameObject CreateCube(string name,Vector3 pos,Vector3 scale,Material mat,bool keepCollider)
        { var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.position=pos; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=mat; if(!keepCollider) Object.DestroyImmediate(g.GetComponent<Collider>()); return g; }

        static GameObject CreateChildCube(Transform parent,string name,Vector3 localPos,Vector3 localScale,Material mat)
        { var g=GameObject.CreatePrimitive(PrimitiveType.Cube); g.name=name; g.transform.SetParent(parent); g.transform.localPosition=localPos; g.transform.localRotation=Quaternion.identity; g.transform.localScale=localScale; g.GetComponent<Renderer>().sharedMaterial=mat; Object.DestroyImmediate(g.GetComponent<Collider>()); return g; }
    }
}
#endif
