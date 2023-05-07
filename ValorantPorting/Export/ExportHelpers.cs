using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
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

namespace ValorantPorting.Export;

public static class ExportHelpers
{
    public static Tuple<List<UStaticMesh>, List<UMaterialInstanceConstant>, List<ExportAttatchment>> GetVfxMeshes()
    {
        var currentAsset = AppVM.MainVM.CurrentAsset.MainAsset;
        if (currentAsset.TryGetValue(out UBlueprintGeneratedClass[] levels, "Levels"))
        {
            foreach (var currentLevel in levels)
            {
                var classDefaultObj = currentLevel.ClassDefaultObject.Load();
                UBlueprintGeneratedClass localUObject;
                if (classDefaultObj.TryGetValue(out localUObject, "SkinAttachment"))
                {
                    FStructFallback[] vfxStruct;
                    var skinAttachment = localUObject.ClassDefaultObject.Load();
                    if (skinAttachment.TryGetValue(out vfxStruct, "VFX Meshes"))
                    {
                        List<UStaticMesh> vfxMesh = new();
                        List<UMaterialInstanceConstant> overrideMaterial = new();
                        List<ExportAttatchment> attachment = new();
                        for (int i = 0; i < vfxStruct.Length; i++)
                        {
                            var attach = new ExportAttatchment();
                            if (vfxStruct[i].GetOrDefault<FPackageIndex>("Mesh_2_F4F3A0874905DA0E7987EDB9EA823F16").TryLoad(out UStaticMesh mesh))
                            {
                                vfxMesh.Add(mesh);
                                attach.AttatchmentName = vfxMesh.Last().Name + "_LOD0.mo";
                            }
                            if (vfxStruct[i].GetOrDefault<FPackageIndex>("Material_9_2DB1229240DECB0BC013F4AAF45EA539").TryLoad(out UMaterialInstanceConstant material)) overrideMaterial.Add(material);
                            vfxStruct[i].TryGetValue(out FName attachSocket, "AttachSocket_6_5BE0CAE14A9C7BB424A96CB1FE9F5DAF"); 
                            vfxStruct[i].TryGetValue(out FVector offset, "Offset_17_31AB75334559002C947D3CB9D35AAC45");
                            vfxStruct[i].TryGetValue(out FRotator rotation, "Rotation_18_3C7AD0914F2FC8A61C88F295F2E435B7");
                            attach.BoneName = attachSocket.ToString();
                            attach.Offset = offset;
                            attach.Rotation = rotation;
                            attachment.Add(attach);
                        }
                        return Tuple.Create(vfxMesh, overrideMaterial, attachment);
                    }
                }
            }
        }
        return null;
    }

    public static void CharacterParts(IEnumerable<UObject> inputParts, List<ExportPart> exportParts, UObject ogObjects)
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
                    exportMaterial.ParentName = materialInstance.Parent.Name;
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
    public static USkeletalMesh GetBaseWeapon()
    {
        UBlueprintGeneratedClass local_bgc_1;
        UBlueprintGeneratedClass local_bgc_2;
        UObject object_Return;
        var idks = AppVM.MainVM.CurrentAsset.MainAsset;
        
        if (idks.TryGetValue(out local_bgc_1, "Equippable"));
        {
            var bgc1Equippable =local_bgc_1.ClassDefaultObject.Load();
            if (bgc1Equippable.TryGetValue(out local_bgc_2, "Equippable"))
            {
                var bgc2Equippable = local_bgc_2.ClassDefaultObject.Load();
                if (bgc2Equippable.TryGetValue(out object_Return, "Mesh1P"))
                {
                    return object_Return.Get<USkeletalMesh>("SkeletalMesh");
                }
            }
        }
        return null;
    }
    // for some reason the mag mash is not in the properties here so gotta load all exports
    public static UStaticMesh GetMagMesh()
    {
        // initializers
        UBlueprintGeneratedClass magBgn;
        UObject finalMagBgn;
        // main asset current (PrimaryDataAsset reference)
        var u_MainAsset = AppVM.MainVM.CurrentAsset.MainAsset;
        if (u_MainAsset.TryGetValue(out magBgn, "Equippable"))
        {
            var cdo = magBgn.ClassDefaultObject.Load();
            if (cdo.TryGetValue(out finalMagBgn, "Equippable"))
            {
                var mainObjectExports = AppVM.CUE4ParseVM.Provider.LoadAllObjects(finalMagBgn.GetPathName().Substring(0, finalMagBgn.GetPathName().LastIndexOf(".")));
                foreach (var VARIABLE in mainObjectExports)
                {
                    if (VARIABLE.Name.Contains("Magazine_1P"))
                    {
                        return VARIABLE.Get<UStaticMesh>("StaticMesh");
                    }
                }
            }
        }

        return null;
    }
    
    public static UStaticMesh GetAllWeaponSM()
    {
        // initializers
        UBlueprintGeneratedClass magBgn;
        UObject final;
        
        // main gun asset current (PrimaryDataAsset reference)
        var u_MainAsset = AppVM.MainVM.CurrentAsset.MainAsset;
        if (u_MainAsset.TryGetValue(out magBgn, "Equippable"))
        {
            var cdo = magBgn.ClassDefaultObject.Load();
            if (cdo.TryGetValue(out final, "Equippable"))
            {
                var mainObjectExports = AppVM.CUE4ParseVM.Provider.LoadAllObjects(final.GetPathName().Substring(0, final.GetPathName().LastIndexOf(".")));
                foreach (var VARIABLE in mainObjectExports)
                {
                    return VARIABLE.Get<UStaticMesh>("StaticMesh");
                }
            }
        }

        return null;
    }


    public static Tuple<List<string>,List<USkeletalMesh>,List<UMaterialInstanceConstant[]>> GetWeaponAttatchments(UObject current, UScriptMap scriptMap)
    {
        // initializer for return tuple stuff
        var fullSockets = new List<string>();
        var fullOverrideMaterials = new List<UMaterialInstanceConstant[]>();
        var meshes = new List<USkeletalMesh>();
        //  loop 
        foreach (var scriptMapVariable in scriptMap.Properties)
        {
            // initializers
            UStaticMesh localMesh_z;
            UMaterialInstanceConstant[] overrideMaterials_z = new UMaterialInstanceConstant[] { };
            string localSocket_z = null;
            // main_values
            var scriptMapValue = (FSoftObjectPath)scriptMapVariable.Value.GenericValue;
            var valueLoaded = (UBlueprintGeneratedClass)scriptMapValue.Load();
            var classDefaultObject = valueLoaded.ClassDefaultObject.Load();

            string[] scope = { "1pReflexMesh", "MaterialOverrides", "Reflex"};
            string[] silencer = { "1p Mesh", "1p MaterialOverrides", "Barrel"};  
            List<List<string>> currentAttatchList  = new List<List<string>>();
            currentAttatchList.Add(new List<string>(scope));
            currentAttatchList.Add(new List<string>(silencer));

            // 
            for (int i = 0; i < currentAttatchList.Count; i++)
            {
                var currentAttach = currentAttatchList[i];
                classDefaultObject.TryGetValue(out USkeletalMesh localMesh, currentAttach[0]);
                classDefaultObject.TryGetValue(out UMaterialInstanceConstant[] localmat, currentAttach[1]);
                if (localMesh == null)
                {
                    continue;
                }
                fullSockets.Add(currentAttach[2]);
                meshes.Add(localMesh);
                fullOverrideMaterials.Add(localmat);
            }

        }
        
        return Tuple.Create(fullSockets, meshes, fullOverrideMaterials);
    }
    
    public static void Weapon( List<ExportPart> exportParts, ExportData ActualData, Tuple<USkeletalMesh, UMaterialInstanceConstant[], UMaterialInstanceConstant[], UStaticMesh> em_tuple)
    {
        var current = AppVM.MainVM.CurrentAsset.MainAsset;
        UScriptMap dant_attatchs;
        if (em_tuple.Item1 != null)
        {
            Mesh(em_tuple.Item1, exportParts);
            if (em_tuple.Item2 != null) OverrideMaterials(em_tuple.Item2, exportParts.Last().OverrideMaterials);
        }
        else
        {
            Mesh(GetBaseWeapon(), exportParts);
            if (em_tuple.Item2 != null) OverrideMaterials(em_tuple.Item2, exportParts.Last().OverrideMaterials);
        }

        if (em_tuple.Item4 != null)
        {
            SMesh(em_tuple.Item4, exportParts);
            if (em_tuple.Item3 != null) OverrideMaterials(em_tuple.Item2, exportParts.Last().OverrideMaterials);
        }
        else
        {
            SMesh(GetMagMesh(), exportParts);
            if (em_tuple.Item3 != null) OverrideMaterials(em_tuple.Item2, exportParts.Last().OverrideMaterials);
        }



        var attach = new ExportAttatchment();
        attach.BoneName = "Magazine_Main";
        attach.AttatchmentName = exportParts.Last().MeshName;
        exportParts.First().Attatchments.Add(attach);
        
        if (current.TryGetValue(out dant_attatchs, "AttachmentOverrides"))
        {
            var scopeTuple = GetWeaponAttatchments(current, dant_attatchs);
            for (int i = 0; i < scopeTuple.Item2.Count; i++)
            {
                var lMesh = scopeTuple.Item2[i];
                var lMats = scopeTuple.Item3[i];
                var l_sockets = scopeTuple.Item1[i];
                Mesh(lMesh, exportParts);
                var scope_tach = new ExportAttatchment();
                scope_tach.BoneName = l_sockets;
                scope_tach.AttatchmentName = exportParts.Last().MeshName;
                exportParts.First().Attatchments.Add(scope_tach);
                if (lMats != null)
                {
                    OverrideMaterials(lMats,exportParts.Last().OverrideMaterials);
                }
            }
        }
        //vfx meshes
        /*
        var vfxTuple = GetVfxMeshes();
        if (vfxTuple != null)
        {
            for (int i = 0; i < vfxTuple.Item1.Count; i++)
            {
                if (vfxTuple.Item1[i] != null)
                {
                    var mesh = vfxTuple.Item1[i];
                    UMaterialInstanceConstant[] material = new UMaterialInstanceConstant[1];
                    SMesh(mesh, exportParts);
                    if (vfxTuple.Item2[i] != null)
                    {
                        material[0] = vfxTuple.Item2[i];
                        OverrideMaterials(material, exportParts.Last().OverrideMaterials);
                    }
                    if (vfxTuple.Item3[i] != null)
                    {
                        exportParts.First().Attatchments.Add(vfxTuple.Item3[i]);
                    }
                }
            }
        }
        */
    }
    
    public static int Mesh(USkeletalMesh? skeletalMesh, List<ExportPart> exportParts)
    {
        if (skeletalMesh is null) return -1;
        if (!skeletalMesh.TryConvert(out var convertedMesh)) return -1;
        if (convertedMesh.LODs.Count <= 0) return -1;

        var exportPart = new ExportPart();
        exportPart.MeshPath = skeletalMesh.GetPathName();
        exportPart.MeshName = skeletalMesh.Name + "_LOD0.ao";
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
                exportMaterial.ParentName = materialInstance.Parent.Name;
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
        exportPart.MeshName = staticMesh.Name + "_LOD0.mo";
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
                exportMaterial.ParentName = materialInstance.Parent.Name;
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
                MaterialNameToSwap = material.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.PlainText.SubstringAfterLast("."),
            };

            if (material is UMaterialInstanceConstant materialInstance)
            {
                var (textures, scalars, vectors) = MaterialParameters(materialInstance);
                exportMaterial.Textures = textures;
                exportMaterial.Scalars = scalars;
                exportMaterial.Vectors = vectors;
                exportMaterial.ParentName = material.Parent.Name;
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
                        String SavedFilePath;
                        exporter.TryWriteToDir(App.AssetsFolder, out _, out SavedFilePath);
                        break;
                    }

                    case UStaticMesh staticMesh:
                    {
                        var path = GetExportPath(obj, "pskx");
                        if (File.Exists(path)) return;

                        var exporter = new MeshExporter(staticMesh, ExportOptions, false);
                        String SavedFilePath;
                        exporter.TryWriteToDir(App.AssetsFolder, out _, out SavedFilePath);
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