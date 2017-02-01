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
	set "MsBuildCppDir=%MsBuildRootDir%\Platforms"
)
if %n%==1 (
	set VsVersion=2012
	set "MsBuildCppDir=%MsBuildRootDir%\V110\Platforms"
)
if %n%==2 (
	set VsVersion=2013
	set "MsBuildCppDir=%MsBuildRootDir%\V120\Platforms"
)
if %n%==3 (
	set VsVersion=2015
	set "MsBuildCppDir=%MsBuildRootDir%\V140\Platforms"
)
if %n%==4 GOTO complete

if not exist "%MsBuildCppDir%" (
	set /a n=%n%+1
	goto loopVisualStudioVersion
)

Echo.Installing into Visual Studio %VsVersion%
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

Echo. Removing "%CppVersion%" (%VsVersion%) ...
rmdir "%MsBuildCppDir%\%CppVersion%" /s /q
if exist "%MsBuildCppDir%\%CppVersion%\Microsoft.Cpp.%CppVersion%.props" (
	Echo.Failed to remove "%MsBuildCppDir%\%CppVersion%"
)
echo.

if errorlevel 1 (
	echo.Problem with deleting.
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