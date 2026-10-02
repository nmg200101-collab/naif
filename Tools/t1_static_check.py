from pathlib import Path
import sys

ROOT = Path("RealDrivingAcademy_Prototype")
RDA = ROOT / "Assets" / "RealDrivingAcademy"

required = [
    ROOT / "ProjectSettings" / "ProjectVersion.txt",
    ROOT / "Packages" / "manifest.json",
    RDA / "Scripts" / "Core" / "RdaBootstrap.cs",
    RDA / "Scripts" / "Core" / "RdaGameManager.cs",
    RDA / "Scripts" / "Core" / "RdaSceneLoader.cs",
    RDA / "Scripts" / "Core" / "RdaSceneNames.cs",
    RDA / "Scripts" / "Persistence" / "RdaPersistenceService.cs",
    RDA / "Scripts" / "Persistence" / "RdaSaveData.cs",
    RDA / "Scripts" / "Persistence" / "RdaUserSettings.cs",
    RDA / "Scripts" / "Persistence" / "RdaProgressData.cs",
    RDA / "Scripts" / "UI" / "RdaMenuActions.cs",
    RDA / "Scripts" / "UI" / "RdaSettingsPanel.cs",
    RDA / "Editor" / "T1FoundationBuilder.cs",
    RDA / "Editor" / "AndroidBuild.cs",
]

missing = [str(p) for p in required if not p.exists()]
if missing:
    print("Missing required T1 files:")
    for item in missing:
        print(" -", item)
    sys.exit(1)

version = (ROOT / "ProjectSettings" / "ProjectVersion.txt").read_text(encoding="utf-8")
if "2022.3.62f1" not in version:
    print("Unexpected Unity version")
    sys.exit(1)

runtime_scripts = list((RDA / "Scripts").rglob("*.cs"))
bad_editor_refs = []
for script in runtime_scripts:
    text = script.read_text(encoding="utf-8")
    if "using UnityEditor" in text or "UnityEditor." in text:
        bad_editor_refs.append(str(script))

if bad_editor_refs:
    print("Editor API found in runtime scripts:")
    for item in bad_editor_refs:
        print(" -", item)
    sys.exit(1)

scene_names = (RDA / "Scripts" / "Core" / "RdaSceneNames.cs").read_text(encoding="utf-8")
for name in ["Welcome", "MainMenu", "TrainingMenu", "TestMenu", "Garage", "Progress", "Settings", "Stage2_TrainingGround"]:
    if f'"{name}"' not in scene_names:
        print("Missing scene name:", name)
        sys.exit(1)

builder = (RDA / "Editor" / "T1FoundationBuilder.cs").read_text(encoding="utf-8")
for token in ["BuildAllScenes", "EditorBuildSettings.scenes", "Stage2PrototypeBuilder.BuildScene"]:
    if token not in builder:
        print("Foundation builder is missing:", token)
        sys.exit(1)

android = (RDA / "Editor" / "AndroidBuild.cs").read_text(encoding="utf-8")
if "T1FoundationBuilder.BuildAllScenes()" not in android:
    print("Android build is not wired to T1 scene generation")
    sys.exit(1)

for script in required:
    if script.suffix != ".cs":
        continue
    text = script.read_text(encoding="utf-8")
    if text.count("{") != text.count("}"):
        print("Unbalanced braces:", script)
        sys.exit(1)

print("RDA T1 static checks PASSED")
print("Runtime C# files checked:", len(runtime_scripts))
