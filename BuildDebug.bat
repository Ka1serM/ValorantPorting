dotnet publish ValorantPorting -c Debug --no-self-contained -r win-x64 -o "./Debug" -p:PublishSingleFile=true -p:DebugType=Full -p:DebugSymbols=true -p:IncludeNativeLibrariesForSelfExtract=true 