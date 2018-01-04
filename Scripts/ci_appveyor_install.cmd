@echo off
echo Installing and setting up for [%PLATFORM%]...

if "%PLATFORM%" == "Emscripten" (
  %EMSDK%\emsdk update

  REM We install latest to get node, java, etc
  %EMSDK%\emsdk install latest
  %EMSDK%\emsdk activate latest

  set EMSCRIPTEN=%EMSCRIPTEN_ROOT%
)

if "%PLATFORM%" == "Android" (
  move %ANDROID_HOME%\tools %ANDROID_HOME%\old_tools >nul 2>&1
  %ANDROID_HOME%\old_tools\bin\sdkmanager.bat tools >nul 2>&1
  rmdir /s /q %ANDROID_HOME%\old_tools

  %ANDROID_HOME%\tools\bin\sdkmanager.bat platform-tools
  %ANDROID_HOME%\tools\bin\sdkmanager.bat build-tools;25.0.0
  %ANDROID_HOME%\tools\bin\sdkmanager.bat platforms;android-25
  %ANDROID_HOME%\tools\bin\sdkmanager.bat extras;android;m2repository
  %ANDROID_HOME%\tools\bin\sdkmanager.bat --channel=1 ndk-bundle
  %ANDROID_HOME%\tools\bin\sdkmanager.bat cmake;3.6.4111459
)