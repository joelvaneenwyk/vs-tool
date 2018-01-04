@echo off

set MINGW_BIN=C:\Program Files (x86)\mingw-w64\i686-7.2.0-posix-dwarf-rt_v5-rev1\mingw32\bin
set CLANG_BIN=C:\Program Files\LLVM\bin

call :SET_EMSCRIPTEN_DIR D:\SDKs\emsdk\emscripten\1.37.26

set DEVENV_VS2010="C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe"
set DEVENV_VS2012="C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\devenv.exe"
set DEVENV_VS2013="C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\devenv.exe"
set DEVENV_VS2015="C:\Program Files (x86)\Microsoft Visual Studio 14.0\Common7\IDE\devenv.exe"
set DEVENV_VS2017="c:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\IDE\devenv.exe"

goto:eof

:SET_EMSCRIPTEN_DIR
    if exist %1\emcc.bat set EMSCRIPTEN=%~dpfn1
    if exist %EMSCRIPTEN%\emcc.bat (
        set ERRORLEVEL=0
    ) else (
        set ERRORLEVEL=1
    )
    goto:eof