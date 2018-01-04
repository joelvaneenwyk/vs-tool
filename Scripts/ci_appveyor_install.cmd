if "%PLATFORM%" == "Emscripten" (
  %EMSDK%\emsdk update

  REM We install latest to get node, java, etc but we intentionally
  REM also install the version we know about so we can specify it
  REM manually and pass it in correctly.
  %EMSDK%\emsdk install latest
  %EMSDK%\emsdk install clang-e%EMSCRIPTEN_VERSION%-64bit
  %EMSDK%\emsdk install emscripten-%EMSCRIPTEN_VERSION%

  REM Set environment up for Emscripten by first setting up everything and then
  REM setting specific versions.
  %EMSDK%\emsdk activate latest
  %EMSDK%\emsdk activate clang-e%EMSCRIPTEN_VERSION%-64bit
  %EMSDK%\emsdk activate emscripten-%EMSCRIPTEN_VERSION%
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