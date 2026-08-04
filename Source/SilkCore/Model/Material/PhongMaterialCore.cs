/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model;
/// <summary>
/// </summary>
public class PhongMaterialCore : MaterialCore {
    private Color4 ambientColor = Color.DarkGray;

    private TextureModel diffuseAlphaMap;

    private Color4 diffuseColor = Color.White;

    private TextureModel diffuseMap;

    private SamplerStateDescription diffuseMapSampler = DefaultSamplers.LinearSamplerWrapAni4;

    private TextureModel displacementMap;


    private SamplerStateDescription displacementMapSampler = DefaultSamplers.LinearSamplerWrapAni1;

    private Vector4 displacementMapScaleMask;

    private Color4 emissiveColor = Color.Black;

    private TextureModel emissiveMap;

    private bool enableAutoTangent;

    private bool enableFlatShading;

    private bool enableTessellation;

    private float maxDistanceTessellationFactor = 1;

    private float maxTessellationDistance = 100;

    private MeshTopologyEnum meshType = MeshTopologyEnum.PNTriangles;

    private float minDistanceTessellationFactor = 2;

    private float minTessellationDistance = 10;

    private TextureModel normalMap;

    private Color4 reflectiveColor = Color.Black;

    private bool renderDiffuseAlphaMap = true;

    private bool renderDiffuseMap = true;

    private bool renderDisplacementMap = true;

    private bool renderEmissiveMap = true;

    private bool renderEnvironmentMap;
    private bool renderNormalMap = true;

    private bool renderShadowMap;

    private bool renderSpecularColorMap = true;

    private Color4 specularColor = Color.Gray;

    private TextureModel specularColorMap;

    private float specularShininess = 1;

    private UVTransform uvTransform = UVTransform.Identity;

    private float vertexColorBlendingFactor;

    /// <summary>
    ///     Gets or sets the color of the ambient.
    /// </summary>
    /// <value>
    ///     The color of the ambient.
    /// </value>
    public Color4 AmbientColor {
        get => ambientColor;
        set => Set(ref ambientColor, value);
    }

    /// <summary>
    ///     Gets or sets the color of the diffuse.
    /// </summary>
    /// <value>
    ///     The color of the diffuse.
    /// </value>
    public Color4 DiffuseColor {
        get => diffuseColor;
        set => Set(ref diffuseColor, value);
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
    ///     Gets or sets the color of the reflective.
    /// </summary>
    /// <value>
    ///     The color of the reflective.
    /// </value>
    public Color4 ReflectiveColor {
        get => reflectiveColor;
        set => Set(ref reflectiveColor, value);
    }

    /// <summary>
    ///     Gets or sets the color of the specular.
    /// </summary>
    /// <value>
    ///     The color of the specular.
    /// </value>
    public Color4 SpecularColor {
        get => specularColor;
        set => Set(ref specularColor, value);
    }

    /// <summary>
    ///     Gets or sets the specular shininess.
    /// </summary>
    /// <value>
    ///     The specular shininess.
    /// </value>
    /// <exception cref="System.NotImplementedException">
    /// </exception>
    public float SpecularShininess {
        get => specularShininess;
        set => Set(ref specularShininess, value);
    }

    /// <summary>
    ///     Gets or sets the diffuse map.
    /// </summary>
    /// <value>
    ///     The diffuse map.
    /// </value>
    public TextureModel DiffuseMap {
        get => diffuseMap;
        set => Set(ref diffuseMap, value);
    }

    /// <summary>
    ///     Gets or sets the diffuse map file path. For export only
    /// </summary>
    /// <value>
    ///     The diffuse map file path.
    /// </value>
    public string DiffuseMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the DiffuseAlphaMap.
    /// </summary>
    /// <value>
    ///     DiffuseAlphaMap
    /// </value>
    public TextureModel DiffuseAlphaMap {
        get => diffuseAlphaMap;
        set => Set(ref diffuseAlphaMap, value);
    }

    /// <summary>
    ///     Gets or sets the diffuse alpha map file path. For export only
    /// </summary>
    /// <value>
    ///     The diffuse alpha map file path.
    /// </value>
    public string DiffuseAlphaMapFilePath { get; set; }

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
    ///     Gets or sets the normal map file path. For export only
    /// </summary>
    /// <value>
    ///     The normal map file path.
    /// </value>
    public string NormalMapFilePath { get; set; }

    /// <summary>
    ///     Gets or sets the specular color map.
    /// </summary>
    /// <value>
    ///     The specular color map.
    /// </value>
    public TextureModel SpecularColorMap {
        get => specularColorMap;
        set => Set(ref specularColorMap, value);
    }

    /// <summary>
    ///     Gets or sets the specular color map file path. For export only
    /// </summary>
    /// <value>
    ///     The specular color map file path.
    /// </value>
    public string SpecularColorMapFilePath { get; set; }

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
    ///     Gets or sets the displacement file path. For export only
    /// </summary>
    /// <value>
    ///     The displacement file path.
    /// </value>
    public string DisplacementMapFilePath { get; set; }

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
    ///     Gets or sets the emissive map file path. For export only
    /// </summary>
    /// <value>
    ///     The emissive map file path.
    /// </value>
    public string EmissiveMapFilePath { get; set; }

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
    ///     Gets or sets the DiffuseMapSampler.
    /// </summary>
    /// <value>
    ///     DiffuseMapSampler
    /// </value>
    public SamplerStateDescription DiffuseMapSampler {
        get => diffuseMapSampler;
        set => Set(ref diffuseMapSampler, value);
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
    /// </summary>
    public bool RenderDiffuseMap {
        get => renderDiffuseMap;
        set => Set(ref renderDiffuseMap, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderDiffuseAlphaMap {
        get => renderDiffuseAlphaMap;
        set => Set(ref renderDiffuseAlphaMap, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderNormalMap {
        get => renderNormalMap;
        set => Set(ref renderNormalMap, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render specular color map].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render specular color map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderSpecularColorMap {
        get => renderSpecularColorMap;
        set => Set(ref renderSpecularColorMap, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderDisplacementMap {
        get => renderDisplacementMap;
        set => Set(ref renderDisplacementMap, value);
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
        return new PhongMaterialVariables(manager, technique, this);
    }
}
