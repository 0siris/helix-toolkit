/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

#if DEBUG
//#define DEBUGMEMORY
#endif
using System.Diagnostics.CodeAnalysis;
using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core;

public sealed class EffectsManagerConfiguration
{
    public int AdapterIndex { get; set; } = -1;

    /// <summary>
    ///     Use software rendering.
    ///     <para>
    ///         Limitation: Must enable swap chain rendering to support this feature.
    ///     </para>
    /// </summary>
    public bool EnableSoftwareRendering { get; set; } = false;
}

/// <summary>
///     Shader and Technique manager
/// </summary>
public class EffectsManager : DisposeObject, IEffectsManager
{
    private static readonly ILogger logger = LogManager.Create<EffectsManager>();

    /// <summary>
    ///     Occurs when [on dispose resources].
    /// </summary>
    public event EventHandler<EventArgs> DisposingResources;

    /// <summary>
    ///     Occurs when [device created].
    /// </summary>
    public event EventHandler<EventArgs> Reinitialized;

    /// <summary>
    ///     Occurs when [on invalidate renderer].
    /// </summary>
    public event EventHandler<EventArgs> InvalidateRender;

    private readonly Dictionary<string, Lazy<IRenderTechnique>> techniqueDict = new();
    private readonly Dictionary<string, TechniqueDescription> techniqueDescriptions = new();

    /// <summary>
    ///     <see cref="IEffectsManager.RenderTechniques" />
    /// </summary>
    public IEnumerable<string> RenderTechniques => techniqueDict.Keys;

    private IConstantBufferPool constantBufferPool;

    /// <summary>
    ///     <see cref="IDevice3DResources.ConstantBufferPool" />
    /// </summary>
    public IConstantBufferPool ConstantBufferPool => constantBufferPool;

    private IShaderPoolManager shaderPoolManager;

    /// <summary>
    ///     <see cref="IEffectsManager.ShaderManager" />
    /// </summary>
    public IShaderPoolManager ShaderManager => shaderPoolManager;

    private IStatePoolManager statePoolManager;

    /// <summary>
    ///     <see cref="IDevice3DResources.StateManager" />
    /// </summary>
    public IStatePoolManager StateManager => statePoolManager;

    /// <summary>
    ///     Gets the geometry buffer manager.
    /// </summary>
    /// <value>
    ///     The geometry buffer manager.
    /// </value>
    public IGeometryBufferManager GeometryBufferManager => geometryBufferManager;

    private IGeometryBufferManager geometryBufferManager;

    /// <summary>
    ///     Gets the material texture manager.
    /// </summary>
    /// <value>
    ///     The material texture manager.
    /// </value>
    public ITextureResourceManager MaterialTextureManager => materialTextureManager;

    private ITextureResourceManager materialTextureManager;

    public IMaterialVariablePool MaterialVariableManager => materialVariableManager;
    private IMaterialVariablePool materialVariableManager;

    public IStructArrayPool StructArrayPool => structArrayPool;
    private StructArrayPool structArrayPool;

    #region 3D Resoruces

    private INativeDeviceResources nativeDeviceResources;

    public INativeDeviceResources NativeDeviceResources => nativeDeviceResources;

    /// <summary>
    /// </summary>
    public SilkD3DDevice Device => nativeDeviceResources?.Device;

    /// <summary>
    /// </summary>
    public DriverType DriverType { get; private set; }

    private IDeviceContextPool deviceContextPool;

    /// <summary>
    ///     Gets the device context pool.
    /// </summary>
    /// <value>
    ///     The device context pool.
    /// </value>
    public IDeviceContextPool DeviceContextPool => deviceContextPool;

    #endregion

    #region 2D Resources

    private D2DDevice device2D;

    /// <summary>
    ///     Gets the device2d.
    /// </summary>
    /// <value>
    ///     The device2 d.
    /// </value>
    public D2DDevice Device2D => device2D;


    private D2DDeviceContext deviceContext2D;

    /// <summary>
    ///     Gets or sets the device2 d context.
    /// </summary>
    /// <value>
    ///     The device2 d context.
    /// </value>
    public D2DDeviceContext DeviceContext2D => deviceContext2D;

    /// <summary>
    ///     Gets the factory2 d.
    /// </summary>
    /// <value>
    ///     The factory2 d.
    /// </value>
    public D2DFactory Factory2D => factory2D;

    private D2DFactory factory2D;

    private WICImagingFactory wicImgFactory;

    /// <summary>
    ///     Gets the wic img factory.
    /// </summary>
    /// <value>
    ///     The wic img factory.
    /// </value>
    public WICImagingFactory WICImgFactory => wicImgFactory;

    private DirectWriteFactory directWriteFactory;

    /// <summary>
    ///     Gets the direct write factory.
    /// </summary>
    /// <value>
    ///     The direct write factory.
    /// </value>
    public DirectWriteFactory DirectWriteFactory => directWriteFactory;

    #endregion

    /// <summary>
    /// </summary>
    public int AdapterIndex { get; private set; } = -1;

    /// <summary>
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    /// </summary>
    public bool EnableSoftwareRendering { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EffectsManager" /> class.
    /// </summary>
    public EffectsManager()
        : this(new EffectsManagerConfiguration())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EffectsManager" /> class.
    /// </summary>
    /// <param name="adapterIndex">Index of the adapter.</param>
    public EffectsManager(int adapterIndex)
        : this(new EffectsManagerConfiguration
        {
            AdapterIndex = adapterIndex
        })
    {
    }

    public EffectsManager(EffectsManagerConfiguration configuration)
    {
        EnableSoftwareRendering = configuration.EnableSoftwareRendering;
        Initialize(configuration.AdapterIndex);
    }

    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    private void Initialize()
    {
#if DEBUGMEMORY
            global::SharpDX.Configuration.EnableObjectTracking = true;
#endif
        if (AdapterIndex == -1)
            Initialize(0);
        else
            Initialize(AdapterIndex);
    }

    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    private void Initialize(int adapterIndex)
    {
        logger.LogInformation("Adapter Index = {0}", adapterIndex);
        AdapterIndex = Math.Max(0, adapterIndex);
#if DX11
        DriverType = EnableSoftwareRendering ? DriverType.Warp : DriverType.Hardware;
        RemoveAndDispose(ref nativeDeviceResources);
        nativeDeviceResources = SilkD3D11DeviceFactory.CreateDefault(
            AdapterIndex,
            DriverType == DriverType.Warp ? SilkDriverType.Warp : SilkDriverType.Hardware);
#endif
#else
            throw new PlatformNotSupportedException("DirectX 11 support is required.");
#endif

        logger.LogInformation("Direct3D device initilized. DriverType: {0}; FeatureLevel: {1}", DriverType,
            nativeDeviceResources.Device.FeatureLevel);

        #region Initial Internal Pools

        logger.LogInformation("Initializing resource pools");
        RemoveAndDispose(ref constantBufferPool);
        constantBufferPool = new ConstantBufferPool(nativeDeviceResources.Device);

        RemoveAndDispose(ref shaderPoolManager);
        shaderPoolManager = new ShaderPoolManager(nativeDeviceResources.Device, constantBufferPool);

        RemoveAndDispose(ref statePoolManager);
        statePoolManager = new StatePoolManager(nativeDeviceResources.Device);

        RemoveAndDispose(ref geometryBufferManager);
        geometryBufferManager = new GeometryBufferManager(this);

        RemoveAndDispose(ref materialTextureManager);
        materialTextureManager = new TextureResourceManager(nativeDeviceResources.Device);

        RemoveAndDispose(ref materialVariableManager);
        materialVariableManager = new MaterialVariablePool(this);

        RemoveAndDispose(ref deviceContextPool);
        deviceContextPool = new DeviceContextPool(nativeDeviceResources.Device);

        RemoveAndDispose(ref structArrayPool);
        structArrayPool = new StructArrayPool();

        #endregion

        logger.LogInformation("Initializing Direct2D resource handles");
        factory2D = new D2DFactory();
        wicImgFactory = new WICImagingFactory();
        directWriteFactory = new DirectWriteFactory();
        device2D = new D2DDevice(nativeDeviceResources.Device);
        deviceContext2D = new D2DDeviceContext(device2D);
        Initialized = true;
    }

    /// <summary>
    ///     <see cref="IEffectsManager.AddTechnique(TechniqueDescription)" />
    /// </summary>
    /// <param name="description"></param>
    public void AddTechnique(TechniqueDescription description)
    {
        if (techniqueDict.ContainsKey(description.Name))
            throw new ArgumentException($"Technique {description.Name} already exists.");
        techniqueDescriptions.Add(description.Name, description);
        techniqueDict.Add(description.Name,
            new Lazy<IRenderTechnique>(() => { return Initialized ? new Technique(description, this) : null; }, true));
    }

    /// <summary>
    ///     Reinitializes all resources after calling <see cref="DisposeAllResources" />.
    /// </summary>
    public void Reinitialize()
    {
        if (!Initialized)
        {
            Initialize();
            foreach (var tech in techniqueDescriptions.Values)
                techniqueDict.Add(tech.Name,
                    new Lazy<IRenderTechnique>(() => { return Initialized ? new Technique(tech, this) : null; }, true));
            Reinitialized?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Disposes all resources. This is used to handle such as DeviceLost or DeviceRemoved Error
    /// </summary>
    public void DisposeAllResources()
    {
        if (Initialized) DisposeResources();
    }

    /// <summary>
    ///     Determines whether the specified name has technique.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>
    ///     <c>true</c> if the specified name has technique; otherwise, <c>false</c>.
    /// </returns>
    public bool HasTechnique(string name)
    {
        return techniqueDict.ContainsKey(name);
    }

    /// <summary>
    ///     <see cref="IEffectsManager.RemoveTechnique(string)" />
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool RemoveTechnique(string name)
    {
        if (techniqueDict.TryGetValue(name, out var t))
        {
            if (t.IsValueCreated)
            {
                var v = t.Value;
                RemoveAndDispose(ref v);
            }

            techniqueDescriptions.Remove(name);
            return techniqueDict.Remove(name);
        }

        return false;
    }

    /// <summary>
    ///     Removes all technique.
    /// </summary>
    public void RemoveAllTechniques()
    {
        var names = techniqueDict.Keys.ToArray();
        foreach (var name in names) RemoveTechnique(name);
    }

    /// <summary>
    ///     Gets the technique.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    /// <exception cref="Exception">Manager has not been initialized.</exception>
    /// <exception cref="ArgumentException"></exception>
    public IRenderTechnique GetTechnique(string name)
    {
        if (!techniqueDict.TryGetValue(name, out var t))
        {
            logger.LogWarning("Technique {0} does not exist. Return a null technique.", name);
            return new Technique(new TechniqueDescription {Name = name, IsNull = true}, this);
        }

        return t.Value;
    }

    /// <summary>
    ///     Gets the <see cref="IRenderTechnique" /> with the specified name.
    /// </summary>
    /// <value>
    ///     The <see cref="IRenderTechnique" />.
    /// </value>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public IRenderTechnique this[string name] => GetTechnique(name);

    /// <summary>
    ///     <see cref="DisposeObject.OnDispose(bool)" />
    /// </summary>
    /// <param name="disposeManagedResources"></param>
    [SuppressMessage("Microsoft.Usage", "CA2213", Justification = "False positive.")]
    protected override void OnDispose(bool disposeManagedResources)
    {
        DisposeResources();
        Initialized = false;
        base.OnDispose(disposeManagedResources);
#if DEBUGMEMORY
            ReportResources();
#endif
    }

    private void DisposeResources()
    {
        DisposingResources?.Invoke(this, EventArgs.Empty);
        foreach (var technique in techniqueDict.Values.ToArray())
            if (technique.IsValueCreated)
            {
                var t = technique.Value;
                RemoveAndDispose(ref t);
            }

        techniqueDict.Clear();
        RemoveAndDispose(ref geometryBufferManager);
        RemoveAndDispose(ref materialTextureManager);
        RemoveAndDispose(ref materialVariableManager);
        RemoveAndDispose(ref directWriteFactory);
        RemoveAndDispose(ref shaderPoolManager);
        RemoveAndDispose(ref constantBufferPool);
        RemoveAndDispose(ref statePoolManager);
        RemoveAndDispose(ref deviceContextPool);
        RemoveAndDispose(ref deviceContext2D);
        RemoveAndDispose(ref device2D);
        RemoveAndDispose(ref factory2D);
        RemoveAndDispose(ref wicImgFactory);
        RemoveAndDispose(ref structArrayPool);
        Initialized = false;
        RemoveAndDispose(ref nativeDeviceResources);
#if DEBUGMEMORY
            ReportResources();
#endif
    }

#if DEBUGMEMORY
        protected void ReportResources()
        {
            logger.LogDebug(global::SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
            var liveObjects = global::SharpDX.Diagnostics.ObjectTracker.FindActiveObjects();
            logger.LogDebug("Live object count = {0}", liveObjects.Count);
            //if (liveObjects.Count != 0)
            //{
            //    foreach(var obj in liveObjects)
            //    {
            //        logger.LogDebug(obj.ToString());
            //    }
            //}
        }
#endif

    public void RaiseInvalidateRender()
    {
        InvalidateRender?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Outputs the resource cout summary.
    /// </summary>
    /// <returns></returns>
    public string GetResourceCountSummary()
    {
        return $"ConstantBuffer Count: {constantBufferPool.Count}\n" +
               $"BlendState Count: {statePoolManager.BlendStatePool.Count}\n" +
               $"DepthStencilState Count: {statePoolManager.DepthStencilStatePool.Count}\n" +
               $"RasterState Count: {statePoolManager.RasterStatePool.Count}\n" +
               $"SamplerState Count: {statePoolManager.SamplerStatePool.Count}\n" +
               $"GeometryBuffer Count:{geometryBufferManager.Count}\n" +
               $"MaterialTexture Count:{materialTextureManager.Count}\n" +
               $"MaterialVariable Count:{materialVariableManager.Count}\n";
    }
}