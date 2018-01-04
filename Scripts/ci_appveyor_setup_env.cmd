@echo off
echo Setting up environment for [%PLATFORM%]...

if "%PLATFORM%" == "Emscripten" (
  call %EMSDK%\emsdk_env.bat --global
)

echo Platform: [%PLATFORM%]
echo Project: [%PROJECT%]
echo Emscripten: [%EMSCRIPTEN%]