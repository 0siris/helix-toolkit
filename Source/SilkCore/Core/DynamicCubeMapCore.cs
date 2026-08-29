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
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public class DynamicCubeMapCore : RenderCore, IDynamicReflector {
    /// <summary>
    ///     Initializes a new instance of the <see cref="DynamicCubeMapCore" /> class.
    /// </summary>
    public DynamicCubeMapCore() : base(RenderType.PreProc) {
        modelCb = AddComponent(new ConstantBufferComponent(new ConstantBufferDescription(
            DefaultBufferNames.GlobalTransformCb,
            GlobalTransformStruct.SizeInBytes)));
        UpdateTargets();
    }

    private void RenderCubeFace(RenderContext context, int index) {
        if (contextPool is not { } pool)
            return;

        var ctx = pool.Get();
        ctx.ClearRenderTargetView(cubeRtVs[index]!, Color.Transparent);
        ctx.ClearDepthStencilView(cubeDsVs[index]!, DepthStencilClearFlags.Depth);
        ctx.SetRenderTarget(cubeDsVs[index], cubeRtVs[index]);
        ctx.SetViewport(0, 0, FaceSize, FaceSize);
        ctx.SetScissorRectangle(0, 0, FaceSize, FaceSize);
        var transforms = new GlobalTransformStruct {
            Projection = cubeFaceCameras.Cameras[index].Projection,
            View = cubeFaceCameras.Cameras[index].View,
            Viewport = new Vector4(FaceSize, FaceSize, 1 / FaceSize, 1 / FaceSize)
        };
        transforms.ViewProjection = transforms.View * transforms.Projection;

        modelCb.Upload(ctx, ref transforms);

        commands[index] = ctx.FinishCommandList(true);
        contextPool.Put(ctx);
    }

    private void UpdateTargets() {
        for (var i = 0; i < 6; ++i) {
            targets[i] = center + lookVector[i];
            cubeFaceCameras.Cameras[i].View =
                (IsLeftHanded
                    ? SilkMath.LookAtLh(center, targets[i], upVectors[i])
                    : SilkMath.LookAtRh(center, targets[i], upVectors[i])) * SilkMath.Scaling(-1, 1, 1);
            cubeFaceCameras.Cameras[i].Projection = IsLeftHanded
                ? SilkMath.PerspectiveFovLh(
                    (float)Math.PI * 0.5f,
                    1,
                    NearField,
                    FarField)
                : SilkMath.PerspectiveFovRh(
                    (float)Math.PI * 0.5f,
                    1,
                    NearField,
                    FarField);
        }
    }

    #region

    private readonly Vector3[] targets = new Vector3[6];

    private readonly Vector3[] lookVector =
        [Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ];

    private readonly Vector3[] upVectors =
        [Vector3.UnitY, Vector3.UnitY, -Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitY, Vector3.UnitY];

    private readonly CubeFaceCamerasStruct cubeFaceCameras = new() {
        Cameras = new CubeFaceCamera[6]
    };

    // Create the cube map TextureCube (array of 6 textures)
    private Texture2DDescription textureDesc = new() {
        Format = Format.FormatR8G8B8A8Unorm,
        ArraySize = 6, // 6-sides of the cube
        BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
        OptionFlags = ResourceOptionFlags.GenerateMipMaps | ResourceOptionFlags.TextureCube,
        SampleDescription = new SampleDescription(1, 0),
        MipLevels = 0,
        Usage = ResourceUsage.Default,
        CpuAccessFlags = CpuAccessFlags.None
    };

    private Texture2DDescription dsvTextureDesc = new() {
        Format = Format.FormatD16Unorm,
        BindFlags = BindFlags.DepthStencil,
        Usage = ResourceUsage.Default,
        SampleDescription = new SampleDescription(1, 0),
        CpuAccessFlags = CpuAccessFlags.None,
        MipLevels = 1,
        OptionFlags = ResourceOptionFlags.TextureCube,
        ArraySize = 6
    };

    private int cubeTextureSlot;
    private int textureSamplerSlot;

    private ShaderResourceViewProxy? CubeDsv {
        get;
        set {
            if (field != value)
                field?.Dispose();
            field = value;
        }
    }

    // The RTVs, one for each face of cubemap
    private readonly RenderTargetView?[] cubeRtVs = new RenderTargetView?[6];

    // The DSVs, one for each face of cubemap
    private readonly DepthStencilView?[] cubeDsVs = new DepthStencilView?[6];

    private SamplerStateProxy? TextureSampler {
        get;
        set {
            if (field != value)
                field?.Dispose();
            field = value;
        }
    }

    private IDeviceContextPool? contextPool;
    private readonly CommandList?[] commands = new CommandList[6];
    private readonly ConstantBufferComponent modelCb;

    #endregion

    #region Properties

    public HashSet<Guid> IgnoredGuid { get; } = [];

    private ShaderResourceViewProxy? CubeMap {
        get;
        set {
            if (field != value)
                field?.Dispose();
            field = value;
        }
    }

    private bool enableReflector = true;

    /// <summary>
    ///     Gets or sets a value indicating whether [enable reflector].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [enable reflector]; otherwise, <c>false</c>.
    /// </value>
    public bool EnableReflector {
        get => enableReflector;
        set => SetAffectsRender(ref enableReflector, value);
    }

    private int faceSize = 256;

    public int FaceSize {
        get => faceSize;
        set => SetAffectsRender(ref faceSize, value);
    }

    /// <summary>
    ///     Name of the default pass inside a technique.
    ///     <para>Default: <see cref="DefaultPassNames.Default" /></para>
    /// </summary>
    public string DefaultShaderPassName {
        get;
        set => SetAffectsRender(ref field, value);
    } = DefaultPassNames.Default;

    /// <summary>
    /// </summary>
    protected ShaderPass DefaultShaderPass {
        get;
        private set {
            if (SetAffectsRender(ref field, value)) {
                cubeTextureSlot =
                    value.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(ShaderCubeTextureName);
                textureSamplerSlot =
                    value.PixelShader.SamplerMapping.TryGetBindSlot(ShaderCubeTextureSamplerName);
                RaiseInvalidateRender();
            }
        }
    } = ShaderPass.NullPass;

    /// <summary>
    ///     Gets or sets the sampler description.
    /// </summary>
    /// <value>
    ///     The sampler description.
    /// </value>
    public SamplerStateDescription SamplerDescription {
        get;
        set => SetAffectsRender(ref field, value);
    } = DefaultSamplers.IblSampler;

    /// <summary>
    ///     Gets or sets a value indicating whether this coordinate system is left handed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this coordinate system is left handed; otherwise, <c>false</c>.
    /// </value>
    public bool IsLeftHanded {
        get;
        set {
            if (SetAffectsRender(ref field, value))
                UpdateTargets();
        }
    }

    /// <summary>
    ///     Gets or sets the near field of perspective.
    /// </summary>
    /// <value>
    ///     The near field.
    /// </value>
    public float NearField {
        get;
        set {
            if (SetAffectsRender(ref field, value))
                UpdateTargets();
        }
    } = 0.1f;

    /// <summary>
    ///     Gets or sets the far field of perspective.
    /// </summary>
    /// <value>
    ///     The far field.
    /// </value>
    public float FarField {
        get;
        set {
            if (SetAffectsRender(ref field, value))
                UpdateTargets();
        }
    } = 100f;

    private Vector3 center = Vector3.Zero;

    /// <summary>
    ///     Gets or sets the center.
    /// </summary>
    /// <value>
    ///     The center.
    /// </value>
    public Vector3 Center {
        get => center;
        set {
            if (SetAffectsRender(ref center, value))
                UpdateTargets();
        }
    }

    /// <summary>
    ///     Gets or sets the name of the shader cube texture.
    /// </summary>
    /// <value>
    ///     The name of the shader cube texture.
    /// </value>
    public string ShaderCubeTextureName { get; set; } = DefaultBufferNames.CubeMapTb;

    /// <summary>
    ///     Gets or sets the name of the shader cube texture sampler.
    /// </summary>
    /// <value>
    ///     The name of the shader cube texture sampler.
    /// </value>
    public string ShaderCubeTextureSamplerName { get; set; } = DefaultSamplerStateNames.CubeMapSampler;

    /// <summary>
    ///     Gets or sets a value indicating whether this scene is dynamic scene.
    ///     If true, reflection map will be updated in each frame. Otherwise it will only be updated if scene graph or
    ///     visibility changed.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is dynamic scene; otherwise, <c>false</c>.
    /// </value>
    public bool IsDynamicScene {
        get;
        set => SetAffectsRender(ref field, value);
    }

    #endregion Properties

    #region IReflector

    private SamplerStateProxy?[]? currSampler;
    private ShaderResourceView?[]? currRes;

    /// <summary>
    ///     Binds the cube map.
    /// </summary>
    /// <param name="deviceContext">The device context.</param>
    public void BindCubeMap(DeviceContextProxy deviceContext) {
        currSampler = deviceContext.GetSampler<PixelShaderType>(textureSamplerSlot, 1);
        ;
        currRes = deviceContext.GetShaderResources<PixelShaderType>(cubeTextureSlot, 1);
        if (EnableReflector) {
            deviceContext.SetShaderResource<PixelShaderType>(cubeTextureSlot, CubeMap);
            deviceContext.SetSampler<PixelShaderType>(textureSamplerSlot, TextureSampler);
        }
    }

    /// <summary>
    ///     Uns the bind cube map.
    /// </summary>
    /// <param name="deviceContext">The device context.</param>
    public void UnBindCubeMap(DeviceContextProxy deviceContext) {
        if (currRes is not { } resources || currSampler is not { } samplers)
            return;

        deviceContext.SetShaderResources<PixelShaderType>(cubeTextureSlot, resources);
        deviceContext.SetSamplers<PixelShaderType>(textureSamplerSlot, samplers);

        currSampler.OfType<SamplerStateProxy>()
            .DisposeAll();
        currRes.OfType<ShaderResourceView>()
            .DisposeAll();

        currSampler = [];
        currRes = [];
    }

    #endregion IReflector
}
