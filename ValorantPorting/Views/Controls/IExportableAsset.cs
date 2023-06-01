using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports;
using SkiaSharp;

namespace ValorantPorting.Views.Controls;

public interface IExportableAsset
{
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
}