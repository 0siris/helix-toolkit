/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.Logger;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core.Render;
/// <summary>
/// </summary>
public class SwapChainRenderHost : DefaultRenderHost {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    protected readonly nint Surface;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SwapChainRenderHost" /> class.
    /// </summary>
    /// <param name="surface">The window PTR.</param>
    public SwapChainRenderHost(nint surface) {
        Surface = surface;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SwapChainRenderHost" /> class.
    /// </summary>
    /// <param name="surface">The surface.</param>
    /// <param name="createRenderer">The create renderer.</param>
    public SwapChainRenderHost(nint surface, Func<IDevice3DResources, IRenderer> createRenderer) : base(
        createRenderer) {
        Surface = surface;
    }

    /// <summary>
    ///     Creates the render buffer.
    /// </summary>
    /// <returns></returns>
    protected override DX11RenderBufferProxyBase CreateRenderBuffer() {
        Logger.Info("Creating DX11SwapChainRenderBufferProxy");
        return new DX11SwapChainRenderBufferProxy(Surface, EffectsManager);
    }
}
