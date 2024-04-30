# ValorantPorting [![Discord](https://discord.com/api/guilds/866821077769781249/widget.png?style=shield)](https://discord.com/invite/valorant3d)
A free and open-source tool created to automate the Valorant porting process to Blender and Unreal Engine

# Installation

### Requirements
* [Blender](https://www.blender.org/download/)
* [Visual C++ Distributables x64](https://docs.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170)
* [.NET 6.0 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime)
> ⚠️ ValorantPorting requires **.NET 6.0 or later** to work, download it from the link above and select the **Windows Desktop x64** version.

## ValorantPorting Client
* Download `ValorantPorting.zip` from the [latest release](https://github.com/KaiserM21/ValorantPorting/releases)
* Unzip the archive to a location where programs have read/write permissions (Avoid Downloads/Desktop)
* Launch the `ValorantPorting.exe` executable

## Building ValorantPorting

To build ValorantPorting from source, first clone the repository and all of its submodules.

```
git clone https://github.com/Ka1serM/ValorantPorting --recursive
```

Then run BuildRelease.bat or open the project directory in a terminal window and publish

```
dotnet publish ValorantPorting -c Release --no-self-contained -r win-x64 -o "./Release" -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -p:IncludeNativeLibrariesForSelfExtract=true
```

## ValorantPorting Server

### Blender

* Navigate to the Add-ons tab located in **Edit -> Preferences**

  <img src="https://docs.blender.org/manual/en/latest/_images/editors_preferences_section_addons.png" alt="Addon Tab" height=260 width=330>

* Press **Install** and select the `ValorantPortingBlender.zip` file **DO NOT EXTRACT THIS FILE AT ALL**
* Type `Valorant Porting` in the search bar and enable the addon
  
* Restart Blender

To build the Blender plugins from source, run BuildBlenderPlugins.bat.  If you'd prefer to zip them manually, copy the script from BlenderPlugins/PSA_PSK_Import/ into the ValorantPortingBlender (or ValorantPortingBlenderOctane) folder and then zip the entire folder.




### Unreal Engine
* ***TBD***

### Credits:
* Valorant [live] Code: https://github.com/4sval/FModel & https://github.com/FortniteCentral/MercuryCommons 
* https://github.com/halfuwu
* https://github.com/djhaled
* https://github.com/KaiserM21
* https://github.com/Bmarquez1997
