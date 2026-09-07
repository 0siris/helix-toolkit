/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Interface;

/// <summary>
/// </summary>
public interface IGeometryRenderCore {
    /// <summary>
    ///     Gets or sets the instance buffer.
    /// </summary>
    /// <value>
    ///     The instance buffer.
    /// </value>
    IElementsBufferModel InstanceBuffer { get; set; }

    /// <summary>
    ///     Gets or sets the geometry buffer.
    /// </summary>
    /// <value>
    ///     The geometry buffer.
    /// </value>
    IAttachableBufferModel? GeometryBuffer { get; set; }

    /// <summary>
    ///     Gets or sets the raster description.
    /// </summary>
    /// <value>
    ///     The raster description.
    /// </value>
    RasterizerStateDescription RasterDescription { get; set; }
}

/// <summary>
/// </summary>
public interface IMaterialRenderParams {
}

/// <summary>
/// </summary>
public interface IMeshRenderParams : IInvertNormal, IMaterialRenderParams {
    bool RenderWireframe { get; set; }

    Color4 WireframeColor { get; set; }
}

/// <summary>
/// </summary>
public interface IDynamicReflector {
    bool IsDynamicScene { get; set; }

    bool EnableReflector { get; set; }

    Vector3 Center { get; set; }

    int FaceSize { get; set; }

    float NearField { get; set; }

    float FarField { get; set; }

    bool IsLeftHanded { get; set; }
}

/// <summary>
/// </summary>
public interface IDynamicReflectable {
    IDynamicReflector? DynamicReflector { get; set; }
}

/// <summary>
/// </summary>
public interface IInvertNormal {
    /// <summary>
    ///     Gets or sets a value indicating whether [invert normal].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [invert normal]; otherwise, <c>false</c>.
    /// </value>
    bool InvertNormal { get; set; }
}

/// <summary>
/// </summary>
public interface IBillboardRenderParams {
    /// <summary>
    ///     Gets or sets the type.
    /// </summary>
    /// <value>
    ///     The type.
    /// </value>
    BillboardType Type { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [fixed size].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [fixed size]; otherwise, <c>false</c>.
    /// </value>
    bool FixedSize { get; set; }

    /// <summary>
    ///     Gets or sets the sampler description.
    /// </summary>
    /// <value>
    ///     The sampler description.
    /// </value>
    SamplerStateDescription SamplerDescription { get; set; }
}

/// <summary>
/// </summary>
public interface ICrossSectionRenderParams {
    /// <summary>
    ///     Cutting operation, intersects or substract
    /// </summary>
    CuttingOperation CuttingOperation { get; set; }

    /// <summary>
    ///     Gets or sets the color of the section.
    /// </summary>
    /// <value>
    ///     The color of the section.
    /// </value>
    Color4 SectionColor { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [plane1/plane2/plane3/plane4 enabled].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [plane1/plane2/plane3/plane4 enabled]; otherwise, <c>false</c>.
    /// </value>
    Bool4 PlaneEnabled { get; set; }

    /// <summary>
    ///     Gets or sets the plane5 to 8 enabled.
    /// </summary>
    /// <value>
    ///     The plane5 to8 enabled.
    /// </value>
    Bool4 Plane5To8Enabled { get; set; }

    /// <summary>
    ///     Defines the plane (Normal + d)
    /// </summary>
    Vector4 Plane1Params { get; set; }

    /// <summary>
    ///     Gets or sets the plane2 parameters.(Normal + d)
    /// </summary>
    /// <value>
    ///     The plane2 parameters.
    /// </value>
    Vector4 Plane2Params { get; set; }

    /// <summary>
    ///     Gets or sets the plane3 parameters.(Normal + d)
    /// </summary>
    /// <value>
    ///     The plane3 parameters.
    /// </value>
    Vector4 Plane3Params { get; set; }

    /// <summary>
    ///     Gets or sets the plane4 parameters.(Normal + d)
    /// </summary>
    /// <value>
    ///     The plane4 parameters.
    /// </value>
    Vector4 Plane4Params { get; set; }

    /// <summary>
    ///     Gets or sets the plane5 parameters.(Normal + d)
    /// </summary>
    /// <value>
    ///     The plane5 parameters.
    /// </value>
    Vector4 Plane5Params { get; set; }

    /// <summary>
    ///     Gets or sets the plane6 parameters.(Normal + d)
    /// </summary>
    /// <value>
    ///     The plane6 parameters.
    /// </value>
    Vector4 Plane6Params { get; set; }

    /// <summary>
    ///     Gets or sets the plane7 parameters.(Normal + d)
    /// </summary>
    /// <value>
    ///     The plane7 parameters.
    /// </value>
    Vector4 Plane7Params { get; set; }

    /// <summary>
    ///     Gets or sets the plane8 parameters.(Normal + d)
    /// </summary>
    /// <value>
    ///     The plane8 parameters.
    /// </value>
    Vector4 Plane8Params { get; set; }
}

/// <summary>
/// </summary>
public interface IMeshOutlineParams {
    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    Color4 Color { get; set; }

    /// <summary>
    ///     Enable outline
    /// </summary>
    bool OutlineEnabled { get; set; }

    /// <summary>
    ///     Draw original mesh
    /// </summary>
    bool DrawMesh { get; set; }

    /// <summary>
    ///     Draw outline order
    /// </summary>
    bool DrawOutlineBeforeMesh { get; set; }

    /// <summary>
    ///     Outline fading
    /// </summary>
    float OutlineFadingFactor { get; set; }
}

/// <summary>
/// </summary>
public static class MeshTopologies {
    /// <summary>
    ///     Gets the topologies.
    /// </summary>
    /// <value>
    ///     The topologies.
    /// </value>
    public static IEnumerable<MeshTopologyEnum> Topologies {
        get {
            yield return MeshTopologyEnum.PnTriangles;
            yield return MeshTopologyEnum.PnQuads;
        }
    }
}

/// <summary>
/// </summary>
public interface IPointRenderParams {
    /// <summary>
    /// </summary>
    Color4 PointColor { get; set; }

    /// <summary>
    /// </summary>
    float Width { get; set; }

    /// <summary>
    /// </summary>
    float Height { get; set; }

    /// <summary>
    /// </summary>
    PointFigure Figure { get; set; }

    /// <summary>
    /// </summary>
    float FigureRatio { get; set; }
}

/// <summary>
/// </summary>
public interface IShadowMapRenderParams {
    /// <summary>
    /// </summary>
    int Width { get; set; }

    /// <summary>
    /// </summary>
    int Height { get; set; }

    /// <summary>
    /// </summary>
    float Bias { get; set; }

    /// <summary>
    /// </summary>
    float Intensity { get; set; }

    /// <summary>
    /// </summary>
    Matrix LightView { get; set; }

    /// <summary>
    /// </summary>
    Matrix LightProjection { get; set; }

    /// <summary>
    ///     Update shadow map every N frames
    /// </summary>
    int UpdateFrequency { get; set; }
}

/// <summary>
/// </summary>
public interface ISkyboxRenderParams {
    /// <summary>
    ///     Gets or sets the cube texture.
    /// </summary>
    /// <value>
    ///     The cube texture.
    /// </value>
    TextureModel? CubeTexture { get; set; }

    /// <summary>
    ///     Skip environment map rendering, but still keep it available for other object to use.
    /// </summary>
    bool SkipRendering { get; set; }
}

/// <summary>
/// </summary>
public interface IThrowingShadow {
    /// <summary>
    ///     Gets or sets a value indicating whether this instance is throwing shadow.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is throwing shadow; otherwise, <c>false</c>.
    /// </value>
    bool IsThrowingShadow { get; set; }
}

/// <summary>
/// </summary>
public interface ILineRenderParams {
    /// <summary>
    /// </summary>
    float Thickness { get; set; }

    /// <summary>
    /// </summary>
    float Smoothness { get; set; }

    /// <summary>
    ///     Final Line Color = LineColor * PerVertexLineColor
    /// </summary>
    Color4 LineColor { get; set; }
}
