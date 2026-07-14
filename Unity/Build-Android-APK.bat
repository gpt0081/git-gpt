@echo off
set UNITY=C:\Program Files\Unity\Hub\Editor\6000.3.19f1\Editor\Unity.exe
set PROJECT=%~dp0
"%UNITY%" -batchmode -quit -projectPath "%PROJECT%" -executeMethod Fjordfall.EditorTools.FjordfallSetup.BuildAndroidApk -logFile "%PROJECT%Builds\android-build.log"
pause
