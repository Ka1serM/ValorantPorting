using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using FortnitePorting.AppUtils;
using FortnitePorting.Views.Controls;
using FortnitePorting.Views.Extensions;
using Serilog;

namespace FortnitePorting.ViewModels;

public class AssetHandlerViewModel
{
    public readonly Dictionary<EAssetType, AssetHandlerData> Handlers ;
    public Dictionary<FName, UObject> BaseMeshMap;
    public Dictionary<string,USkeletalMesh> CurrentBaseMesh;

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
        BaseMeshMap = new Dictionary<FName, UObject>();
        CurrentBaseMesh = new Dictionary<string, USkeletalMesh>();

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
        ClassNames = new List<string> { "EquippableSkinDataAsset" },
        RemoveList = {},
        IconGetter = UI_Asset =>
        {
            //var ui_data = UI_Asset.Get<UBlueprintGeneratedClass>("UIData");
            //var real_ui_data =  ui_data.ClassDefaultObject.Load();
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
                // for ClassNames
                foreach (var cName in ClassNames)
                {
                    if (tValue.Key.PlainText == "PrimaryAssetType" && tValue.Value.ToString() == cName && !VARIABLE.AssetName.ToString().Contains("NPE"))  
                    {
                        items.Add(VARIABLE);
                    }
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
    // make new function to handle labels
    public Tuple<string,USkeletalMesh> GetBaseMesh(UObject ObjectUsed)
    {
        ObjectUsed.TryGetValue<UBlueprintGeneratedClass>(out var bp, "Equippable");
        var cdo_load = bp.ClassDefaultObject.Load();
        cdo_load.TryGetValue<UBlueprintGeneratedClass>(out var weap_equippable, "Equippable");
        var real_cdo_load = weap_equippable.ClassDefaultObject.Load();
        real_cdo_load.TryGetValue<UObject>(out var zbp, "Mesh1P");
        zbp.TryGetValue<USkeletalMesh>(out var jkjk, "SkeletalMesh");
        string mesh_name = jkjk.Name.ToString();
        //string weapo_name = mesh_name.Split("_")[1];
        int move = 5;
        if (mesh_name.Contains("Melee"))
        {
            move = 4;
        }
        string weapo_name = ObjectUsed.Outer.Name.Split("/")[move];
        return Tuple.Create(weapo_name, jkjk);
    }
    public bool bWeaponNeedsBaseMesh(UObject LabelObject)
    {
        bool bLocal = true;
        LabelObject.TryGetValue<FSoftObjectPath[]>(out var lv, "Levels");
        // for loop lv
        if (lv.Length < 2) return false;
        foreach (var skin_level in lv)
        {
            var assetName = skin_level.AssetPathName.PlainText;
            if (assetName == "None")
            {
                continue;
            }
            var assetFixString = "Default__" + assetName.Split('.').Last();
            var newAssetName = assetName.Split('.')[0] + "." + assetFixString;
            var LoadObject = AppVM.CUE4ParseVM.Provider.LoadObjectAsync(newAssetName).Result;
            if (LoadObject.TryGetValue(out UBlueprintGeneratedClass SkinAttach, "SkinAttachment"))
            {
                var SkinObject = SkinAttach.ClassDefaultObject.Load();
                if (SkinObject.TryGetValue(out FSoftObjectPath BaseMesh, "Weapon 1P"))
                {
                    return false;
                }
            }

        }
        return bLocal;
    }

    private async Task DoLoad(FAssetData data, bool random = false)
    {
        await PauseState.WaitIfPaused();
        // remove everything after the last . in data.ObjectPath
        UObject actual_asset;
        UObject UI_Asset = null;
        var FirstTag = data.TagsAndValues.First().Value.Replace("BlueprintGeneratedClass", "").Replace("'", ""); ;
        actual_asset = await AppVM.CUE4ParseVM.Provider.LoadObjectAsync(FirstTag);
        var uBlueprintGeneratedClass = actual_asset as UBlueprintGeneratedClass;
        actual_asset = uBlueprintGeneratedClass.ClassDefaultObject.Load();
        var mainA = actual_asset;
        if (data.AssetName.Text.Contains("Random"))
        {
            return;
        }
        if (actual_asset.TryGetValue(out UBlueprintGeneratedClass UIObject, "UIData"))
        {
            UI_Asset = UIObject.ClassDefaultObject.Load();
        }
        // switch on asset type
        if (AssetType == EAssetType.Weapon)
        {
            if (bWeaponNeedsBaseMesh(actual_asset))
            {
                if (actual_asset.Name.Contains("LeverSniper"))
                {
                    return;
                }
                var baseTuple = GetBaseMesh(actual_asset);
                if (!AppVM.AssetHandlerVM.CurrentBaseMesh.ContainsKey(baseTuple.Item1))
                {
                    AppVM.AssetHandlerVM.CurrentBaseMesh.Add(baseTuple.Item1, baseTuple.Item2);
                }
            }
        }
        string Loadable = "";
        switch (AssetType)
        {
            case EAssetType.Character:
                Loadable = "Character";
                break;
            case EAssetType.Weapon:
                actual_asset.TryGetValue<UBlueprintGeneratedClass[]>(out var bGp, "Levels");
                actual_asset = bGp[0].ClassDefaultObject.Load();
                //actual_asset = bp.ClassDefaultObject.Load();
                Loadable = "None";
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
        await Application.Current.Dispatcher.InvokeAsync(() => TargetCollection.Add(new AssetSelectorItem(actual_asset, UI_Asset,mainA ,previewImage, random)), DispatcherPriority.Background);
    }
}