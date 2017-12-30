@echo off

call :BuildProject vs2017
call :BuildProject vs2015
call :BuildProject vs2013
call :BuildProject vs2012
call :BuildProject vs2010

goto :eof

:BuildProject
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

if "%1" == "vs2010" (
	set CONFIG="Release"
) else (
	set CONFIG="Release"
)

if exist %DEVENV% (
	echo Compiling %1 [%VS%]

	set SLN=%~dp0/workspace/%1/vs-android.Build.CPPTasks.Android.sln
	set PRJ=%~dp0/workspace/%1/vs-android.Build.CPPTasks.Android.csproj

	%DEVENV% %SLN% %VERBOSE% /Build %CONFIG% /Project %PRJ%
) else (
	echo Can't find compiler for %1
)

goto:eof