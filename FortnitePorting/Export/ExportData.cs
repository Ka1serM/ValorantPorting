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
using CUE4Parse.UE4.Objects.UObject;
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

    public static UObject HandleBaseChroma(UObject usedObj)
    {
        if (usedObj.TryGetValue(out UBlueprintGeneratedClass blueprint, "SkinAttachment"))
        {
            var defaultObject = blueprint.ClassDefaultObject.Load();
            return defaultObject;

        }
        return null;
    }

    public static UObject GetCSMesh()
    {
        UObject muob;
        var idks = AppVM.MainVM.CurrentAsset.MainAsset;
        idks.TryGetValue(out  muob, "CharacterSelectFXC");
        var nanrtw = AppVM.CUE4ParseVM.Provider.LoadObjectExports(muob.GetPathName().Substring(0, muob.GetPathName().LastIndexOf(".")));
        foreach (var VARIABLE in nanrtw)
        {
            if (VARIABLE.ExportType == "SkeletalMeshComponent" && !VARIABLE.Name.Contains("Camera") )
            {
                return VARIABLE;
            }
        }

        return null;
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
                    // one day make option to use character select or not
                    meshes[1] = GetCSMesh();
                    //asset.TryGetValue(out meshes[1], "MeshCosmetic3P");
                    ExportHelpers.CharacterParts(meshes, data.Parts, asset);
                    break;
                }
                case EAssetType.Weapon:
                {
                    ExportHelpers.Weapon(asset, data.Parts);
                    if (style != null)
                    {
                        ExportHelpers.OverrideMaterials( HandleStyle(style).GetOrDefault("MaterialOverrides", Array.Empty<UMaterialInstanceConstant>()), data.StyleMaterials);
                    }
                    else
                    {
                        ExportHelpers.OverrideMaterials(HandleBaseChroma(asset).GetOrDefault("1p MaterialOverrides", Array.Empty<UMaterialInstanceConstant>()), data.StyleMaterials);
                    }
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