from pathlib import Path
import sys

ROOT = Path("RealDrivingAcademy_Prototype")
RDA = ROOT / "Assets" / "RealDrivingAcademy"

required = [
    RDA / "Editor" / "Stage2PrototypeBuilder.cs",
    RDA / "Editor" / "T4RealismBuilder.cs",
    RDA / "Editor" / "T4FinalValidator.cs",
    RDA / "Scripts" / "Vehicle" / "VehicleVisualLayer.cs",
    RDA / "Scripts" / "Vehicle" / "DriverRigAnchors.cs",
    RDA / "Scripts" / "Vehicle" / "RealisticCarController.cs",
]

for path in required:
    if not path.exists():
        print("Missing:", path)
        sys.exit(1)

builder = (RDA / "Editor" / "Stage2PrototypeBuilder.cs").read_text(encoding="utf-8")
realism = (RDA / "Editor" / "T4RealismBuilder.cs").read_text(encoding="utf-8")
validator = (RDA / "Editor" / "T4FinalValidator.cs").read_text(encoding="utf-8")
visual = (RDA / "Scripts" / "Vehicle" / "VehicleVisualLayer.cs").read_text(encoding="utf-8")
anchors = (RDA / "Scripts" / "Vehicle" / "DriverRigAnchors.cs").read_text(encoding="utf-8")

checks = {
    "T4 environment hook": "T4RealismBuilder.ApplyEnvironment();" in builder,
    "T4 vehicle hook": "T4RealismBuilder.Apply(car.gameObject);" in builder,
    "visual separation": '"VisualModel_Old"' in realism and '"Sedan_Realistic"' in realism,
    "smooth body mesh": "BuildSedanBodyMesh" in realism and "T4_SedanBody.asset" in realism,
    "PBR paint": "T4_CarPaint" in realism and '"_Metallic"' in realism,
    "transparent glass": "CreateTransparentMaterial" in realism and "T4_Glass" in realism,
    "realistic wheels": "Wheel_FL_Realistic" in realism and "Wheel_RR_Realistic" in realism,
    "steering torus": "BuildTorusMesh" in realism and "T4_SteeringWheel.asset" in realism,
    "real cockpit": '"Interior"' in realism and '"Dashboard"' in realism,
    "driver anchors": "DriverRigAnchors" in realism and "leftHandTarget" in anchors and "rightHandTarget" in anchors,
    "linear color": "PlayerSettings.colorSpace = ColorSpace.Linear" in realism,
    "procedural sky": "Skybox/Procedural" in realism,
    "reflection probe": "T4_ReflectionProbe" in realism,
    "roadside detail": "Sidewalk_Left" in realism and "StreetLight_T4" in realism,
    "prototype mountains removed": 'x.name == "MountainPlaceholder"' in realism,
    "visual layer runtime switch": "UseRealisticVisuals" in visual and "UsePrototypeVisuals" in visual,
    "final Unity gate": "Build & Validate Realism Stage" in validator and "RDA T4 EDITOR VALIDATION PASSED" in validator,
}

failed = [name for name, ok in checks.items() if not ok]
if failed:
    print("RDA T4 checks FAILED:")
    for name in failed:
        print(" -", name)
    sys.exit(1)

for path in required:
    text = path.read_text(encoding="utf-8")
    if text.count("{") != text.count("}"):
        print("Unbalanced braces:", path)
        sys.exit(1)

print("RDA T4 static checks PASSED")
for name in checks:
    print(" -", name)
