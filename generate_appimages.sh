#!/bin/bash

function NotLinux() {
    echo "This script will only run in Linux"
    exit
}

function NotX64() {
    echo "Unsupported CPU architecture"
    exit
}

function NotCompiled() {
    echo "You must run publish.ps1 first!"
    exit
}

# run checks
[ "$(uname)" = "Linux" ]
[ "$?" = "0" ] || NotLinux
[ "$(uname -m)" = "x86_64" ]
[ "$?" = "0" ] || NotX64
[ -f "out/linux-x64/MarkuseM2lupulk/AppRun" ]
[ "$?" = "0" ] || NotCompiled
# generate appimages
chmod +x out/linux-x64/MarkuseM2lupulk/AppRun
chmod +x out/linux-arm64/MarkuseM2lupulk/AppRun
chmod +x AppImagetool.AppImage
ARCH=x86_64 ./AppImageTool.AppImage ./out/linux-x64/MarkuseM2lupulk/ ./out/linux-x64/MarkuseM2lupulk.AppImage
ARCH=aarch64 ./AppImageTool.AppImage ./out/linux-arm64/MarkuseM2lupulk/ ./out/linux-arm64/MarkuseM2lupulk.AppImage
rm -rf ./out/linux-x64/MarkuseM2lupulk
rm -rf ./out/linux-arm64/MarkuseM2lupulk
