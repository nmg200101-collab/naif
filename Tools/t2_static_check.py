from pathlib import Path
import sys

ROOT = Path("RealDrivingAcademy_Prototype")
RDA = ROOT / "Assets" / "RealDrivingAcademy"

required = [
    RDA / "Editor" / "Stage2PrototypeBuilder.cs",
    RDA / "Editor" / "T2FinalValidator.cs",
    RDA / "Scripts" / "Vehicle" / "VehicleConfig.cs",
    RDA / "Scripts" / "Vehicle" / "CarInputState.cs",
    RDA / "Scripts" / "Vehicle" / "RealisticCarController.cs",
    RDA / "Scripts" / "Mobile" / "HoldHandbrakeButton.cs",
]

for path in required:
    if not path.exists():
        print("Missing:", path)
        sys.exit(1)

builder = (RDA / "Editor" / "Stage2PrototypeBuilder.cs").read_text(encoding="utf-8")
config = (RDA / "Scripts" / "Vehicle" / "VehicleConfig.cs").read_text(encoding="utf-8")
inputs = (RDA / "Scripts" / "Vehicle" / "CarInputState.cs").read_text(encoding="utf-8")
controller = (RDA / "Scripts" / "Vehicle" / "RealisticCarController.cs").read_text(encoding="utf-8")
handbrake = (RDA / "Scripts" / "Mobile" / "HoldHandbrakeButton.cs").read_text(encoding="utf-8")

checks = {
    # T2-5 stability
    "low spawn height": 'root.transform.position = new Vector3(0f,0.13f,-8f)' in builder,
    "chassis collider": 'root.AddComponent<BoxCollider>()' in builder,
    "stable rigidbody": 'rb.angularDrag=1.2f' in builder and 'rb.maxAngularVelocity=4f' in builder,
    "driver anchor": 'DriverHeadAnchor' in builder and 'cc.driverSeatAnchor=headAnchor' in builder,
    "camera clip": 'cam.nearClipPlane=.08f' in builder,
    "wheel force point": 'wc.forceAppPointDistance=.12f' in builder,
    "suspension tune": 'sp.spring=35000f' in builder and 'sp.damper=4800f' in builder,
    "friction tune": 'ff.stiffness=1.25f' in builder and 'sf.stiffness=1.35f' in builder,
    "correct COM": 'new Vector3(0f, 0.34f, -0.05f)' in config,
    "keyboard handbrake toggle": 'handbrake = !handbrake' in inputs,
    "hybrid throttle input": 'uiThrottle' in inputs and 'Mathf.Max(uiThrottle, keyboardThrottle)' in inputs,
    "hybrid brake input": 'uiBrake' in inputs and 'Mathf.Max(uiBrake, keyboardBrake)' in inputs,
    "hybrid steering input": 'uiSteering' in inputs and 'keyboardSteering' in inputs,
    "neutral start": 'public int selectedGear = 0;' in controller,

    # T2-6 drivability
    "signed speed": 'signedSpeedKph = Vector3.Dot(rb.velocity, transform.forward) * 3.6f' in controller,
    "speed adaptive steering": 'config.fullSteerUntilKph' in controller and 'config.reducedSteerAtKph' in controller,
    "brake cuts throttle": 'float effectiveThrottle = input.brake > 0.05f ? 0f : input.throttle;' in controller,
    "handbrake cuts drive torque": 'selectedGear == 0 || input.handbrake' in controller,
    "launch torque limiter": 'config.launchTorqueMultiplier' in controller and 'config.launchAssistEndKph' in controller,
    "reverse torque limiter": 'config.reverseTorqueMultiplier' in controller,
    "reverse speed cap": 'config.maxReverseSpeedKph' in controller,
    "coast drag": 'brake = config.coastBrakeTorque;' in controller,
    "safe direction change": 'config.safeDirectionChangeSpeedKph' in controller and 'public bool SetGear(int gear)' in controller,
    "handbrake ui sync": 'latched = input.handbrake;' in handbrake and 'latched = !input.handbrake;' in handbrake,
    "prototype T2-6 tuning": 'cfg.fullSteerUntilKph=18f' in builder and 'cfg.launchTorqueMultiplier=.62f' in builder,
    "drive diagnostic HUD": 'DriveDebug' in builder and 'driveDebugText' in (RDA / "Scripts" / "UI" / "PrototypeHUD.cs").read_text(encoding="utf-8"),
    "camera rig": 'VehicleCameraRig' in builder and (RDA / "Scripts" / "Vehicle" / "VehicleCameraRig.cs").exists(),
    "driver camera culling": 'cam.cullingMask &= ~(1 << 2)' in builder,
    "chase camera": 'ChaseCamera' in builder and 'cameraRig.chaseCamera=chaseCam' in builder,
    "camera toggle button": '"CAM"' in builder and 'cameraRig.ToggleCamera' in builder,
    "visual lamps": 'LeftHeadlamp' in builder and 'RightTailLamp' in builder,
    "vehicle light visuals": (RDA / "Scripts" / "Vehicle" / "VehicleLightVisuals.cs").exists() and 'VehicleLightVisuals' in builder,
    "functional indicators": 'LeftFrontIndicator' in builder and 'RightRearIndicator' in builder,
    "functional reverse lamps": 'LeftReverseLamp' in builder and 'RightReverseLamp' in builder,
    "low beam spotlights": 'LeftLowBeam' in builder and 'RightLowBeam' in builder,
    "hazard button": 'CockpitControl.ControlAction.Hazards' in builder and '"HAZ"' in builder,
    "system status HUD": 'SystemsStatus' in builder and 'systemStatusText' in (RDA / "Scripts" / "UI" / "PrototypeHUD.cs").read_text(encoding="utf-8"),
    "ABS runtime flag": 'public bool absActive;' in controller and 'absActive = true;' in controller,
    "TCS runtime flag": 'public bool tractionControlActive;' in controller and 'tractionControlActive = true;' in controller,
    "safe recovery reset": 'ResetDrivingInputs' in inputs and 'lastSafePosition' in (RDA / "Scripts" / "Vehicle" / "VehicleSafetyAssist.cs").read_text(encoding="utf-8"),
    "rollover recovery": 'upsideDownSecondsBeforeReset' in (RDA / "Scripts" / "Vehicle" / "VehicleSafetyAssist.cs").read_text(encoding="utf-8"),
    "reset button": '"RESET"' in builder and 'safetyAssist.ResetVehicle' in builder,
    "final editor validator": 'Build & Validate Vehicle Stage' in (RDA / "Editor" / "T2FinalValidator.cs").read_text(encoding="utf-8") and 'RDA T2 EDITOR VALIDATION PASSED' in (RDA / "Editor" / "T2FinalValidator.cs").read_text(encoding="utf-8"),
}

failed = [name for name, ok in checks.items() if not ok]
if failed:
    print("T2 checks FAILED:")
    for name in failed:
        print(" -", name)
    sys.exit(1)

for path in required:
    text = path.read_text(encoding="utf-8")
    if text.count("{") != text.count("}"):
        print("Unbalanced braces:", path)
        sys.exit(1)

print("RDA T2-10 static checks PASSED")
for name in checks:
    print(" -", name)
