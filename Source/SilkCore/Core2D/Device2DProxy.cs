/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core2D;
/// <summary>
/// </summary>
public sealed class D2DTargetProxy : DisposeObject {
    /// <summary>
    ///     Gets the d2d target. Which is bind to the 3D back buffer/texture
    /// </summary>
    /// <value>
    ///     The d2d target.
    /// </value>
    public BitmapProxy? D2DTarget {
        get;
        private set {
            if (field != value)
                field?.Dispose();
            field = value;
        }
    }

    
    /// <summary>
    /// </summary>
    /// <param name="swapChain"></param>
    /// <param name="deviceContext"></param>
    [MemberNotNull(nameof(D2DTarget))]
    public void Initialize(object swapChain, D2DDeviceContext deviceContext) 
        => D2DTarget = BitmapProxy.Create("SwapChainTarget", deviceContext, swapChain);

    /// <summary>
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="deviceContext"></param>
    [MemberNotNull(nameof(D2DTarget))]
    public void Initialize(Texture2D texture, D2DDeviceContext deviceContext) 
        => D2DTarget = BitmapProxy.Create("TextureTarget", deviceContext, texture);

    protected override void OnDispose(bool disposeManagedResources) {
        D2DTarget = null;
        base.OnDispose(disposeManagedResources);
    }
}
