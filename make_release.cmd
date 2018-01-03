@echo off
set RELEASE_ZIP=vs-tool-release.zip
set FILES=license.txt readme.txt Bin\10.0\vs-tool*.dll Bin\11.0\vs-tool*.dll Bin\12.0\vs-tool*.dll Bin\14.0\vs-tool*.dll Bin\15.0\vs-tool*.dll MSBuild\Android\* MSBuild\Clang\* MSBuild\MinGW\* MSBuild\NaCl\*
if exist %RELEASE_ZIP% del %RELEASE_ZIP%
"C:\Program Files\7-Zip\7z.exe" a %RELEASE_ZIP% %FILES%
