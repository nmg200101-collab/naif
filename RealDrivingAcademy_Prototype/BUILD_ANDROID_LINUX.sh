#!/usr/bin/env bash
set -euo pipefail
UNITY="${UNITY:-/opt/Unity/Editor/Unity}"
PROJECT="$(cd "$(dirname "$0")" && pwd)"
mkdir -p "$PROJECT/Builds/Android"
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -executeMethod RealDrivingAcademy.EditorTools.AndroidBuild.BuildApk -logFile "$PROJECT/Builds/Android/build.log"
echo "APK: $PROJECT/Builds/Android/RealDrivingAcademy_Stage2.apk"
