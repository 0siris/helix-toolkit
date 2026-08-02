/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace SharpDX.Toolkit.Graphics;

/// <summary>
///     Base class for all <see cref="GraphicsResource" />.
/// </summary>
public abstract class GraphicsResource : Component {
    /// <summary>
    ///     The attached Direct3D11 resource to this instance.
    /// </summary>
    internal NativeD3DResource Resource;

    internal GraphicsResource() { }

    /// <summary>
    /// </summary>
    /// <param name="graphicsDevice"></param>
    protected GraphicsResource(NativeD3DDevice graphicsDevice) : this(graphicsDevice, null) { }

    /// <summary>
    /// </summary>
    /// <param name="graphicsDevice"></param>
    /// <param name="name"></param>
    protected GraphicsResource(NativeD3DDevice graphicsDevice, string name) : base(name) {
        if (graphicsDevice == null)
            ArgumentNullException.ThrowIfNull(graphicsDevice);

        GraphicsDevice = graphicsDevice;
    }

    /// <summary>
    ///     Device used to create this instance.
    /// </summary>
    public NativeD3DDevice GraphicsDevice { get; internal set; }

    /// <summary>
    ///     Initializes the specified device local.
    /// </summary>
    /// <param name="resource">The resource.</param>
    protected virtual void Initialize(NativeD3DResource resource) {
        Resource = ToDispose(resource);
    }

    /// <summary>
    ///     Implicit casting operator to the native D3D resource.
    /// </summary>
    /// <param name="from">The GraphicsResource to convert from.</param>
    public static implicit operator NativeD3DResource(GraphicsResource from) {
        return from?.Resource;
    }

    /// <summary>
    ///     Gets the CPU access flags from the <see cref="ResourceUsage" />.
    /// </summary>
    /// <param name="usage">The usage.</param>
    /// <returns>The CPU access flags</returns>
    protected static CpuAccessFlags GetCpuAccessFlagsFromUsage(ResourceUsage usage) {
        switch (usage) {
            case ResourceUsage.Dynamic:
                return CpuAccessFlags.Write;
            case ResourceUsage.Staging:
                return CpuAccessFlags.Read | CpuAccessFlags.Write;
        }

        return CpuAccessFlags.None;
    }

    /// <summary>
    /// </summary>
    /// <param name="disposeManagedResources"></param>
    protected override void Dispose(bool disposeManagedResources) {
        base.Dispose(disposeManagedResources);
        if (disposeManagedResources)
            Resource = null;
    }

    /// <summary>
    ///     Called when name changed for this component.
    /// </summary>
    protected override void OnPropertyChanged(string propertyName) {
        base.OnPropertyChanged(propertyName);
    }
}
