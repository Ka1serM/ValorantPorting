using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Views.Extensions;
using SkiaSharp;

namespace FortnitePorting.Views.Controls;

public partial class StyleSelectorItem
{
    
    public BitmapSource IconSource { get; set; }
    
    public StyleSelectorItem(UObject asset, UObject UIasset, UTexture2D Texture)
    {
        InitializeComponent();
        var previewBitmap = Texture.Decode();
        var fullBitmap = new SKBitmap(previewBitmap.Width, previewBitmap.Height, previewBitmap.ColorType, previewBitmap.AlphaType);
        using (var fullCanvas = new SKCanvas(fullBitmap))
        {
            //DrawBackground(fullCanvas, Math.Max(previewBitmap.Width, previewBitmap.Height));
            fullCanvas.DrawBitmap(previewBitmap, 0, 0);
        }
        IconSource = previewBitmap.ToBitmapSource();
    }

}