using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Views.Extensions;
using SkiaSharp;

namespace FortnitePorting.Views.Controls;

public partial class StyleSelectorItem
{
    
    public BitmapSource IconSource { get; set; }
    
    public StyleSelectorItem(SKBitmap previewBitmap)
    {
        InitializeComponent();
        IconSource = previewBitmap.ToBitmapSource();

    }
    private void TestButton(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("TestButton");
    }
}