using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CUE4Parse.Encryption.Aes;
using CUE4Parse.FileProvider;
using CUE4Parse.MappingsProvider;
using CUE4Parse.UE4.AssetRegistry;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Versions;
using ValorantPorting.AppUtils;
using ValorantPorting.Services;
using ValorantPorting.Services.Endpoints.Models;

namespace ValorantPorting.ViewModels;

public class CUE4ParseViewModel : ObservableObject
{
    public readonly DefaultFileProvider Provider;

    public FAssetRegistryState? AssetRegistry;
    
    public RarityCollection[] RarityData = new RarityCollection[8];

    public CUE4ParseViewModel(string directory)
    {
        Provider = new DefaultFileProvider(directory, SearchOption.TopDirectoryOnly, isCaseInsensitive: true, new VersionContainer(EGame.GAME_Valorant));
    }
    
    public async Task Initialize()
    {
        Provider.Initialize();

        await InitializeKeys();
        
        Provider.LoadVirtualPaths();

        var assetArchive = await Provider.TryCreateReaderAsync("ShooterGame/AssetRegistry.bin");
        if (assetArchive is not null)
        {
            AssetRegistry = new FAssetRegistryState(assetArchive);
        }
        
        
    }

    private async Task InitializeKeys()
    {
        var keyResponse = AppSettings.Current.AesResponse;
        var keyString = "0x4BE71AF2459CF83899EC9DC2CB60E22AC4B3047E0211034BBABE9D174C069DD6";
        await Provider.SubmitKeyAsync(Globals.ZERO_GUID, new FAesKey(keyString));
    }
}

[StructFallback]
public class RarityCollection
{
    public FLinearColor Color1;
    public FLinearColor Color2;
    public FLinearColor Color3;
    public FLinearColor Color4;
    public FLinearColor Color5;
    public float Radius;
    public float Falloff;
    public float Brightness;
    public float Roughness;
    
    public RarityCollection(FStructFallback fallback)
    {
        Color1 = fallback.GetOrDefault<FLinearColor>(nameof(Color1));
        Color2 = fallback.GetOrDefault<FLinearColor>(nameof(Color2));
        Color3 = fallback.GetOrDefault<FLinearColor>(nameof(Color3));
        Color4 = fallback.GetOrDefault<FLinearColor>(nameof(Color4));
        Color5 = fallback.GetOrDefault<FLinearColor>(nameof(Color5));
        
        Radius = fallback.GetOrDefault<float>(nameof(Radius));
        Falloff = fallback.GetOrDefault<float>(nameof(Falloff));
        Brightness = fallback.GetOrDefault<float>(nameof(Brightness));
        Roughness = fallback.GetOrDefault<float>(nameof(Roughness));
    }

}