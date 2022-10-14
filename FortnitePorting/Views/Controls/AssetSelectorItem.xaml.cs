using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.ViewModels;
using SkiaSharp;
using CUE4Parse.UE4.Objects.UObject;

namespace FortnitePorting.Views.Controls;

public partial class AssetSelectorItem
{
    public UObject UIAsset;
    public SKBitmap IconBitmap;
    public SKBitmap FullBitmap;
    public BitmapImage FullSource;
    public UObject Asset;
    
    public bool IsRandom { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string TooltipName { get; set; }
    public string ID { get; set; }

    public AssetSelectorItem(UObject asset,UObject UIasset, UTexture2D previewTexture, bool isRandomSelector = false)
    {
        InitializeComponent();
        DataContext = this;

        UIAsset = UIasset;
        Asset = asset;
        DisplayName = UIAsset.GetOrDefault("DisplayName", new FText("Unnamed")).Text;
        Description = UIAsset.GetOrDefault("Description", new FText("No description.")).Text;
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

    private const int MARGIN = 2;
}