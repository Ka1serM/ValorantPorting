using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using ValorantPorting.Views.Controls;

namespace ValorantPorting.Export;

public class ExportData
{
    public string Name;
    public List<ExportPart> Parts = new();
    public string Type;

    public static async Task<UObject> CreateUiData(UBlueprintGeneratedClass asset)
    {
        return await Task.Run(() => asset.ClassDefaultObject.Load());
    }

    
    public static async Task<ExportData> Create(UObject asset, EAssetType assetType, UObject style)
    {
        var data = new ExportData();
        data.Name = asset.GetOrDefault("DeveloperName", new FText("Unnamed")).Text;
        data.Type = assetType.ToString();
        await Task.Run(() =>
        {
            switch (assetType)
            {
                case EAssetType.Character:
                    ExportHelpers.Character(data.Parts, asset);
                    break;
                case EAssetType.Weapon:
                    ExportHelpers.Weapon(data.Parts, style);
                    break;
                case EAssetType.GunBuddy:
                    ExportHelpers.GunBuddy(data.Parts, asset);
                    break;
            }
        });

        await Task.WhenAll(ExportHelpers.Tasks);
        return data;
    }
}
