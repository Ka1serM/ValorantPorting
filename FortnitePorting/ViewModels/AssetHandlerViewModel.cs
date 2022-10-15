using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using FortnitePorting.AppUtils;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;
using Serilog;

namespace FortnitePorting.ViewModels;

public class AssetHandlerViewModel
{
    public readonly Dictionary<EAssetType, AssetHandlerData> Handlers ;
    
    public AssetHandlerViewModel()
    {
        Handlers = new Dictionary<EAssetType, AssetHandlerData>
        {
            { EAssetType.Character, OutfitHandler },
            { EAssetType.GunBuddy, BuddyHandler },
            { EAssetType.Weapon, WeaponHandler },
            { EAssetType.Maps, MapsHandler },
            { EAssetType.Bundles, BundlesHandler },
        };

    }

    private readonly AssetHandlerData OutfitHandler = new()
    {
        AssetType = EAssetType.Character,
        TargetCollection = AppVM.MainVM.Outfits,
        ClassNames = new List<string> { "CharacterDataAsset" },
        RemoveList = new List<string> { "_NPC", "_TBD", "_VIP", "_Creative", "_SG"},
        IconGetter = UI_Asset =>
        {
            UI_Asset.TryGetValue(out UTexture2D? previewImage, "DisplayIcon");
            return previewImage;
        }
    };

    private readonly AssetHandlerData WeaponHandler = new()
    {
        AssetType = EAssetType.Weapon,
        TargetCollection = AppVM.MainVM.HarvestingTools,
        ClassNames = new List<string> { "EquippableSkinDataAsset"},
        RemoveList = {},
        IconGetter = UI_Asset =>
        {
            UI_Asset.TryGetValue(out UTexture2D? pImage, "DisplayIcon");
            return pImage;
        }
    };


    private readonly AssetHandlerData BuddyHandler = new()
    {
        AssetType = EAssetType.GunBuddy,
        TargetCollection = AppVM.MainVM.Dances,
        ClassNames = new List<string> { "EquippableCharmLevelDataAsset" },
        RemoveList = { "_CT", "_NPC"},
        IconGetter = UI_Asset => UI_Asset.GetOrDefault<UTexture2D?>("DisplayIcon")
    };
    private readonly AssetHandlerData MapsHandler = new()
    {
        AssetType = EAssetType.Maps,
        TargetCollection = AppVM.MainVM.Maps,
        ClassNames = new List<string> { "MapDataAsset" },
        RemoveList = {},
        IconGetter = UI_Asset => UI_Asset.GetOrDefault<UTexture2D?>("DisplayIcon")
    };
    private readonly AssetHandlerData BundlesHandler = new()
    {
        AssetType = EAssetType.Bundles,
        TargetCollection = AppVM.MainVM.Bundles,
        ClassNames = new List<string> { "PrimaryAssetLabel" },
        RemoveList = new List<string> { "_NPC", "_TBD", "_VIP", "_Creative", "_SG"},
        IconGetter = actual_asset =>
        {
            return null;
        }
    };
    public async Task Initialize()
    {
        await OutfitHandler.Execute(); // default tab
    }
}


public class AssetHandlerData
{
    public bool HasStarted { get; private set; }
    public Pauser PauseState { get; } = new();
    
    public EAssetType AssetType;
    public ObservableCollection<AssetSelectorItem> TargetCollection;
    public List<string> ClassNames;
    public List<string> RemoveList = Enumerable.Empty<string>().ToList();
    public Func<UObject, UTexture2D?> IconGetter;

    public async Task Execute()
    {
        if (HasStarted) return;
        HasStarted = true;
        string value = "";
        var items = new List<FAssetData>();
        foreach (var VARIABLE in AppVM.CUE4ParseVM.AssetRegistry.PreallocatedAssetDataBuffers)
        {
            foreach (var tValue in VARIABLE.TagsAndValues)
            {
                if (tValue.Key.PlainText == "PrimaryAssetType" && tValue.Value.ToString() == ClassNames[0] && !VARIABLE.AssetName.ToString().Contains("NPE"))  
                {
                    items.Add(VARIABLE);
                }
            }
        }
        // prioritize random first cuz of parallel list positions
        // console write line items length
        var random = items.FirstOrDefault(x => x.AssetName.PlainText.Contains("Random", StringComparison.OrdinalIgnoreCase));
        if (random is not null)
        {
            items.Remove(random);
            await DoLoad(random, true);
        }

        var addedAssets = new List<string>();

        await Parallel.ForEachAsync(items, async (data, token) =>
        {
            var assetName = data.AssetName.PlainText;
            if (AssetType == EAssetType.Weapon)
            {
                var reg = Regex.Match(assetName, @"(.*)_(.*)_(.*)_T[0-9][0-9]");
                if (reg.Success && addedAssets.Any(x => x.Contains(reg.Groups[1].Value, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }
                addedAssets.Add(assetName);
            }
            
            await DoLoad(data);
        });
    }

    private async Task DoLoad(FAssetData data, bool random = false)
    {
        await PauseState.WaitIfPaused();
        // remove everything after the last . in data.ObjectPath
        UObject actual_asset;
        UObject UI_Asset = null;
        var FirstTag = data.TagsAndValues.First().Value.Replace("BlueprintGeneratedClass", "").Replace("'", "");;
        actual_asset = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(FirstTag);
        var uBlueprintGeneratedClass = actual_asset as UBlueprintGeneratedClass;
        actual_asset = uBlueprintGeneratedClass.ClassDefaultObject.Load();
        if (data.AssetName.Text.Contains("Random"))
        {
            return;
        }
        if (actual_asset.TryGetValue(out UBlueprintGeneratedClass UIObject, "UIData"))
        {
            UI_Asset = UIObject.ClassDefaultObject.Load();
        }
        // switch on asset type
        string Loadable = "";
        switch (AssetType)
        {
            case EAssetType.Character:
                Loadable = "Character";
                break;
            case EAssetType.Weapon:
                Loadable = "SkinAttachment";
                break;
            default:
                Loadable = "CharmAttachment";
                break;
        }
        
        if (actual_asset.TryGetValue(out UBlueprintGeneratedClass EquippableObject, Loadable))
        {
            actual_asset = EquippableObject.ClassDefaultObject.Load();
        }
        if (UI_Asset is null)
        {
            UI_Asset = actual_asset;
        }
        
        var previewImage = IconGetter(UI_Asset);
        if (previewImage is null) return;
        await Application.Current.Dispatcher.InvokeAsync(() => TargetCollection.Add(new AssetSelectorItem(actual_asset,UI_Asset, previewImage, random)), DispatcherPriority.Background);
    }
}