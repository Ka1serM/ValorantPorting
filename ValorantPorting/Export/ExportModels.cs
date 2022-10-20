using System.Collections.Generic;
using CUE4Parse.UE4.Objects.Core.Math;
using Newtonsoft.Json;

namespace ValorantPorting.Export;

public class ExportPart
{
    public string MeshPath;
    public string MeshName;
    public string? Part;
    public List<ExportMaterial> Materials = new();
    public List<ExportMaterial> OverrideMaterials = new();
    public List<ExportAttatchment> Attatchments = new();
}

public record ExportMaterial
{
    public string MaterialName;
    public int SlotIndex;
    public string? MaterialNameToSwap;
    public List<TextureParameter> Textures = new();
    public List<ScalarParameter> Scalars = new();
    public List<VectorParameter> Vectors = new();
    public string ParentName;
}

public record ExportAttatchment
{
    public string AttatchmentName;
    public string BoneName;
}
public record TextureParameter(string Name, string Value);

public record ScalarParameter(string Name, float Value);

public record VectorParameter(string Name, FLinearColor Value);