/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model;
public class PBRMaterialCore : MaterialCore {
    private Color4 albedoColor = Color.White;

    private TextureModel albedoMap;

    private float ambientOcclusionFactor = 1;

    private TextureModel ambientOcculsionMap;

    private float clearCoatRoughness;

    private float clearCoatStrength;


    private TextureModel displacementMap;

    private SamplerStateDescription displacementMapSampler = DefaultSamplers.LinearSamplerWrapAni1;

    private Vector4 displacementMapScaleMask;

    private Color4 emissiveColor = Color.Black;

    private TextureModel emissiveMap;

    private bool enableAutoTangent;

    private bool enableFlatShading;

    private bool enableTessellation;

    private SamplerStateDescription iblSampler = DefaultSamplers.IBLSampler;

    private TextureModel irradianceMap;

    private float maxDistanceTessellationFactor = 1;

    private float maxTessellationDistance = 100;

    private MeshTopologyEnum meshType = MeshTopologyEnum.PNTriangles;

    private float metallicFactor;

    private float minDistanceTessellationFactor = 2;

    private float minTessellationDistance = 10;

    private TextureModel normalMap;

    private float reflectanceFactor;

    private bool renderAlbodoMap = true;

    private bool renderAmbientOcclusionMap = true;

    private bool renderDisplacementMap = true;

    private bool renderEmissiveMap = true;

    private bool renderEnvironmentMap;

    private bool renderIrradianceMap = true;

    private bool renderNormalMap = true;

    private bool renderRoughnessMetallicMap = true;

    private bool renderShadowMap;

    private float roughnessFactor;

    private TextureModel roughnessMetallicMap;

    private SamplerStateDescription surfaceMapSampler = DefaultSamplers.LinearSamplerWrapAni4;

    private UVTransform uvTransform = UVTransform.Identity;

    private float vertexColorBlendingFactor;

    /// <summary>
    ///     Gets or sets the color of the albedo.
    /// </summary>
    /// <value>
    ///     The color of the albedo.
    /// </value>
    public Color4 AlbedoColor {
        get => albedoColor;
        set => Set(ref albedoColor, value);
    }

    /// <summary>
    ///     Gets or sets the color of the emissive.
    /// </summary>
    /// <value>
    ///     The color of the emissive.
    /// </value>
    public Color4 EmissiveColor {
        get => emissiveColor;
        set => Set(ref emissiveColor, value);
    }

    /// <summary>
    ///     Gets or sets the metallic factor. If RMA map is used, for each pixel, metallic factor =
    ///     <see cref="MetallicFactor" /> * RMA map B Channel
    /// </summary>
    /// <value>
    ///     The metallic factor.
    /// </value>
    public float MetallicFactor {
        get => metallicFactor;
        set => Set(ref metallicFactor, value);
    }

    /// <summary>
    ///     Gets or sets the roughness factor. If RMA map is used, for each pixel, roughness factor =
    ///     <see cref="RoughnessFactor" /> * RMA map G Channel
    /// </summary>
    /// <value>
    ///     The roughness factor.
    /// </value>
    public float RoughnessFactor {
        get => roughnessFactor;
        set => Set(ref roughnessFactor, value);
    }

    /// <summary>
    ///     Gets or sets the ambient occlusion factor. If RMA map is used, for each pixel, ambient occlusion factor =
    ///     <see cref="AmbientOcclusionFactor" /> * RMA map R Channel
    /// </summary>
    /// <value>
    ///     The ambient occlusion factor.
    /// </value>
    public float AmbientOcclusionFactor {
        get => ambientOcclusionFactor;
        set => Set(ref ambientOcclusionFactor, value);
    }

    /// <summary>
    ///     Gets or sets the reflectance factor.
    /// </summary>
    /// <value>
    ///     The reflectance factor.
    /// </value>
    public float ReflectanceFactor {
        get => reflectanceFactor;
        set => Set(ref reflectanceFactor, value);
    }

    /// <summary>
    ///     Gets or sets the clear coat strength.
    /// </summary>
    /// <value>
    ///     The clear coat strength.
    /// </value>
    public float ClearCoatStrength {
        get => clearCoatStrength;
        set => Set(ref clearCoatStrength, value);
    }

    /// <summary>
    ///     Gets or sets the clear coat roughness.
    /// </summary>
    /// <value>
    ///     The clear coat roughness.
    /// </value>
    public float ClearCoatRoughness {
        get => clearCoatRoughness;
        set => Set(ref clearCoatRoughness, value);
    }

    public bool RenderAlbedoMap {
        get => renderAlbodoMap;
        set => Set(ref renderAlbodoMap, value);
    }

    public bool RenderNormalMap {
        get => renderNormalMap;
        set => Set(ref renderNormalMap, value);
    }

    public bool RenderRoughnessMetallicMap {
        get => renderRoughnessMetallicMap;
        set => Set(ref renderRoughnessMetallicMap, value);
    }

    public bool RenderAmbientOcclusionMap {
        get => renderAmbientOcclusionMap;
        set => Set(ref renderAmbientOcclusionMap, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderDisplacementMap {
        get => renderDisplacementMap;
        set => Set(ref renderDisplacementMap, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderShadowMap {
        get => renderShadowMap;
        set => Set(ref renderShadowMap, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderEnvironmentMap {
        get => renderEnvironmentMap;
        set => Set(ref renderEnvironmentMap, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render irrandiance map].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render irrandiance map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderIrradianceMap {
        get => renderIrradianceMap;
        set => Set(ref renderIrradianceMap, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render emissive map].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render emissive map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderEmissiveMap {
        get => renderEmissiveMap;
        set => Set(ref renderEmissiveMap, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable automatic tangent].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable automatic tangent]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableAutoTangent {
        get => enableAutoTangent;
        set => Set(ref enableAutoTangent, value);
    }

    /// <summary>
    ///     Gets or sets the albedo map.
    /// </summary>
    /// <value>
    ///     The albedo map.
    /// </value>
    public TextureModel AlbedoMap {
        get => albedoMap;
        set => Set(ref albedoMap, value);
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
        get => emissiveMap;
        set => Set(ref emissiveMap, value);
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
        get => normalMap;
        set => Set(ref normalMap, value);
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
        get => displacementMap;
        set => Set(ref displacementMap, value);
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
        get => irradianceMap;
        set => Set(ref irradianceMap, value);
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
        get => roughnessMetallicMap;
        set => Set(ref roughnessMetallicMap, value);
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
        get => ambientOcculsionMap;
        set => Set(ref ambientOcculsionMap, value);
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
        get => displacementMapScaleMask;
        set => Set(ref displacementMapScaleMask, value);
    }

    /// <summary>
    ///     Gets or sets the uv transform.
    /// </summary>
    /// <value>
    ///     The uv transform.
    /// </value>
    public UVTransform UVTransform {
        get => uvTransform;
        set => Set(ref uvTransform, value);
    }

    /// <summary>
    ///     Gets or sets the surface map sampler.
    /// </summary>
    /// <value>
    ///     The surface map sampler.
    /// </value>
    public SamplerStateDescription SurfaceMapSampler {
        get => surfaceMapSampler;
        set => Set(ref surfaceMapSampler, value);
    }

    /// <summary>
    ///     Gets or sets the DisplacementMapSampler.
    /// </summary>
    /// <value>
    ///     DisplacementMapSampler
    /// </value>
    public SamplerStateDescription DisplacementMapSampler {
        get => displacementMapSampler;
        set => Set(ref displacementMapSampler, value);
    }

    /// <summary>
    ///     Gets or sets the IBL sampler.
    /// </summary>
    /// <value>
    ///     The IBL sampler.
    /// </value>
    public SamplerStateDescription IBLSampler {
        get => iblSampler;
        set => Set(ref iblSampler, value);
    }

    public float MinTessellationDistance {
        get => minTessellationDistance;
        set => Set(ref minTessellationDistance, value);
    }

    public float MaxTessellationDistance {
        get => maxTessellationDistance;
        set => Set(ref maxTessellationDistance, value);
    }

    /// <summary>
    ///     Gets or sets the tessellation factor at <see cref="MinTessellationDistance" />.
    /// </summary>
    /// <value>
    ///     The minimum distance tessellation factor.
    /// </value>
    public float MinDistanceTessellationFactor {
        get => minDistanceTessellationFactor;
        set => Set(ref minDistanceTessellationFactor, value);
    }

    /// <summary>
    ///     Gets or sets the tessellation factor at <see cref="MaxDistanceTessellationFactor" />
    /// </summary>
    /// <value>
    ///     The maximum distance tessellation factor.
    /// </value>
    public float MaxDistanceTessellationFactor {
        get => maxDistanceTessellationFactor;
        set => Set(ref maxDistanceTessellationFactor, value);
    }

    public MeshTopologyEnum MeshType {
        get => meshType;
        set => Set(ref meshType, value);
    }

    public bool EnableTessellation {
        get => enableTessellation;
        set => Set(ref enableTessellation, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable flat shading].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable flat shading]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableFlatShading {
        get => enableFlatShading;
        set => Set(ref enableFlatShading, value);
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
        get => vertexColorBlendingFactor;
        set => Set(ref vertexColorBlendingFactor, value);
    }

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    ) {
        return new PBRMaterialVariable(manager, technique, this);
    }
}
