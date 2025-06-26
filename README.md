![Markuse mälupulk logo](Markuse%20mälupulk%202.0/Resources/mas_flash_wide.png)

# Markuse mälupulk 2.0

## Süsteeminõuded

* [VALIKULINE] Kõigi funktsioonide kasutamiseks Markuse asjad nõuetele vastav arvuti kehtiva Verifile 2.0 räsiga.
* dotnet-sdk-9.0 või muu ühilduv lahendus kompileerimiseks
* Microsoft Powershell devToolide kasutamiseks
* Avalonia UI pluginad vastava IDE jaoks (Microsoft Visual Studio või JetBrains Rider)
* Operatsioonsüsteem: Windows, macOS või Linux (muid süsteeme ei toetata)
* Nõutav mäluruum:
  * Mälupulgale kopeeritavad failid: ~450 MiB
  * Arenduskeskkond: ~2GiB

## Powershelli paigaldamine
Kui dotnet-sdk olemas, saab käivitada käsureal: `dotnet tool install --global PowerShell`

Vastasel korral, saate lugeda [Microsofti saidilt](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell?view=powershell-7.5), kuidas seda teha.

## Rakenduse ehitamine

* Kustuta out/ kaust kui see juba eksisteerib'
  * rm -rf out/
* Käivita publish.ps1 PowerShellis - see genereerib automaatselt binraarid kõigi toetatud operatsioonsüsteemide jaoks
  * pwsh publish.ps1
* Kui kasutad Linuxit - käivita ka generate_appimages.sh (toimib ainult x64 protsessori arhitektuuril)
  * chmod +x generate_appimages.sh
  * ./generate_appimages.sh

## Rakenduse juurutamine

Kopeeri out/ sisu mälupulgal kausta "Mälupulga juhtpaneel". Struktuur peaks olema järgmine:
  * / (juurkaust)
    * Mälupulga juhtapneel/
      * linux-arm64/
        * MarkuseM2lupulk.AppImage (kui kasutasite generate_appimages.sh skripti)
      * linux-x64/
        * MarkuseM2lupulk.AppImage (kui kasutasite generate_appimages.sh skripti)
      * osx-arm64/
        * Markuse mälupulk 2.0.app/ (genereeritakse automaatselt)
          * Contents/
            * MacOS/
              * libAvaloniaNative.dylib
              * libHarfBuzzSharp.dylib
              * libSkiaSharp.dylib
              * Markuse mälupulk 2.0
            * Resources/
              * AppIcon.icns
            * Info.plist
      * osx-x64/
        * Markuse mälupulk 2.0.app/ (genereeritakse automaatselt)
          * Contents/
            * MacOS/
              * libAvaloniaNative.dylib
              * libHarfBuzzSharp.dylib
              * libSkiaSharp.dylib
              * Markuse mälupulk 2.0
            * Resources/
              * AppIcon.icns
            * Info.plist
      * win-arm64
        * Markuse mälupulk 2.0.exe
      * win-x64
        * Markuse mälupulk 2.0.exe
    * autorun.exe (Windows x64 binraar)
    * autorun.ico (Markuse mälupulk 2.0/mas_flash.ico)
    * autorun.inf
    * Markuse mälupulk.exe (Windows x64 binraar)

NB: Soovitatav on kustutada pärast juurutamist .pdb failid, sest need ei ole rakenduse jooksutamiseks vajalikud. Säilitage need failid ainult siis, kui soovite ehitatud rakendust siluda.