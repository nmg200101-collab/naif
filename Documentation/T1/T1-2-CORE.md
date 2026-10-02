# RDA T1-2 Core System

Status: implemented and structurally verified.

## Added
- RdaBootstrap
- RdaGameManager
- RdaSceneLoader
- GameSessionState
- GameFlowState
- RdaSceneNames

## Design
- The core manager is created automatically before the first scene loads.
- The manager survives scene changes with DontDestroyOnLoad.
- Duplicate managers self-destruct.
- Scene changes are centralized and reject scenes that are not available in Build Settings.
- Session state supports guest and registered profiles.
- High-level app flow has explicit states for menu, training, test, garage, progress and settings.

## Acceptance checks
- No new package dependency.
- Runtime code is under Scripts/Core.
- No Editor-only API is referenced by runtime core scripts.
- Existing Stage 2 scripts were not moved or renamed.
- The existing generated training scene remains compatible because bootstrap is automatic.
