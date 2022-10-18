using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Textures;
using CUE4Parse.UE4.Assets.Exports;
using CUE4Parse.UE4.Assets.Exports.Material;
using CUE4Parse.UE4.Assets.Exports.SkeletalMesh;
using CUE4Parse.UE4.Assets.Exports.StaticMesh;
using CUE4Parse.UE4.Assets.Exports.Texture;
using CUE4Parse.UE4.Assets.Objects;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.Utils;
using SkiaSharp;

namespace FortnitePorting.Export;

public static class ExportHelpers
{
    public static void CharacterParts(IEnumerable<UObject> inputParts, List<ExportPart> exportParts, UObject OGObjects)
    {
        foreach (var part in inputParts)
        {
            if (part is USkeletalMesh skeletalMesh)
            {
            }
            else
            {
                skeletalMesh = part.Get<USkeletalMesh?>("SkeletalMesh");
            }
            if (skeletalMesh is null) continue;
            if (!skeletalMesh.TryConvert(out var convertedMesh)) continue;
            if (convertedMesh.LODs.Count <= 0) continue;
            
            var exportPart = new ExportPart();
            exportPart.MeshPath = skeletalMesh.GetPathName();
            Save(skeletalMesh);
            exportPart.Part = "FP";
            // OPTION CharacterSelect or not later plz
            if (skeletalMesh.Name.Contains("CS"))
            {
                exportPart.Part = "CS";
            }
            var sections = convertedMesh.LODs[0].Sections.Value;
            for (var idx = 0; idx < sections.Length; idx++)
            {
                var section = sections[idx];
                if (section.Material is null) continue;
                
                
                if (!section.Material.TryLoad(out var material)) continue;
                
                var exportMaterial = new ExportMaterial
                {
                    MaterialName = material.Name,
                    SlotIndex = idx
                };

                if (material is UMaterialInstanceConstant materialInstance)
                {
                    var (textures, scalars, vectors) = MaterialParameters(materialInstance);
                    exportMaterial.Textures = textures;
                    exportMaterial.Scalars = scalars;
                    exportMaterial.Vectors = vectors;
                }
                exportPart.Materials.Add(exportMaterial);

            }
            if (part.TryGetValue(out UMaterialInstanceConstant[] materialOverrides, "MaterialOverrides"))
            {
                OverrideMaterials(materialOverrides, exportPart.OverrideMaterials);
            }

            exportParts.Add(exportPart);
        }
    }
    public static USkeletalMesh GetWeapBase()
    {
        UBlueprintGeneratedClass muob;
        UBlueprintGeneratedClass Agane;
        UObject rturn;
        var idks = AppVM.MainVM.CurrentAsset.MainAsset;
        var e = idks.TryGetValue(out muob, "Equippable");
        var n =muob.ClassDefaultObject.Load();
        var eb = n.TryGetValue(out Agane, "Equippable");
        var g = Agane.ClassDefaultObject.Load();
        if (g.TryGetValue(out rturn, "Mesh1P"))
        {
            return rturn.Get<USkeletalMesh>("SkeletalMesh");
        }
        return null;
    }
    public static void Weapon(UObject weaponDefinition, List<ExportPart> exportParts, ExportData ActualData)
    {
        
        if (weaponDefinition.TryGetValue(out UBlueprintGeneratedClass blueprint, "SkinAttachment"))
        {
            var defaultObject = blueprint.ClassDefaultObject.Load();
            USkeletalMesh weaponMeshData;
            USkeletalMesh weap_cosmetic;
            UStaticMesh MagazineWeap;
            if (defaultObject.TryGetValue(out  weaponMeshData, "Weapon 1P") || defaultObject.TryGetValue(out  weaponMeshData, "NewMesh") )
            {
                if (weaponMeshData.Name.ToLower().Contains("basemesh"))
                {
                    if (defaultObject.TryGetValue(out weap_cosmetic, "Weapon 1P Cosmetic"))
                    {
                        weaponMeshData = weap_cosmetic;
                    }
                }

                Mesh(weaponMeshData, exportParts);
            }
            else
            {
                Mesh(GetWeapBase(), exportParts);
            }

            if (defaultObject.TryGetValue(out MagazineWeap, "Magazine 1P"))
            {
                SMesh(MagazineWeap, exportParts);
                var attach = new ExportAttatchment();
                attach.BoneName = "Magazine_Main";
                attach.AttatchmentName = exportParts.Last().MeshName;
                exportParts.First().Attatchments.Add(attach);
                //ActualData.Attatchments.Add(attach);
            }
            if (defaultObject.TryGetValue(out UMaterialInstanceConstant[] WeapOverrides, "1p MaterialOverrides"))
            {
                OverrideMaterials(WeapOverrides,exportParts[0].OverrideMaterials);
            }
            
        }
        
    }
    public static int Mesh(USkeletalMesh? skeletalMesh, List<ExportPart> exportParts)
    {
        if (skeletalMesh is null) return -1;
        if (!skeletalMesh.TryConvert(out var convertedMesh)) return -1;
        if (convertedMesh.LODs.Count <= 0) return -1;

        var exportPart = new ExportPart();
        exportPart.MeshPath = skeletalMesh.GetPathName();
        exportPart.MeshName = skeletalMesh.Name + ".ao";
        Save(skeletalMesh);

        var sections = convertedMesh.LODs[0].Sections.Value;
        for (var idx = 0; idx < sections.Length; idx++)
        {
            var section = sections[idx];
            if (section.Material is null) continue;

            if (!section.Material.TryLoad(out var material)) continue;

            var exportMaterial = new ExportMaterial
            {
                MaterialName = material.Name,
                SlotIndex = idx
            };

            if (material is UMaterialInstanceConstant materialInstance)
            {
                var (textures, scalars, vectors) = MaterialParameters(materialInstance);
                exportMaterial.Textures = textures;
                exportMaterial.Scalars = scalars;
                exportMaterial.Vectors = vectors;
            }

            exportPart.Materials.Add(exportMaterial);
        }

        exportParts.Add(exportPart);
        return exportParts.Count - 1;
    }
    public static int SMesh(UStaticMesh? staticMesh, List<ExportPart> exportParts)
    {
        if (staticMesh is null) return -1;
        if (!staticMesh.TryConvert(out var convertedMesh)) return -1;
        if (convertedMesh.LODs.Count <= 0) return -1;
        var exportPart = new ExportPart();
        exportPart.MeshPath = staticMesh.GetPathName();
        exportPart.MeshName = staticMesh.Name + ".mo";
        Save(staticMesh);

        var sections = convertedMesh.LODs[0].Sections.Value;
        for (var idx = 0; idx < sections.Length; idx++)
        {
            var section = sections[idx];
            if (section.Material is null) continue;


            if (!section.Material.TryLoad(out var material)) continue;

            var exportMaterial = new ExportMaterial
            {
                MaterialName = material.Name,
                SlotIndex = idx
            };

            if (material is UMaterialInstanceConstant materialInstance)
            {
                var (textures, scalars, vectors) = MaterialParameters(materialInstance);
                exportMaterial.Textures = textures;
                exportMaterial.Scalars = scalars;
                exportMaterial.Vectors = vectors;
            }

            exportPart.Materials.Add(exportMaterial);
        }

        exportParts.Add(exportPart);
        return exportParts.Count - 1;
    }

    public static void OverrideMaterials(UMaterialInstanceConstant[] overrides, List<ExportMaterial> exportMaterials)
    {
        for (int i = 0; i < overrides.Length; i++)
        {
            var material = overrides[i];
            var exportMaterial = new ExportMaterial
            {
                MaterialName = material.Name,
                SlotIndex = i,
                MaterialNameToSwap = material.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.PlainText.SubstringAfterLast(".")
            };

            if (material is UMaterialInstanceConstant materialInstance)
            {
                var (textures, scalars, vectors) = MaterialParameters(materialInstance);
                exportMaterial.Textures = textures;
                exportMaterial.Scalars = scalars;
                exportMaterial.Vectors = vectors;
            }

            exportMaterials.Add(exportMaterial);
        }
    }
    public static (List<TextureParameter>, List<ScalarParameter>, List<VectorParameter>) MaterialParameters(UMaterialInstanceConstant materialInstance)
    {
        var textures = new List<TextureParameter>();
        foreach (var parameter in materialInstance.TextureParameterValues)
        {
            if (!parameter.ParameterValue.TryLoad(out UTexture2D texture)) continue;
            textures.Add(new TextureParameter(parameter.ParameterInfo.Name.PlainText, texture.GetPathName()));
            Save(texture);
        }
        
        var scalars = new List<ScalarParameter>();
        foreach (var parameter in materialInstance.ScalarParameterValues)
        {
            scalars.Add(new ScalarParameter(parameter.ParameterInfo.Name.PlainText, parameter.ParameterValue));
        }
        
        var vectors = new List<VectorParameter>();
        foreach (var parameter in materialInstance.VectorParameterValues)
        {
            if (parameter.ParameterValue is null) continue;
            vectors.Add(new VectorParameter(parameter.ParameterInfo.Name.PlainText, parameter.ParameterValue.Value));
        }

        if (materialInstance.Parent is UMaterialInstanceConstant { Parent: UMaterialInstanceConstant } materialParent)
        {
            var (parentTextures, parentScalars, parentVectors) = MaterialParameters(materialParent);
            foreach (var parentTexture in parentTextures)
            {
                if (textures.Any(x => x.Name.Equals(parentTexture.Name))) continue;
                textures.Add(parentTexture);
            }
            
            foreach (var parentScalar in parentScalars)
            {
                if (scalars.Any(x => x.Name.Equals(parentScalar.Name))) continue;
                scalars.Add(parentScalar);
            }
            
            foreach (var parentVector in parentVectors)
            {
                if (vectors.Any(x => x.Name.Equals(parentVector.Name))) continue;
                vectors.Add(parentVector);
            }
        }
        return (textures, scalars, vectors);
    }
    
    public static readonly List<Task> Tasks = new();
    private static readonly ExporterOptions ExportOptions = new()
    {
        Platform = ETexturePlatform.DesktopMobile,
        LodFormat = ELodFormat.AllLods,
        MeshFormat = EMeshFormat.ActorX,
        TextureFormat = ETextureFormat.Png,
        ExportMorphTargets = false
    };
    
    public static void Save(UObject obj)
    {
        Tasks.Add(Task.Run(() =>
        {
            try
            {
                switch (obj)
                {
                    case USkeletalMesh skeletalMesh:
                    {
                        var path = GetExportPath(obj, "psk");
                        if (File.Exists(path)) return;

                        var exporter = new MeshExporter(skeletalMesh, ExportOptions, false);
                        exporter.TryWriteToDir(App.AssetsFolder, out _);
                        break;
                    }

                    case UStaticMesh staticMesh:
                    {
                        var path = GetExportPath(obj, "pskx");
                        if (File.Exists(path)) return;

                        var exporter = new MeshExporter(staticMesh, ExportOptions, false);
                        exporter.TryWriteToDir(App.AssetsFolder, out _);
                        break;
                    }
                    case UTexture2D texture:
                    {
                        var path = GetExportPath(obj, "png");
                        if (File.Exists(path)) return;
                        Directory.CreateDirectory(path.Replace('\\', '/').SubstringBeforeLast('/'));

                        using var bitmap = texture.Decode(texture.GetFirstMip());
                        using var data = bitmap?.Encode(SKEncodedImageFormat.Png, 100);

                        if (data is null) return;
                        File.WriteAllBytes(path, data.ToArray());
                        break;
                    }
                }
            }
            catch (IOException) { }
        }));
    }

    private static string GetExportPath(UObject obj, string ext, string extra = "")
    {
        var path = obj.Owner.Name;
        path = path.SubstringBeforeLast('.');
        if (path.StartsWith("/")) path = path[1..];

        var finalPath = Path.Combine(App.AssetsFolder.FullName, path) + $"{extra}.{ext.ToLower()}";
        return finalPath;
    }
}