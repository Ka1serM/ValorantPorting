using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using FortnitePorting.Views.Extensions;

namespace FortnitePorting.Export;

public class ExportData
{
    public string Name;
    public string Type;
    public List<ExportPart> Parts = new();
    public List<ExportPart> StyleParts = new();
    public List<ExportMaterial> StyleMaterials = new();
    public static async Task<UObject> CreateUIData(UBlueprintGeneratedClass asset )
    {
        UObject Sla = null;
        await Task.Run(() =>
        {
            Sla = asset.ClassDefaultObject.Load();
        });
        return Task.FromResult(Sla).Result;
    }

    public static UObject HandleStyle(UObject Sty)
    {
        var useStyle = Sty as UBlueprintGeneratedClass;
        var styleCdo = useStyle?.ClassDefaultObject.Load();
        var loka = new UBlueprintGeneratedClass();
        styleCdo?.TryGetValue(out loka, "EquippableSkinChroma");
        var ReturnStyle = loka.ClassDefaultObject.Load();
        
        return ReturnStyle;

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
                {
                    var meshes = new UObject[2];
                    asset.TryGetValue(out meshes[0], "MeshOverlay1P");
                    asset.TryGetValue(out meshes[1], "MeshCosmetic3P");
                    ExportHelpers.CharacterParts(meshes, data.Parts, asset);
                    break;
                }
                case EAssetType.Weapon:
                {
                    ExportHelpers.Weapon(asset, data.Parts);
                    ExportHelpers.OverrideMaterials( HandleStyle(style).GetOrDefault("MaterialOverrides", Array.Empty<UMaterialInstanceConstant>()), data.StyleMaterials);

                    break;
                }
                case EAssetType.GunBuddy:
                {
                    var buddymesh = new  UObject[1];
                    buddymesh[0] = asset.GetOrDefault("Charm", new UObject());
                    ExportHelpers.CharacterParts(buddymesh, data.Parts, asset);
                    break;
                    
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });


        await Task.WhenAll(ExportHelpers.Tasks);
        return data;
    }
}