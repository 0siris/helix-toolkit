/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

#if DEBUG
//#define DEBUGMEMORY
#endif
using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core;

public sealed class EffectsManagerConfiguration {
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
public class EffectsManager : DisposeObject, IEffectsManager {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    /// <summary>
    ///     Occurs when [on dispose resources].
    /// </summary>
    public event EventHandler<EventArgs>? DisposingResources;

    /// <summary>
    ///     Occurs when [device created].
    /// </summary>
    public event EventHandler<EventArgs>? Reinitialized;

    /// <summary>
    ///     Occurs when [on invalidate renderer].
    /// </summary>
    public event EventHandler<EventArgs>? InvalidateRender;

    private readonly Dictionary<string, Lazy<IRenderTechnique?>> techniqueDict = [];
    private readonly Dictionary<string, TechniqueDescription> techniqueDescriptions = [];

    /// <summary>
    ///     <see cref="IEffectsManager.RenderTechniques" />
    /// </summary>
    public IEnumerable<string> RenderTechniques => techniqueDict.Keys;

    private IConstantBufferPool? constantBufferPool;

    /// <summary>
    ///     <see cref="IDevice3DResources.ConstantBufferPool" />
    /// </summary>
    public IConstantBufferPool ConstantBufferPool
        => constantBufferPool.AssertNotNull("Effects manager is not initialized.");

    private IShaderPoolManager? shaderPoolManager;

    /// <summary>
    ///     <see cref="IEffectsManager.ShaderManager" />
    /// </summary>
    public IShaderPoolManager ShaderManager => shaderPoolManager.AssertNotNull("Effects manager is not initialized.");

    private IStatePoolManager? statePoolManager;

    /// <summary>
    ///     <see cref="IDevice3DResources.StateManager" />
    /// </summary>
    public IStatePoolManager StateManager => statePoolManager.AssertNotNull("Effects manager is not initialized.");

    /// <summary>
    ///     Gets the geometry buffer manager.
    /// </summary>
    /// <value>
    ///     The geometry buffer manager.
    /// </value>
    public IGeometryBufferManager GeometryBufferManager =>
        geometryBufferManager.AssertNotNull("Effects manager is not initialized.");

    private IGeometryBufferManager? geometryBufferManager;

    /// <summary>
    ///     Gets the material texture manager.
    /// </summary>
    /// <value>
    ///     The material texture manager.
    /// </value>
    public ITextureResourceManager MaterialTextureManager =>
        materialTextureManager.AssertNotNull("Effects manager is not initialized.");

    private ITextureResourceManager? materialTextureManager;

    public IMaterialVariablePool MaterialVariableManager =>
        materialVariableManager.AssertNotNull("Effects manager is not initialized.");

    private IMaterialVariablePool? materialVariableManager;

    public IStructArrayPool StructArrayPool => structArrayPool.AssertNotNull("Effects manager is not initialized.");
    private StructArrayPool? structArrayPool;

    #region 3D Resoruces

    private INativeDeviceResources? nativeDeviceResources;

    public INativeDeviceResources NativeDeviceResources
        => nativeDeviceResources.AssertNotNull("Effects manager is not initialized.");

    /// <summary>
    /// </summary>
    public SilkD3DDevice Device => NativeDeviceResources.Device;

    /// <summary>
    /// </summary>
    public DriverType DriverType { get; private set; }

    private IDeviceContextPool? deviceContextPool;

    /// <summary>
    ///     Gets the device context pool.
    /// </summary>
    /// <value>
    ///     The device context pool.
    /// </value>
    public IDeviceContextPool DeviceContextPool
        => deviceContextPool.AssertNotNull("Effects manager is not initialized.");

    #endregion

    #region 2D Resources

    private D2DDevice? device2D;

    /// <summary>
    ///     Gets the device2d.
    /// </summary>
    /// <value>
    ///     The device2 d.
    /// </value>
    public D2DDevice Device2D => device2D.AssertNotNull("Effects manager is not initialized.");


    private D2DDeviceContext? deviceContext2D;

    /// <summary>
    ///     Gets or sets the device2 d context.
    /// </summary>
    /// <value>
    ///     The device2 d context.
    /// </value>
    public D2DDeviceContext DeviceContext2D => deviceContext2D.AssertNotNull("Effects manager is not initialized.");

    /// <summary>
    ///     Gets the factory2 d.
    /// </summary>
    /// <value>
    ///     The factory2 d.
    /// </value>
    public D2DFactory Factory2D => factory2D.AssertNotNull("Effects manager is not initialized.");

    private D2DFactory? factory2D;

    private WicImagingFactory? wicImgFactory;

    /// <summary>
    ///     Gets the wic img factory.
    /// </summary>
    /// <value>
    ///     The wic img factory.
    /// </value>
    public WicImagingFactory WicImgFactory => wicImgFactory.AssertNotNull("Effects manager is not initialized.");

    private DirectWriteFactory? directWriteFactory;

    /// <summary>
    ///     Gets the direct write factory.
    /// </summary>
    /// <value>
    ///     The direct write factory.
    /// </value>
    public DirectWriteFactory DirectWriteFactory
        => directWriteFactory.AssertNotNull("Effects manager is not initialized.");

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
        : this(new EffectsManagerConfiguration()) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="EffectsManager" /> class.
    /// </summary>
    /// <param name="adapterIndex">Index of the adapter.</param>
    public EffectsManager(int adapterIndex)
        : this(new EffectsManagerConfiguration {
            AdapterIndex = adapterIndex
        }) { }

    public EffectsManager(EffectsManagerConfiguration configuration) {
        EnableSoftwareRendering = configuration.EnableSoftwareRendering;
        Initialize(configuration.AdapterIndex);
    }

    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    private void Initialize() {
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
    private void Initialize(int adapterIndex) {
        Logger.Info("Adapter Index = {Value0}", adapterIndex);
        AdapterIndex = Math.Max(0, adapterIndex);
        DriverType = EnableSoftwareRendering
            ? DriverType.Warp
            : DriverType.Hardware;
        RemoveAndDispose(ref nativeDeviceResources);
        var resources = SilkD3D11DeviceFactory.CreateDefault(AdapterIndex,
            DriverType == DriverType.Warp
                ? SilkDriverType.Warp
                : SilkDriverType.Hardware);
        nativeDeviceResources = resources;

        Logger.Info("Direct3D device initilized. DriverType: {Value0}; FeatureLevel: {Value1}",
            DriverType,
            resources.Device.FeatureLevel);

        #region Initial Internal Pools

        Logger.Info("Initializing resource pools");
        RemoveAndDispose(ref constantBufferPool);
        constantBufferPool = new ConstantBufferPool(resources.Device);

        RemoveAndDispose(ref shaderPoolManager);
        shaderPoolManager = new ShaderPoolManager(resources.Device, constantBufferPool);

        RemoveAndDispose(ref statePoolManager);
        statePoolManager = new StatePoolManager(resources.Device);

        RemoveAndDispose(ref geometryBufferManager);
        geometryBufferManager = new GeometryBufferManager(this);

        RemoveAndDispose(ref materialTextureManager);
        materialTextureManager = new TextureResourceManager(resources.Device);

        RemoveAndDispose(ref materialVariableManager);
        materialVariableManager = new MaterialVariablePool(this);

        RemoveAndDispose(ref deviceContextPool);
        deviceContextPool = new DeviceContextPool(resources.Device);

        RemoveAndDispose(ref structArrayPool);
        structArrayPool = new StructArrayPool();

        #endregion

        Logger.Info("Initializing Direct2D resource handles");
        factory2D = new D2DFactory();
        wicImgFactory = new WicImagingFactory();
        directWriteFactory = new DirectWriteFactory();
        device2D = new D2DDevice(resources.Device);
        deviceContext2D = new D2DDeviceContext(device2D);
        Initialized = true;
    }

    /// <summary>
    ///     <see cref="IEffectsManager.AddTechnique(TechniqueDescription)" />
    /// </summary>
    /// <param name="description"></param>
    public void AddTechnique(TechniqueDescription description) {
        var name = description.Name.AssertNotNull("Technique name must be initialized.");
        if (techniqueDict.ContainsKey(name))
            throw new ArgumentException($"Technique {name} already exists.");
        techniqueDescriptions.Add(name, description);
        techniqueDict.Add(name,
            new Lazy<IRenderTechnique?>(() => Initialized
                    ? new Technique(description, this)
                    : null,
                true));
    }

    /// <summary>
    ///     Reinitializes all resources after calling <see cref="DisposeAllResources" />.
    /// </summary>
    public void Reinitialize() {
        if (!Initialized) {
            Initialize();
            foreach (var tech in techniqueDescriptions.Values) {
                var name = tech.Name.AssertNotNull("Technique name must be initialized.");
                techniqueDict.Add(name,
                    new Lazy<IRenderTechnique?>(() => Initialized
                            ? new Technique(tech, this)
                            : null,
                        true));
            }

            Reinitialized?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    ///     Disposes all resources. This is used to handle such as DeviceLost or DeviceRemoved Error
    /// </summary>
    public void DisposeAllResources() {
        if (Initialized) DisposeResources();
    }

    /// <summary>
    ///     Determines whether the specified name has technique.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>
    ///     <c>true</c> if the specified name has technique; otherwise, <c>false</c>.
    /// </returns>
    public bool HasTechnique(string name) => techniqueDict.ContainsKey(name);

    /// <summary>
    ///     <see cref="IEffectsManager.RemoveTechnique(string)" />
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool RemoveTechnique(string name) {
        if (techniqueDict.TryGetValue(name, out var t)) {
            if (t.IsValueCreated) {
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
    public void RemoveAllTechniques() {
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
    public IRenderTechnique GetTechnique(string name) {
        if (!techniqueDict.TryGetValue(name, out var t)) {
            Logger.Warn("Technique {Value0} does not exist. Return a null technique.", name);
            return new Technique(new TechniqueDescription {
                Name = name,
                IsNull = true
            }, this);
        }

        return t.Value ?? Technique.NullTechnique;
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
    protected override void OnDispose(bool disposeManagedResources) {
        DisposeResources();
        Initialized = false;
        base.OnDispose(disposeManagedResources);
#if DEBUGMEMORY
            ReportResources();
#endif
    }

    private void DisposeResources() {
        DisposingResources?.Invoke(this, EventArgs.Empty);
        foreach (var technique in techniqueDict.Values.ToArray())
            if (technique.IsValueCreated) {
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
            Logger.Debug(global::SharpDX.Diagnostics.ObjectTracker.ReportActiveObjects());
            var liveObjects = global::SharpDX.Diagnostics.ObjectTracker.FindActiveObjects();
            Logger.Debug("Live object count = {Value0}", liveObjects.Count);
            //if (liveObjects.Count != 0)
            //{
            //    foreach(var obj in liveObjects)
            //    {
            //        Logger.Debug(obj.ToString());
            //    }
            //}
        }
#endif

    public void RaiseInvalidateRender() {
        InvalidateRender?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    ///     Outputs the resource cout summary.
    /// </summary>
    /// <returns></returns>
    public string GetResourceCountSummary() => $"ConstantBuffer Count: {ConstantBufferPool.Count}\n" +
                                               $"BlendState Count: {StateManager.BlendStatePool.Count}\n" +
                                               $"DepthStencilState Count: {StateManager.DepthStencilStatePool.Count}\n" +
                                               $"RasterState Count: {StateManager.RasterStatePool.Count}\n" +
                                               $"SamplerState Count: {StateManager.SamplerStatePool.Count}\n" +
                                               $"GeometryBuffer Count:{GeometryBufferManager.Count}\n" +
                                               $"MaterialTexture Count:{MaterialTextureManager.Count}\n" +
                                               $"MaterialVariable Count:{MaterialVariableManager.Count}\n";
}