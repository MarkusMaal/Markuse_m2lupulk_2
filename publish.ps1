#
# Note: If you're planning to have Linux support, please run generate_appimages.sh AFTER running this script.
#

function Generate-MacOSContainer {
	mkdir "Markuse mälupulk 2.0.app"
	mkdir "Markuse mälupulk 2.0.app/Contents"
	mkdir "Markuse mälupulk 2.0.app/Contents/MacOS"
	mkdir "Markuse mälupulk 2.0.app/Contents/Resources"
	$info_plist = @"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
	  <key>CFBundleName</key>
	  <string>FlashDrivePanel</string>
	  <key>CFBundleDisplayName</key>
	  <string>Mälupulga juhtpaneel</string>
	  <key>CFBundleIdentifier</key>
	  <string>ee.mas.FlashDrivePanel</string>
	  <key>CFBundleVersion</key>
	  <string>1.0.0</string>
	  <key>CFBundleExecutable</key>
	  <string>Markuse mälupulk 2.0</string>
	  <key>CFBundlePackageType</key>
	  <string>APPL</string>
	  <key>CFBundleIconFile</key>
	  <string>AppIcon</string>
	  <key>LSMinimumSystemVersion</key>
	  <string>10.13</string>
  </dict>
  </plist>
"@
	Set-Content -Path "Markuse mälupulk 2.0.app/Contents/Info.plist" -Value $info_plist
	Copy-Item -Path "../../AppIcon.icns" -Destination "Markuse mälupulk 2.0.app/Contents/Resources/AppIcon.icns"
	Move-Item -Path "libAvaloniaNative.dylib" -Destination "Markuse mälupulk 2.0.app/Contents/MacOS"
	Move-Item -Path "libHarfBuzzSharp.dylib" -Destination "Markuse mälupulk 2.0.app/Contents/MacOS"
	Move-Item -Path "libSkiaSharp.dylib" -Destination "Markuse mälupulk 2.0.app/Contents/MacOS"
	Move-Item -Path "Markuse mälupulk 2.0" -Destination "Markuse mälupulk 2.0.app/Contents/MacOS"
}

function Generate-LinuxAppImage {
	mkdir "MarkuseM2lupulk"
	mkdir "MarkuseM2lupulk/usr"
	mkdir "MarkuseM2lupulk/usr/bin"
	Move-Item "Markuse mälupulk 2.0" -Destination "MarkuseM2lupulk/usr/bin"
	Copy-Item "../../Markuse mälupulk 2.0/Resources/mas_flash.png" -Destination "MarkuseM2lupulk"
	$desktop = @"
[Desktop Entry]
Type=Application
Name=Markuse mälupulk 2.0
Exec=Markuse mälupulk 2.0
Icon=mas_flash
Categories=Utility;
"@
	$apprun = @"
#!/bin/bash
exec `$APPDIR/usr/bin/Markuse\ mälupulk\ 2.0
"@
	Set-Content -Path "MarkuseM2lupulk/MarkuseM2lupulk.desktop" -Value $desktop
	Set-Content -Path "MarkuseM2lupulk/AppRun" -Value $apprun
}

& .\logo.ps1

"win", "osx", "linux" | Foreach-Object {
	Write-Output " - Compiling $_-x64"
	dotnet publish "Markuse mälupulk 2.0" -r $_-x64 -c Release -o out/$_-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true
	Write-Output " - Compiling $_-arm64"
	dotnet publish "Markuse mälupulk 2.0" -r $_-arm64 -c Release -o out/$_-arm64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true
}

Write-Output "- Generating macOS package (Apple Silicon)"
Set-Location out
Set-Location "osx-arm64"
Generate-MacOSContainer
Write-Output "- Generating macOS package (Intel)"
Set-Location ".."
Set-Location "osx-x64"
Generate-MacOSContainer
Set-Location "../.."

Write-Output "- Generating AppImage (ARM64)"
Set-Location out
Set-Location "linux-arm64"
Generate-LinuxAppImage
Write-Output "- Generating AppImage (x64)"
Set-Location ".."
Set-Location "linux-x64"
Generate-LinuxAppImage
Set-Location "../.."