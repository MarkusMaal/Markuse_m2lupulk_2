"win", "osx", "linux" | Foreach-Object {
	Write-Output " - Compiling $_-x64"
	dotnet publish "Markuse mälupulk 2.0" -r $_-x64 -c Release -o out/$_-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true
	Write-Output " - Compiling $_-arm64"
	dotnet publish "Markuse mälupulk 2.0" -r $_-arm64 -c Release -o out/$_-arm64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:PublishTrimmed=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true
}