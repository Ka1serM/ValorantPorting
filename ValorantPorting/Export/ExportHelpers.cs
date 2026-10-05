using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CUE4Parse_Conversion;
using CUE4Parse_Conversion.Options;
using CUE4Parse_Conversion.Exporters;
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
using ValorantPorting.AppUtils;

namespace ValorantPorting.Export;

public static class ExportHelpers
{
    public static readonly List<Task> Tasks = new();

    private static readonly ExportOptions ExportOptions = new(
        meshFormat: EMeshFormat.ActorX,
        texturePlatform: ETexturePlatform.DesktopMobile,
        textureFormat: ETextureFormat.Png,
        exportMaterials: false,
        exportMorphTargets: false
    );
    
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
            var exports = AppVM.CUE4ParseVM.Provider.LoadPackageObjects(characterSelectFxc.GetPathName().Substring(0, characterSelectFxc.GetPathName().LastIndexOf(".")));
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
        LogSilencerDiagnostic($"[Weapon] mainAsset={mainAsset.Name}, properties=" +
            string.Join(", ", mainAsset.Properties.Select(p => p.Name.Text)));
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
        var handledStyleGun = style != null ? HandleStyle(style) : null;
        if (handledStyleGun != null)
            //get 3P overwrites for 1P gun because riot games ;-;
            OverrideMaterials(handledStyleGun.GetOrDefault("3p Material Overrides", Array.Empty<UMaterialInstanceConstant>()), exportParts.Last().StyleMaterials);
        //mag mesh
        if (levelTuple.Item4 != null)
        {
            SMesh(levelTuple.Item4, exportParts);
            if (levelTuple.Item2 != null) OverrideMaterials(levelTuple.Item2, exportParts.Last().OverrideMaterials);
        }
        else
        {
            SMesh(GetMagMesh(), exportParts);
            if (levelTuple.Item2 != null) OverrideMaterials(levelTuple.Item2, exportParts.Last().OverrideMaterials);
        }

        //handle style materials for mag mesh
        var handledStyleMag = style != null ? HandleStyle(style) : null;
        if (handledStyleMag != null)
        {
            var magOverrides = handledStyleMag.GetOrDefault("3pMagazineMaterial Overrides", Array.Empty<UMaterialInstanceConstant>());
            if (magOverrides.Length == 0)
                magOverrides = handledStyleMag.GetOrDefault("1pMagazine MaterialOverrides", Array.Empty<UMaterialInstanceConstant>());
            if (magOverrides.Length == 0 && handledStyleGun != null)
                magOverrides = handledStyleGun.GetOrDefault("3p Material Overrides", Array.Empty<UMaterialInstanceConstant>());
            OverrideMaterials(magOverrides, exportParts.Last().StyleMaterials);
        }

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
                // GetWeaponAttatchments always returns fixed-size-2 arrays even when a weapon only
                // has one real attachment (e.g. Operator-class scopes with no silencer slot) - the
                // unfilled slot has a null mesh and must be skipped entirely, or exportParts.Last()
                // silently stays pointed at the previous real attachment and gets its materials
                // overwritten by this phantom entry's fallback (confirmed via diagnostic log: the
                // sniper scope's correct materials were immediately overwritten by the main body's).
                if (attachmentTuple.Item2[i] == null) continue;

                Mesh(attachmentTuple.Item2[i], exportParts);
                var scope_tach = new ExportAttatchment();
                scope_tach.BoneName = attachmentTuple.Item1[i];
                scope_tach.AttatchmentName = exportParts.Last().MeshName;
                exportParts.First().Attatchments.Add(scope_tach);
                if (attachmentTuple.Item3[i] != null) OverrideMaterials(attachmentTuple.Item3[i], exportParts.Last().OverrideMaterials);
                
                                //handle attachment style mats
                if (style != null)
                {
                    bool foundAttachmentMats = false;
                    
                    //scope, muzzle
                    string[] matNames = { "3p MaterialOverrides", "1p MaterialOverrides" };
                    foreach (var matName in matNames)
                    {
                        var styleAttachmentMats = GetStyleAttatchmentMats(style, matName, attachmentTuple.Item1[i]);
                        if (styleAttachmentMats != null)
                        {
                            OverrideMaterials(styleAttachmentMats, exportParts.Last().StyleMaterials);
                            foundAttachmentMats = true;
                        }
                    }
                    
                    LogSilencerDiagnostic($"[call site] socket={attachmentTuple.Item1[i]}, mesh={exportParts.Last().MeshName}, foundAttachmentMats={foundAttachmentMats}, handledStyleGun null={handledStyleGun == null}");

                    // Fallback: some skins store all chroma materials (gun + attachments) in the main chroma CDO
                    if (!foundAttachmentMats && handledStyleGun != null)
                    {
                        var fallbackMats = handledStyleGun.GetOrDefault("3p Material Overrides", Array.Empty<UMaterialInstanceConstant>());
                        LogSilencerDiagnostic($"[call site] fallback check for socket={attachmentTuple.Item1[i]}, fallbackMats.Length={fallbackMats.Length}");
                        if (fallbackMats.Length > 0)
                            OverrideMaterials(fallbackMats, exportParts.Last().StyleMaterials);
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
                ready.TryGetValue(out USkeletalMesh cosmeticMesh, "Weapon 1P Cosmetic");
                ready.TryGetValue(out USkeletalMesh actualWeaponMesh, "Weapon 1P");
                ready.TryGetValue(out USkeletalMesh newMesh, "NewMesh");

                // Default true = safe fallback (matches old baseline) if we can't read bone data at all.
                bool cosmeticLooksLikeAWeapon = true;

                // Real gun-mechanism bone names confirmed present on every tested weapon mesh
                // (Cyberknight, Revolver Lv2 Edge, Daedalus) and absent on the Aquarium2 fish mesh.
                // "Magazine_Main" (the original guess) never actually exists on any of these meshes -
                // that's why both attempt 2a and 2b failed no matter which way the default was flipped.
                string[] weaponIndicatorBones = { "Muzzle", "Mag_Holder", "Hammer", "Gun_Buddy", "Magazine_Extra" };

                if (cosmeticMesh != null)
                {
                    var logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    var logPath = System.IO.Path.Combine(logDir, "bonecheck_diagnostics.log");
                    try
                    {
                        System.IO.Directory.CreateDirectory(logDir);
                        var sb = new System.Text.StringBuilder();
                        sb.AppendLine($"--- {DateTime.Now:HH:mm:ss} ---");
                        sb.AppendLine($"Mesh Name: {cosmeticMesh.Name}");

                        var refSkeleton = cosmeticMesh.ReferenceSkeleton;
                        if (refSkeleton == null)
                        {
                            sb.AppendLine("ReferenceSkeleton is null.");
                        }
                        else
                        {
                            var rsType = refSkeleton.GetType();
                            sb.AppendLine($"ReferenceSkeleton CLR Type: {rsType.FullName}");
                            sb.AppendLine("All members:");

                            object boneArray = null;
                            string boneArrayMemberName = null;

                            foreach (var prop in rsType.GetProperties())
                            {
                                object val = null;
                                try { val = prop.GetValue(refSkeleton); } catch { }
                                var countStr = "";
                                if (val is System.Collections.IEnumerable en && !(val is string))
                                {
                                    var count = 0;
                                    foreach (var _ in en) count++;
                                    countStr = $" (Count={count})";
                                    if (boneArray == null && count > 0) { boneArray = val; boneArrayMemberName = prop.Name; }
                                }
                                sb.AppendLine($"  [property] {prop.Name} : {prop.PropertyType.Name}{countStr}");
                            }
                            foreach (var field in rsType.GetFields())
                            {
                                object val = null;
                                try { val = field.GetValue(refSkeleton); } catch { }
                                var countStr = "";
                                if (val is System.Collections.IEnumerable en && !(val is string))
                                {
                                    var count = 0;
                                    foreach (var _ in en) count++;
                                    countStr = $" (Count={count})";
                                    if (boneArray == null && count > 0) { boneArray = val; boneArrayMemberName = field.Name; }
                                }
                                sb.AppendLine($"  [field] {field.Name} : {field.FieldType.Name}{countStr}");
                            }

                            if (boneArray is System.Collections.IEnumerable boneList)
                            {
                                sb.AppendLine($"Dumping bone names from '{boneArrayMemberName}':");
                                var boneNames = new System.Collections.Generic.List<string>();
                                foreach (var boneInfo in boneList)
                                {
                                    if (boneInfo == null) continue;
                                    var boneInfoType = boneInfo.GetType();
                                    object nameMember = (object)boneInfoType.GetField("Name") ?? (object)boneInfoType.GetProperty("Name");
                                    object nameValue = nameMember switch
                                    {
                                        System.Reflection.FieldInfo f => f.GetValue(boneInfo),
                                        System.Reflection.PropertyInfo p => p.GetValue(boneInfo),
                                        _ => null
                                    };
                                    string boneNameText = null;
                                    if (nameValue != null)
                                    {
                                        var textProp = nameValue.GetType().GetProperty("Text");
                                        boneNameText = textProp != null ? textProp.GetValue(nameValue)?.ToString() : nameValue.ToString();
                                    }
                                    boneNames.Add(boneNameText ?? "(null)");
                                }
                                sb.AppendLine("  " + string.Join(", ", boneNames));
                                cosmeticLooksLikeAWeapon = boneNames.Any(n => weaponIndicatorBones.Contains(n, StringComparer.OrdinalIgnoreCase));
                                sb.AppendLine($"  Matched a weapon-indicator bone: {cosmeticLooksLikeAWeapon}");
                            }
                            else
                            {
                                sb.AppendLine("Could not find any non-empty enumerable member to use as a bone array.");
                            }
                        }

                        System.IO.File.AppendAllText(logPath, sb.ToString() + "\n");
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            System.IO.Directory.CreateDirectory(logDir);
                            System.IO.File.AppendAllText(logPath, $"--- {DateTime.Now:HH:mm:ss} --- EXCEPTION: {ex}\n\n");
                        }
                        catch { }
                    }
                }

                USkeletalMesh localMeshUsed = cosmeticLooksLikeAWeapon
                    ? (cosmeticMesh ?? actualWeaponMesh ?? newMesh)
                    : (actualWeaponMesh ?? newMesh ?? cosmeticMesh);
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
        if (mainAsset.TryGetValue(out UBlueprintGeneratedClass equippable, "Equippable"))
        {
            var classDefaultObject = equippable.ClassDefaultObject.Load();
            if (classDefaultObject.TryGetValue(out UBlueprintGeneratedClass localEqippable, "Equippable"))
            {
                var loadedEquippable = localEqippable.ClassDefaultObject.Load();
                if (loadedEquippable.TryGetValue(out UObject objectReturn, "Mesh1P") &&
                    objectReturn.TryGetValue(out USkeletalMesh skeletalMesh, "SkeletalMesh"))
                    return skeletalMesh;
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
                var mainObjectExports = AppVM.CUE4ParseVM.Provider.LoadPackageObjects(localEquippable.GetPathName().Substring(0, localEquippable.GetPathName().LastIndexOf(".")));
                foreach (var export in mainObjectExports)
                    if (export.Name.Contains("Magazine_1P") && export.TryGetValue(out UStaticMesh staticMesh, "StaticMesh"))
                        return staticMesh;
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

            LogSilencerDiagnostic($"[GetWeaponAttatchments] CDO={classDefaultObject.Name}, properties=" +
                string.Join(", ", classDefaultObject.Properties.Select(p => p.Name.Text)));

            string[] scope = { "1pReflexMesh", "MaterialOverrides", "Reflex" };
            string[] silencer = { "1p Mesh", "3p MaterialOverrides", "Barrel" };
            var currentAttatchList = new List<List<string>>();
            currentAttatchList.Add(new List<string>(scope));
            currentAttatchList.Add(new List<string>(silencer));
            // 
            for (var i = 0; i < currentAttatchList.Count; i++)
            {
                var currentAttach = currentAttatchList[i];
                classDefaultObject.TryGetValue(out USkeletalMesh localMesh, currentAttach[0]);
                classDefaultObject.TryGetValue(out UMaterialInstanceConstant[] localmat, currentAttach[1]);

                if (localMesh == null)
                {
                    var parentClass = valueLoaded.SuperStruct?.Load<UBlueprintGeneratedClass>();
                    var parentCdo = parentClass?.ClassDefaultObject?.Load();
                    LogSilencerDiagnostic($"[GetWeaponAttatchments] '{currentAttach[0]}' missing on leaf CDO {classDefaultObject.Name}. " +
                        $"Parent class={parentClass?.Name ?? "null"}, parent CDO={parentCdo?.Name ?? "null"}, " +
                        $"parent properties=" + (parentCdo != null ? string.Join(", ", parentCdo.Properties.Select(p => p.Name.Text)) : "n/a"));
                }

                if (localMesh == null) continue;
                if (meshes[i] != null)
                {
                    LogSilencerDiagnostic($"[GetWeaponAttatchments] OVERWRITING slot {i} ({currentAttach[2]}): " +
                        $"'{meshes[i].Name}' (from CDO {classDefaultObject.Name}'s predecessor) -> '{localMesh.Name}'");
                }
                fullSockets[i] = currentAttach[2];
                meshes[i] = localMesh;
                fullOverrideMaterials[i] = localmat;
                paramNames[i] = currentAttach[1];
            }
        }

        return Tuple.Create(fullSockets, meshes, fullOverrideMaterials, paramNames);
    }

    private static void LogSilencerDiagnostic(string line)
    {
        try
        {
            var logDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            System.IO.Directory.CreateDirectory(logDir);
            var logPath = System.IO.Path.Combine(logDir, "silencer_diagnostics.log");
            System.IO.File.AppendAllText(logPath, line + "\n");
        }
        catch { }
    }

    public static UMaterialInstanceConstant[] GetStyleAttatchmentMats(UObject style, string paramName, string socketName)
    {
        var bpGnCast = style as UBlueprintGeneratedClass;
        var styleClassDefaultObject = bpGnCast.ClassDefaultObject.Load();
        
        // Try the drilled chroma CDO first (matches HandleStyle()'s proven-correct precedence
        // for the gun body itself); raw style CDO as fallback. The raw CDO can carry valid but
        // wrong inherited/default attachment data that would otherwise match before the real
        // chroma-specific data ever gets checked.
        var sources = new List<UObject>();
        if (styleClassDefaultObject.TryGetValue(out UBlueprintGeneratedClass chromaBp, "EquippableSkinChroma"))
        {
            var chromaCdo = chromaBp.ClassDefaultObject.Load();
            if (chromaCdo != null)
                sources.Add(chromaCdo);
        }
        sources.Add(styleClassDefaultObject);
        
        // Try multiple property name variants (skins use different naming conventions)
        var paramNamesToTry = new List<string> { paramName };
        if (paramName == "3p MaterialOverrides")
        {
            paramNamesToTry.Add("MaterialOverrides");
            paramNamesToTry.Add("3p Material Overrides");
        }
        else if (paramName == "1p MaterialOverrides")
        {
            paramNamesToTry.Add("1p Material Overrides");
        }
        
        LogSilencerDiagnostic($"--- {DateTime.Now:HH:mm:ss} --- GetStyleAttatchmentMats socketName={socketName}, paramName={paramName}, sources={sources.Count}");

        foreach (var source in sources)
        {
            if (!source.TryGetValue(out UScriptMap styleAttachmentOverrides, "AttachmentOverrides"))
            {
                LogSilencerDiagnostic("  source has no AttachmentOverrides map, skipping.");
                continue;
            }

            LogSilencerDiagnostic($"  source has AttachmentOverrides map with {styleAttachmentOverrides.Properties.Count} entries.");

            foreach (var scriptMapVariable in styleAttachmentOverrides.Properties)
            {
                var scriptMapValue = (FSoftObjectPath)scriptMapVariable.Value.GenericValue;
                var valueLoaded = (UBlueprintGeneratedClass)scriptMapValue.Load();
                var classDefaultObject = valueLoaded.ClassDefaultObject.Load();

                try
                {
                    var propNames = new List<string>();
                    foreach (var prop in classDefaultObject.Properties)
                    {
                        try
                        {
                            var nameObj = prop.Name;
                            var textProp = nameObj.GetType().GetProperty("Text");
                            propNames.Add(textProp != null ? textProp.GetValue(nameObj)?.ToString() : nameObj.ToString());
                        }
                        catch { propNames.Add("(unreadable)"); }
                    }
                    LogSilencerDiagnostic($"    entry properties: {string.Join(", ", propNames)}");
                }
                catch (Exception ex)
                {
                    LogSilencerDiagnostic($"    entry property dump failed: {ex.Message}");
                }

                // Match attachment by socket name
                string[] scope = { "1pReflexMesh", "MaterialOverrides", "Reflex" };
                string[] silencer = { "1p Mesh", "3p MaterialOverrides", "Barrel" };
                var checkList = new List<string[]> { scope, silencer };
                
                foreach (var check in checkList)
                {
                    classDefaultObject.TryGetValue(out USkeletalMesh mesh, check[0]);
                    LogSilencerDiagnostic($"      check[{check[2]}]: has '{check[0]}' mesh = {mesh != null}");
                    // Mesh presence is informational only — a chroma-only override entry can
                    // carry valid material data with no mesh reference at all (confirmed via
                    // diagnostic log on Gaia/Ashen), so it must not gate the material lookup.
                    if (check[2] == socketName)
                    {
                        foreach (var tryParamName in paramNamesToTry)
                        {
                            classDefaultObject.TryGetValue(out UMaterialInstanceConstant[] materials, tryParamName);
                            if (materials != null && materials.Length > 0)
                            {
                                LogSilencerDiagnostic($"      MATCHED via '{tryParamName}', {materials.Length} materials.");
                                return materials;
                            }
                        }
                    }
                }
            }
        }

        LogSilencerDiagnostic("  No match found in GetStyleAttatchmentMats, returning null (fallback path may trigger).");
        return null;
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

        var sections = convertedMesh.LODs[0].Sections;
        for (var idx = 0; idx < sections.Length; idx++)
        {
            var section = sections[idx];
            var materialSlot = convertedMesh.GetMaterial(section);
            if (materialSlot is not { Material: { } materialRef }) continue;

            if (!materialRef.TryLoad(out var material)) continue;

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
        exportPart.MeshName = staticMesh.Name + ".mo";
        Save(staticMesh);

        var sections = convertedMesh.LODs[0].Sections;
        for (var idx = 0; idx < sections.Length; idx++)
        {
            var section = sections[idx];
            var materialSlot = convertedMesh.GetMaterial(section);
            if (materialSlot is not { Material: { } materialRef }) continue;

            if (!materialRef.TryLoad(out var material)) continue;

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
        if (overrides is null) return;
        for (var i = 0; i < overrides.Length; i++)
        {
            var material = overrides[i];
            if (material is null) continue;

            try
            {
                var swapPath = material.GetOrDefault<FSoftObjectPath>("MaterialToSwap").AssetPathName.PlainText;
                var exportMaterial = new ExportMaterial
                {
                    MaterialName = material.Name,
                    SlotIndex = i,
                    MaterialNameToSwap = string.IsNullOrEmpty(swapPath) ? string.Empty : swapPath.SubstringAfterLast(".")
                };

                if (material is UMaterialInstanceConstant materialInstance)
                {
                    var (textures, scalars, vectors) = MaterialParameters(materialInstance);
                    exportMaterial.Textures = textures;
                    exportMaterial.Scalars = scalars;
                    exportMaterial.Vectors = vectors;
                    if (material.Parent != null)
                        exportMaterial.ParentName = material.Parent.Name;
                }

                exportMaterials.Add(exportMaterial);
            }
            catch (Exception ex)
            {
                AppLog.Warning($"Skipped a material override due to an error: {ex.Message}");
            }
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

        if (materialInstance.Parent != null && materialInstance.Parent.TryLoad(out UMaterialInstanceConstant parent))
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

                        var session = new ExportSession();
                        session.Add(skeletalMesh);
                        session.RunAsync(App.AssetsFolder.FullName, ExportOptions).GetAwaiter().GetResult();
                        break;
                    }

                    case UStaticMesh staticMesh:
                    {
                        var path = GetExportPath(obj, "pskx");
                        if (File.Exists(path)) return;

                        var session = new ExportSession();
                        session.Add(staticMesh);
                        session.RunAsync(App.AssetsFolder.FullName, ExportOptions).GetAwaiter().GetResult();
                        break;
                    }
                    case UTexture2D texture:
                    {
                        var path = GetExportPath(obj, "png");
                        if (File.Exists(path)) return;
                        Directory.CreateDirectory(path.Replace('\\', '/').SubstringBeforeLast('/'));

                        var cTexture = texture.Decode(texture.GetFirstMip());
                        if (cTexture is null) return;
                        var imageData = cTexture.Encode(ETextureFormat.Png, false, out _);
                        File.WriteAllBytes(path, imageData);
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
