/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model;
[DataContract]
public class DiffuseMaterialCore : MaterialCore {
    /// <summary>
    ///     Gets or sets the color of the diffuse.
    /// </summary>
    /// <value>
    ///     The color of the diffuse.
    /// </value>
    public Color4 DiffuseColor {
        get;
        set => Set(ref field, value);
    } = Color.White;

    /// <summary>
    ///     Gets or sets the diffuse map.
    /// </summary>
    /// <value>
    ///     The diffuse map.
    /// </value>
    public TextureModel? DiffuseMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the diffuse map file path. Only for export
    /// </summary>
    /// <value>
    ///     The diffuse map file path.
    /// </value>
    public string? DiffuseMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the uv transform.
    /// </summary>
    /// <value>
    ///     The uv transform.
    /// </value>
    public UvTransform UvTransform {
        get;
        set => Set(ref field, value);
    } = UvTransform.Identity;

    /// <summary>
    ///     Gets or sets the DiffuseMapSampler.
    /// </summary>
    /// <value>
    ///     DiffuseMapSampler
    /// </value>
    public SamplerStateDescription DiffuseMapSampler {
        get;
        set => Set(ref field, value);
    } = DefaultSamplers.LinearSamplerWrapAni4;

    /// <summary>
    /// </summary>
    public bool RenderDiffuseMap {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether disable lighting. Directly render diffuse color and diffuse map
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable un lit]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableUnLit {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable flat shading].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable flat shading]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableFlatShading {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the vert color blending factor.
    ///     Diffuse = (1- <see cref="VertexColorBlendingFactor" />) * Diffuse + <see cref="VertexColorBlendingFactor" /> *
    ///     Vertex Color
    /// </summary>
    /// <value>
    ///     The vert color blending factor.
    /// </value>
    public float VertexColorBlendingFactor {
        get;
        set => Set(ref field, value);
    }

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new DiffuseMaterialVariables(DefaultPassNames.Diffuse, manager, technique, this);
}

public sealed class ViewCubeMaterialCore : DiffuseMaterialCore {
    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    )
        => new DiffuseMaterialVariables(DefaultPassNames.ViewCube, manager, technique, this);
}
