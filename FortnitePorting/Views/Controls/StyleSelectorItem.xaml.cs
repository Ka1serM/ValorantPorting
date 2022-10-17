using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Views.Extensions;
using SkiaSharp;

namespace FortnitePorting.Views.Controls;

public partial class StyleSelectorItem
{
    public UObject OptionData;
    public UObject ObjectData;
    public string DisplayName { get; set; }
    public BitmapSource IconSource { get; set; }
    
    public StyleSelectorItem(UObject Data,UObject option,string Name, SKBitmap previewBitmap)
    {
        InitializeComponent();
        OptionData = option;
        ObjectData = Data;
        DisplayName = Name;
        IconSource = previewBitmap.ToBitmapSource();

    }
}