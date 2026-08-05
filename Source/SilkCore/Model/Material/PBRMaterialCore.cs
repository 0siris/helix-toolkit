/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model;
public class PBRMaterialCore : MaterialCore {
    /// <summary>
    ///     Gets or sets the color of the albedo.
    /// </summary>
    /// <value>
    ///     The color of the albedo.
    /// </value>
    public Color4 AlbedoColor {
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
    ///     Gets or sets the metallic factor. If RMA map is used, for each pixel, metallic factor =
    ///     <see cref="MetallicFactor" /> * RMA map B Channel
    /// </summary>
    /// <value>
    ///     The metallic factor.
    /// </value>
    public float MetallicFactor {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the roughness factor. If RMA map is used, for each pixel, roughness factor =
    ///     <see cref="RoughnessFactor" /> * RMA map G Channel
    /// </summary>
    /// <value>
    ///     The roughness factor.
    /// </value>
    public float RoughnessFactor {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the ambient occlusion factor. If RMA map is used, for each pixel, ambient occlusion factor =
    ///     <see cref="AmbientOcclusionFactor" /> * RMA map R Channel
    /// </summary>
    /// <value>
    ///     The ambient occlusion factor.
    /// </value>
    public float AmbientOcclusionFactor {
        get;
        set => Set(ref field, value);
    } = 1;

    /// <summary>
    ///     Gets or sets the reflectance factor.
    /// </summary>
    /// <value>
    ///     The reflectance factor.
    /// </value>
    public float ReflectanceFactor {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the clear coat strength.
    /// </summary>
    /// <value>
    ///     The clear coat strength.
    /// </value>
    public float ClearCoatStrength {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the clear coat roughness.
    /// </summary>
    /// <value>
    ///     The clear coat roughness.
    /// </value>
    public float ClearCoatRoughness {
        get;
        set => Set(ref field, value);
    }

    public bool RenderAlbedoMap {
        get;
        set => Set(ref field, value);
    } = true;

    public bool RenderNormalMap {
        get;
        set => Set(ref field, value);
    } = true;

    public bool RenderRoughnessMetallicMap {
        get;
        set => Set(ref field, value);
    } = true;

    public bool RenderAmbientOcclusionMap {
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
    ///     Gets or sets a value indicating whether [render irrandiance map].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render irrandiance map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderIrradianceMap {
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

    /// <summary>
    ///     Gets or sets the albedo map.
    /// </summary>
    /// <value>
    ///     The albedo map.
    /// </value>
    public TextureModel AlbedoMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the albedo map file path. Used for export only
    /// </summary>
    /// <value>
    ///     The albedo map file path.
    /// </value>
    public string AlbedoMapFilePath { get; set; }

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
    ///     Gets or sets the emissive map file path. Only for export
    /// </summary>
    /// <value>
    ///     The emissive map.
    /// </value>
    public string EmissiveMapFilePath { get; set; }

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
    ///     Gets or sets the normal map file path. Only for export
    /// </summary>
    /// <value>
    ///     The normal map file path.
    /// </value>
    public string NormalMapFilePath { get; set; }

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
    ///     Gets or sets the displacement map file path. Only for export
    /// </summary>
    /// <value>
    ///     The displacement map file path.
    /// </value>
    public string DisplacementMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the irradiance map.
    /// </summary>
    /// <value>
    ///     The irradiance map.
    /// </value>
    public TextureModel IrradianceMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the irradiance map file path. Only for export
    /// </summary>
    /// <value>
    ///     The irradiance map file path.
    /// </value>
    public string IrradianceMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the Roughness, Metallic map.
    ///     glTF2 defines occlusion as R channel, roughness as G channel, metalness as B channel.
    ///     If provides RMA map in one texture, set both <see cref="RoughnessMetallicMap" /> and
    ///     <see cref="AmbientOcculsionMap" /> to the same texture.
    /// </summary>
    /// <value>
    ///     The rma map.
    /// </value>
    public TextureModel RoughnessMetallicMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the rma map file path. Only for export
    /// </summary>
    /// <value>
    ///     The rma map file path.
    /// </value>
    public string RoughnessMetallicMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the separate Ambient Occlusion map.
    ///     glTF2 defines occlusion as R channel, roughness as G channel, metalness as B channel.
    ///     If provides RMA map in one texture, set both <see cref="RoughnessMetallicMap" /> and
    ///     <see cref="AmbientOcculsionMap" /> to the same texture.
    /// </summary>
    /// <value>
    ///     The ao map.
    /// </value>
    public TextureModel AmbientOcculsionMap {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the ao map file path.
    /// </summary>
    /// <value>
    ///     The ao map file path.
    /// </value>
    public string AmbientOcculsionMapFilePath { get; set; }

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
    public UVTransform UVTransform {
        get;
        set => Set(ref field, value);
    } = UVTransform.Identity;

    /// <summary>
    ///     Gets or sets the surface map sampler.
    /// </summary>
    /// <value>
    ///     The surface map sampler.
    /// </value>
    public SamplerStateDescription SurfaceMapSampler {
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
    ///     Gets or sets the IBL sampler.
    /// </summary>
    /// <value>
    ///     The IBL sampler.
    /// </value>
    public SamplerStateDescription IBLSampler {
        get;
        set => Set(ref field, value);
    } = DefaultSamplers.IBLSampler;

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
    } = MeshTopologyEnum.PNTriangles;

    public bool EnableTessellation {
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
    ) {
        return new PBRMaterialVariable(manager, technique, this);
    }
}
