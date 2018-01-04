If ($env:Platform -Match "Android") {
  Start-FileDownload 'https://dl.google.com/android/repository/sdk-tools-windows-3859397.zip'
  & 7z x android-tools.zip -o"%ANDROID_HOME%" | out-null

  if (Test-Path "C:\Users\appveyor\") {
    if (-not (Test-Path "C:\Users\appveyor\.android")) {
      New-Item -ItemType directory -Path "C:\Users\appveyor\.android"
    }
    Set-Variable -name RepositoryConfig -value "C:\Users\appveyor\.android\repositories.cfg"
    if (-not (Test-Path $RepositoryConfig)) {
      New-Item $RepositoryConfig -type file | out-null
    }
    for ( $i=0; $i -lt 30; $i++) {
      $response += "y`n"
    };
    $response | & $Env:ANDROID_HOME\tools\bin\sdkmanager --licenses | out-null
  }
}

If ($env:Platform -Match "Emscripten") {
  Start-FileDownload 'https://s3.amazonaws.com/mozilla-games/emscripten/releases/emsdk-portable-64bit.zip'
  & 7z x emsdk-portable-64bit.zip -o%EMSDK% | out-null
}