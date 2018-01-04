@echo off

call :BuildProject vs2017
call :BuildProject vs2015
call :BuildProject vs2013
call :BuildProject vs2012
call :BuildProject vs2010

goto :eof

:BuildProject
    SETLOCAL ENABLEDELAYEDEXPANSION
    REM set VERBOSE=/verbosity:diagnostic

    if "%1" == "vs2017" (
        set VS=15.0
        set TOOLS_VERSION=!VS!
        set DEFINES=VS2017DLL
    ) else if "%1" == "vs2015" (
        set VS=14.0
        set TOOLS_VERSION=!VS!
        set DEFINES=VS2015DLL
    ) else if "%1" == "vs2013" (
        set VS=12.0
        set TOOLS_VERSION=!VS!
        set DEFINES=VS2013DLL
    ) else if "%1" == "vs2012" (
        set TOOLS_VERSION=4.0
        set VS=11.0
        set DEFINES=VS2012DLL
    ) else if "%1" == "vs2010" (
        set TOOLS_VERSION=4.0
        set VS=10.0
        set DEFINES=VS2010DLL
    )

    if "%1" == "vs2017" (
        set DEVENV="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\devenv.com"
    ) else (
        set DEVENV="C:\Program Files (x86)\Microsoft Visual Studio %VS%\Common7\IDE\devenv.com"
    )

    set MSBUILD="c:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\amd64\MSBuild.exe"

    set SLN=%~dp0\Workspace\vs-tool.%1.sln

    set PROJECT_VSTOOL=%~dp0\Source\vs-tool.Build.CPPTasks\vs-tool.Build.CPPTasks.%1.csproj
    set PROJECT_SAMPLE_SIMPLE=%~dp0\Samples\simple\simple.%1.vcxproj

    set DEVENV_PROJECT_VSTOOL=%~dp0\Source\vs-tool.Build.CPPTasks\vs-tool.Build.CPPTasks.%1.csproj
    set DEVENV_PROJECT_SAMPLE_SIMPLE=%~dp0\Samples\simple\simple.%1.vcxproj

    if "%1" == "vs2010" (
        set DEVENV_PROJECT_VSTOOL=vs-tool.Build.CPPTasks.%1
        set DEVENV_PROJECT_SAMPLE_SIMPLE=simple.%1
    )

    echo ===========================================================================-----
    echo Compiling %1 [%VS%]
    echo ===========================================================================-----

    if not exist !DEVENV! (
        goto:eof
    )

    endlocal & (
        set _MSBUILD_VSTOOL=%MSBUILD% %PROJECT_VSTOOL% %VERBOSE% /property:Configuration=Release /p:Platform="AnyCPU" /p:DefineConstants="%DEFINES%" /t:Clean,Build /tv:%TOOLS_VERSION%
        set _MSBUILD_SAMPLE_SIMPLE=%MSBUILD% %PROJECT_SAMPLE_SIMPLE% %VERBOSE% /property:Configuration=Release /p:Platform="Emscripten" /t:Clean,Build /tv:%TOOLS_VERSION%

        set _DEVENV_VSTOOL=%DEVENV% %SLN% %VERBOSE% /Rebuild Release /Project %DEVENV_PROJECT_VSTOOL%
        set _DEVENV_SAMPLE_SIMPLE=%DEVENV% %SLN% %VERBOSE% /Rebuild "Release|Emscripten" /Project %DEVENV_PROJECT_SAMPLE_SIMPLE%
    )

    call :SET_EMSCRIPTEN_DIR D:\SDKs\emsdk\emscripten\1.37.26

    echo %_MSBUILD_VSTOOL%
    %_MSBUILD_VSTOOL%
    echo.
    echo ===========================================================================-----

    echo %_MSBUILD_SAMPLE_SIMPLE%
    %_MSBUILD_SAMPLE_SIMPLE%
    echo.
    echo ===========================================================================-----

    echo %_DEVENV_VSTOOL%
    %_DEVENV_VSTOOL%
    echo.
    echo ===========================================================================-----

    echo %_DEVENV_SAMPLE_SIMPLE%
    %_DEVENV_SAMPLE_SIMPLE%

    echo.
    echo ===========================================================================-----
    echo.
    goto:eof

:SET_EMSCRIPTEN_DIR
	if exist %1\emcc.bat set EMSCRIPTEN=%~dpfn1
	if exist %EMSCRIPTEN%\emcc.bat (
		set ERRORLEVEL=0
	) else (
		set ERRORLEVEL=1
	)
    goto:eof