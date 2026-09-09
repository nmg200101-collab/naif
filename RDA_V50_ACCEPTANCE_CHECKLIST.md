# RDA V50 — PHASE 0 Acceptance Checklist

Status: **IN PROGRESS — DO NOT FREEZE** until every mandatory item is verified in Unity.

## Immutable baseline
- [x] Working branch: `rda-v50-clean-rebuild`
- [x] Baseline commit recorded: `db7d725a3426da5fa2147a2a1af0cc3299a84e33`
- [x] Unity target version file: `2022.3.62f2`
- [ ] `RDA-V50-SAFE-BASE-KEEP-2026-09-07.zip` independently verified/preserved outside mutable work

## Repository integrity
- [x] Canonical `Assets/RealDrivingAcademy/` root
- [x] Runtime / Editor / Tests assemblies separated
- [x] Stable `.meta` GUIDs committed for tracked Unity assets
- [x] Unity-generated directories excluded by `.gitignore`
- [x] Old incompatible V50 runtime controller/bootstrap isolated from canonical source
- [x] Deterministic foundation installer present

## Structure
- [x] Core/Input/Vehicle/Camera/Academy/UI/Save foundations present
- [x] Editor validation/build tools present
- [x] Installer defines Art/Vehicles, Art/Environment, Art/UI, Audio, Materials, Prefabs/Vehicles, Prefabs/World, Prefabs/UI, Scenes, Settings and Tests structure
- [ ] Generated structure materialized and inspected in Unity locally

## Runtime foundation
- [x] Scene-flow API with missing-scene guard
- [x] Save foundation with schema version and safe fallback
- [x] Quality/mobile baseline API
- [x] Input-state foundation with clamping/reset
- [x] Driving-evaluation foundation
- [x] Six-mode camera foundation
- [x] Vehicle-system state hooks
- [x] WheelCollider/Ackermann controller foundation
- [x] Boot/session initialization foundation
- [x] Login Guest -> Main Menu -> Login smoke-navigation code

## Test / validation foundation
- [x] EditMode tests authored
- [x] Missing-script validator authored
- [x] Required-scene/order validator authored
- [x] Android package/API/ARM64 validator authored
- [x] Local 62f1 compatibility validation path documented
- [x] Strict 62f2 validation path retained

## Unity project / Android — must be proven locally
- [ ] Project imports with zero compile errors
- [ ] Foundation installer completes without errors
- [ ] Required scenes exist and are enabled in exact order: RDA_Boot, RDA_Login, RDA_MainMenu
- [ ] Missing-script validation passes
- [ ] Boot -> Login -> Continue As Guest -> Main Menu -> Back To Login smoke test passes
- [ ] Save/load smoke test passes
- [ ] EditMode tests pass
- [ ] Android package ID `com.realdrivingacademy.rda` verified
- [ ] Android minimum API 26 verified
- [ ] IL2CPP + ARM64 verified
- [ ] Android development APK build succeeds when Android Build Support is available
- [ ] Input-system/player setting baseline reviewed
- [ ] URP/graphics baseline reviewed

## Final strict freeze gate
- [ ] Unity 2022.3.62f2 strict validation passes
- [ ] Zero compile errors on 2022.3.62f2
- [ ] Tests pass on final target editor
- [ ] Android build succeeds on final target editor/CI
- [ ] No temporary/primitive content is accepted as production asset

Only after every mandatory final gate passes may checkpoint **`RDA-V50-FOUNDATION-FROZEN`** be created and V51 begin.
