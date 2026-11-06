using System;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D9;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>Preserves native output addresses and single COM ownership during image interop initialization.</summary>
internal static unsafe class D3DImageInteropNative {
    /// <summary>Creates D3D9Ex through a stack output, adopting exactly the returned reference.</summary>
    /// <param name="api">The retained D3D9 export provider.</param>
    /// <returns>The sole owned D3D9Ex reference.</returns>
    /// <exception cref="COMException">Creation fails or succeeds without returning an interface.</exception>
    internal static ComPtr<IDirect3D9Ex> CreateDirect3D9(D3D9 api) {
        IDirect3D9Ex* native = null;
        var result = api.Direct3DCreate9Ex(D3D9.SdkVersion, &native);
        if (result < 0) {
            if (native != null) native->Release();
            throw new COMException("Direct3DCreate9Ex failed", result);
        }
        if (native == null)
            throw new COMException("Direct3DCreate9Ex succeeded without returning IDirect3D9Ex", unchecked((int) 0x80004003));
        return new ComPtr<IDirect3D9Ex> { Handle = native };
    }

    /// <summary>Clears the actual owner before releasing, including fields and array elements.</summary>
    /// <typeparam name="T">The owned COM interface.</typeparam>
    /// <param name="owner">The sole reference owner, empty on return.</param>
    internal static void Release<T>(ref ComPtr<T> owner) where T : unmanaged, IComVtbl<T> {
        var native = owner.Detach();
        if (native != null) ((IUnknown*) native)->Release();
    }
}
