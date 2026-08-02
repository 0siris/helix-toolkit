/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Shaders;

#pragma warning disable CS8601, CS8602 // WPF invokes dependency-property callbacks with the owning material and initialized core.

namespace HelixToolkit.Wpf.SharpDX;

public abstract class VolumeTextureMaterialBase : Material, IVolumeTextureMaterial {
    public static readonly DependencyProperty SamplerProperty =
        DependencyProperty.Register("Sampler",
                                    typeof(SamplerStateDescription),
                                    typeof(VolumeTextureMaterialBase),
                                    new PropertyMetadata(DefaultSamplers.VolumeSampler,
                                                         (d, e) => {
                                                             ((d as VolumeTextureMaterialBase).Core as
                                                              VolumeTextureDDS3DMaterialCore).Sampler =
                                                                 (SamplerStateDescription)e.NewValue;
                                                         }));

    public static readonly DependencyProperty SampleDistanceProperty =
        DependencyProperty.Register("SampleDistance",
                                    typeof(double),
                                    typeof(VolumeTextureMaterialBase),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as VolumeTextureMaterialBase).Core as
                                                              IVolumeTextureMaterial).SampleDistance =
                                                                 (double)e.NewValue;
                                                         }));

    public static readonly DependencyProperty MaxIterationsProperty =
        DependencyProperty.Register("MaxIterations",
                                    typeof(int),
                                    typeof(VolumeTextureMaterialBase),
                                    new PropertyMetadata(int.MaxValue,
                                                         (d, e) => {
                                                             ((d as VolumeTextureMaterialBase).Core as
                                                              IVolumeTextureMaterial).MaxIterations = (int)e.NewValue;
                                                         }));


    public static readonly DependencyProperty IterationOffsetProperty =
        DependencyProperty.Register("IterationOffset",
                                    typeof(int),
                                    typeof(VolumeTextureMaterialBase),
                                    new PropertyMetadata(0,
                                                         (d, e) => {
                                                             ((d as VolumeTextureMaterialBase).Core as
                                                              IVolumeTextureMaterial).IterationOffset =
                                                                 (int)e.NewValue;
                                                         }));

    public static readonly DependencyProperty IsoValueProperty =
        DependencyProperty.Register("IsoValue",
                                    typeof(double),
                                    typeof(VolumeTextureMaterialBase),
                                    new PropertyMetadata(0.0,
                                                         (d, e) => {
                                                             ((d as VolumeTextureMaterialBase).Core as
                                                              IVolumeTextureMaterial).IsoValue = (double)e.NewValue;
                                                         }));


    public static readonly DependencyProperty ColorProperty =
        DependencyProperty.Register("Color4",
                                    typeof(Color4),
                                    typeof(VolumeTextureMaterialBase),
                                    new PropertyMetadata(new Color4(1, 1, 1, 1),
                                                         (d, e) => {
                                                             ((d as VolumeTextureMaterialBase).Core as
                                                              IVolumeTextureMaterial).Color = (Color4)e.NewValue;
                                                         }));

    public static readonly DependencyProperty TransferMapProperty =
        DependencyProperty.Register("TransferMap",
                                    typeof(Color4[]),
                                    typeof(VolumeTextureMaterialBase),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as VolumeTextureMaterialBase).Core as
                                                              IVolumeTextureMaterial).TransferMap =
                                                                 (Color4[])e.NewValue;
                                                         }));

    // Using a DependencyProperty as the backing store for EnablePlaneAlignment.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty EnablePlaneAlignmentProperty =
        DependencyProperty.Register("EnablePlaneAlignment",
                                    typeof(bool),
                                    typeof(VolumeTextureMaterialBase),
                                    new PropertyMetadata(true,
                                                         (d, e) => {
                                                             ((d as VolumeTextureMaterialBase).Core as
                                                              IVolumeTextureMaterial).EnablePlaneAlignment =
                                                                 (bool)e.NewValue;
                                                         }));

    public VolumeTextureMaterialBase() { }

    public VolumeTextureMaterialBase(IVolumeTextureMaterial core) : base(core as MaterialCore) {
        SampleDistance = core.SampleDistance;
        MaxIterations = core.MaxIterations;
        Sampler = core.Sampler;
        Color = core.Color;
        TransferMap = core.TransferMap;
        IsoValue = core.IsoValue;
        IterationOffset = core.IterationOffset;
        EnablePlaneAlignment = core.EnablePlaneAlignment;
    }

    public SamplerStateDescription Sampler {
        get => (SamplerStateDescription)GetValue(SamplerProperty);
        set => SetValue(SamplerProperty, value);
    }

    /// <summary>
    ///     Gets or sets the sample distance. Default = 1.0
    /// </summary>
    /// <value>
    ///     The size of the step.
    /// </value>
    public double SampleDistance {
        get => (double)GetValue(SampleDistanceProperty);
        set => SetValue(SampleDistanceProperty, value);
    }


    /// <summary>
    ///     Gets or sets the max iterations. Usually equal to the Texture Depth. Default = INT.MAX for automatic iteration
    /// </summary>
    /// <value>
    ///     The iterations.
    /// </value>
    public int MaxIterations {
        get => (int)GetValue(MaxIterationsProperty);
        set => SetValue(MaxIterationsProperty, value);
    }


    /// <summary>
    ///     Gets or sets the iteration offset. This can be used to achieve cross section
    /// </summary>
    /// <value>
    ///     The iteration offset.
    /// </value>
    public int IterationOffset {
        get => (int)GetValue(IterationOffsetProperty);
        set => SetValue(IterationOffsetProperty, value);
    }


    /// <summary>
    ///     Gets or sets the iso value. Only data with isovalue > sepecified iso value will be displayed
    ///     Value must be normalized to 0~1. Default = 1, show all data.
    /// </summary>
    /// <value>
    ///     The iso value.
    /// </value>
    public double IsoValue {
        get => (double)GetValue(IsoValueProperty);
        set => SetValue(IsoValueProperty, value);
    }


    /// <summary>
    ///     Gets or sets the color. It can also used to adjust opacity
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Color4 Color {
        get => (Color4)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }


    /// <summary>
    ///     Gets or sets the Color Transfer Map.
    /// </summary>
    /// <value>
    ///     The gradient map.
    /// </value>
    public Color4[] TransferMap {
        get => (Color4[])GetValue(TransferMapProperty);
        set => SetValue(TransferMapProperty, value);
    }


    public bool EnablePlaneAlignment {
        get => (bool)GetValue(EnablePlaneAlignmentProperty);
        set => SetValue(EnablePlaneAlignmentProperty, value);
    }
}

/// <summary>
///     Default Volume Texture Material. Supports 3D DDS memory stream as
///     <see cref="VolumeTextureMaterialCoreBase{T}.VolumeTexture" />
/// </summary>
public sealed class VolumeTextureDDS3DMaterial : VolumeTextureMaterialBase {
    public static readonly DependencyProperty TextureProperty =
        DependencyProperty.Register("Texture",
                                    typeof(TextureModel),
                                    typeof(VolumeTextureDDS3DMaterial),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as VolumeTextureDDS3DMaterial).Core as
                                                              VolumeTextureDDS3DMaterialCore).VolumeTexture =
                                                                 (TextureModel)e.NewValue;
                                                         }));

    public VolumeTextureDDS3DMaterial() { }

    public VolumeTextureDDS3DMaterial(VolumeTextureDDS3DMaterialCore core) : base(core) {
        Texture = core.VolumeTexture;
    }

    /// <summary>
    ///     Gets or sets the texture. Only supports 3D DDS texture stream
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public TextureModel Texture {
        get => (TextureModel)GetValue(TextureProperty);
        set => SetValue(TextureProperty, value);
    }

    protected override MaterialCore OnCreateCore() {
        return new VolumeTextureDDS3DMaterialCore {
            Name = Name,
            VolumeTexture = Texture,
            SampleDistance = SampleDistance,
            MaxIterations = MaxIterations,
            Sampler = Sampler,
            Color = Color,
            TransferMap = TransferMap,
            IsoValue = IsoValue,
            IterationOffset = IterationOffset,
            EnablePlaneAlignment = EnablePlaneAlignment
        };
    }

    protected override Freezable CreateInstanceCore() {
        return new VolumeTextureDDS3DMaterial {
            Name = Name,
            Texture = Texture,
            SampleDistance = SampleDistance,
            MaxIterations = MaxIterations,
            Sampler = Sampler,
            Color = Color,
            TransferMap = TransferMap,
            IsoValue = IsoValue,
            IterationOffset = IterationOffset,
            EnablePlaneAlignment = EnablePlaneAlignment
        };
    }
}

/// <summary>
///     Used to use raw data as Volume 3D texture.
///     User must create their own data reader to read texture files as pixel byte[] and pass the necessary information as
///     <see cref="VolumeTextureParams" />
///     <para>
///         Pixel Byte[] is equal to Width * Height * Depth * BytesPerPixel.
///     </para>
/// </summary>
public sealed class VolumeTextureRawDataMaterial : VolumeTextureMaterialBase {
    public static readonly DependencyProperty TextureProperty =
        DependencyProperty.Register("Texture",
                                    typeof(VolumeTextureParams),
                                    typeof(VolumeTextureRawDataMaterial),
                                    new PropertyMetadata(new VolumeTextureParams(),
                                                         (d, e) => {
                                                             ((d as VolumeTextureRawDataMaterial).Core as
                                                              VolumeTextureRawDataMaterialCore).VolumeTexture =
                                                                 (VolumeTextureParams)e.NewValue;
                                                         }));

    public VolumeTextureRawDataMaterial() { }

    public VolumeTextureRawDataMaterial(VolumeTextureRawDataMaterialCore core) : base(core) {
        Texture = core.VolumeTexture;
    }

    /// <summary>
    ///     Gets or sets the texture.
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public VolumeTextureParams Texture {
        get => (VolumeTextureParams)GetValue(TextureProperty);
        set => SetValue(TextureProperty, value);
    }

    protected override MaterialCore OnCreateCore() {
        return new VolumeTextureRawDataMaterialCore {
            Name = Name,
            VolumeTexture = Texture,
            SampleDistance = SampleDistance,
            MaxIterations = MaxIterations,
            Sampler = Sampler,
            Color = Color,
            TransferMap = TransferMap,
            IsoValue = IsoValue,
            IterationOffset = IterationOffset,
            EnablePlaneAlignment = EnablePlaneAlignment
        };
    }

    protected override Freezable CreateInstanceCore() {
        return new VolumeTextureRawDataMaterial {
            Name = Name,
            Texture = Texture,
            SampleDistance = SampleDistance,
            MaxIterations = MaxIterations,
            Sampler = Sampler,
            Color = Color,
            TransferMap = TransferMap,
            IsoValue = IsoValue,
            IterationOffset = IterationOffset,
            EnablePlaneAlignment = EnablePlaneAlignment
        };
    }
}

/// <summary>
///     Used to use gradient data as Volume 3D texture.
///     User must create their own data reader to read texture files as pixel byte[] and pass the necessary information as
///     <see cref="VolumeTextureParams" />
///     <para>
///         Pixel Byte[] is equal to Width * Height * Depth * BytesPerPixel.
///     </para>
/// </summary>
public sealed class VolumeTextureDiffuseMaterial : VolumeTextureMaterialBase {
    public static readonly DependencyProperty TextureProperty =
        DependencyProperty.Register("Texture",
                                    typeof(VolumeTextureGradientParams),
                                    typeof(VolumeTextureDiffuseMaterial),
                                    new PropertyMetadata(new VolumeTextureGradientParams(),
                                                         (d, e) => {
                                                             ((d as VolumeTextureDiffuseMaterial).Core as
                                                              VolumeTextureDiffuseMaterialCore).VolumeTexture =
                                                                 (VolumeTextureGradientParams)e.NewValue;
                                                         }));

    public VolumeTextureDiffuseMaterial() { }

    public VolumeTextureDiffuseMaterial(VolumeTextureDiffuseMaterialCore core) : base(core) {
        Texture = core.VolumeTexture;
    }

    /// <summary>
    ///     Gets or sets the texture.
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public VolumeTextureGradientParams Texture {
        get => (VolumeTextureGradientParams)GetValue(TextureProperty);
        set => SetValue(TextureProperty, value);
    }

    protected override MaterialCore OnCreateCore() {
        return new VolumeTextureDiffuseMaterialCore {
            Name = Name,
            VolumeTexture = Texture,
            SampleDistance = SampleDistance,
            MaxIterations = MaxIterations,
            Sampler = Sampler,
            Color = Color,
            TransferMap = TransferMap,
            IsoValue = IsoValue,
            IterationOffset = IterationOffset,
            EnablePlaneAlignment = EnablePlaneAlignment
        };
    }

    protected override Freezable CreateInstanceCore() {
        return new VolumeTextureDiffuseMaterial {
            Name = Name,
            Texture = Texture,
            SampleDistance = SampleDistance,
            MaxIterations = MaxIterations,
            Sampler = Sampler,
            Color = Color,
            TransferMap = TransferMap,
            IsoValue = IsoValue,
            IterationOffset = IterationOffset,
            EnablePlaneAlignment = EnablePlaneAlignment
        };
    }
}
