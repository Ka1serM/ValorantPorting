using System.Windows;
using System.Windows.Media.Imaging;
using CUE4Parse.UE4.Assets.Exports;

namespace ValorantPorting.Views.Controls;

public class MeshAssetItem : IExportableAsset
{
    public UObject Asset { get; set; }
    public bool IsRandom { get; set; }
    public UObject UIAsset { get; set; }
    public UObject MainAsset { get; set; }
    public string DisplayName { get; set; }
    public EAssetType aType { get; set; }
    public string DisplayNameSource { get; set; }
    public string Description { get; set; }
    public string TooltipName { get; set; }
    public string ID { get; set; }
    public BitmapImage FullSource { get; set; }
    public EAssetType Type { get; set; }
    public Visibility PreviewImageVisibility { get; set; }

    public MeshAssetItem(UObject asset)
    {
        Asset = asset;
        DisplayName = asset.Name;
        DisplayNameSource = DisplayName;
        Description = asset.ExportType switch
        {
            "Skeleton" => "Skeleton",
            "StaticMesh" => "Static Mesh",
            "SkeletalMesh" => "Skeletal Mesh"
        };

        Type = EAssetType.Mesh;
        PreviewImageVisibility = Visibility.Collapsed;
    }
}