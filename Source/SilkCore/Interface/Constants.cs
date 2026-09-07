/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core2D.Abstract;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Interface;

// Marker structs used for static function overloading

public interface IShaderType {
    static abstract int Index { get; }
}

public interface IShaderResourceType : IShaderType;
public interface ISamplerShaderType : IShaderType;
public interface IUnorderedAccessShaderType : IShaderType;

public readonly struct VertexShaderType :
    IShaderResourceType,
    ISamplerShaderType
{
    public static int Index => Constants.VertexIdx;
}

public readonly struct HullShaderType :
    IShaderResourceType,
    ISamplerShaderType
{
    public static int Index => Constants.HullIdx;
}

public readonly struct DomainShaderType :
    IShaderResourceType,
    ISamplerShaderType
{
    public static int Index => Constants.DomainIdx;
}

public readonly struct GeometryShaderType :
    IShaderResourceType,
    ISamplerShaderType
{
    public static int Index => Constants.GeometryIdx;
}

public readonly struct PixelShaderType :
    IShaderResourceType,
    ISamplerShaderType
{
    public static int Index => Constants.PixelIdx;
}

public readonly struct ComputeShaderType :
    IShaderResourceType,
    ISamplerShaderType,
    IUnorderedAccessShaderType
{
    public static int Index => Constants.ComputeIdx;
}



public static class Constants {
    public const int MaxLights = 8;

    /// Number of shader stages
    public const int NumShaderStages = 6;

    /// Stages that can bind textures
    public const ShaderStage CanBindTextureStages =
        ShaderStage.Vertex | ShaderStage.Pixel | ShaderStage.Domain | ShaderStage.Compute;

    public const int VertexIdx = 0, HullIdx = 1, DomainIdx = 2, GeometryIdx = 3, PixelIdx = 4, ComputeIdx = 5;

    public static readonly char[] Separators = [';', ' ', ','];
    public static readonly FastList<(int Key, SceneNode Value)> EmptyRenderablePair = [];
    public static readonly FastList<SceneNode> EmptyRenderable = [];
    public static readonly List<RenderCore> EmptyCore = [];
    internal static readonly ObservableFastList<SceneNode> EmptyRenderableArray = [];
    internal static readonly ReadOnlyObservableFastList<SceneNode> EmptyReadOnlyRenderableArray;
    internal static readonly ObservableFastList<SceneNode2D> EmptyRenderable2D = [];
    internal static readonly ReadOnlyObservableFastList<SceneNode2D> EmptyReadOnlyRenderable2DArray;
    public static readonly IList<RenderCore2D> EmptyCore2D = [];

    static Constants() {
        EmptyReadOnlyRenderableArray = new ReadOnlyObservableFastList<SceneNode>(EmptyRenderableArray);
        EmptyReadOnlyRenderable2DArray = new ReadOnlyObservableFastList<SceneNode2D>(EmptyRenderable2D);
    }

    /// <summary>
    ///     Convert shader stage into 0~5 stage numbers
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ToIndex(this ShaderStage type) {
        switch (type) {
            case ShaderStage.Vertex:
                return VertexIdx;
            case ShaderStage.Hull:
                return HullIdx;
            case ShaderStage.Domain:
                return DomainIdx;
            case ShaderStage.Geometry:
                return GeometryIdx;
            case ShaderStage.Pixel:
                return PixelIdx;
            case ShaderStage.Compute:
                return ComputeIdx;
            default:
                return -1;
        }
    }

    /// <summary>
    ///     To the shader stage.
    /// </summary>
    /// <param name="index">The index.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ShaderStage ToShaderStage(this int index) {
        switch (index) {
            case VertexIdx:
                return ShaderStage.Vertex;
            case DomainIdx:
                return ShaderStage.Domain;
            case HullIdx:
                return ShaderStage.Hull;
            case GeometryIdx:
                return ShaderStage.Geometry;
            case PixelIdx:
                return ShaderStage.Pixel;
            case ComputeIdx:
                return ShaderStage.Compute;
            default:
                return ShaderStage.None;
        }
    }
}
