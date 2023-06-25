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
    
    public static async Task<UObject> CreateUiData(UBlueprintGeneratedClass asset )
    {
        UObject loadedCdoUi = null;
        await Task.Run(() =>
        {
            loadedCdoUi = asset.ClassDefaultObject.Load();
        });
        return Task.FromResult(loadedCdoUi).Result;
    }
    
    public static UObject GetCsMesh()
    {
        UObject csObject;
        var main_asset_loaded = AppVM.MainVM.CurrentAsset.MainAsset;
        main_asset_loaded.TryGetValue(out  csObject, "CharacterSelectFXC");
        var csExports = AppVM.CUE4ParseVM.Provider.LoadAllObjects(csObject.GetPathName().Substring(0, csObject.GetPathName().LastIndexOf(".")));
        foreach (var propExp in csExports)
        {
            if (propExp.ExportType == "SkeletalMeshComponent" && propExp.Name == "SkeletalMesh_GEN_VARIABLE" )
            {
                return propExp;
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
                    ExportHelpers.Weapon(data.Parts, style);
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
                case EAssetType.Map:
                {
                    ExportHelpers.Map();
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