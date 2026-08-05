/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model;
public struct VolumeTextureParams {
    public byte[] VolumeTextures { get; }
    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }
    public Format Format { get; }

    public VolumeTextureParams(byte[] data, int width, int height, int depth, Format format) {
        VolumeTextures = data;
        Width = width;
        Height = height;
        Depth = depth;
        Format = format;
    }
}

public struct VolumeTextureGradientParams {
    public Half4[] VolumeTextures { get; }
    public int Width { get; }
    public int Height { get; }
    public int Depth { get; }
    public Format Format { get; }

    public VolumeTextureGradientParams(Half4[] data, int width, int height, int depth) {
        VolumeTextures = data;
        Width = width;
        Height = height;
        Depth = depth;
        Format = Format.FormatR16G16B16A16Float;
    }
}

public interface IVolumeTextureMaterial {
    SamplerStateDescription Sampler { get; set; }

    /// <summary>
    ///     Gets or sets the step size, controls the quality.
    /// </summary>
    /// <value>
    ///     The size of the step.
    /// </value>
    double SampleDistance { get; set; }

    /// <summary>
    ///     Gets or sets the iteration. Usually set to VolumeDepth.
    /// </summary>
    /// <value>
    ///     The iteration.
    /// </value>
    int MaxIterations { get; set; }

    /// <summary>
    ///     Gets or sets the iteration offset. This can be used to achieve cross section
    /// </summary>
    /// <value>
    ///     The iteration offset.
    /// </value>
    int IterationOffset { get; set; }

    /// <summary>
    ///     Gets or sets the iso value. Only data with isovalue > sepecified iso value will be displayed.
    ///     Value must be normalized to 0~1. Default = 1, show all data.
    /// </summary>
    /// <value>
    ///     The iso value.
    /// </value>
    double IsoValue { get; set; }

    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    Color4 Color { get; set; }

    /// <summary>
    ///     Gets or sets the transfer map.
    /// </summary>
    /// <value>
    ///     The transfer map.
    /// </value>
    Color4[] TransferMap { get; set; }

    bool EnablePlaneAlignment { get; set; }
}

/// <summary>
///     Abstract class for VolumeTextureMaterial
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class VolumeTextureMaterialCoreBase<T> : MaterialCore, IVolumeTextureMaterial {
    public T VolumeTexture {
        get;
        set => Set(ref field, value);
    }

    protected virtual string DefaultPassName { get; } = DefaultPassNames.Default;

    public SamplerStateDescription Sampler {
        get;
        set => Set(ref field, value);
    } = DefaultSamplers.VolumeSampler;

    /// <summary>
    ///     Gets or sets the step size, controls the quality.
    /// </summary>
    /// <value>
    ///     The size of the step.
    /// </value>
    public double SampleDistance {
        get;
        set => Set(ref field, value);
    } = 1.0;

    /// <summary>
    ///     Gets or sets the iteration. Usually set to VolumeDepth.
    /// </summary>
    /// <value>
    ///     The iteration.
    /// </value>
    public int MaxIterations {
        get;
        set => Set(ref field, value);
    } = 512;

    /// <summary>
    ///     Gets or sets the iteration offset. This can be used to achieve cross section
    /// </summary>
    /// <value>
    ///     The iteration offset.
    /// </value>
    public int IterationOffset {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the iso value. Only data with isovalue > sepecified iso value will be displayed
    ///     Value must be normalized to 0~1. Default = 1, show all data.
    /// </summary>
    /// <value>
    ///     The iso value.
    /// </value>
    public double IsoValue {
        get;
        set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the color.
    /// </summary>
    /// <value>
    ///     The color.
    /// </value>
    public Color4 Color {
        get;
        set => Set(ref field, value);
    } = new(1, 1, 1, 1);

    public Color4[] TransferMap {
        get;
        set => Set(ref field, value);
    }

    public bool EnablePlaneAlignment {
        get;
        set => Set(ref field, value);
    } = true;

    public override MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    ) {
        return new VolumeMaterialVariable<T>(manager, technique, this, DefaultPassName) {
            OnCreateTexture = (material, effectsManager) => OnCreateTexture(effectsManager)
        };
    }

    protected abstract ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager);
}

/// <summary>
///     Default Volume Texture Material. Supports 3D DDS memory stream as
///     <see cref="VolumeTextureMaterialCoreBase{T}.VolumeTexture" />
/// </summary>
public sealed class VolumeTextureDDS3DMaterialCore : VolumeTextureMaterialCoreBase<TextureModel> {
    protected override ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager) {
        return manager.MaterialTextureManager.Register(VolumeTexture, true);
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
public sealed class VolumeTextureRawDataMaterialCore : VolumeTextureMaterialCoreBase<VolumeTextureParams> {
    protected override ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager) {
        if (VolumeTexture.VolumeTextures != null)
            return ShaderResourceViewProxy.CreateViewFromPixelData(manager.NativeDeviceResources,
                                                                   VolumeTexture.VolumeTextures,
                                                                   VolumeTexture.Width,
                                                                   VolumeTexture.Height,
                                                                   VolumeTexture.Depth,
                                                                   VolumeTexture.Format,
                                                                   true,
                                                                   false);

        return null;
    }

    public static VolumeTextureParams LoadRAWFile(string filename, int width, int height, int depth) {
        using var file = new FileStream(filename, FileMode.Open);
        var length = file.Length;
        var bytePerPixel = length / (width * height * depth);
        var buffer = new byte[width * height * depth * bytePerPixel];
        using (var reader = new BinaryReader(file)) {
            reader.Read(buffer, 0, buffer.Length);
        }

        var format = Format.FormatUnknown;
        switch (bytePerPixel) {
            case 1:
                format = Format.FormatR8Unorm;
                break;
            case 2:
                format = Format.FormatR16Unorm;
                break;
            case 4:
                format = Format.FormatR32Float;
                break;
        }

        return new VolumeTextureParams(buffer, width, height, depth, format);
    }
}

/// <summary>
/// </summary>
public sealed class
    VolumeTextureDiffuseMaterialCore : VolumeTextureMaterialCoreBase<VolumeTextureGradientParams> {
    protected override string DefaultPassName => DefaultPassNames.Diffuse;

    protected override ShaderResourceViewProxy OnCreateTexture(IEffectsManager manager) {
        if (VolumeTexture.VolumeTextures != null)
            return ShaderResourceViewProxy.CreateViewFromPixelData(manager.NativeDeviceResources,
                                                                   VolumeTexture.VolumeTextures,
                                                                   VolumeTexture.Width,
                                                                   VolumeTexture.Height,
                                                                   VolumeTexture.Depth,
                                                                   VolumeTexture.Format,
                                                                   true,
                                                                   false);

        return null;
    }
}
