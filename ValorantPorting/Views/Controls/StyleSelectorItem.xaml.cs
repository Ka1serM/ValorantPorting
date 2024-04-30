using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports;
using SkiaSharp;
using ValorantPorting.Views.Extensions;

namespace ValorantPorting.Views.Controls;

public partial class StyleSelectorItem
{
    public UObject ObjectData;
    public UObject OptionData;

    public StyleSelectorItem(UObject Data, UObject option, string Name, SKBitmap previewBitmap)
    {
        InitializeComponent();
        OptionData = option;
        ObjectData = Data;
        DisplayName = Name;
        IconSource = previewBitmap.ToBitmapSource();
    }

    public string DisplayName { get; set; }
    public BitmapSource IconSource { get; set; }
}