using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using ValorantPorting.ViewModels;
using SkiaSharp;
using CUE4Parse.UE4.Objects.UObject;

namespace ValorantPorting.Views.Controls;

public partial class AssetSelectorItem  : IExportableAsset
{
    public SKBitmap IconBitmap;
    public SKBitmap FullBitmap;
    public UObject UIAsset { get; set; }
    public UObject MainAsset { get; set; }
    public BitmapImage FullSource { get; set; }
    public UObject Asset { get; set; }
    public bool IsRandom { get; set; }
    public string DisplayName { get; set; }
    public EAssetType aType { get; set; }
    public string Description { get; set; }
    public string TooltipName { get; set; }
    public string ID { get; set; }

    public AssetSelectorItem(UObject asset,UObject UIasset,UObject MainDataAsset, UTexture2D previewTexture, bool isRandomSelector = false)
    {
        InitializeComponent();
        DataContext = this;
        UIAsset = UIasset;
        Asset = asset;
        MainAsset = MainDataAsset;
        DisplayName = UIAsset.GetOrDefault("DisplayName", new FText("")).Text;
        Description = UIAsset.GetOrDefault("Description", new FText("")).Text;
        ID = UIAsset.Name;

        TooltipName = $"{DisplayName} ({ID})";
        IsRandom = isRandomSelector;
        
        var iconBitmap = previewTexture.Decode();
        if (iconBitmap is null) return;
        IconBitmap = iconBitmap;
        
        FullBitmap = new SKBitmap(iconBitmap.Width, iconBitmap.Height, iconBitmap.ColorType, iconBitmap.AlphaType);
        using (var fullCanvas = new SKCanvas(FullBitmap))
        {
            fullCanvas.DrawBitmap(iconBitmap, 0, 0);
        }
        
        FullSource = new BitmapImage { CacheOption = BitmapCacheOption.OnDemand};
        FullSource.BeginInit();
        FullSource.StreamSource = FullBitmap.Encode(SKEncodedImageFormat.Png, 100).AsStream();
        FullSource.EndInit();

        DisplayImage.Source = FullSource;
        //BeginAnimation(OpacityProperty, AppearAnimation);
    }
    public bool Match(string filter, bool useRegex = false)
    {
        if (useRegex)
        {
            return Regex.IsMatch(DisplayName, filter) || Regex.IsMatch(ID, filter);
        }

        return DisplayName.Contains(filter, StringComparison.OrdinalIgnoreCase) || ID.Contains(filter, StringComparison.OrdinalIgnoreCase);
    }

    private const int MARGIN = 2;
}