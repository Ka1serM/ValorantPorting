using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
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

namespace ValorantPorting.Export;

public static class ExportHelpers
{
    public static readonly List<Task> Tasks = new();

    private static readonly ExporterOptions ExportOptions = new()
    {
        Platform = ETexturePlatform.DesktopMobile,
        LodFormat = ELodFormat.AllLods,
        MeshFormat = EMeshFormat.ActorX,
        TextureFormat = ETextureFormat.Png,
        ExportMorphTargets = false
    };
    
    public static void GunBuddy(List<ExportPart> exportParts, UObject asset)
    {
        if (asset.TryGetValue(out UObject charm, "Charm"))
        {
            if (charm is UStaticMesh staticMesh)
            {
                SMesh(staticMesh, exportParts);
            }
            else if (charm is USkeletalMesh skeletalMesh)
            {
                Mesh(skeletalMesh, exportParts);
            }
        }
    }
    
    public static void Character(List<ExportPart> exportParts, UObject asset)
    {
        var components = new List<UObject>();
        //1P Mesh
        if (asset.TryGetValue(out UObject meshOverlay1P, "MeshOverlay1P"))
        {
            if (meshOverlay1P.Properties.Count < 2 && asset.TryGetValue(out UObject mesh1P, "Mesh1P"))
            {
                components.Add(mesh1P);
            }
            else
            {
                components.Add(meshOverlay1P);
            }
        }
        //3P Mesh
        if (asset.TryGetValue(out UObject meshCosmetic3P, "MeshCosmetic3P"))
        {
            components.Add(meshCosmetic3P);
        }
        //CS Mesh
        if (AppVM.MainVM.CurrentAsset.MainAsset.TryGetValue(out UObject characterSelectFxc, "CharacterSelectFXC"))
        {
            var exports = AppVM.CUE4ParseVM.Provider.LoadAllObjects(characterSelectFxc.GetPathName().Substring(0, characterSelectFxc.GetPathName().LastIndexOf(".")));
            foreach (var export in exports)
            {
                if (export.ExportType == "SkeletalMeshComponent" && export.Name == "SkeletalMesh_GEN_VARIABLE") components.Add(export);
            }
        }
        
        foreach (var component in components)
        {
            if (component.TryGetValue(out USkeletalMesh skelMesh, "SkeletalMesh"))
            {
                Mesh(skelMesh, exportParts);
                if (skelMesh.TryGetValue(out UMaterialInstanceConstant[] materialOverrides, "MaterialOverrides"))
                    OverrideMaterials(materialOverrides, exportParts.Last().OverrideMaterials);
            }
        }
    }
    
    
    public static void Weapon(List<ExportPart> exportParts, UObject style)
    {
        var mainAsset = AppVM.MainVM.CurrentAsset.MainAsset;
        var levelTuple = GetHighestLevel();
        //gun mesh
        if (levelTuple.Item1 != null)
        {
            Mesh(levelTuple.Item1, exportParts);
            if (levelTuple.Item2 != null) OverrideMaterials(levelTuple.Item2, exportParts.Last().OverrideMaterials);
        }
        else //if not in asset, use base gun mesh
        {
            Mesh(GetBaseWeapon(), exportParts);
            if (levelTuple.Item2 != null) OverrideMaterials(levelTuple.Item2, exportParts.Last().OverrideMaterials);
        }
        //handle style materials for gun mesh
        if (style != null && HandleStyle(style) != null)
            //get 3P overwrites for 1P gun because riot games ;-;
            OverrideMaterials(HandleStyle(style).GetOrDefault("3p Material Overrides", Array.Empty<UMaterialInstanceConstant>()), exportParts.Last().StyleMaterials);
        //mag mesh
        if (levelTuple.Item4 != null)
        {
            SMesh(levelTuple.Item4, exportParts);
            if (levelTuple.Item3 != null) OverrideMaterials(levelTuple.Item2, exportParts.Last().OverrideMaterials);
        }
        else
        {
            SMesh(GetMagMesh(), exportParts);
            if (levelTuple.Item3 != null) OverrideMaterials(levelTuple.Item2, exportParts.Last().OverrideMaterials);
        }

        //handle style materials for mag mesh
        if (style != null && HandleStyle(style) != null)
            OverrideMaterials(HandleStyle(style).GetOrDefault("1pMagazine MaterialOverrides", Array.Empty<UMaterialInstanceConstant>()), exportParts.Last().StyleMaterials);

        //attach mag to gun body
        var attachMag = new ExportAttatchment();
        attachMag.BoneName = "Magazine_Main";
        attachMag.AttatchmentName = exportParts.Last().MeshName;
        exportParts.First().Attatchments.Add(attachMag);

        //attachment (scope & silencer)
        if (mainAsset.TryGetValue(out UScriptMap attachmentOverrides, "AttachmentOverrides"))
        {
            var attachmentTuple = GetWeaponAttatchments(attachmentOverrides);
            for (var i = 0; i < attachmentTuple.Item2.Length; i++)
            {
                Mesh(attachmentTuple.Item2[i], exportParts);
                var scope_tach = new ExportAttatchment();
                scope_tach.BoneName = attachmentTuple.Item1[i];
                scope_tach.AttatchmentName = exportParts.Last().MeshName;
                exportParts.First().Attatchments.Add(scope_tach);
                if (attachmentTuple.Item3[i] != null) OverrideMaterials(attachmentTuple.Item3[i], exportParts.Last().OverrideMaterials);
                
                //handle attachment style mats
                if (style != null)
                {
                    //scope, muzzle
                    string[] matNames = { "3pMaterialOverrides", "1p MaterialOverrides" };
                    foreach (var matName in matNames)
                    {
                        if (GetStyleAttatchmentMats(style, matName) != null)
                            OverrideMaterials(GetStyleAttatchmentMats(style, matName), exportParts.Last().StyleMaterials);
                    }
                }
            }
        }
    }
    
    public static UObject? HandleStyle(UObject style)
    {
        var bpGnCast = style as UBlueprintGeneratedClass;
        var styleClassDefaultObject = bpGnCast.ClassDefaultObject.Load();
        if (styleClassDefaultObject.TryGetValue(out UBlueprintGeneratedClass attachmentOverrides, "EquippableSkinChroma")) 
            return attachmentOverrides.ClassDefaultObject.Load();
        return null;
    }

    public static Tuple<USkeletalMesh, UMaterialInstanceConstant[], UMaterialInstanceConstant[], UStaticMesh>
        GetHighestLevel()
    {
        var mainAsset = AppVM.MainVM.CurrentAsset.MainAsset;
        // 
        USkeletalMesh highestMeshUsed = null;
        UMaterialInstanceConstant[] highestWeapMaterialUsed = { };
        UMaterialInstanceConstant[] highestMagMaterialUsed = { };
        UStaticMesh highestMagMeshUsed = null;
        //
        mainAsset.TryGetValue(out UBlueprintGeneratedClass[] levels, "Levels");
        for (var i = 0; i < levels.Length; i++)
        {
            var activeO = levels[i];
            var cdoLo = activeO.ClassDefaultObject.Load();
            UBlueprintGeneratedClass localUob;
            if (cdoLo.TryGetValue(out localUob, "SkinAttachment"))
            {
                var ready = localUob.ClassDefaultObject.Load();
                ready.TryGetValue(out USkeletalMesh localMeshUsed, "Weapon 1P Cosmetic", "Weapon 1P", "NewMesh");
                if (localMeshUsed != null) highestMeshUsed = localMeshUsed;
                ready.TryGetValue(out UMaterialInstanceConstant[] localMatUsed, "1p MaterialOverrides");
                if (localMatUsed != null) highestWeapMaterialUsed = localMatUsed;
                ready.TryGetValue(out UMaterialInstanceConstant[] magOverrides, "1pMagazine MaterialOverrides");
                if (magOverrides != null) highestMagMaterialUsed = magOverrides;
                ready.TryGetValue(out UStaticMesh magMesh, "Magazine 1P", "SpeedLoader");
                if (magMesh != null) highestMagMeshUsed = magMesh;
            }
        }

        return Tuple.Create(highestMeshUsed, highestWeapMaterialUsed, highestMagMaterialUsed, highestMagMeshUsed);
    }
    
    
    
    public static USkeletalMesh GetBaseWeapon()
    {
        var mainAsset = AppVM.MainVM.CurrentAsset.MainAsset;
        if (mainAsset.TryGetValue(out UBlueprintGeneratedClass equippable, "Equippable")) ;
        {
            var classDefaultObject = equippable.ClassDefaultObject.Load();
            if (classDefaultObject.TryGetValue(out UBlueprintGeneratedClass localEqippable, "Equippable"))
            {
                var loadedEquippable = localEqippable.ClassDefaultObject.Load();
                if (loadedEquippable.TryGetValue(out UObject objectReturn, "Mesh1P"))
                    return objectReturn.Get<USkeletalMesh>("SkeletalMesh");
            }
        }
        return null;
    }

    // for some reason the mag mash is not in the properties here so gotta load all exports
    public static UStaticMesh GetMagMesh()
    {
        var mainAsset = AppVM.MainVM.CurrentAsset.MainAsset;
        if (mainAsset.TryGetValue(out UBlueprintGeneratedClass equippable, "Equippable"))
        {
            var classDefaultObject = equippable.ClassDefaultObject.Load();
            if (classDefaultObject.TryGetValue(out UObject localEquippable, "Equippable"))
            {
                var mainObjectExports = AppVM.CUE4ParseVM.Provider.LoadAllObjects(localEquippable.GetPathName().Substring(0, localEquippable.GetPathName().LastIndexOf(".")));
                foreach (var export in mainObjectExports)
                    if (export.Name.Contains("Magazine_1P"))
                        return export.Get<UStaticMesh>("StaticMesh");
            }
        }
        return null;
    }
    
    public static Tuple<string[], USkeletalMesh[], UMaterialInstanceConstant[][], string[]> GetWeaponAttatchments(
        UScriptMap scriptMap)
    {
        // initializer for return tuple stuff
        var fullSockets = new string[2];
        var fullOverrideMaterials = new UMaterialInstanceConstant[2][];
        var meshes = new USkeletalMesh[2];
        var paramNames = new string[2];
        //  loop 
        foreach (var scriptMapVariable in scriptMap.Properties)
        {
            var scriptMapValue = (FSoftObjectPath)scriptMapVariable.Value.GenericValue;
            var valueLoaded = (UBlueprintGeneratedClass)scriptMapValue.Load();
            var classDefaultObject = valueLoaded.ClassDefaultObject.Load();

            string[] scope = { "1pReflexMesh", "MaterialOverrides", "Reflex" };
            string[] silencer = { "1p Mesh", "1p MaterialOverrides", "Barrel" };
            var currentAttatchList = new List<List<string>>();
            currentAttatchList.Add(new List<string>(scope));
            currentAttatchList.Add(new List<string>(silencer));
            // 
            for (var i = 0; i < currentAttatchList.Count; i++)
            {
                var currentAttach = currentAttatchList[i];
                classDefaultObject.TryGetValue(out USkeletalMesh localMesh, currentAttach[0]);
                classDefaultObject.TryGetValue(out UMaterialInstanceConstant[] localmat, currentAttach[1]);
                if (localMesh == null) continue;
                fullSockets[i] = currentAttach[2];
                meshes[i] = localMesh;
                fullOverrideMaterials[i] = localmat;
                paramNames[i] = currentAttach[1];
            }
        }

        return Tuple.Create(fullSockets, meshes, fullOverrideMaterials, paramNames);
    }

    public static UMaterialInstanceConstant[] GetStyleAttatchmentMats(UObject style, string paramName)
    {
        var bpGnCast = style as UBlueprintGeneratedClass;
        var styleClassDefaultObject = bpGnCast.ClassDefaultObject.Load();
        if (styleClassDefaultObject.TryGetValue(out UScriptMap styleAttachmentOverrides, "AttachmentOverrides"))
            //  loop 
            foreach (var scriptMapVariable in styleAttachmentOverrides.Properties)
            {
                var scriptMapValue = (FSoftObjectPath)scriptMapVariable.Value.GenericValue;
                var valueLoaded = (UBlueprintGeneratedClass)scriptMapValue.Load();
                var classDefaultObject = valueLoaded.ClassDefaultObject.Load();
                classDefaultObject.TryGetValue(out UMaterialInstanceConstant[] materials, paramName);
                return materials;
            }

        return null;
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
                if(materialInstance.Parent != null)
                    exportMaterial.ParentName = materialInstance.Parent.Name;
            }

            exportPart.Materials.Add(exportMaterial);
        }

        exportParts.Add(exportPart);
        return exportParts.Count - 1;
    }

    public static void OverrideMaterials(UMaterialInstanceConstant[] overrides, List<ExportMaterial> exportMaterials)
    {
        for (var i = 0; i < overrides.Length; i++)
        {
            var material = overrides[i];
            var exportMaterial = new ExportMaterial
            {
                MaterialName = material.Name,
                SlotIndex = i,
                MaterialNameToSwap = material.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.PlainText
                    .SubstringAfterLast(".")
            };

            if (material is UMaterialInstanceConstant materialInstance)
            {
                var (textures, scalars, vectors) = MaterialParameters(materialInstance);
                exportMaterial.Textures = textures;
                exportMaterial.Scalars = scalars;
                exportMaterial.Vectors = vectors;
                if(material.Parent != null)
                    exportMaterial.ParentName = material.Parent.Name;
            }

            exportMaterials.Add(exportMaterial);
        }
    }

    public static (List<TextureParameter>, List<ScalarParameter>, List<VectorParameter>) MaterialParameters(UMaterialInstanceConstant materialInstance)
    {
        var textures = new List<TextureParameter>();
        var scalars = new List<ScalarParameter>();
        var vectors = new List<VectorParameter>();
        
        ParentMaterialInstanceParameters(materialInstance, textures, scalars, vectors);
        return (textures, scalars, vectors);
    }

    public static void ParentMaterialInstanceParameters(UMaterialInstanceConstant materialInstance, List<TextureParameter> textures, List<ScalarParameter> scalars, List<VectorParameter> vectors)
    {
        if (materialInstance == null) return;
        foreach (var parameter in materialInstance.TextureParameterValues)
        {
            if (parameter == null) continue;
            if (!parameter.ParameterValue.TryLoad(out UTexture2D texture)) continue;
            if (textures.Any(x => x.Name.Equals(parameter.Name))) continue;
            textures.Add(new TextureParameter(parameter.ParameterInfo.Name.PlainText, texture.GetPathName()));
            Save(texture);
        }

        foreach (var parameter in materialInstance.ScalarParameterValues)
        {
            if (parameter == null) continue;
            if (scalars.Any(x => x.Name.Equals(parameter.Name))) continue;
            scalars.Add(new ScalarParameter(parameter.ParameterInfo.Name.PlainText, parameter.ParameterValue));
        }

        foreach (var parameter in materialInstance.VectorParameterValues)
        {
            if (parameter == null) continue;
            if (parameter.ParameterValue is null) continue;
            if (vectors.Any(x => x.Name.Equals(parameter.Name))) continue;
            vectors.Add(new VectorParameter(parameter.ParameterInfo.Name.PlainText, parameter.ParameterValue.Value));
        }

        if (materialInstance.Parent != null && materialInstance.Parent is UMaterialInstanceConstant parent)
            ParentMaterialInstanceParameters(parent, textures, scalars, vectors);
    }

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
                        string SavedFilePath;
                        exporter.TryWriteToDir(App.AssetsFolder, out _, out SavedFilePath);
                        break;
                    }

                    case UStaticMesh staticMesh:
                    {
                        var path = GetExportPath(obj, "pskx");
                        if (File.Exists(path)) return;

                        var exporter = new MeshExporter(staticMesh, ExportOptions, false);
                        string SavedFilePath;
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
            catch (IOException)
            {
            }
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