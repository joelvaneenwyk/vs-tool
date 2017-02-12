@echo off

REM This will bail if we don't have admin privs.
net session >nul 2>&1
    if errorLevel 1 (
		Echo.This batch file needs to be run with administrative privileges. Since it copies files to the \Program Files directory.
		Pause
		Goto cleanup
)

REM On 64-bit machines, Visual Studio 2013/2015 and MsBuild are in the (x86) directory. So try that last.
if exist "%ProgramFiles%" set "MsBuildRootDir=%ProgramFiles%\MSBuild\Microsoft.Cpp\v4.0"
if exist "%ProgramFiles(x86)%" set "MsBuildRootDir=%ProgramFiles(x86)%\MSBuild\Microsoft.Cpp\v4.0"

echo.%MsBuildRootDir%

set /a n=0

if "%1" == "2012" set /a n=1
if "%1" == "2013" set /a n=2
if "%1" == "2015" set /a n=3

:loopVisualStudioVersion

if %n%==0 (
	set VsVersion=2010
	set VsVersionAlt=10
	set "MsBuildCppDir=%MsBuildRootDir%\Platforms"
)
if %n%==1 (
	set VsVersion=2012
	set VsVersionAlt=11
	set "MsBuildCppDir=%MsBuildRootDir%\V%VsVersionAlt%0\Platforms"
)
if %n%==2 (
	set VsVersion=2013
	set VsVersionAlt=12
	set "MsBuildCppDir=%MsBuildRootDir%\V%VsVersionAlt%0\Platforms"
)
if %n%==3 (
	set VsVersion=2015
	set VsVersionAlt=14
	set "MsBuildCppDir=%MsBuildRootDir%\V%VsVersionAlt%0\Platforms"
)
if %n%==4 GOTO complete

if not exist "%MsBuildCppDir%" (
	set /a n=%n%+1
	goto loopVisualStudioVersion
)

echo.
echo.============================================================
echo. Installing into Visual Studio %VsVersion%
echo.============================================================
echo.

set /a i=0
:loop
if %i%==0 set CppVersion=Clang
if %i%==1 set CppVersion=Emscripten
if %i%==2 set CppVersion=MinGW
if %i%==3 set CppVersion=NaCl
if %i%==4 (
	set /a n=%n%+1
	if not "%1" == "" goto complete
	goto loopVisualStudioVersion
)

if exist "%MsBuildCppDir%\%CppVersion%\Microsoft.Cpp.%CppVersion%.props" (
	echo. "%CppVersion%" Cpp MsBuild toolset already exists. Removing old version.
	rmdir "%MsBuildCppDir%\%CppVersion%" /s /q
	if exist "%MsBuildCppDir%\%CppVersion%\Microsoft.Cpp.%CppVersion%.props" (
		echo. Failed to remove directory!
		goto cleanup
	)
	echo.
)

if not exist "%MsBuildCppDir%\%CppVersion%" mkdir "%MsBuildCppDir%\%CppVersion%"

echo. Installing %CppVersion% MSBuild files...
cd /d %~dp0
xcopy "MSBuild\%CppVersion%\*.*" "%MsBuildCppDir%\%CppVersion%" /E /Q
if %CppVersion%==Emscripten xcopy "Bin\%VsVersionAlt%.0\*.dll" "%MsBuildCppDir%\%CppVersion%" /E /Q

if errorlevel 1 (
	echo. Problem with copying!
	Pause
	goto cleanup
)

SET /a i=%i%+1
GOTO loop

:complete
echo.
echo.Done! You will need to close and re-open existing instances of Visual Studio.
Pause

:cleanup