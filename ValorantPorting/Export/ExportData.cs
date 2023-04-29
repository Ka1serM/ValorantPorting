using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.i18N;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using ValorantPorting.Views.Extensions;

namespace ValorantPorting.Export;

public class ExportData
{
    public string Name;
    public string Type;
    public List<ExportPart> Parts = new();
    public List<ExportPart> StyleParts = new();
    public List<ExportMaterial> StyleMaterials = new();
    //public List<ExportAttatchment> Attatchments = new();
    
    public static async Task<UObject> CreateUiData(UBlueprintGeneratedClass asset )
    {
        UObject loadedCdoUi = null;
        await Task.Run(() =>
        {
            loadedCdoUi = asset.ClassDefaultObject.Load();
        });
        return Task.FromResult(loadedCdoUi).Result;
    }

    public static UObject HandleStyle(UObject sty)
    {
        var bpGnCast = sty as UBlueprintGeneratedClass;
        var cdo_style = bpGnCast?.ClassDefaultObject.Load();
        var returnBpGn = new UBlueprintGeneratedClass();
        cdo_style?.TryGetValue(out returnBpGn, "EquippableSkinChroma");
        var returnStyle = returnBpGn.ClassDefaultObject.Load();
        
        return returnStyle;

    }
    
    
    public static UObject HandleBaseChroma(UObject usedObj)
    {
        if (usedObj.TryGetValue(out UBlueprintGeneratedClass blueprint, "SkinAttachment"))
        {
            var defaultObject = blueprint.ClassDefaultObject.Load();
            if (defaultObject != null) return defaultObject;
        }
        return null;
    }
    
    public static UObject GetCsMesh()
    {
        UObject csObject;
        var main_asset_loaded = AppVM.MainVM.CurrentAsset.MainAsset;
        main_asset_loaded.TryGetValue(out  csObject, "CharacterSelectFXC");
        var csExports = AppVM.CUE4ParseVM.Provider.LoadObjectExports(csObject.GetPathName().Substring(0, csObject.GetPathName().LastIndexOf(".")));
        foreach (var propExp in csExports)
        {
            if (propExp.ExportType == "SkeletalMeshComponent" && propExp.Name == "SkeletalMesh_GEN_VARIABLE" )
            {
                return propExp;
            }
        }

        return null;
    }
    
    public static async Task<ExportData> Create(UObject asset, EAssetType assetType, UObject style,
        Tuple<USkeletalMesh, UMaterialInstanceConstant[], UMaterialInstanceConstant[], UStaticMesh> ent_tuple)

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
                    var meshes = new UObject[3];
                    asset.TryGetValue(out meshes[0], "MeshOverlay1P");
                    if (meshes[0].Properties.Count < 2)
                    {
                        asset.TryGetValue(out meshes[0], "Mesh1P");
                    }
                    asset.TryGetValue(out meshes[1], "MeshCosmetic3P");
                    meshes[2] = GetCsMesh();
                    ExportHelpers.CharacterParts(meshes, data.Parts, asset);
                    break;
                }
                case EAssetType.Weapon:
                {
                    //Console.WriteLine(ent_tuple.Item1.Name);
                    ExportHelpers.Weapon(data.Parts,data, ent_tuple);
                    if (style != null)
                    {
                        ExportHelpers.OverrideMaterials(
                            HandleStyle(style).GetOrDefault("MaterialOverrides",
                                Array.Empty<UMaterialInstanceConstant>()), data.StyleMaterials);
                    }
                    else
                    {
                        ExportHelpers.OverrideMaterials(
                            HandleBaseChroma(asset).GetOrDefault("1p MaterialOverrides",
                                Array.Empty<UMaterialInstanceConstant>()), data.StyleMaterials);
                    }

                    break;
                }
                case EAssetType.GunBuddy:
                {
                    var buddymesh = new UObject[1];
                    asset.TryGetValue(out buddymesh[0], "Charm");
                    if (buddymesh[0] is UStaticMesh staticMesh)
                    {
                        ExportHelpers.SMesh(staticMesh, data.Parts);
                    }
                    else
                    {
                        if (buddymesh[0] is USkeletalMesh skeletalMesh)
                        {
                            ExportHelpers.Mesh(skeletalMesh, data.Parts);
                        }
                    }
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