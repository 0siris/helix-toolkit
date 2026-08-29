/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#define TEST

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public class ShadowMapCore : RenderCore, IShadowMapRenderParams {
    /// <summary>
    /// </summary>
    public ShadowMapCore() : base(RenderType.PreProc) {
        modelCb = AddComponent(new ConstantBufferComponent(
                                   new ConstantBufferDescription(
                                       DefaultBufferNames.ShadowParamCb,
                                       ShadowMapParamStruct.SizeInBytes)));
        Bias = 0.0015f;
        Intensity = 0.5f;
        Width = Height = 1024;
    }

    public event EventHandler<UpdateLightSourceEventArgs>? OnUpdateLightSource;

    /// <summary>
    ///     Creates the shared b5 payload from the current renderer-independent shadow settings.
    /// </summary>
    /// <param name="hasShadowMap">Whether the completed depth texture is available for sampling.</param>
    /// <returns>The current shadow-map constant-buffer payload.</returns>
    internal ShadowMapParamStruct CreateD3D12Parameters(bool hasShadowMap) {
        var result = modelStruct;
        result.HasShadowMap = hasShadowMap ? 1 : 0;
        return result;
    }

    public sealed class UpdateLightSourceEventArgs : EventArgs {
        public UpdateLightSourceEventArgs(RenderContext context) {
            Context = context;
        }

        public RenderContext Context { get; private set; }
    }

    #region Variables

    private ShaderResourceViewProxy? viewResource;
    private int currentFrame;
    private bool resolutionChanged = true;
    private ShadowMapParamStruct modelStruct;

    /// <summary>
    /// </summary>
    protected virtual NativeTexture2DDescription ShadowMapTextureDesc =>
        new() {
            Format = Format.FormatR32Typeless, //!!!! because of depth and shader resource
            ArraySize = 1,
            MipLevels = 1,
            Width = Width,
            Height = Height,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource, //!!!!
            CpuAccessFlags = CpuAccessFlags.None,
            OptionFlags = ResourceOptionFlags.None
        };

    /// <summary>
    /// </summary>
    protected virtual DepthStencilViewDescription DepthStencilViewDesc =>
        new() {
            Format = Format.FormatD32Float,
            Dimension = DepthStencilViewDimension.Texture2D,
            Texture2D = new DepthStencilViewDescription.Texture2DResource {
                MipSlice = 0
            }
        };

    /// <summary>
    /// </summary>
    protected virtual ShaderResourceViewDescription ShaderResourceViewDesc =>
        new() {
            Format = Format.FormatR32Float,
            Dimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new ShaderResourceViewDescription.Texture2DResource {
                MipLevels = 1,
                MostDetailedMip = 0
            }
        };

    private readonly ConstantBufferComponent modelCb;

    #endregion

    #region Properties

    /// <summary>
    /// </summary>
    public int Width {
        get => (int)modelStruct.ShadowMapSize.X;
        set {
            if (SetAffectsRender(ref modelStruct.ShadowMapSize.X, value)) resolutionChanged = true;
        }
    }

    /// <summary>
    /// </summary>
    public int Height {
        get => (int)modelStruct.ShadowMapSize.Y;
        set {
            if (SetAffectsRender(ref modelStruct.ShadowMapSize.Y, value)) resolutionChanged = true;
        }
    }

    /// <summary>
    /// </summary>
    public float Intensity {
        get => modelStruct.ShadowMapInfo.X;
        set => SetAffectsRender(ref modelStruct.ShadowMapInfo.X, value);
    }

    /// <summary>
    /// </summary>
    public float Bias {
        get => modelStruct.ShadowMapInfo.Z;
        set => SetAffectsRender(ref modelStruct.ShadowMapInfo.Z, value);
    }

    /// <summary>
    /// </summary>
    public Matrix LightView {
        get => modelStruct.LightView;
        set => SetAffectsRender(ref modelStruct.LightView, value);
    }

    /// <summary>
    /// </summary>
    public Matrix LightProjection {
        get => modelStruct.LightProjection;
        set => SetAffectsRender(ref modelStruct.LightProjection, value);
    }

    /// <summary>
    ///     Set to true if found the light source, otherwise false.
    /// </summary>
    public bool FoundLightSource { get; set; } = false;

    /// <summary>
    ///     Update shadow map every N frames
    /// </summary>
    public int UpdateFrequency { get; set; } = 1;

    public bool NeedRender { get; set; } = true;

    #endregion
}
