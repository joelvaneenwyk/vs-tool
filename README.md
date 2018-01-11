# vs-tool

[![License: Zlib](https://img.shields.io/badge/License-Zlib-lightgrey.svg)](https://opensource.org/licenses/Zlib)

<img src="http://www.gavpugh.com/img/vs-android/paneicon.png" align="right">

vs-tool is a Visual Studio plugin originally created by [Jukka Jylänki](http://clb.demon.fi) ([original vs-tool](https://github.com/juj/vs-tool)) to integrate external compiler/linker toolchains to the VS IDE. It supports Visual Studio 2010, 2012, 2013, 2015, and 2017. Earlier versions (pre-VS2010) lack the MSBuild integration with the C/C++ compilation systems. It's also intended to provide a collection of scripts and utilities to support integrated development of Android NDK C/C++ software under Microsoft Visual Studio.

This version of vs-tool differs in the following ways:

1. You can use these plugins without installing (aka. copying) it to the MSBuild directory. Instead you can use the _Import_ command to load them directly. This is done by using **$(MSBuildThisFileDirectory)** in most places instead of using **$(VCTargetsPath)**.
2. Samples are provided in the directory tree and are tested.
3. The source code for vs-android.Build.CPPTasks.Android (now renamed to vs-tool.Build.CPPTasks) is part of the repository.

This plugin is a derivation of the excellent work done by Gavin Pugh for the [vs-android](http://code.google.com/p/vs-android/) tool, and is therefore also licensed under the [Zlib license](http://en.wikipedia.org/wiki/Zlib_License).

Also, the free "Express" editions of Visual Studio are **not** supported. If you wish to use a free version of Visual Studio, use the new "Community Edition" of Visual Studio 2013. That version works fine with external plugins like this one.

## Features

* Compile and link Android C/C++ projects within Visual Studio.
* Integrated development, no makefiles. Works as another 'Platform' type.
* Android settings co-exist within the same VS projects as other platforms.
* Supports static library projects, and links them in if marked as project dependencies.
* Intellisense and 'External Dependencies' correctly pull in Android NDK headers.
* Cygwin install is not required.
* Over twice as fast as using ndk-build under Cygwin. For full rebuilds, and incremental changes.
* Ctrl-F7: compile single file functions, has no dependency checking wait.
* Applications build to .apk package files, and can be deployed and ran using vs-android.
* Supports selection of STL types, Arm Architecture, and API versions.
* Support for the ARM, MIPS and x86 toolchains.
* Seamlessly integrates MinGW (GCC), Clang and Emscripten toolchains to Visual Studio.
* Appears as a new Solution Platform to the Visual Studio projects. Does not destroy or interfere with existing Visual Studio functionality.
* Does not require setting up new solution/project files to target, can be added to existing solutions.
* Adapts the Project Properties dialog to show compiler/linker parameters specific to each tool.

**Important!** At the current stage, the vs-tool plugin should be considered experimental and hackish in nature. This means that when you update to a newer version of the plugin, solutions that use vs-tool CAN LOSE previously set configuration in the solution/project property pages, or even completely FAIL to load into Visual Studio. **You were warned**.

If you want to help with the project, please contribute with bug reports and patches.

## Installation

* [A guide to installing vs-android](https://github.com/gavinpugh/vs-android/blob/wiki/Installation.md)

## Usage

* [Compiling a simple Android app with vs-android](https://github.com/gavinpugh/vs-android/blob/wiki/HowTo_Samples.md)
* [Troubleshooting](https://github.com/gavinpugh/vs-android/blob/wiki/Troubleshooting.md)

## Tech Notes / Future Plans

* [Technical notes about implementation, and future plans for vs-android] (https://github.com/gavinpugh/vs-android/blob/wiki/TechNotesFuturePlans.md)

## Plugin Installation

For each *platform* you are interested in using (Android, Clang, Emscripten, MinGW and/or NaCl), do the following:

1. Copy the folder *platform*\ from this repository to C:\Program Files (x86)\MSBuild\Microsoft.Cpp\v4.0\Platforms\\*platform*\ (or the corresponding location where MSBuild exists on your system)
1. To enable an existing solution to be built via vs-tool, create a a platform for it from Configuration Manager -> Active Solution Platform -> New... -> *platform*.
1. To choose the toolchain to build with, edit the dropdown list at Project Properties -> General -> Platform Toolset.

## Setup for MinGW

MinGW is a toolchain to compile native Windows applications using a Windows port of the GCC compiler. With vs-tool you can build Visual Studio solutions using MinGW.

1. Install MinGW from [http://www.mingw.org/](http://www.mingw.org/). For x64 see here: [https://sourceforge.net/projects/mingw-w64/](https://sourceforge.net/projects/mingw-w64/)
1. Set MINGW_BIN environment variable to point to the bin\ directory where the MinGW toolchain executables (gcc.exe et al.) reside in.
1. Alternatively, in Visual Studio, go to Project Properties -> Toolchain Directories -> MinGW Compiler Path, and specify there the bin\ directory where the MinGW toolchain executables are located.

## Setup for Clang

[Clang](http://clang.llvm.org/) is a C/C++ frontend for the [LLVM Compiler Infrastructure](http://llvm.org/). vs-tool can also invoke Clang to build Visual Studio solutions. Note however, that Clang does not yet support building native Windows applications. Downloads available here: [http://releases.llvm.org/download.html](http://releases.llvm.org/download)

1. Obtain Clang. For example, follow this web page for instruction how to manually build Clang: [http://clang.llvm.org/get_started.html](http://clang.llvm.org/get_started.html)
1. Set CLANG_BIN environment variable to point to the directory where the Clang toolchain executables (clang.exe and the rest) reside in.
1. Alternatively, in Visual Studio, go to Project Properties -> Toolchain Directories -> Clang Compiler Path, and specify there the directory where the Clang toolchain executables are located.

## Setup for Emscripten (emcc)

Emscripten is a compiler/linker that allows you to compile C/C++ code to JavaScript. See [http://emscripten.org/](http://emscripten.org/). **vs-tool** can be used to build Visual Studio solutions to JavaScript.

1. Follow these instructions to set up Emscripten: [https://github.com/kripken/emscripten/wiki/Tutorial](https://github.com/kripken/emscripten/wiki/Tutorial)
1. Emscripten requires Clang to be set up. Follow the above instructions to set up Clang for vs-tool.
1. Set **EMSCRIPTEN** environment variable to point to the emscripten root directory (location of emcc.bat et al)
1. Alternatively, in Visual Studio, go to Project Properties -> Toolchain Directories -> Emcc Linker Path, and specify there the directory to the emscripten root directory.

## Resources

* Useful information on how tasks work in Visual Studio: [https://gist.github.com/retorillo/c55148f1f4cbc68f8572f70a9b949b62](MsBuild exploration for C++)
* [Debugging MSBuild](https://blogs.msdn.microsoft.com/visualstudio/2010/07/06/debugging-msbuild-script-with-visual-studio/)

## Version History

v0.964 - 28th December 2014

* Updated to support the r10d NDK.
* GCC 4.6 has been deprecated by Google. It is still supported, but the default GCC version vs-android uses, is now GCC 4.8.
* Added support for GCC 4.9.
* Added support for the "android-21" target (aka "Android 5.0", "Lollipop").
* Integrated "tlog" generation fix, thanks to "drew@thedunlops".
* NOTE: This version has "Ignore All Default Libraries" defaulting to "No" for GCC 4.8 and 4.9. Linker errors occur otherwise, when using any C++ features. GCC 4.6 still defaults this to "Yes", as it doesn't exhibit issues.

v0.963 - 19th July 2014

* Fix for issue with the "Windows 64-bit" NDK. The "TRACKER : error TRK0002: Failed to execute command" error.
  * Thanks to Ilya Konstantinov for the fix.
* Added warning message for using the new "(64-bit Target)" NDK.
  * Confusingly there are 32-bit and 64-bit Windows flavors of the "(32-bit Target)" and "(64-bit Target)" NDKs.
  * vs-android only supports the "(32-bit Target)" NDK currently.
* Also tested vs-android against the new 'r10' NDK. All toolchains and platforms are current.

v0.962 - 9th June 2014

* Added support for lone-installs of Visual Studio 2013.
  * Thanks to ted@lindenlab, and "mellean". I simplified the approach in the end, using project imports rather than duplicating code.
* Fixed pre/post-build steps not functioning correctly.
  * Thanks to "mellean" for the fix.
* Tested against new "r9d" NDK. Appears to work fine, please let me know on the "Issues" page if I am mistaken.

v0.961 - 24th February 2014

* Added Precompiled Header support. Thanks to help from Richard Forster.
* PCH support works similarly to the Win32/x64 compilers. You can enable "Create" for the compilation unit which should create the PCH, and "Use" for the project so have all files use that PCH.
* Projects mistakenly setting the 'ObjectFileName' to a directory, will now have an explanatory error.
* /libs/armeabi-v7a is now used when the architecture is set to "armeabi-v7a". This addresses an issue when submitting apk's to the Play Store. Thanks to Ilja Plutschouw.
* The default 'PlatformToolset' for newly added configurations is now correctly set. Thanks to Chuck Evans.
* The 'BrowseInformation' flag is now ignored for 'ClCompile'. Oft-imported setting when creating an Android platform on a Win32 project. Compilation fails if it was enabled. Thanks to C.Aragones for the headsup.

v0.96 - 4th January 2014

* Visual Studio 2013 has preliminary support now. It requires VS2012 to be installed alongside it. Unfortunately Microsoft have radically changed how MSBuild scripts implement new platforms. VS2012 express is probably fine.
* Fix for running on the `r9` NDKs. The minimum requirement is now NDK r9c.
* New NDK sees 4.3.3 and 4.7 GCC toolchains removed, and 4.8 added. vs-android now reflects these changes.
* GCC 4.8 however still seems to have issues when using the STL. It appears fine if you don't use STL. Suggestions welcome on how to address it, my Google-fu has failed me when looking up the link errors. Stick with GCC 4.6 if you need STL support.
* Added support for missing Android API targets: 12, 13, and 15 through 19. Goes up to Android 4.4 API now.

v0.951 - 22nd May 2013

* Fix for running on machines with a lone install of vs2010. I was doing all my testing on machines which have a dual vs2010 and vs2012 install. Apologies.

v0.95 - 22nd May 2013

* Visual Studio 2012 is now fully supported.
* There are separate installers for vs2010 and vs2012. Scripts are identical between version, via use of conditionals in MSBuild. However both use different supporting DLL files.
* .vcxproj and .sln files are cross-compatible between vs2012 and vs2010. Support for vs2010 will continue for vs-android in the foreseeable future.
* The x64 version of the NDK is now supported.
* Fix for Google breaking change to paths: "4.6.x-google" -> "4.6".
* The default GCC toolchain version has been changed from 4.4.3 to 4.6.
* The build scripts are now compliant with NDK r8e. Previous NDK versions are no longer supported.
* GCC 4.7 is now an available toolchain choice. However, it is not currently usable with the STL. Google state that 4.7 is still experimental. I'd welcome any suggested fix, if it does indeed work in ndk-build.

v0.94 - 24th July 2012

* Completely reworked the deploy and run portions of vs-android.
* Deploy has its own configuration pane: "Android Deployment", and saves these settings to the .user file.
* The Deployment process in general is much more robust. "Build->Cancel" also now works correctly when deploying.
* "Build->Deploy Solution/Deploy Project" now works on Android APK projects, to simply run the deploy step. Enable the "Deploy" checkbox for your project in the Solution "Configuration Manager" to enable this.
* You can now run a deployed app by using "Debug->Run" (F5)! It's a bit of a hacky method, but appears to work fine.
* NDK r8b was a breaking change for vs-android. This version now requires r8b or newer to be installed.
* Fixed breaking changes to the location of libstdc++ STL libraries.
* Fix for Google breaking change to x86 paths: "i686-android-linux" -> "i686-linux-android".
* Added support for the new GCC 4.6 toolchains.
* Added support for the new MIPS toolchains.
* Tested and hopefully fixed issues such that vs-android works fully with a 64-bit JDK install.
* Fix to make it possible to build projects in paths that contain spaces. Thanks to 'null77'.
* Added 'Forced Include File' to "C/C++ -> Advanced" property sheet. Thanks to 'danfilner'.
* Fix to make sure ARM5/ARM7 GCC flags are passed correctly to the compiler. Thanks to 'Drew Dunlop'.

v0.93 - 13th November 2011

* NDK 'r7' was a breaking change for vs-android. This version now requires 'r7' or newer to be installed.
* Fixed breaking changes to the location of STL libraries. Also fixed new linking issues introduced by STL changes.
* Removed support for defunct arm 4.4.0 toolset.
* Added support for android-14, Android API v4.0.
* Added support for the dynamic (shared) version of the GNU libstdc++ STL.
* Tested against newest JDK - jdk-7u1-windows-i586.
* Added support for building assembly files. '.s' and '.asm' extensions will be treated as assembly code.
* Correct passing of ANT_OPTS to the Ant Build step. Thanks to 'mark.bozeman'.
* Corrected expected apk name for release builds.
* Added to Ant Build property page; the ability to add extra flags to the calls to adb.
* Fixed bug with arm arch preprocessor defines not making it onto the command line.
* Fixed bad quote removal on paths, in the C# code. Thanks to 'hoesing@kleinbottling'.
* Removed stlport project from Google Code - This was an oversight by Google in the `r6` NDK, prebuilt is back again.
* Updated sample projects to work with 'R15' SDK tools.

v0.92 - 3rd August 2011

* Fixed jump-to-line clickable errors. Reworked code to use regular expressions instead. Tried a number of different compiler/linker warnings and errors and all seems to be good
* Default warning level is now 'Normal Warnings', instead of 'Disable All Warnings'. Whoops!
* Fixed rtti-related warnings when compiling .c files with 'Enable All Warnings (-Wall)', turned on.

v0.91 - 2nd August 2011

* Windows debugger is now usable. Fixed 'CLRSupport' error.
* Error checking to ensure the 32-bit JDK is used.
* Added JVM Heap options to Ant Build step. Initial and Maximum sizes are able to be set there now.
* Added asafhel...@gmail's 'clickable errors from compiler' C# code.
* Modified clickable errors code to also work with #include errors, which specify the column number too.
* Added clickable error support to linker too.

v0.9 - 20th July 2011

* Major update, hence the skip in numbers. Closing in on a v1.0 release.
* Verified working with Android NDK r5b, r5c, and 'r6'.
* Much of vs-android functionality moved from MSBuild script to C# tasks. Similar approach now to Microsoft's existing Win32 setup.
* Dependency checking rewritten to use tracking log files.
* Dependency issues fixed, dependency checking also now far quicker.
* Android Property sheets now completely replace the Microsoft ones, no more rafts of unused sheets.
* Property sheets populated with many options. Switches are no longer hard-coded within vs-android script.
* STL support added. Choice between 'None', 'Minimal', 'libstdc++', and 'stlport'.
* Support for x86 compilation with 'r6' NDK.
* Full support for v7-a arm architecture, as well as the existing v5.
* Support for Android API directories other than just 'android-9'.
* Separated support for 'dynamic libraries' and 'applications'. Applications build to apk files.
* Response files used in build, no more command-line length limitations.
* Deploy and run within Visual Studio, adb is now invoked by vs-android.
* 'Echo command lines' feature fixed.
* All support SDK/libs (NDK, SDK, Ant, JDK) are okay living in directories with spaces in them now.
* All bugs logged within Google Code are addressed.

v0.21 - 10th Feb 2011

* Fixed issues with the 'ant build' step.
* Added a sensible error message if the NDK envvar isn't set, or is set incorrectly.

v0.2 - 1st Feb 2011

* Changed default preprocessor symbols to work the same way Microsoft's stuff does. Should fix any issues with intellisense as well.
* Added support for scanning header dependencies.

v0.1 - 30th Jan 2011

* Initial version.
* All major functionality present, barring header dependency checking.

## License

(c) 2015 <a href="https://plus.google.com/116894316812948433768?rel=author" rel="author">Gavin Pugh</a>

vs-android and vs-tool are released under the zlib license.
http://en.wikipedia.org/wiki/Zlib_License