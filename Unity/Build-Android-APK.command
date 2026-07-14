#!/bin/bash
set -e
UNITY="/Applications/Unity/Hub/Editor/6000.3.19f1/Unity.app/Contents/MacOS/Unity"
PROJECT="$(cd "$(dirname "$0")" && pwd)"
"$UNITY" -batchmode -quit -projectPath "$PROJECT" -executeMethod Fjordfall.EditorTools.FjordfallSetup.BuildAndroidApk -logFile "$PROJECT/Builds/android-build.log"
