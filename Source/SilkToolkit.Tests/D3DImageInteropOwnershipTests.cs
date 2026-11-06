using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Core.Contexts;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D9;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Checks COM creation and cleanup without creating a window or native device.</summary>
public sealed unsafe class D3DImageInteropOwnershipTests {
    /// <summary>Releases a partially initialized real owner twice without touching the borrowed device.</summary>
    /// <param name="type">The production owner type.</param>
    [Theory]
    [InlineData(typeof(D3D12ImageInteropBridge))]
    [InlineData(typeof(D3DImageInteropProbe))]
    public void PartialInitializationCleanupClearsFieldsAndArrayElementsTest(Type type) {
        using var context = new FakeContext(0, false);
        var owner = RuntimeHelpers.GetUninitializedObject(type);
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic |
                                                     System.Reflection.BindingFlags.Instance;
        var destinations = new ComPtr<Silk.NET.Direct3D11.ID3D11Texture2D>[2];
        var surfaces = new ComPtr<IDirect3DSurface9>[2];
        var textures = new ComPtr<IDirect3DTexture9>[2];
        surfaces[1].Handle = (IDirect3DSurface9*) context.Native;
        type.GetField("destinations11", flags)!.SetValue(owner, destinations);
        type.GetField("surfaces9", flags)!.SetValue(owner, surfaces);
        type.GetField("textures9", flags)!.SetValue(owner, textures);
        type.GetField("direct3D9", flags)!.SetValue(owner, new ComPtr<IDirect3D9Ex> { Handle = context.Native });
        var cleanup = type.GetMethod("ReleaseResources", flags)!;
        cleanup.Invoke(owner, null);
        cleanup.Invoke(owner, null);
        var direct3D = (ComPtr<IDirect3D9Ex>) type.GetField("direct3D9", flags)!.GetValue(owner)!;
        Assert.Equal(2, context.Releases);
        Assert.Equal(0, (nint) direct3D.Handle);
        Assert.Equal(0, (nint) surfaces[1].Handle);
    }

    /// <summary>Enumerates through each real owner after collection during native creation.</summary>
    /// <param name="type">The production owner type.</param>
    [Theory]
    [InlineData(typeof(D3D12ImageInteropBridge))]
    [InlineData(typeof(D3DImageInteropProbe))]
    public void CreationAllowsAdapterEnumerationAfterCollectionTest(Type type) {
        using var context = new FakeContext(0, true);
        using var api = new D3D9(context);
        var owner = RuntimeHelpers.GetUninitializedObject(type);
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic |
                                                     System.Reflection.BindingFlags.Instance;
        var direct3D = D3DImageInteropNative.CreateDirect3D9(api);
        var field = type.GetField("direct3D9", flags)!;
        field.SetValue(owner, direct3D);
        try {
            var luid = new Luid { Low = 101, High = 202 };
            var enumerate = type.GetMethod("FindD3D9Adapter", flags)!;
            var adapter = (uint) enumerate.Invoke(owner, [luid])!;

            Assert.Equal(1u, adapter);
            Assert.Equal(0, context.Releases);
            Assert.Equal(0, context.AddRefs);
        } finally {
            field.SetValue(owner, default(ComPtr<IDirect3D9Ex>));
            D3DImageInteropNative.Release(ref direct3D);
        }
        Assert.Equal(1, context.Releases);
    }

    /// <summary>Demonstrates a stale output from the legacy generic overload without writing freed heap memory.</summary>
    [Fact]
    public void LegacyComPtrOutputIsNotPinnedAcrossCollectionTest() {
        using var context = new FakeContext(0, false);
        using var api = new D3D9(context);
        context.LegacyOwner = new HeapOwner();
        var result = api.Direct3DCreate9Ex(D3D9.SdkVersion, ref context.LegacyOwner.Value);
        Assert.Equal(0, result);
        Assert.True(context.OutputMoved);
        Assert.Equal(0, (nint) context.LegacyOwner.Value.Handle);
    }

    /// <summary>Models the bridge's movable COM field.</summary>
    private sealed class HeapOwner {
        /// <summary>Stores an initially empty COM owner.</summary>
        internal ComPtr<IDirect3D9Ex> Value;
    }
    /// <summary>Exercises collection during a native export, adoption, failure and repeated cleanup.</summary>
    /// <param name="result">The native creation HRESULT.</param>
    /// <param name="returnsInterface">Whether the export supplies an owned COM reference.</param>
    [Theory]
    [InlineData(0, true)]
    [InlineData(0, false)]
    [InlineData(unchecked((int) 0x8876086A), false)]
    [InlineData(unchecked((int) 0x8876086A), true)]
    public void CreationKeepsOutputStableAndBalancesOwnershipTest(int result, bool returnsInterface) {
        using var context = new FakeContext(result, returnsInterface);
        using var api = new D3D9(context);
        ComPtr<IDirect3D9Ex> owner = default;
        COMException? failure = null;
        try {
            owner = D3DImageInteropNative.CreateDirect3D9(api);
        } catch (COMException error) {
            failure = error;
        }
        if (result >= 0 && returnsInterface) {
            Assert.Null(failure);
            Assert.Equal((nint) context.Native, (nint) owner.Handle);
            Assert.Equal(0, context.Releases);
        } else {
            Assert.NotNull(failure);
            Assert.Equal(result < 0 ? result : unchecked((int) 0x80004003), failure.HResult);
            Assert.Equal(0, (nint) owner.Handle);
        }
        D3DImageInteropNative.Release(ref owner);
        D3DImageInteropNative.Release(ref owner);
        Assert.Equal(0, (nint) owner.Handle);
        Assert.Equal(returnsInterface ? 1 : 0, context.Releases);
        Assert.Equal(0, context.AddRefs);
    }

    /// <summary>Supplies managed callbacks as native exports and a minimal reference-counted COM object.</summary>
    private sealed class FakeContext : INativeContext {
        /// <summary>Identifies this serial fixture to unmanaged callbacks.</summary>
        private static FakeContext current = null!;
        /// <summary>Retains the requested native HRESULT.</summary>
        private readonly int result;
        /// <summary>Controls partial native initialization.</summary>
        private readonly bool returnsInterface;
        /// <summary>Optionally models the old heap output.</summary>
        internal HeapOwner? LegacyOwner;
        /// <summary>Records actual movement without dereferencing the stale address.</summary>
        internal bool OutputMoved;
        /// <summary>Prevents repeated native-memory cleanup.</summary>
        private bool disposed;
        /// <summary>Owns the D3D9Ex table with only reference counting and adapter identity implemented.</summary>
        private readonly void** table = (void**) NativeMemory.AllocZeroed(22, (nuint) sizeof(nint));
        /// <summary>Gets the fake interface; it never reaches a Direct3D API.</summary>
        internal IDirect3D9Ex* Native { get; } = (IDirect3D9Ex*) NativeMemory.AllocZeroed((nuint) sizeof(nint));
        /// <summary>Gets the observed number of releases.</summary>
        internal int Releases { get; private set; }
        /// <summary>Gets unexpected reference increments.</summary>
        internal int AddRefs { get; private set; }
        /// <summary>Initializes native memory and callbacks.</summary>
        /// <param name="result">The native HRESULT.</param>
        /// <param name="returnsInterface">Whether to supply an interface.</param>
        internal FakeContext(int result, bool returnsInterface) {
            this.result = result;
            this.returnsInterface = returnsInterface;
            current = this;
            table[1] = (void*) (delegate* unmanaged[Stdcall]<void*, uint>) &AddRef;
            table[2] = (void*) (delegate* unmanaged[Stdcall]<void*, uint>) &Release;
            table[4] = (void*) (delegate* unmanaged[Stdcall]<void*, uint>) &GetAdapterCount;
            table[21] = (void*) (delegate* unmanaged[Stdcall]<void*, uint, Luid*, int>) &GetAdapterLuid;
            *(void***) Native = table;
        }
        /// <inheritdoc />
        public nint GetProcAddress(string proc, int? slot = null) =>
            (nint) (delegate* unmanaged[Cdecl]<uint, IDirect3D9Ex**, int>) &Create;
        /// <inheritdoc />
        public bool TryGetProcAddress(string proc, out nint addr, int? slot = null) {
            addr = GetProcAddress(proc, slot);
            return true;
        }
        /// <summary>Collects while native code retains the output address, then writes the result.</summary>
        /// <param name="sdk">The SDK version.</param>
        /// <param name="output">The caller-owned output address.</param>
        /// <returns>The configured HRESULT.</returns>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        private static int Create(uint sdk, IDirect3D9Ex** output) {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            if (current.LegacyOwner is { } owner) {
                fixed (IDirect3D9Ex** actual = &owner.Value.Handle)
                    current.OutputMoved = actual != output;
                return current.result;
            }
            *output = current.returnsInterface ? current.Native : null;
            return current.result;
        }
        /// <summary>Records unexpected reference acquisition.</summary>
        /// <param name="self">The fake interface.</param>
        /// <returns>A fake count.</returns>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint AddRef(void* self) { current.AddRefs++; return 2; }
        /// <summary>Records reference release.</summary>
        /// <param name="self">The fake interface.</param>
        /// <returns>A fake count.</returns>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint Release(void* self) { current.Releases++; return 0; }
        /// <summary>Provides two adapters without consulting hardware.</summary>
        /// <param name="self">The fake interface.</param>
        /// <returns>The fake adapter count.</returns>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static uint GetAdapterCount(void* self) => 2;
        /// <summary>Matches only the second adapter to exercise identity rather than ordinal selection.</summary>
        /// <param name="self">The fake interface.</param>
        /// <param name="adapter">The adapter ordinal.</param>
        /// <param name="luid">The caller's identity output.</param>
        /// <returns>A successful HRESULT.</returns>
        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
        private static int GetAdapterLuid(void* self, uint adapter, Luid* luid) {
            *luid = new Luid { Low = adapter == 1 ? 101u : 100u, High = 202 };
            return 0;
        }
        /// <inheritdoc />
        public void Dispose() {
            // D3D9 also disposes its native context.
            if (disposed) return;
            disposed = true;
            *(void***) Native = null;
            NativeMemory.Free(table);
            NativeMemory.Free(Native);
        }
    }
}
