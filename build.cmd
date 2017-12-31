@echo off

call :BuildProject vs2017
call :BuildProject vs2015
call :BuildProject vs2013
call :BuildProject vs2012
call :BuildProject vs2010

goto :eof

:BuildProject
SETLOCAL ENABLEDELAYEDEXPANSION
REM set VERBOSE=verbosity:diagnostic

if "%1" == "vs2017" (
	set VS=15.0
) else if "%1" == "vs2015" (
	set VS=14.0
) else if "%1" == "vs2013" (
	set VS=12.0
) else if "%1" == "vs2012" (
	set VS=11.0
) else if "%1" == "vs2010" (
	set VS=10.0
)

if "%1" == "vs2017" (
	set DEVENV="C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\devenv.com"
) else (
	set DEVENV="C:\Program Files (x86)\Microsoft Visual Studio %VS%\Common7\IDE\devenv.com"
)

set SLN=%~dp0\workspace\vs-tool.%1.sln

if "%1" == "vs2010" (
	set PRJ1=vs-tool.Build.CPPTasks.%1
	set PRJ2=sample.%1
) else (
	set PRJ1=%~dp0\vs-tool.Build.CPPTasks\vs-tool.Build.CPPTasks.%1.csproj
	set PRJ2=%~dp0\sample\sample.%1.vcxproj
)

set OPTIONS_VSTOOL=!SLN! %VERBOSE% /Rebuild Release /Project !PRJ1!
set OPTIONS_EMSCRIPTEN=!SLN! %VERBOSE% /Rebuild "Release|Emscripten" /Project !PRJ2!

echo.
echo ===========================================================================
echo.

if exist %DEVENV% (
	echo Compiling %1 [%VS%]
	echo %DEVENV% %OPTIONS_VSTOOL%
	%DEVENV% %OPTIONS_VSTOOL%
	echo %DEVENV% %OPTIONS_EMSCRIPTEN%
	%DEVENV% %OPTIONS_EMSCRIPTEN%
) else (
	echo Can't find compiler for %1
)

echo.
echo ===========================================================================
echo.

ENDLOCAL
goto:eof