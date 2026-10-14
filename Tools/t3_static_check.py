from pathlib import Path
import sys

ROOT = Path("RealDrivingAcademy_Prototype")
RDA = ROOT / "Assets" / "RealDrivingAcademy"

required = [
    RDA / "Scripts" / "Training" / "DrivingLessonManager.cs",
    RDA / "Scripts" / "Training" / "LessonCheckpointZone.cs",
    RDA / "Scripts" / "Training" / "LessonStopZone.cs",
    RDA / "Scripts" / "Training" / "DrivingViolationSystem.cs",
    RDA / "Scripts" / "Training" / "VehicleCollisionPenalty.cs",
    RDA / "Scripts" / "UI" / "PrototypeHUD.cs",
    RDA / "Editor" / "Stage2PrototypeBuilder.cs",
    RDA / "Editor" / "T2FinalValidator.cs",
]

for path in required:
    if not path.exists():
        print("Missing:", path)
        sys.exit(1)

lesson = (RDA / "Scripts" / "Training" / "DrivingLessonManager.cs").read_text(encoding="utf-8")
checkpoint = (RDA / "Scripts" / "Training" / "LessonCheckpointZone.cs").read_text(encoding="utf-8")
stop_zone = (RDA / "Scripts" / "Training" / "LessonStopZone.cs").read_text(encoding="utf-8")
hud = (RDA / "Scripts" / "UI" / "PrototypeHUD.cs").read_text(encoding="utf-8")
builder = (RDA / "Editor" / "Stage2PrototypeBuilder.cs").read_text(encoding="utf-8")
violations = (RDA / "Scripts" / "Training" / "DrivingViolationSystem.cs").read_text(encoding="utf-8")
collision = (RDA / "Scripts" / "Training" / "VehicleCollisionPenalty.cs").read_text(encoding="utf-8")

checks = {
    "T2 handoff marker": Path("Documentation/T2/RDA-T2-PASSED.md").exists(),
    "nine step lesson": all(x in lesson for x in [
        "FastenSeatBelt", "StartEngine", "SelectDrive", "ReleaseHandbrake",
        "UseLeftIndicator", "ReachSpeed", "PassCheckpoint",
        "StopInTargetZone", "SecureVehicle"
    ]),
    "checkpoint callback": "MarkCheckpoint" in lesson and "lesson.MarkCheckpoint" in checkpoint,
    "stop zone callback": "SetInsideStopZone" in lesson and "lesson.SetInsideStopZone" in stop_zone,
    "completion event": "OnLessonCompleted" in lesson,
    "progress API": "Progress01" in lesson and "CompletedSteps" in lesson,
    "checkpoint gate builder": "LessonCheckpointZone" in builder and "T3_GATE_01" in builder,
    "stop target builder": "LessonStopTarget" in builder and "LessonStopZone" in builder,
    "progress HUD": "lessonProgressText" in hud and "LessonProgress" in builder,
    "score grade": "public string Grade" in violations and "Grade:" in hud,
    "violation cooldown": "lastViolationTimes" in violations and "cooldownSeconds" in violations,
    "collision penalty": "VehicleCollisionPenalty" in builder and "ViolationType.Collision" in collision,
    "missing indicator penalty": "ViolationType.NoIndicator" in checkpoint and "checkpointZone.violations" in builder,
}

failed = [name for name, ok in checks.items() if not ok]
if failed:
    print("RDA T3-2 checks FAILED:")
    for name in failed:
        print(" -", name)
    sys.exit(1)

for path in required:
    text = path.read_text(encoding="utf-8")
    if text.count("{") != text.count("}"):
        print("Unbalanced braces:", path)
        sys.exit(1)

print("RDA T3-2 static checks PASSED")
for name in checks:
    print(" -", name)
