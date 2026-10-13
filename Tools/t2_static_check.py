from pathlib import Path
import sys

ROOT = Path("RealDrivingAcademy_Prototype")
RDA = ROOT / "Assets" / "RealDrivingAcademy"

required = [
    RDA / "Editor" / "Stage2PrototypeBuilder.cs",
    RDA / "Scripts" / "Vehicle" / "VehicleConfig.cs",
    RDA / "Scripts" / "Vehicle" / "CarInputState.cs",
    RDA / "Scripts" / "Vehicle" / "RealisticCarController.cs",
]

for path in required:
    if not path.exists():
        print("Missing:", path)
        sys.exit(1)

builder = (RDA / "Editor" / "Stage2PrototypeBuilder.cs").read_text(encoding="utf-8")
config = (RDA / "Scripts" / "Vehicle" / "VehicleConfig.cs").read_text(encoding="utf-8")
inputs = (RDA / "Scripts" / "Vehicle" / "CarInputState.cs").read_text(encoding="utf-8")
controller = (RDA / "Scripts" / "Vehicle" / "RealisticCarController.cs").read_text(encoding="utf-8")

checks = {
    "low spawn height": 'root.transform.position = new Vector3(0f,0.13f,-8f)' in builder,
    "chassis collider": 'root.AddComponent<BoxCollider>()' in builder,
    "stable rigidbody": 'rb.angularDrag=1.2f' in builder and 'rb.maxAngularVelocity=4f' in builder,
    "driver anchor": 'DriverHeadAnchor' in builder and 'cc.driverSeatAnchor=headAnchor' in builder,
    "camera clip": 'cam.nearClipPlane=.08f' in builder,
    "wheel force point": 'wc.forceAppPointDistance=.12f' in builder,
    "suspension tune": 'sp.spring=35000f' in builder and 'sp.damper=4800f' in builder,
    "friction tune": 'ff.stiffness=1.25f' in builder and 'sf.stiffness=1.35f' in builder,
    "correct COM": 'new Vector3(0f, 0.34f, -0.05f)' in config,
    "handbrake toggle": 'handbrake = !handbrake' in inputs,
    "neutral start": 'public int selectedGear = 0;' in controller,
}

failed = [name for name, ok in checks.items() if not ok]
if failed:
    print("T2-5 checks FAILED:")
    for name in failed:
        print(" -", name)
    sys.exit(1)

print("RDA T2-5 static checks PASSED")
for name in checks:
    print(" -", name)
