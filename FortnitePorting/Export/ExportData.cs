using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Objects.Core.i18N;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.Export;

public class ExportData
{
    public string Name;
    public string Type;
    public List<ExportPart> Parts = new();

    public static async Task<ExportData> Create(UObject asset, EAssetType assetType)
    {
        var data = new ExportData();
        data.Name = asset.GetOrDefault("DeveloperName", new FText("Unnamed")).Text;
        data.Type = assetType.ToString();
        await Task.Run(() =>
        {
            switch (assetType)
            {
                case EAssetType.Character:
                {
                    
                    var meshes = new UObject[2];
                    // add to meshes array
                    meshes[0] = asset.GetOrDefault("MeshOverlay1P", new UObject());
                    meshes[1] = asset.GetOrDefault("MeshCosmetic3P", new UObject());
                    ExportHelpers.CharacterParts(meshes, data.Parts);
                    break;
                }
                case EAssetType.Weapon:
                {
                    var weapmeshes = new UObject[1];
                    weapmeshes[0] = asset.GetOrDefault("NewMesh", new UObject());
                    ExportHelpers.CharacterParts(weapmeshes, data.Parts);
                    break;
                }
                case EAssetType.GunBuddy:
                {
                    var buddymesh = new  UObject[1];
                    buddymesh[0] = asset.GetOrDefault("Charm", new UObject());
                    ExportHelpers.CharacterParts(buddymesh, data.Parts);
                    break;
                    
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });


        await Task.WhenAll(ExportHelpers.RunningExporters);
        return data;
    }
}