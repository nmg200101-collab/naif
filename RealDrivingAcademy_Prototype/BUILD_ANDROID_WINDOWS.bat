@echo off
set PROJECT=%~dp0
if "%UNITY%"=="" set UNITY=C:\Program Files\Unity\Hub\Editor\2022.3.62f1\Editor\Unity.exe
if not exist "%PROJECT%Builds\Android" mkdir "%PROJECT%Builds\Android"
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod RealDrivingAcademy.EditorTools.AndroidBuild.BuildApk -logFile "%PROJECT%Builds\Android\build.log"
echo APK: %PROJECT%Builds\Android\RealDrivingAcademy_Stage2.apk
pause
