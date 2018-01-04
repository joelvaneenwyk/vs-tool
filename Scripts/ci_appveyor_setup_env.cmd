@echo off
echo Setting up environment for [%PLATFORM%]...

if "%PLATFORM%" == "Emscripten" (
  call %EMSDK%\emsdk_env.bat
  set EMSCRIPTEN=%EMSCRIPTEN_ROOT%
  echo Emscripten: [%EMSCRIPTEN%]
)