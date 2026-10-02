# RDA T1-4 Foundation UI

Status: implementation complete; Unity/device validation belongs to T1-GATE.

## Generated scenes
1. Welcome
2. MainMenu
3. TrainingMenu
4. TestMenu
5. Garage
6. Progress
7. Settings
8. Stage2_TrainingGround

## Navigation
Welcome:
- Continue as Guest
- Local Learner Profile

Main menu:
- Training
- Driving Test
- Garage
- Progress
- Settings
- End Session
- Quit

Training:
- Start Training Drive
- Back to Main Menu

Settings:
- Master volume
- Steering sensitivity
- Camera sensitivity
- Haptics
- Reset settings
- Back to Main Menu

Android/keyboard Back (Escape) returns from sub-scenes to Main Menu.

## Editor command
Real Driving Academy > T1 > Build Foundation Scenes

## Build integration
AndroidBuild now calls T1FoundationBuilder.BuildAllScenes automatically before building the APK.
