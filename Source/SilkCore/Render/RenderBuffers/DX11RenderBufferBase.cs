/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Core2D;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Render.RenderBuffers;
/// <summary>
/// </summary>
public abstract class DX11RenderBufferProxyBase : DisposeObject {
    

    /// <summary>
    ///     The vertical synchronize internal. Only valid under swapchain rendering mode. Default = 0
    ///     <para>0: disable; 1: Sync with frame.</para>
    /// </summary>
    public int VSyncInterval = 0;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DX11RenderBufferProxyBase" /> class.
    /// </summary>
    /// <param name="deviceResource">The device resources.</param>
    /// <param name="useDepthStencilBuffer"></param>
    public DX11RenderBufferProxyBase(IDeviceResources deviceResource, bool useDepthStencilBuffer = true) {
        DeviceResources = deviceResource;
        DeviceContextPool = new DeviceContextPool(deviceResource.NativeDeviceResources.Device);
        UseDepthStencilBuffer = useDepthStencilBuffer;
    }

    [AllowNull]
    public ShaderResourceViewProxy ColorBuffer {
        get => field ?? throw new InvalidOperationException("ColorBuffer buffer is not initialized.");
        private set {
            if (EqualityComparer<ShaderResourceView>.Default.Equals(value,field))
                return;
            
            field?.Dispose();
            field = value;
            
        }
    }

    [AllowNull]
    public ShaderResourceViewProxy BackBuffer {
        get => field ?? throw new InvalidOperationException("BackBuffer buffer is not initialized.");
        private set {
            if (EqualityComparer<ShaderResourceView>.Default.Equals(value,field))
                return;

            field?.Dispose();
            field = value;
        }
    }

    ///the depth stencil buffer
    [AllowNull]
    public ShaderResourceViewProxy DepthStencilBuffer {
        get => field ?? throw new InvalidOperationException("DepthStencilBuffer buffer is not initialized.");
        
        private set {
            if (EqualityComparer<ShaderResourceView>.Default.Equals(value,field))
                return;

            field?.Dispose();
            field = value;
        }
        
    }

    /// The depth stencil buffer
    public ShaderResourceViewProxy? DepthStencilBufferNoMsaa {
        get;
        private set {
            if(EqualityComparer<ShaderResourceView>.Default.Equals(value,field))
                return;
            
            field?.Dispose();
            field = value;
        }
    }

    /// <summary>
    ///     Gets the d2 d controls.
    /// </summary>
    /// <value>
    ///     The d2 d controls.
    /// </value>
    public D2DTargetProxy? D2DTarget {
        get;
        protected set {
            if(EqualityComparer<D2DTargetProxy>.Default.Equals(value,field))
                return;

            field?.Dispose();
            field = value;
        }
    }

    /// <summary>
    ///     Gets or sets the width of the target.
    /// </summary>
    /// <value>
    ///     The width of the target.
    /// </value>
    public int TargetWidth { get; private set; }

    /// <summary>
    ///     Gets or sets the height of the target.
    /// </summary>
    /// <value>
    ///     The height of the target.
    /// </value>
    public int TargetHeight { get; private set; }

    /// <summary>
    ///     Gets the device context pool.
    /// </summary>
    /// <value>
    ///     The device context pool.
    /// </value>
    [AllowNull]
    public IDeviceContextPool DeviceContextPool {
        get => field ?? throw new InvalidOperationException("Render buffer is disposed.");
        private set {
            if(EqualityComparer<IDeviceContextPool>.Default.Equals(value, field))
                return;
            field?.Dispose();
            field = value;
        }
    }
        

    /// <summary>
    ///     Gets or sets a value indicating whether this is initialized.
    /// </summary>
    /// <value>
    ///     <c>true</c> if initialized; otherwise, <c>false</c>.
    /// </value>
    public bool Initialized { get; private set; }

    /// <summary>
    ///     Gets or sets the texture format.
    /// </summary>
    /// <value>
    ///     The format.
    /// </value>
    public Format Format { get; set; } = Format.FormatB8G8R8A8Unorm;
    /// <summary>
    ///     Set MSAA level. If set to Two/Four/Eight, the actual level is set to minimum between Maximum and Two/Four/Eight
    /// </summary>
    public MsaaLevel Msaa { get; private set; } = MsaaLevel.Disable;
    /// <summary>
    ///     The currently used Direct3D Device
    /// </summary>
    public NativeD3DDevice Device => DeviceResources.Device;

    /// <summary>
    ///     Gets the device2 d.
    /// </summary>
    /// <value>
    ///     The device2 d.
    /// </value>
    public D2DDevice Device2D => DeviceResources.Device2D;

    /// <summary>
    ///     Gets the device context2 d.
    /// </summary>
    /// <value>
    ///     The device context2 d.
    /// </value>
    public D2DDeviceContext DeviceContext2D => DeviceResources.DeviceContext2D;

    /// <summary>
    ///     Gets or sets the device resources.
    /// </summary>
    /// <value>
    ///     The device resources.
    /// </value>
    protected IDeviceResources DeviceResources { get; }

    /// <summary>
    ///     Gets or sets a value indicating whether [use depth stencil buffer].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [use depth stencil buffer]; otherwise, <c>false</c>.
    /// </value>
    public bool UseDepthStencilBuffer { get; }

    /// <summary>
    ///     Gets or sets the sample description.
    /// </summary>
    /// <value>
    ///     The sample description.
    /// </value>
    public SampleDescription ColorBufferSampleDesc { get; private set; }

    /// <summary>
    ///     Whether render target/depth stencil buffer are MSAA buffers.
    /// </summary>
    public bool HasMsaa => ColorBufferSampleDesc.Count > 1;

    /// <summary>
    ///     Occurs when [on new buffer created].
    /// </summary>
    public event EventHandler<Texture2DArgs>? OnNewBufferCreated;

    /// <summary>
    ///     Occurs when [on device lost].
    /// </summary>
    public event EventHandler<EventArgs>? DeviceLost;

    private void CreateNonMsaaDepthStencilBuffer(int width, int height) {
        if (HasMsaa) {
            var depthFormat = Format.FormatD32FloatS8X24Uint;
            var depthdesc = new Texture2DDescription {
                BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource,
                Format = depthFormat.ComputeTextureFormat(out _),
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };
            DepthStencilBufferNoMsaa = new ShaderResourceViewProxy(DeviceResources, depthdesc);
            DepthStencilBufferNoMsaa.CreateDepthStencilView(new DepthStencilViewDescription {
                Format = depthFormat.ComputeDsvFormat(),
                Dimension = DepthStencilViewDimension.Texture2D
            });
            DepthStencilBufferNoMsaa.CreateTextureView(new ShaderResourceViewDescription {
                Format = depthFormat.ComputeSrvFormat(),
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource { MipLevels = 1 }
            });
        } else {
            DepthStencilBufferNoMsaa = DepthStencilBuffer;
        }
    }

    private ShaderResourceViewProxy CreateRenderTarget(int width, int height, MsaaLevel msaa) {
        Msaa = msaa;
        TargetWidth = width;
        TargetHeight = height;
        DisposeBuffers();
        ColorBufferSampleDesc = GetMsaaSampleDescription();
        OnCreateRenderTargetAndDepthBuffers(width,
                                            height,
                                            UseDepthStencilBuffer,
                                            out var colorBuffer,
                                            out var depthStencilBuffer);
        ColorBuffer = colorBuffer;
        DepthStencilBuffer = depthStencilBuffer;
        
        CreateNonMsaaDepthStencilBuffer(width, height);
        BackBuffer = OnCreateBackBuffer(width, height);
        BackBuffer.CreateRenderTargetView();

        #region Initialize Texture Pool

        InitializeTexturePools(width, height);

        #endregion

        Initialized = true;
        OnNewBufferCreated?.Invoke(this, new Texture2DArgs(BackBuffer));
        return BackBuffer;
    }

    private void InitializeTexturePools(int width, int height) {
        fullResPpBuffer = new PingPongColorBuffers(Format, width, height, DeviceResources);
        fullResDepthStencilPool = new TexturePool(DeviceResources,
                                                  new Texture2DDescription {
                                                      Width = width,
                                                      Height = height,
                                                      ArraySize = 1,
                                                      BindFlags = BindFlags.DepthStencil,
                                                      CpuAccessFlags = CpuAccessFlags.None,
                                                      Usage = ResourceUsage.Default,
                                                      MipLevels = 1,
                                                      OptionFlags = ResourceOptionFlags.None,
                                                      SampleDescription = new SampleDescription(1, 0)
                                                  });

        fullResRenderTargetPool = new TexturePool(DeviceResources,
                                                  new Texture2DDescription {
                                                      Width = width,
                                                      Height = height,
                                                      BindFlags = BindFlags.RenderTarget |
                                                                  BindFlags.ShaderResource,
                                                      CpuAccessFlags = CpuAccessFlags.None,
                                                      Usage = ResourceUsage.Default,
                                                      ArraySize = 1,
                                                      MipLevels = 1,
                                                      OptionFlags = ResourceOptionFlags.None,
                                                      SampleDescription = new SampleDescription(1, 0)
                                                  });

        halfResDepthStencilPool = new TexturePool(DeviceResources,
                                                  new Texture2DDescription {
                                                      Width = Math.Max(2, width / 2),
                                                      Height = Math.Max(2, height / 2),
                                                      ArraySize = 1,
                                                      BindFlags = BindFlags.DepthStencil,
                                                      CpuAccessFlags = CpuAccessFlags.None,
                                                      Usage = ResourceUsage.Default,
                                                      MipLevels = 1,
                                                      OptionFlags = ResourceOptionFlags.None,
                                                      SampleDescription = new SampleDescription(1, 0)
                                                  });

        halfResRenderTargetPool = new TexturePool(DeviceResources,
                                                  new Texture2DDescription {
                                                      Width = Math.Max(2, width / 2),
                                                      Height = Math.Max(2, height / 2),
                                                      BindFlags = BindFlags.RenderTarget |
                                                                  BindFlags.ShaderResource,
                                                      CpuAccessFlags = CpuAccessFlags.None,
                                                      Usage = ResourceUsage.Default,
                                                      ArraySize = 1,
                                                      MipLevels = 1,
                                                      OptionFlags = ResourceOptionFlags.None,
                                                      SampleDescription = new SampleDescription(1, 0)
                                                  });

        quarterResDepthStencilPool = new TexturePool(DeviceResources,
                                                     new Texture2DDescription {
                                                         Width = Math.Max(2, width / 4),
                                                         Height = Math.Max(2, height / 4),
                                                         ArraySize = 1,
                                                         BindFlags = BindFlags.DepthStencil,
                                                         CpuAccessFlags = CpuAccessFlags.None,
                                                         Usage = ResourceUsage.Default,
                                                         MipLevels = 1,
                                                         OptionFlags = ResourceOptionFlags.None,
                                                         SampleDescription = new SampleDescription(1, 0)
                                                     });

        quarterResRenderTargetPool = new TexturePool(DeviceResources,
                                                     new Texture2DDescription {
                                                         Width = Math.Max(2, width / 4),
                                                         Height = Math.Max(2, height / 4),
                                                         BindFlags = BindFlags.RenderTarget |
                                                                     BindFlags.ShaderResource,
                                                         CpuAccessFlags = CpuAccessFlags.None,
                                                         Usage = ResourceUsage.Default,
                                                         ArraySize = 1,
                                                         MipLevels = 1,
                                                         OptionFlags = ResourceOptionFlags.None,
                                                         SampleDescription = new SampleDescription(1, 0)
                                                     });
    }

    private void DisposeTexturePools() {
        RemoveAndDispose(ref fullResPpBuffer);
        RemoveAndDispose(ref fullResDepthStencilPool);
        RemoveAndDispose(ref fullResRenderTargetPool);
        RemoveAndDispose(ref halfResDepthStencilPool);
        RemoveAndDispose(ref halfResRenderTargetPool);
        RemoveAndDispose(ref quarterResDepthStencilPool);
        RemoveAndDispose(ref quarterResRenderTargetPool);
    }

    /// <summary>
    ///     Disposes the buffers.
    /// </summary>
    protected virtual void DisposeBuffers() {
        DeviceContext2D.Target = null;
        DisposeTexturePools();
        D2DTarget = null;
        ColorBuffer = null;
        DepthStencilBuffer = null;
        DepthStencilBufferNoMsaa = null;
        BackBuffer = null;
    }

    protected abstract ShaderResourceViewProxy OnCreateBackBuffer(int width, int height);

    protected virtual SampleDescription GetMsaaSampleDescription() {
        var sampleCount = 1;
        var sampleQuality = 0;
        if (Msaa != MsaaLevel.Disable)
            do {
                var newSampleCount = sampleCount * 2;
                var newSampleQuality =
                    Device.CheckMultisampleQualityLevels(Format.FormatB8G8R8A8Unorm, newSampleCount) - 1;

                if (newSampleQuality < 0)
                    break;

                sampleCount = newSampleCount;
                sampleQuality = newSampleQuality;
                if (sampleCount == (int)Msaa) break;
            } while (sampleCount < 32);
        return new SampleDescription(sampleCount, sampleQuality);
    }

    /// <summary>
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="createDepthStencilBuffer"></param>
    /// <param name="colorBuffer"></param>
    /// <param name="depthStencilBuffer"></param>
    /// <returns></returns>
    protected virtual void OnCreateRenderTargetAndDepthBuffers(
        int width,
        int height,
        bool createDepthStencilBuffer,
        out ShaderResourceViewProxy colorBuffer,
        out ShaderResourceViewProxy? depthStencilBuffer
    ) {
        var sampleDesc = ColorBufferSampleDesc;
        var optionFlags = ResourceOptionFlags.None;

        var colordesc = new Texture2DDescription {
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            Format = Format,
            Width = width,
            Height = height,
            MipLevels = 1,
            SampleDescription = sampleDesc,
            Usage = ResourceUsage.Default,
            OptionFlags = optionFlags,
            CpuAccessFlags = CpuAccessFlags.None,
            ArraySize = 1
        };

        colorBuffer = new ShaderResourceViewProxy(DeviceResources, colordesc);
        colorBuffer.CreateRenderTargetView();
        colorBuffer.CreateTextureView();
        if (createDepthStencilBuffer) {
            var depthdesc = new Texture2DDescription {
                BindFlags = BindFlags.DepthStencil,
                Format = Format.FormatD32FloatS8X24Uint.ComputeTextureFormat(out var canUseAsShaderResource),
                Width = width,
                Height = height,
                MipLevels = 1,
                SampleDescription = sampleDesc,
                Usage = ResourceUsage.Default,
                OptionFlags = ResourceOptionFlags.None,
                CpuAccessFlags = CpuAccessFlags.None,
                ArraySize = 1
            };
            canUseAsShaderResource &= !HasMsaa;
            if (canUseAsShaderResource) depthdesc.BindFlags |= BindFlags.ShaderResource;
            depthStencilBuffer = new ShaderResourceViewProxy(DeviceResources, depthdesc);
            depthStencilBuffer.CreateDepthStencilView(new DepthStencilViewDescription {
                Format = depthdesc.Format.ComputeDsvFormat(),
                Dimension = HasMsaa
                                ? DepthStencilViewDimension.Texture2DMultisampled
                                : DepthStencilViewDimension.Texture2D
            });
            if (canUseAsShaderResource)
                depthStencilBuffer.CreateTextureView(new ShaderResourceViewDescription {
                    Format = depthdesc.Format.ComputeSrvFormat(),
                    Dimension = ShaderResourceViewDimension.Texture2D,
                    Texture2D = new ShaderResourceViewDescription.Texture2DResource { MipLevels = depthdesc.MipLevels }
                });
        } else {
            depthStencilBuffer = null;
        }
    }

    /// <summary>
    ///     Sets the default render-targets
    /// </summary>
    public void SetDefaultRenderTargets(DeviceContextProxy.DeviceContextProxy context, bool isColorBuffer = true) {
        context.SetRenderTargets(isColorBuffer ? DepthStencilBuffer : null,
                                 [isColorBuffer ? ColorBuffer : BackBuffer]);
        
        //context.OutputMerger.SetTargets(depthStencilBuffer, new RenderTargetView[] { isColorBuffer ? colorBuffer : backBuffer });
        context.SetViewport(0, 0, TargetWidth, TargetHeight);
        context.SetScissorRectangle(0, 0, TargetWidth, TargetHeight);
    }

    /// <summary>
    ///     Clears the render target binding.
    /// </summary>
    /// <param name="context">The context.</param>
    public void ClearRenderTargetBinding(DeviceContextProxy.DeviceContextProxy context) {
        context.ClearRenderTagetBindings();
    }

    /// <summary>
    ///     Clears the render target.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="color">The color.</param>
    public void ClearRenderTarget(DeviceContextProxy.DeviceContextProxy context, Color4 color) 
        => ClearRenderTarget(context, color, clearBackBuffer: true, clearDepthStencilBuffer: true);

    /// <summary>
    ///     Clears the buffers with the clear-color
    /// </summary>
    /// <param name="context"></param>
    /// <param name="color"></param>
    /// <param name="clearBackBuffer"></param>
    /// <param name="clearDepthStencilBuffer"></param>
    public void ClearRenderTarget(
        DeviceContextProxy.DeviceContextProxy context,
        Color4 color,
        bool clearBackBuffer,
        bool clearDepthStencilBuffer
    ) {
        if (clearBackBuffer) context.ClearRenderTargetView(ColorBuffer, color);

        if (clearDepthStencilBuffer)
            context.ClearDepthStencilView(DepthStencilBuffer,
                                          DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil);
    }

    /// <summary>
    ///     Initializes.
    /// </summary>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    /// <param name="msaa">The msaa.</param>
    /// <returns></returns>
    public ShaderResourceViewProxy Initialize(int width, int height, MsaaLevel msaa) => CreateRenderTarget(width, height, msaa);

    /// <summary>
    ///     Resize render target and depthbuffer resolution
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public virtual ShaderResourceViewProxy Resize(int width, int height) => CreateRenderTarget(width, height, Msaa);

    /// <summary>
    ///     Begins the draw.
    /// </summary>
    /// <returns></returns>
    public virtual bool BeginDraw() => Initialized;

    /// <summary>
    ///     Ends the draw.
    /// </summary>
    /// <returns></returns>
    public virtual bool EndDraw() => true;

    /// <summary>
    ///     Presents this drawing..
    /// </summary>
    /// <returns></returns>
    public virtual bool Present() => true;

    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposeManagedResources">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to
    ///     release only unmanaged resources.
    /// </param>
    protected override void OnDispose(bool disposeManagedResources) {
        OnNewBufferCreated = null;
        DeviceLost = null;
        DisposeBuffers();
        DisposeTexturePools();
        DeviceContextPool = null;
        Initialized = false;
        base.OnDispose(disposeManagedResources);
    }

    #region ERROR HANDLING

    /// <summary>
    ///     Raises the on device lost.
    /// </summary>
    protected void RaiseOnDeviceLost() {
        DeviceLost?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Offscreen Texture Pools

    private PingPongColorBuffers? fullResPpBuffer;
    public PingPongColorBuffers FullResPpBuffer
        => fullResPpBuffer ?? throw new InvalidOperationException("Render buffer is not initialized.");

    private TexturePool? fullResDepthStencilPool;
    public TexturePool FullResDepthStencilPool
        => fullResDepthStencilPool ?? throw new InvalidOperationException("Render buffer is not initialized.");

    private TexturePool? fullResRenderTargetPool;
    public TexturePool FullResRenderTargetPool
        => fullResRenderTargetPool ?? throw new InvalidOperationException("Render buffer is not initialized.");

    private TexturePool? halfResDepthStencilPool;
    public TexturePool HalfResDepthStencilPool
        => halfResDepthStencilPool ?? throw new InvalidOperationException("Render buffer is not initialized.");

    private TexturePool? halfResRenderTargetPool;
    public TexturePool HalfResRenderTargetPool
        => halfResRenderTargetPool ?? throw new InvalidOperationException("Render buffer is not initialized.");

    private TexturePool? quarterResDepthStencilPool;
    public TexturePool QuarterResDepthStencilPool
        => quarterResDepthStencilPool ?? throw new InvalidOperationException("Render buffer is not initialized.");

    private TexturePool? quarterResRenderTargetPool;
    public TexturePool QuarterResRenderTargetPool
        => quarterResRenderTargetPool ?? throw new InvalidOperationException("Render buffer is not initialized.");

    #endregion
}
