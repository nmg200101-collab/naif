# RDA T1-1 Project Structure

Status: PASSED after repository verification.

## Stable Unity root
- RealDrivingAcademy_Prototype/

## Asset structure
- Assets/RealDrivingAcademy/Art
- Assets/RealDrivingAcademy/Audio
- Assets/RealDrivingAcademy/Characters
- Assets/RealDrivingAcademy/Data
- Assets/RealDrivingAcademy/Editor
- Assets/RealDrivingAcademy/Materials
- Assets/RealDrivingAcademy/Prefabs
- Assets/RealDrivingAcademy/Scenes
- Assets/RealDrivingAcademy/Scripts
- Assets/RealDrivingAcademy/UI
- Assets/RealDrivingAcademy/Vehicles

## Script domains
Existing:
- Audio
- Cockpit
- Mobile
- Training
- UI
- Vehicle
- World

Reserved for T1:
- Core
- Persistence

## Safety rules
1. Existing scripts are not moved during T1-1.
2. Runtime code stays outside Editor.
3. Editor-only builders stay under Assets/RealDrivingAcademy/Editor.
4. Scenes, prefabs, characters and vehicles have dedicated roots.
5. T1 work must not modify the main branch until the gate passes.
