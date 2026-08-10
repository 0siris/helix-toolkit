using System.ComponentModel;
using System.Runtime.Serialization;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.Wpf.SharpDX.Utilities;

#pragma warning disable CS8601, CS8602 // WPF invokes dependency-property callbacks with the owning material and initialized core.

namespace HelixToolkit.Wpf.SharpDX;

[DataContract]
public class PBRMaterial : Material {
    /// <summary>
    ///     Identifies the System.Windows.Media.Media3D.DiffuseMaterial.Color�dependency
    ///     property.
    /// </summary>
    public static readonly DependencyProperty AlbedoColorProperty =
        DependencyProperty.Register("AlbedoColor",
                                    typeof(Color4),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata((Color4)Color.White,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).AlbedoColor =
                                                                 (Color4)e.NewValue;
                                                         }));

    /// <summary>
    ///     The albedo color property
    /// </summary>
    public static readonly DependencyProperty EmissiveColorProperty =
        DependencyProperty.Register("EmissiveColor",
                                    typeof(Color4),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata((Color4)Color.Black,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).EmissiveColor =
                                                                 (Color4)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty MetallicFactorProperty =
        DependencyProperty.Register("MetallicFactor",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).MetallicFactor =
                                                                 (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty RoughnessFactorProperty =
        DependencyProperty.Register("RoughnessFactor",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).RoughnessFactor =
                                                                 (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty AmbientOcclusionFactorProperty =
        DependencyProperty.Register("AmbientOcclusionFactor",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .AmbientOcclusionFactor = (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty ReflectanceFactorProperty =
        DependencyProperty.Register("ReflectanceFactor",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .ReflectanceFactor = (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty ClearCoatStrengthProperty =
        DependencyProperty.Register("ClearCoatStrength",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .ClearCoatStrength = (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty ClearCoatRoughnessProperty =
        DependencyProperty.Register("ClearCoatRoughness",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .ClearCoatRoughness = (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty AlbedoMapProperty =
        DependencyProperty.Register("AlbedoMap",
                                    typeof(TextureModel),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).AlbedoMap =
                                                                 e.NewValue as TextureModel;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty EmissiveMapProperty =
        DependencyProperty.Register("EmissiveMap",
                                    typeof(TextureModel),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).EmissiveMap =
                                                                 e.NewValue as TextureModel;
                                                         }));

    /// <summary>
    ///     glTF2 defines metalness as B channel, roughness as G channel, and occlusion as R channel
    ///     If uses RMA map, set both <see cref="RoughnessMetallicMap" /> and <see cref="AmbientOcculsionMap" /> to the same
    ///     texture.
    /// </summary>
    public static readonly DependencyProperty RoughnessMetallicMapProperty =
        DependencyProperty.Register("RoughnessMetallicMap",
                                    typeof(TextureModel),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .RoughnessMetallicMap = e.NewValue as TextureModel;
                                                         }));

    /// <summary>
    ///     glTF2 defines metalness as B channel, roughness as G channel, and occlusion as R channel.
    ///     If uses RMA map, set both <see cref="RoughnessMetallicMap" /> and <see cref="AmbientOcculsionMap" /> to the same
    ///     texture.
    /// </summary>
    public static readonly DependencyProperty AmbientOcculsionMapProperty =
        DependencyProperty.Register("AmbientOcculsionMap",
                                    typeof(TextureModel),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .AmbientOcculsionMap = e.NewValue as TextureModel;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty NormalMapProperty =
        DependencyProperty.Register("NormalMap",
                                    typeof(TextureModel),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).NormalMap =
                                                                 e.NewValue as TextureModel;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty DisplacementMapProperty =
        DependencyProperty.Register("DisplacementMap",
                                    typeof(TextureModel),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).DisplacementMap =
                                                                 e.NewValue as TextureModel;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty IrradianceMapProperty =
        DependencyProperty.Register("IrrandianceMap",
                                    typeof(TextureModel),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).IrradianceMap =
                                                                 e.NewValue as TextureModel;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty DisplacementMapScaleMaskProperty =
        DependencyProperty.Register("DisplacementMapScaleMask",
                                    typeof(Vector4),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(new Vector4(0, 0, 0, 1),
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .DisplacementMapScaleMask = (Vector4)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty SurfaceMapSamplerProperty =
        DependencyProperty.Register("SurfaceMapSampler",
                                    typeof(SamplerStateDescription),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(DefaultSamplers.LinearSamplerWrapAni4,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .SurfaceMapSampler =
                                                                 (SamplerStateDescription)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty IBLSamplerProperty =
        DependencyProperty.Register("IBLSampler",
                                    typeof(SamplerStateDescription),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(DefaultSamplers.LinearSamplerWrapAni4,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).IblSampler =
                                                                 (SamplerStateDescription)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty DisplacementMapSamplerProperty =
        DependencyProperty.Register("DisplacementMapSampler",
                                    typeof(SamplerStateDescription),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(DefaultSamplers.LinearSamplerWrapAni1,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .DisplacementMapSampler =
                                                                 (SamplerStateDescription)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty RenderAlbedoMapProperty =
        DependencyProperty.Register("RenderAlbedoMap",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).RenderAlbedoMap =
                                                                 (bool)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty RenderEmissiveMapProperty =
        DependencyProperty.Register("RenderEmissiveMap",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .RenderEmissiveMap = (bool)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty RenderRoughnessMetallicMapProperty =
        DependencyProperty.Register("RenderRoughnessMetallicMap",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .RenderRoughnessMetallicMap = (bool)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty RenderAmbientOcclusionMapProperty =
        DependencyProperty.Register("RenderAmbientOcclusionMap",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .RenderAmbientOcclusionMap = (bool)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty RenderIrradianceMapProperty =
        DependencyProperty.Register("RenderIrradianceMap",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .RenderIrradianceMap = (bool)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty RenderNormalMapProperty =
        DependencyProperty.Register("RenderNormalMap",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).RenderNormalMap =
                                                                 (bool)e.NewValue;
                                                         }));

    /// <summary>
    /// </summary>
    public static readonly DependencyProperty RenderDisplacementMapProperty =
        DependencyProperty.Register("RenderDisplacementMap",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .RenderDisplacementMap = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The render environment map property
    /// </summary>
    public static readonly DependencyProperty RenderEnvironmentMapProperty =
        DependencyProperty.Register("RenderEnvironmentMap",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .RenderEnvironmentMap = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The render shadow map property
    /// </summary>
    public static readonly DependencyProperty RenderShadowMapProperty =
        DependencyProperty.Register("RenderShadowMap",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).RenderShadowMap =
                                                                 (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The enable automatic tangent
    /// </summary>
    public static readonly DependencyProperty EnableAutoTangentProperty =
        DependencyProperty.Register("EnableAutoTangent",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .EnableAutoTangent = (bool)e.NewValue;
                                                         }));

    /// <summary>
    ///     The enable tessellation property
    /// </summary>
    public static readonly DependencyProperty EnableTessellationProperty = DependencyProperty.Register(
        "EnableTessellation",
        typeof(bool),
        typeof(PBRMaterial),
        new PropertyMetadata(false,
                             (d, e) => {
                                 ((d as Material).Core as PbrMaterialCore).EnableTessellation = (bool)e.NewValue;
                             }));

    /// <summary>
    ///     The tessellation factor at <see cref="MaxTessellationDistance" /> property
    /// </summary>
    public static readonly DependencyProperty MaxDistanceTessellationFactorProperty =
        DependencyProperty.Register("MaxDistanceTessellationFactor",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .MaxDistanceTessellationFactor =
                                                                 (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    ///     The tessellation factor at <see cref="MinTessellationDistance" /> property
    /// </summary>
    public static readonly DependencyProperty MinDistanceTessellationFactorProperty =
        DependencyProperty.Register("MinDistanceTessellationFactor",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(2.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .MinDistanceTessellationFactor =
                                                                 (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    ///     The maximum tessellation distance property
    /// </summary>
    public static readonly DependencyProperty MaxTessellationDistanceProperty =
        DependencyProperty.Register("MaxTessellationDistance",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(50.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .MaxTessellationDistance = (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    ///     The minimum tessellation distance property
    /// </summary>
    public static readonly DependencyProperty MinTessellationDistanceProperty =
        DependencyProperty.Register("MinTessellationDistance",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .MinTessellationDistance = (float)(double)e.NewValue;
                                                         }));


    /// <summary>
    ///     The uv transform property
    /// </summary>
    public static readonly DependencyProperty UVTransformProperty =
        DependencyProperty.Register("UVTransform",
                                    typeof(UvTransform),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(UvTransform.Identity,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore).UvTransform =
                                                                 (UvTransform)e.NewValue;
                                                         }));

    /// <summary>
    ///     The enable flat shading property
    /// </summary>
    public static readonly DependencyProperty EnableFlatShadingProperty =
        DependencyProperty.Register("EnableFlatShading",
                                    typeof(bool),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(false,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .EnableFlatShading = (bool)e.NewValue;
                                                         }));

    public static readonly DependencyProperty VertexColorBlendingFactorProperty =
        DependencyProperty.Register("VertexColorBlendingFactor",
                                    typeof(double),
                                    typeof(PBRMaterial),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as Material).Core as PbrMaterialCore)
                                                                 .VertexColorBlendingFactor =
                                                                 (float)(double)e.NewValue;
                                                         }));

    /// <summary>
    ///     Initializes a new instance of the <see cref="PBRMaterial" /> class.
    /// </summary>
    public PBRMaterial() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PBRMaterial" /> class.
    /// </summary>
    /// <param name="core">The core.</param>
    public PBRMaterial(PbrMaterialCore core) : base(core) {
        AlbedoColor = core.AlbedoColor;
        MetallicFactor = core.MetallicFactor;
        RoughnessFactor = core.RoughnessFactor;
        AmbientOcclusionFactor = core.AmbientOcclusionFactor;
        ReflectanceFactor = core.ReflectanceFactor;
        ClearCoatStrength = core.ClearCoatStrength;
        ClearCoatRoughness = core.ClearCoatRoughness;

        AlbedoMap = core.AlbedoMap;
        NormalMap = core.NormalMap;
        EmissiveMap = core.EmissiveMap;
        RoughnessMetallicMap = core.RoughnessMetallicMap;
        AmbientOcculsionMap = core.AmbientOcculsionMap;
        IrradianceMap = core.IrradianceMap;
        DisplacementMap = core.DisplacementMap;
        SurfaceMapSampler = core.SurfaceMapSampler;
        IBLSampler = core.IblSampler;
        DisplacementMapSampler = core.DisplacementMapSampler;

        RenderAlbedoMap = core.RenderAlbedoMap;
        RenderDisplacementMap = core.RenderDisplacementMap;
        RenderEmissiveMap = core.RenderEmissiveMap;
        RenderEnvironmentMap = core.RenderEnvironmentMap;
        RenderIrradianceMap = core.RenderIrradianceMap;
        RenderNormalMap = core.RenderNormalMap;
        RenderRoughnessMetallicMap = core.RenderRoughnessMetallicMap;
        RenderAmbientOcclusionMap = core.RenderAmbientOcclusionMap;
        RenderShadowMap = core.RenderShadowMap;
        EnableAutoTangent = core.EnableAutoTangent;
        DisplacementMapScaleMask = core.DisplacementMapScaleMask;
        UVTransform = core.UvTransform;

        EnableTessellation = core.EnableTessellation;
        MaxDistanceTessellationFactor = core.MaxDistanceTessellationFactor;
        MinDistanceTessellationFactor = core.MinDistanceTessellationFactor;
        MaxTessellationDistance = core.MaxTessellationDistance;
        MinTessellationDistance = core.MinTessellationDistance;
        EnableFlatShading = core.EnableFlatShading;
        VertexColorBlendingFactor = core.VertexColorBlendingFactor;
    }

    /// <summary>
    ///     Gets or sets the diffuse color for the material.
    ///     For details see: http://msdn.microsoft.com/en-us/library/windows/desktop/bb147175(v=vs.85).aspx
    /// </summary>
    [TypeConverter(typeof(Color4Converter))]
    public Color4 AlbedoColor {
        get { return (Color4)GetValue(AlbedoColorProperty); }
        set { SetValue(AlbedoColorProperty, value); }
    }

    public Color4 EmissiveColor {
        get => (Color4)GetValue(EmissiveColorProperty);
        set => SetValue(EmissiveColorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the metallic factor. If RMA map is used, for each pixel, metallic factor =
    ///     <see cref="MetallicFactor" /> * RMA map B Channel
    /// </summary>
    /// <value>
    ///     The metallic factor.
    /// </value>
    public double MetallicFactor {
        get => (double)GetValue(MetallicFactorProperty);
        set => SetValue(MetallicFactorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the roughness factor. If RMA map is used, for each pixel, roughness factor =
    ///     <see cref="RoughnessFactor" /> * RMA map G Channel
    /// </summary>
    /// <value>
    ///     The roughness factor.
    /// </value>
    public double RoughnessFactor {
        get => (double)GetValue(RoughnessFactorProperty);
        set => SetValue(RoughnessFactorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the ambient occlusion factor. If RMA map is used, for each pixel, ambient occlusion factor =
    ///     <see cref="AmbientOcclusionFactor" /> * RMA map R Channel
    /// </summary>
    /// <value>
    ///     The ambient occlusion factor.
    /// </value>
    public double AmbientOcclusionFactor {
        get => (double)GetValue(AmbientOcclusionFactorProperty);
        set => SetValue(AmbientOcclusionFactorProperty, value);
    }

    public double ReflectanceFactor {
        get => (double)GetValue(ReflectanceFactorProperty);
        set => SetValue(ReflectanceFactorProperty, value);
    }


    public double ClearCoatStrength {
        get => (double)GetValue(ClearCoatStrengthProperty);
        set => SetValue(ClearCoatStrengthProperty, value);
    }


    public double ClearCoatRoughness {
        get => (double)GetValue(ClearCoatRoughnessProperty);
        set => SetValue(ClearCoatRoughnessProperty, value);
    }

    public TextureModel AlbedoMap {
        get => (TextureModel)GetValue(AlbedoMapProperty);
        set => SetValue(AlbedoMapProperty, value);
    }


    public TextureModel EmissiveMap {
        get => (TextureModel)GetValue(EmissiveMapProperty);
        set => SetValue(EmissiveMapProperty, value);
    }

    /// <summary>
    ///     Gets or sets the Roughness, Metallic, Ambient Occlusion map.
    ///     glTF2 defines occlusion as R channel, roughness as G channel, metalness as B channel
    /// </summary>
    /// <value>
    ///     The rma map.
    /// </value>
    public TextureModel RoughnessMetallicMap {
        get => (TextureModel)GetValue(RoughnessMetallicMapProperty);
        set => SetValue(RoughnessMetallicMapProperty, value);
    }

    /// <summary>
    ///     Gets or sets the ambient occlusion map.
    ///     glTF2 defines occlusion as R channel, roughness as G channel, metalness as B channel.
    ///     If uses RMA map, set both <see cref="RoughnessMetallicMap" /> and <see cref="AmbientOcculsionMap" /> to the same
    ///     texture
    /// </summary>
    /// <value>
    ///     The ao map.
    /// </value>
    public TextureModel AmbientOcculsionMap {
        get => (TextureModel)GetValue(AmbientOcculsionMapProperty);
        set => SetValue(AmbientOcculsionMapProperty, value);
    }

    /// <summary>
    /// </summary>
    public TextureModel NormalMap {
        get => (TextureModel)GetValue(NormalMapProperty);
        set => SetValue(NormalMapProperty, value);
    }

    /// <summary>
    /// </summary>
    public TextureModel DisplacementMap {
        get => (TextureModel)GetValue(DisplacementMapProperty);
        set => SetValue(DisplacementMapProperty, value);
    }


    public TextureModel IrradianceMap {
        get => (TextureModel)GetValue(IrradianceMapProperty);
        set => SetValue(IrradianceMapProperty, value);
    }

    /// <summary>
    /// </summary>
    public SamplerStateDescription SurfaceMapSampler {
        get => (SamplerStateDescription)GetValue(SurfaceMapSamplerProperty);
        set => SetValue(SurfaceMapSamplerProperty, value);
    }

    /// <summary>
    /// </summary>
    public SamplerStateDescription IBLSampler {
        get => (SamplerStateDescription)GetValue(IBLSamplerProperty);
        set => SetValue(IBLSamplerProperty, value);
    }

    /// <summary>
    /// </summary>
    public SamplerStateDescription DisplacementMapSampler {
        get => (SamplerStateDescription)GetValue(DisplacementMapSamplerProperty);
        set => SetValue(DisplacementMapSamplerProperty, value);
    }

    [TypeConverter(typeof(Vector4Converter))]
    public Vector4 DisplacementMapScaleMask {
        get { return (Vector4)GetValue(DisplacementMapScaleMaskProperty); }
        set { SetValue(DisplacementMapScaleMaskProperty, value); }
    }

    /// <summary>
    /// </summary>
    public bool RenderAlbedoMap {
        get => (bool)GetValue(RenderAlbedoMapProperty);
        set => SetValue(RenderAlbedoMapProperty, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderNormalMap {
        get => (bool)GetValue(RenderNormalMapProperty);
        set => SetValue(RenderNormalMapProperty, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderEmissiveMap {
        get => (bool)GetValue(RenderEmissiveMapProperty);
        set => SetValue(RenderEmissiveMapProperty, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderRoughnessMetallicMap {
        get => (bool)GetValue(RenderRoughnessMetallicMapProperty);
        set => SetValue(RenderRoughnessMetallicMapProperty, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderAmbientOcclusionMap {
        get => (bool)GetValue(RenderAmbientOcclusionMapProperty);
        set => SetValue(RenderAmbientOcclusionMapProperty, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderIrradianceMap {
        get => (bool)GetValue(RenderIrradianceMapProperty);
        set => SetValue(RenderIrradianceMapProperty, value);
    }

    /// <summary>
    /// </summary>
    public bool RenderDisplacementMap {
        get => (bool)GetValue(RenderDisplacementMapProperty);
        set => SetValue(RenderDisplacementMapProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render environment map]. Default is false
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render environment map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderEnvironmentMap {
        get => (bool)GetValue(RenderEnvironmentMapProperty);
        set => SetValue(RenderEnvironmentMapProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [render shadow map]. Default is false
    /// </summary>
    /// <value>
    ///     <c>true</c> if [render shadow map]; otherwise, <c>false</c>.
    /// </value>
    public bool RenderShadowMap {
        get => (bool)GetValue(RenderShadowMapProperty);
        set => SetValue(RenderShadowMapProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable automatic tangent].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable automatic tangent]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableAutoTangent {
        get => (bool)GetValue(EnableAutoTangentProperty);
        set => SetValue(EnableAutoTangentProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable tessellation].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable tessellation]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableTessellation {
        get => (bool)GetValue(EnableTessellationProperty);
        set => SetValue(EnableTessellationProperty, value);
    }

    /// <summary>
    ///     Gets or sets the tessellation factor at <see cref="MaxTessellationDistance" />.
    /// </summary>
    /// <value>
    ///     The maximum tessellation factor.
    /// </value>
    public double MaxDistanceTessellationFactor {
        get => (double)GetValue(MaxDistanceTessellationFactorProperty);
        set => SetValue(MaxDistanceTessellationFactorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the tessellation factor at <see cref="MinTessellationDistance" />
    /// </summary>
    /// <value>
    ///     The minimum tessellation factor.
    /// </value>
    public double MinDistanceTessellationFactor {
        get => (double)GetValue(MinDistanceTessellationFactorProperty);
        set => SetValue(MinDistanceTessellationFactorProperty, value);
    }

    /// <summary>
    ///     Gets or sets the maximum tessellation distance.
    /// </summary>
    /// <value>
    ///     The maximum tessellation distance.
    /// </value>
    public double MaxTessellationDistance {
        get => (double)GetValue(MaxTessellationDistanceProperty);
        set => SetValue(MaxTessellationDistanceProperty, value);
    }

    /// <summary>
    ///     Gets or sets the minimum tessellation distance.
    /// </summary>
    /// <value>
    ///     The minimum tessellation distance.
    /// </value>
    public double MinTessellationDistance {
        get => (double)GetValue(MinTessellationDistanceProperty);
        set => SetValue(MinTessellationDistanceProperty, value);
    }

    /// <summary>
    ///     Gets or sets the texture uv transform.
    /// </summary>
    /// <value>
    ///     The uv transform.
    /// </value>
    public UvTransform UVTransform {
        get => (UvTransform)GetValue(UVTransformProperty);
        set => SetValue(UVTransformProperty, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [enable flat shading].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable flat shading]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableFlatShading {
        get => (bool)GetValue(EnableFlatShadingProperty);
        set => SetValue(EnableFlatShadingProperty, value);
    }

    /// <summary>
    ///     Gets or sets the vertex color blending factor.
    ///     Final Diffuse Color = (1 - VertexColorBlendingFactor) * Diffuse + VertexColorBlendingFactor * Vertex Color
    /// </summary>
    /// <value>
    ///     The vertex color blending factor.
    /// </value>
    public double VertexColorBlendingFactor {
        get => (double)GetValue(VertexColorBlendingFactorProperty);
        set => SetValue(VertexColorBlendingFactorProperty, value);
    }

    protected override MaterialCore OnCreateCore() {
        return new PbrMaterialCore {
            AlbedoColor = AlbedoColor,
            MetallicFactor = (float)MetallicFactor,
            RoughnessFactor = (float)RoughnessFactor,
            AmbientOcclusionFactor = (float)AmbientOcclusionFactor,
            ReflectanceFactor = (float)ReflectanceFactor,
            ClearCoatStrength = (float)ClearCoatStrength,
            ClearCoatRoughness = (float)ClearCoatRoughness,

            AlbedoMap = AlbedoMap,
            NormalMap = NormalMap,
            EmissiveMap = EmissiveMap,
            RoughnessMetallicMap = RoughnessMetallicMap,
            AmbientOcculsionMap = AmbientOcculsionMap,
            IrradianceMap = IrradianceMap,
            DisplacementMap = DisplacementMap,
            SurfaceMapSampler = SurfaceMapSampler,
            IblSampler = IBLSampler,
            DisplacementMapSampler = DisplacementMapSampler,

            RenderAlbedoMap = RenderAlbedoMap,
            RenderDisplacementMap = RenderDisplacementMap,
            RenderEmissiveMap = RenderEmissiveMap,
            RenderEnvironmentMap = RenderEnvironmentMap,
            RenderIrradianceMap = RenderIrradianceMap,
            RenderNormalMap = RenderNormalMap,
            RenderRoughnessMetallicMap = RenderRoughnessMetallicMap,
            RenderAmbientOcclusionMap = RenderAmbientOcclusionMap,
            RenderShadowMap = RenderShadowMap,
            EnableAutoTangent = EnableAutoTangent,
            DisplacementMapScaleMask = DisplacementMapScaleMask,
            UvTransform = UVTransform,

            EnableTessellation = EnableTessellation,
            MaxDistanceTessellationFactor = (float)MaxDistanceTessellationFactor,
            MinDistanceTessellationFactor = (float)MinDistanceTessellationFactor,
            MaxTessellationDistance = (float)MaxTessellationDistance,
            MinTessellationDistance = (float)MinTessellationDistance,
            EnableFlatShading = EnableFlatShading,
            VertexColorBlendingFactor = (float)VertexColorBlendingFactor
        };
    }

    protected override Freezable CreateInstanceCore() {
        return CloneMaterial();
    }

    public virtual PBRMaterial CloneMaterial() {
        return new PBRMaterial {
            AlbedoColor = AlbedoColor,
            MetallicFactor = MetallicFactor,
            RoughnessFactor = RoughnessFactor,
            AmbientOcclusionFactor = AmbientOcclusionFactor,
            ReflectanceFactor = ReflectanceFactor,
            ClearCoatStrength = ClearCoatStrength,
            ClearCoatRoughness = ClearCoatRoughness,
            AlbedoMap = AlbedoMap,
            NormalMap = NormalMap,
            EmissiveMap = EmissiveMap,
            RoughnessMetallicMap = RoughnessMetallicMap,
            AmbientOcculsionMap = AmbientOcculsionMap,
            IrradianceMap = IrradianceMap,
            DisplacementMap = DisplacementMap,
            SurfaceMapSampler = SurfaceMapSampler,
            IBLSampler = IBLSampler,
            DisplacementMapSampler = DisplacementMapSampler,

            RenderAlbedoMap = RenderAlbedoMap,
            RenderDisplacementMap = RenderDisplacementMap,
            RenderEmissiveMap = RenderEmissiveMap,
            RenderEnvironmentMap = RenderEnvironmentMap,
            RenderIrradianceMap = RenderIrradianceMap,
            RenderNormalMap = RenderNormalMap,
            RenderRoughnessMetallicMap = RenderRoughnessMetallicMap,
            RenderAmbientOcclusionMap = RenderAmbientOcclusionMap,
            RenderShadowMap = RenderShadowMap,
            EnableAutoTangent = EnableAutoTangent,
            DisplacementMapScaleMask = DisplacementMapScaleMask,
            UVTransform = UVTransform,

            EnableTessellation = EnableTessellation,
            MaxDistanceTessellationFactor = MaxDistanceTessellationFactor,
            MinDistanceTessellationFactor = MinDistanceTessellationFactor,
            MaxTessellationDistance = MaxTessellationDistance,
            MinTessellationDistance = MinTessellationDistance,
            EnableFlatShading = EnableFlatShading,
            VertexColorBlendingFactor = VertexColorBlendingFactor
        };
    }
}

#pragma warning restore CS8601, CS8602

/// <summary>
///     https://google.github.io/filament/images/material_chart.jpg
/// </summary>
public static class PBRSampleColors {
    // Metallic
    public static readonly Color4 Silver = new(250 / 255f, 249 / 255f, 245 / 255f, 1);
    public static readonly Color4 Aluminum = new(244 / 255f, 245 / 255f, 245 / 255f, 1);
    public static readonly Color4 Platinum = new(214 / 255f, 209 / 255f, 200 / 255f, 1);
    public static readonly Color4 Iron = new(192 / 255f, 189 / 255f, 186 / 255f, 1);
    public static readonly Color4 Titanium = new(206 / 255f, 200 / 255f, 194 / 255f, 1);
    public static readonly Color4 Copper = new(251 / 255f, 216 / 255f, 184 / 255f, 1);
    public static readonly Color4 Gold = new(255 / 255f, 220 / 255f, 157 / 255f, 1);
    public static readonly Color4 Brass = new(244 / 255f, 228 / 255f, 173 / 255f, 1);

    //non metallic samples
    public static readonly Color4 Coal = new(50 / 255f, 50 / 255f, 50 / 255f, 1);
    public static readonly Color4 Rubber = new(53 / 255f, 53 / 255f, 53 / 255f, 1);
    public static readonly Color4 Mud = new(85 / 255f, 61 / 255f, 49 / 255f, 1);
    public static readonly Color4 Wood = new(135 / 255f, 92 / 255f, 60 / 255f, 1);
    public static readonly Color4 Vegetation = new(123 / 255f, 130 / 255f, 78 / 255f, 1);
    public static readonly Color4 Brick = new(148 / 255f, 125 / 255f, 117 / 255f, 1);
    public static readonly Color4 Sand = new(177 / 255f, 168 / 255f, 132 / 255f, 1);
    public static readonly Color4 Concrete = new(192 / 255f, 191 / 255f, 187 / 255f, 1);
}
