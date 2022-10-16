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

public class VariantHandlerViewModel
{
    public readonly Dictionary<ESkinType, VariantHandlerData> Handlers ;
    public ObservableCollection<StyleSelectorItem> Styles { get; set; }
    public VariantHandlerViewModel()
    {
        Handlers = new Dictionary<ESkinType, VariantHandlerData>
        {
            { ESkinType.Level, LevelHandler },
            { ESkinType.Variant, VariantHandler },
        };

    }

    private readonly VariantHandlerData LevelHandler = new()
    {
        Collection = AppVM.MainVM.Outfits,
        GettingIcon = UI_Asset =>
        {
            UI_Asset.TryGetValue(out UTexture2D? previewImage, "DisplayIcon");
            return previewImage;
        }
    };

    private readonly VariantHandlerData VariantHandler = new()
    {
        Collection = AppVM.MainVM.HarvestingTools,
        GettingIcon = UI_Asset =>
        {
            UI_Asset.TryGetValue(out UTexture2D? pImage, "DisplayIcon");
            return pImage;
        }
    };

    public async Task Initialize()
    {
        
    }
}


public class VariantHandlerData
{
    public bool HasStarted { get; private set; }
    
    public EAssetType AssetType;
    public ObservableCollection<AssetSelectorItem> Collection;
    public Func<UObject, UTexture2D?> GettingIcon;

    
    private async Task LoadVariant(FAssetData data, bool random = false)
    {
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
        
        var previewImage = GettingIcon(UI_Asset);
        if (previewImage is null) return;
        await Application.Current.Dispatcher.InvokeAsync(() => Collection.Add(new AssetSelectorItem(actual_asset,UI_Asset, previewImage, random)), DispatcherPriority.Background);
    }
}