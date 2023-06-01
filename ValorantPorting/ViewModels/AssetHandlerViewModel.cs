using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using CUE4Parse.UE4.AssetRegistry.Objects;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using ValorantPorting.AppUtils;
using ValorantPorting.Views.Controls;
using ValorantPorting.Views.Extensions;
using Serilog;

namespace ValorantPorting.ViewModels;

public class AssetHandlerViewModel
{
    public readonly Dictionary<EAssetType, AssetHandlerData> Handlers ;


    public AssetHandlerViewModel()
    {
        Handlers = new Dictionary<EAssetType, AssetHandlerData>
        {
            { EAssetType.Character, _outfitHandler },
            { EAssetType.GunBuddy, _buddyHandler },
            { EAssetType.Weapon, _weaponHandler },
            { EAssetType.Maps, _mapsHandler },
        };

    }

    private readonly AssetHandlerData _outfitHandler = new()
    {
        AssetType = EAssetType.Character,
        TargetCollection = AppVM.MainVM.Outfits,
        ClassNames = new List<string> { "CharacterDataAsset" },
        RemoveList = {},
        IconGetter = UI_Asset =>
        {
            UI_Asset.TryGetValue(out UTexture2D? previewImage, "DisplayIcon");
            return previewImage;
        }
    };

    private readonly AssetHandlerData _weaponHandler = new()
    {
        AssetType = EAssetType.Weapon,
        TargetCollection = AppVM.MainVM.Weapons,
        ClassNames = new List<string> { "EquippableSkinDataAsset" },
        RemoveList = {},
        IconGetter = UI_Asset =>
        {
            UI_Asset.TryGetValue(out UTexture2D? previewImage, "DisplayIcon");
            return previewImage;
        }
    };


    private readonly AssetHandlerData _buddyHandler = new()
    {
        AssetType = EAssetType.GunBuddy,
        TargetCollection = AppVM.MainVM.Gunbuddies,
        ClassNames = new List<string> { "EquippableCharmDataAsset" },
        RemoveList = {},
        IconGetter = UI_Asset =>
        {
            UI_Asset.TryGetValue(out UTexture2D? previewImage, "DisplayIcon");
            return previewImage;
        }
    };
    
    private readonly AssetHandlerData _mapsHandler = new()
    {
        AssetType = EAssetType.Maps,
        TargetCollection = AppVM.MainVM.Maps,
        ClassNames = new List<string> { "MapDataAsset" },
        RemoveList = {},
        IconGetter = UI_Asset => UI_Asset.GetOrDefault<UTexture2D?>("DisplayIcon")
    };
    
    public async Task Initialize()
    {
        await _outfitHandler.Execute(); // default tab
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
        var items = new List<FAssetData>();
        foreach (var variable in AppVM.CUE4ParseVM.AssetRegistry.PreallocatedAssetDataBuffers)
        {
            foreach (var tValue in variable.TagsAndValues)
            {
                // for ClassNames
                foreach (var cName in ClassNames)
                {
                    if (tValue.Key.PlainText == "PrimaryAssetType" && tValue.Value.ToString() == cName)  
                    {
                        items.Add(variable);
                    }
                }
            }
        }
        // d
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
    // make new function to handle labels
   
    
    private async Task DoLoad(FAssetData data, bool random = false)
    {
        await PauseState.WaitIfPaused();
        // remove everything after the last . in data.ObjectPath
        UObject actualAsset;
        UObject uiAsset = null;
        var firstTag = data.ObjectPath;
        if (firstTag.Contains("NPE")) {return;}
        actualAsset = await AppVM.CUE4ParseVM.Provider.TryLoadObjectAsync(firstTag);
        if (actualAsset == null) {return;}
        var uBlueprintGeneratedClass = actualAsset as UBlueprintGeneratedClass;
        actualAsset = uBlueprintGeneratedClass.ClassDefaultObject.Load();
        var mainA = actualAsset;
        
        if (data.AssetName.Text.Contains("Random")) return;
        
        if (actualAsset.TryGetValue(out UBlueprintGeneratedClass uiObject, "UIData"))
        {
            uiAsset = uiObject.ClassDefaultObject.Load();
        }
        // switch on asset type
        string loadable = "";
        switch (AssetType)
        {
            case EAssetType.Character:
                loadable = "Character";
                break;
            case EAssetType.Weapon:
                actualAsset.TryGetValue<UBlueprintGeneratedClass[]>(out var bGp, "Levels");
                // handle more levels
                actualAsset = bGp[0].ClassDefaultObject.Load();
                loadable = "None";
                break;
            default:
                actualAsset.TryGetValue<UBlueprintGeneratedClass[]>(out var bBp, "Levels");
                actualAsset = bBp[0].ClassDefaultObject.Load();
                loadable = "CharmAttachment";
                break;
        }

        if (actualAsset.TryGetValue(out UBlueprintGeneratedClass equippableObject, loadable))
        {
            actualAsset = equippableObject.ClassDefaultObject.Load();
        }
        if (uiAsset is null)
        {
            uiAsset = actualAsset;
        }

        var previewImage = IconGetter(uiAsset);
        if (previewImage is null) return;
        await Application.Current.Dispatcher.InvokeAsync(() => TargetCollection.Add(new AssetSelectorItem(actualAsset, uiAsset,mainA ,previewImage, random)), DispatcherPriority.Background);
    }
}