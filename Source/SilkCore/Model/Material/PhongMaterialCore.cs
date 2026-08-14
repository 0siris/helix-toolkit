/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model;
/// <summary>
/// </summary>
public class PhongMaterialCore : MaterialCore {
    /// <summary>
    ///     Gets or sets the color of the ambient.
    /// </summary>
    /// <value>
    ///     The color of the ambient.
    /// </value>
    public Color4 AmbientColor {
        get;
        set => Set(ref field, value);
    } = Color.DarkGray;

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
    ///     Gets or sets the color of the emissive.
    /// </summary>
    /// <value>
    ///     The color of the emissive.
    /// </value>
    public Color4 EmissiveColor {
        get;
        set => Set(ref field, value);
    } = Color.Black;

    /// <summary>
    ///     Gets or sets the color of the reflective.
    /// </summary>
    /// <value>
    ///     The color of the reflective.
    /// </value>
    public Color4 ReflectiveColor {
        get;
        set => Set(ref field, value);
    } = Color.Black;

    /// <summary>
    ///     Gets or sets the color of the specular.
    /// </summary>
    /// <value>
    ///     The color of the specular.
    /// </value>
    public Color4 SpecularColor {
        get;
        set => Set(ref field, value);
    } = Color.Gray;

    /// <summary>
    ///     Gets or sets the specular shininess.
    /// </summary>
    /// <value>
    ///     The specular shininess.
    /// </value>
    /// <exception cref="System.NotImplementedException">
    /// </exception>
    public float SpecularShininess {
        get;
        set => Set(ref field, value);
    } = 1;

    /// <summary>
    ///     Gets or sets the diffuse map.
    /// </summary>
    /// <value>
    ///     The diffuse map.
    /// </value>
    public TextureModel DiffuseMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the diffuse map file path. For export only
    /// </summary>
    /// <value>
    ///     The diffuse map file path.
    /// </value>
    public string? DiffuseMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the DiffuseAlphaMap.
    /// </summary>
    /// <value>
    ///     DiffuseAlphaMap
    /// </value>
    public TextureModel DiffuseAlphaMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the diffuse alpha map file path. For export only
    /// </summary>
    /// <value>
    ///     The diffuse alpha map file path.
    /// </value>
    public string? DiffuseAlphaMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the NormalMap.
    /// </summary>
    /// <value>
    ///     NormalMap
    /// </value>
    public TextureModel NormalMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the normal map file path. For export only
    /// </summary>
    /// <value>
    ///     The normal map file path.
    /// </value>
    public string? NormalMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the specular color map.
    /// </summary>
    /// <value>
    ///     The specular color map.
    /// </value>
    public TextureModel SpecularColorMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the specular color map file path. For export only
    /// </summary>
    /// <value>
    ///     The specular color map file path.
    /// </value>
    public string? SpecularColorMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the DisplacementMap.
    /// </summary>
    /// <value>
    ///     DisplacementMap
    /// </value>
    public TextureModel DisplacementMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the displacement file path. For export only
    /// </summary>
    /// <value>
    ///     The displacement file path.
    /// </value>
    public string? DisplacementMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the emissive map.
    /// </summary>
    /// <value>
    ///     The emissive map.
    /// </value>
    public TextureModel EmissiveMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the emissive map file path. For export only
    /// </summary>
    /// <value>
    ///     The emissive map file path.
    /// </value>
    public string? EmissiveMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the DisplacementMapScaleMask.
    /// </summary>
    /// <value>
    ///     DisplacementMapScaleMask
    /// </value>
    public Vector4 DisplacementMapScaleMask {
        get;
        set => Set(ref field, value);
    }

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
    ///     Gets or sets the DisplacementMapSampler.
    /// </summary>
    /// <value>
    ///     DisplacementMapSampler
    /// </value>
    public SamplerStateDescription DisplacementMapSampler {
        get;
        set => Set(ref field, value);
    } = DefaultSamplers.LinearSamplerWrapAni1;

    /// <summary>
    /// </summary>
    public bool RenderDiffuseMap {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    /// </summary>
    public bool RenderDiffuseAlphaMap {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    /// </summary>
    public bool RenderNormalMap {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether [render specular color map].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render specular color map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderSpecularColorMap {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    /// </summary>
    public bool RenderDisplacementMap {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether [render emissive map].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render emissive map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderEmissiveMap {
        get;
        set => Set(ref field, value);
    } = true;

    /// <summary>
    ///     Gets or sets a value indicating whether [enable automatic tangent].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable automatic tangent]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableAutoTangent {
        get;
        set => Set(ref field, value);
    }

    public float MinTessellationDistance {
        get;
        set => Set(ref field, value);
    } = 10;

    public float MaxTessellationDistance {
        get;
        set => Set(ref field, value);
    } = 100;

    /// <summary>
    ///     Gets or sets the tessellation factor at <see cref="MinTessellationDistance" />.
    /// </summary>
    /// <value>
    ///     The minimum distance tessellation factor.
    /// </value>
    public float MinDistanceTessellationFactor {
        get;
        set => Set(ref field, value);
    } = 2;

    /// <summary>
    ///     Gets or sets the tessellation factor at <see cref="MaxDistanceTessellationFactor" />
    /// </summary>
    /// <value>
    ///     The maximum distance tessellation factor.
    /// </value>
    public float MaxDistanceTessellationFactor {
        get;
        set => Set(ref field, value);
    } = 1;

    public MeshTopologyEnum MeshType {
        get;
        set => Set(ref field, value);
    } = MeshTopologyEnum.PnTriangles;

    public bool EnableTessellation {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderShadowMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderEnvironmentMap {
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
        => new PhongMaterialVariables(manager, technique, this);
}
