# RDA V50 — Local Validation Path

Status: PRE-FREEZE. This document does not mark V50 as accepted.

## Supported local editor
- Preferred/final: Unity 2022.3.62f2 LTS.
- Compatibility-only local validation: Unity 2022.3.62f1 LTS.
- Opening on 62f1 is for local development validation only. Final V50 freeze still requires strict validation on 62f2.

## First local open
1. Clone/download branch `rda-v50-clean-rebuild` into a NEW folder. Do not overwrite older RDA projects or the immutable backup archive.
2. Open the new copy in Unity Hub.
3. Wait for package import/compilation to finish.
4. Console must have zero compile errors before continuing.
5. Run `RDA > Foundation > Install or Refresh V50 Baseline`.
6. Run `RDA > Validate > Local Foundation`.
7. Open `RDA_Boot` and enter Play Mode. Expected flow: Boot -> Login -> Continue As Guest -> Main Menu -> Back To Login.
8. Run EditMode tests from Test Runner.
9. Android APK validation is optional until Android Build Support is present. Use `RDA > Build > Android Development Validation` when available.

## Freeze rule
Do NOT create or claim `RDA-V50-FOUNDATION-FROZEN` until strict 2022.3.62f2 validation, tests and Android build all pass.
