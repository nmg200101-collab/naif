# RDA V50 — PHASE 0 Acceptance Checklist

Status: IN PROGRESS — DO NOT FREEZE until every mandatory item is verified in Unity/CI.

## Immutable baseline
- [x] Working branch: `rda-v50-clean-rebuild`
- [x] Baseline commit recorded: `db7d725a3426da5fa2147a2a1af0cc3299a84e33`
- [x] Unity version file: `2022.3.62f2`
- [ ] `RDA-V50-SAFE-BASE-KEEP-2026-09-07.zip` verified/preserved outside mutable foundation work

## Structure
- [x] Clean `Assets/RealDrivingAcademy/` foundation started
- [x] Core/Input/Vehicle/Camera/Academy/Save foundations present
- [x] Editor validation tool present
- [ ] Art/Audio/Materials/Prefabs/Scenes/Settings/Tests finalized

## Runtime foundation
- [x] Scene-flow API foundation
- [x] Save foundation with schema version and safe fallback
- [x] Quality/mobile baseline API
- [x] Input state foundation
- [x] Driving evaluation foundation
- [x] Six-mode camera foundation
- [x] Vehicle systems state foundation
- [ ] Existing `RDAVehicleController` audited and migrated/isolated
- [ ] Existing `RDABootstrap` audited and migrated/isolated

## Unity project / Android
- [x] `ProjectVersion.txt` identifies Unity 2022.3.62f2
- [x] `Packages/manifest.json` exists
- [ ] Complete ProjectSettings baseline present and reviewed
- [ ] Android package ID/version/orientation/API/IL2CPP/ARM64 baseline verified
- [ ] Input System activation verified in Player Settings
- [ ] URP baseline verified

## Acceptance gate
- [ ] Unity batch compile passes with zero compile errors
- [ ] Missing-script validation passes
- [ ] Required scenes exist and are enabled: RDA_Boot, RDA_Login, RDA_MainMenu
- [ ] Android development build succeeds
- [ ] Boot → Login/Guest → Main Menu smoke test passes
- [ ] Save/load smoke test passes
- [ ] No temporary primitive/content accepted as production asset

Only after every gate above passes may the checkpoint `RDA-V50-FOUNDATION-FROZEN` be created and PHASE 1 / V51 begin.
