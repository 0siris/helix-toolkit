/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.Maths;
using SilkD3D11BlendStatePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11BlendState>;
using SilkD3D11BufferPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Buffer>;
using SilkD3D11CommandListPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11CommandList>;
using SilkD3D11ComputeShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11ComputeShader>;
using SilkD3D11ContextPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DeviceContext>;
using SilkD3D11DepthStencilStatePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DepthStencilState>;
using SilkD3D11DepthStencilViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DepthStencilView>;
using SilkD3D11DevicePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Device>;
using SilkD3D11DomainShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DomainShader>;
using SilkD3D11GeometryShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11GeometryShader>;
using SilkD3D11HullShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11HullShader>;
using SilkD3D11InputLayoutPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11InputLayout>;
using SilkD3D11PixelShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11PixelShader>;
using SilkD3D11RasterizerStatePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11RasterizerState>;
using SilkD3D11RenderTargetViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11RenderTargetView>;
using SilkD3D11SamplerStatePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11SamplerState>;
using SilkD3D11ShaderResourceViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11ShaderResourceView>;
using SilkD3D11Texture1DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture1D>;
using SilkD3D11Texture2DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture2D>;
using SilkD3D11Texture3DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture3D>;
using SilkD3D11UnorderedAccessViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11UnorderedAccessView>;
using SilkD3D11VertexShaderPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11VertexShader>;

namespace HelixToolkit.SharpDX.Core.Native;

public enum SilkDriverType {
    Unknown = 0,
    Hardware,
    Warp,
    Reference,
    Software
}

public enum SilkFeatureLevel {
    Unknown = 0,
    Level_9_1,
    Level_9_2,
    Level_9_3,
    Level_10_0,
    Level_10_1,
    Level_11_0,
    Level_11_1
}

public sealed unsafe class SilkD3DDevice : IDisposable {
    private SilkD3D11DevicePtr nativeDevice;

    public SilkD3DDevice(
        SilkD3D11DevicePtr nativeDevice,
        SilkDriverType driverType,
        SilkFeatureLevel featureLevel
    ) {
        if (nativeDevice.Handle == null) throw new ArgumentNullException(nameof(nativeDevice));

        this.nativeDevice = nativeDevice;
        DriverType = driverType;
        FeatureLevel = featureLevel;
    }

    public nint NativePointer => (nint)nativeDevice.Handle;

    internal ID3D11Device* Handle => nativeDevice.Handle;

    internal ref SilkD3D11DevicePtr NativeDevice => ref nativeDevice;

    public SilkDriverType DriverType { get; }

    public SilkFeatureLevel FeatureLevel { get; }

    public bool IsDisposed { get; private set; }

    public void Dispose() {
        if (IsDisposed) return;

        nativeDevice.Dispose();
        IsDisposed = true;
    }

    public int CheckMultisampleQualityLevels(Format format, int sampleCount) {
        uint qualityLevels = 0;
        SilkMarshal.ThrowHResult(
            nativeDevice.CheckMultisampleQualityLevels(format, (uint)sampleCount, ref qualityLevels));
        return (int)qualityLevels;
    }

    public SilkD3DDeviceContext CreateDeferredContext() {
        ID3D11DeviceContext* context = null;
        SilkMarshal.ThrowHResult(nativeDevice.CreateDeferredContext(0, ref context));
        return new SilkD3DDeviceContext(new SilkD3D11ContextPtr(context), true);
    }

    public SilkD3D11BufferPtr CreateBuffer(BufferDescription description) {
        var bufferDesc = description.ToSilkDesc();
        ID3D11Buffer* buffer = null;
        SilkMarshal.ThrowHResult(nativeDevice.CreateBuffer(ref bufferDesc, null, ref buffer));
        return new SilkD3D11BufferPtr(buffer);
    }

    public SilkD3D11BufferPtr CreateBuffer(BufferDescription description, nint initialData) {
        if (initialData == nint.Zero) return CreateBuffer(description);

        var bufferDesc = description.ToSilkDesc();
        var subresource = new SubresourceData {
            PSysMem = initialData.ToPointer()
        };

        ID3D11Buffer* buffer = null;
        SilkMarshal.ThrowHResult(nativeDevice.CreateBuffer(ref bufferDesc, ref subresource, ref buffer));
        return new SilkD3D11BufferPtr(buffer);
    }

    public Texture1D CreateTexture1D(Texture1DDescription description, DataBox[]? initialData = null) {
        var textureDesc = description.ToSilkDesc();
        ID3D11Texture1D* texture = null;
        CreateTexture(ref textureDesc, initialData, ref texture);
        return new Texture1D(new SilkD3D11Texture1DPtr(texture), this, description);
    }

    public Texture2D CreateTexture2D(Texture2DDescription description, DataBox[]? initialData = null) {
        var textureDesc = description.ToSilkDesc();
        ID3D11Texture2D* texture = null;
        CreateTexture(ref textureDesc, initialData, ref texture);
        return new Texture2D(new SilkD3D11Texture2DPtr(texture), this, description);
    }

    public Texture3D CreateTexture3D(Texture3DDescription description, DataBox[]? initialData = null) {
        var textureDesc = description.ToSilkDesc();
        ID3D11Texture3D* texture = null;
        CreateTexture(ref textureDesc, initialData, ref texture);
        return new Texture3D(new SilkD3D11Texture3DPtr(texture), this, description);
    }

    public BlendState CreateBlendState(BlendStateDescription description) {
        var stateDesc = description.ToSilkDesc();
        ID3D11BlendState* state = null;
        SilkMarshal.ThrowHResult(nativeDevice.CreateBlendState(ref stateDesc, ref state));
        return new BlendState(new SilkD3D11BlendStatePtr(state), description);
    }

    public DepthStencilState CreateDepthStencilState(DepthStencilStateDescription description) {
        var stateDesc = description.ToSilkDesc();
        ID3D11DepthStencilState* state = null;
        SilkMarshal.ThrowHResult(nativeDevice.CreateDepthStencilState(ref stateDesc, ref state));
        return new DepthStencilState(new SilkD3D11DepthStencilStatePtr(state), description);
    }

    public RasterizerState CreateRasterizerState(RasterizerStateDescription description) {
        var stateDesc = description.ToSilkDesc();
        ID3D11RasterizerState* state = null;
        SilkMarshal.ThrowHResult(nativeDevice.CreateRasterizerState(ref stateDesc, ref state));
        return new RasterizerState(new SilkD3D11RasterizerStatePtr(state), description);
    }

    public SamplerState CreateSamplerState(SamplerStateDescription description) {
        var stateDesc = description.ToSilkDesc();
        ID3D11SamplerState* state = null;
        SilkMarshal.ThrowHResult(nativeDevice.CreateSamplerState(ref stateDesc, ref state));
        return new SamplerState(new SilkD3D11SamplerStatePtr(state), description);
    }

    internal VertexShaderHandle CreateVertexShader(byte[] byteCode) {
        if (byteCode == null || byteCode.Length == 0) return null;

        fixed (byte* byteCodePtr = byteCode) {
            ID3D11VertexShader* shader = null;
            SilkMarshal.ThrowHResult(nativeDevice.CreateVertexShader(byteCodePtr,
                                                                     (nuint)byteCode.Length,
                                                                     (ID3D11ClassLinkage*)null,
                                                                     ref shader));
            return new VertexShaderHandle(new SilkD3D11VertexShaderPtr(shader));
        }
    }

    internal PixelShaderHandle CreatePixelShader(byte[] byteCode) {
        if (byteCode == null || byteCode.Length == 0) return null;

        fixed (byte* byteCodePtr = byteCode) {
            ID3D11PixelShader* shader = null;
            SilkMarshal.ThrowHResult(nativeDevice.CreatePixelShader(byteCodePtr,
                                                                    (nuint)byteCode.Length,
                                                                    (ID3D11ClassLinkage*)null,
                                                                    ref shader));
            return new PixelShaderHandle(new SilkD3D11PixelShaderPtr(shader));
        }
    }

    internal ComputeShaderHandle CreateComputeShader(byte[] byteCode) {
        if (byteCode == null || byteCode.Length == 0) return null;

        fixed (byte* byteCodePtr = byteCode) {
            ID3D11ComputeShader* shader = null;
            SilkMarshal.ThrowHResult(nativeDevice.CreateComputeShader(byteCodePtr,
                                                                      (nuint)byteCode.Length,
                                                                      (ID3D11ClassLinkage*)null,
                                                                      ref shader));
            return new ComputeShaderHandle(new SilkD3D11ComputeShaderPtr(shader));
        }
    }

    internal DomainShaderHandle CreateDomainShader(byte[] byteCode) {
        if (byteCode == null || byteCode.Length == 0) return null;

        fixed (byte* byteCodePtr = byteCode) {
            ID3D11DomainShader* shader = null;
            SilkMarshal.ThrowHResult(nativeDevice.CreateDomainShader(byteCodePtr,
                                                                     (nuint)byteCode.Length,
                                                                     (ID3D11ClassLinkage*)null,
                                                                     ref shader));
            return new DomainShaderHandle(new SilkD3D11DomainShaderPtr(shader));
        }
    }

    internal HullShaderHandle CreateHullShader(byte[] byteCode) {
        if (byteCode == null || byteCode.Length == 0) return null;

        fixed (byte* byteCodePtr = byteCode) {
            ID3D11HullShader* shader = null;
            SilkMarshal.ThrowHResult(nativeDevice.CreateHullShader(byteCodePtr,
                                                                   (nuint)byteCode.Length,
                                                                   (ID3D11ClassLinkage*)null,
                                                                   ref shader));
            return new HullShaderHandle(new SilkD3D11HullShaderPtr(shader));
        }
    }

    internal GeometryShaderHandle CreateGeometryShader(byte[] byteCode) {
        if (byteCode == null || byteCode.Length == 0) return null;

        fixed (byte* byteCodePtr = byteCode) {
            ID3D11GeometryShader* shader = null;
            SilkMarshal.ThrowHResult(nativeDevice.CreateGeometryShader(byteCodePtr,
                                                                       (nuint)byteCode.Length,
                                                                       (ID3D11ClassLinkage*)null,
                                                                       ref shader));
            return new GeometryShaderHandle(new SilkD3D11GeometryShaderPtr(shader));
        }
    }

    internal GeometryShaderHandle CreateGeometryShader(
        byte[] byteCode,
        StreamOutputElement[] streamOutputElements,
        int[] bufferStrides,
        int rasterizedStream
    ) {
        if (byteCode == null || byteCode.Length == 0) return null;

        if (streamOutputElements == null || streamOutputElements.Length == 0)
            return CreateGeometryShader(byteCode);

        var semanticNamePtrs = new nint[streamOutputElements.Length];
        try {
            var streamOutputDescs = stackalloc SODeclarationEntry[streamOutputElements.Length];
            for (var i = 0; i < streamOutputElements.Length; i++) {
                semanticNamePtrs[i] =
                    SilkMarshal.StringToPtr(streamOutputElements[i].SemanticName ?? string.Empty);
                streamOutputDescs[i] = streamOutputElements[i].ToSilkDesc(semanticNamePtrs[i]);
            }

            var stridesLength = bufferStrides == null ? 0 : bufferStrides.Length;
            var strides = stackalloc uint[stridesLength];
            for (var i = 0; i < stridesLength; i++) strides[i] = (uint)bufferStrides[i];

            fixed (byte* byteCodePtr = byteCode) {
                ID3D11GeometryShader* shader = null;
                SilkMarshal.ThrowHResult(nativeDevice.CreateGeometryShaderWithStreamOutput(byteCodePtr,
                                             (nuint)byteCode.Length,
                                             streamOutputDescs,
                                             (uint)streamOutputElements.Length,
                                             stridesLength == 0 ? null : strides,
                                             (uint)stridesLength,
                                             unchecked((uint)rasterizedStream),
                                             (ID3D11ClassLinkage*)null,
                                             ref shader));
                return new GeometryShaderHandle(new SilkD3D11GeometryShaderPtr(shader));
            }
        } finally {
            for (var i = 0; i < semanticNamePtrs.Length; i++)
                if (semanticNamePtrs[i] != nint.Zero)
                    SilkMarshal.Free(semanticNamePtrs[i]);
        }
    }

    internal InputLayout CreateInputLayout(byte[] shaderByteCode, InputElement[] elements) {
        if (shaderByteCode == null || shaderByteCode.Length == 0 || elements == null ||
            elements.Length == 0) return null;

        var semanticNamePtrs = new nint[elements.Length];
        try {
            var inputElements = stackalloc InputElementDesc[elements.Length];
            for (var i = 0; i < elements.Length; i++) {
                semanticNamePtrs[i] = SilkMarshal.StringToPtr(elements[i].SemanticName ?? string.Empty);
                inputElements[i] = elements[i].ToSilkDesc(semanticNamePtrs[i]);
            }

            fixed (byte* byteCodePtr = shaderByteCode) {
                ID3D11InputLayout* layout = null;
                SilkMarshal.ThrowHResult(nativeDevice.CreateInputLayout(inputElements,
                                                                        (uint)elements.Length,
                                                                        byteCodePtr,
                                                                        (nuint)shaderByteCode.Length,
                                                                        ref layout));
                return new InputLayout(new SilkD3D11InputLayoutPtr(layout));
            }
        } finally {
            for (var i = 0; i < semanticNamePtrs.Length; i++)
                if (semanticNamePtrs[i] != nint.Zero)
                    SilkMarshal.Free(semanticNamePtrs[i]);
        }
    }

    public RenderTargetView CreateRenderTargetView(
        Resource resource,
        RenderTargetViewDescription? description = null
    ) {
        if (resource == null) return null;

        ID3D11RenderTargetView* view = null;
        if (description.HasValue) {
            var viewDesc = description.Value.ToSilkDesc();
            SilkMarshal.ThrowHResult(nativeDevice.CreateRenderTargetView(resource.Handle, ref viewDesc, ref view));
            var nativeView = new SilkD3D11RenderTargetViewPtr(view);
            view->Release();
            return new RenderTargetView(nativeView, resource);
        }

        SilkMarshal.ThrowHResult(nativeDevice.CreateRenderTargetView(resource.Handle,
                                                                     (RenderTargetViewDesc*)null,
                                                                     ref view));
        var defaultNativeView = new SilkD3D11RenderTargetViewPtr(view);
        view->Release();
        return new RenderTargetView(defaultNativeView, resource);
    }

    public DepthStencilView CreateDepthStencilView(
        Resource resource,
        DepthStencilViewDescription? description = null
    ) {
        if (resource == null) return null;

        ID3D11DepthStencilView* view = null;
        if (description.HasValue) {
            var viewDesc = description.Value.ToSilkDesc();
            SilkMarshal.ThrowHResult(nativeDevice.CreateDepthStencilView(resource.Handle, ref viewDesc, ref view));
            return new DepthStencilView(new SilkD3D11DepthStencilViewPtr(view));
        }

        SilkMarshal.ThrowHResult(nativeDevice.CreateDepthStencilView(resource.Handle,
                                                                     (DepthStencilViewDesc*)null,
                                                                     ref view));
        return new DepthStencilView(new SilkD3D11DepthStencilViewPtr(view));
    }

    public ShaderResourceView CreateShaderResourceView(
        Resource resource,
        ShaderResourceViewDescription? description = null
    ) {
        if (resource == null) return null;

        ID3D11ShaderResourceView* view = null;
        if (description.HasValue) {
            var viewDesc = description.Value.ToSilkDesc();
            SilkMarshal.ThrowHResult(nativeDevice.CreateShaderResourceView(resource.Handle, ref viewDesc, ref view));
            return new ShaderResourceView(new SilkD3D11ShaderResourceViewPtr(view), description.Value);
        }

        SilkMarshal.ThrowHResult(nativeDevice.CreateShaderResourceView(resource.Handle,
                                                                       (ShaderResourceViewDesc*)null,
                                                                       ref view));
        return new ShaderResourceView(new SilkD3D11ShaderResourceViewPtr(view));
    }

    public UnorderedAccessView CreateUnorderedAccessView(
        Resource resource,
        UnorderedAccessViewDescription? description = null
    ) {
        if (resource == null) return null;

        ID3D11UnorderedAccessView* view = null;
        if (description.HasValue) {
            var viewDesc = description.Value.ToSilkDesc();
            SilkMarshal.ThrowHResult(nativeDevice.CreateUnorderedAccessView(resource.Handle, ref viewDesc, ref view));
            return new UnorderedAccessView(new SilkD3D11UnorderedAccessViewPtr(view), description.Value);
        }

        SilkMarshal.ThrowHResult(nativeDevice.CreateUnorderedAccessView(resource.Handle,
                                                                        (UnorderedAccessViewDesc*)null,
                                                                        ref view));
        return new UnorderedAccessView(new SilkD3D11UnorderedAccessViewPtr(view));
    }

    private void CreateTexture(
        ref Texture1DDesc textureDesc,
        DataBox[] initialData,
        ref ID3D11Texture1D* texture
    ) {
        if (initialData == null || initialData.Length == 0) {
            SilkMarshal.ThrowHResult(nativeDevice.CreateTexture1D(ref textureDesc, null, ref texture));
            return;
        }

        var subresources = ToSubresourceData(initialData);
        fixed (SubresourceData* subresourcePtr = subresources) {
            SilkMarshal.ThrowHResult(nativeDevice.CreateTexture1D(ref textureDesc,
                                                                  subresourcePtr,
                                                                  ref texture));
        }
    }

    private void CreateTexture(
        ref Texture2DDesc textureDesc,
        DataBox[] initialData,
        ref ID3D11Texture2D* texture
    ) {
        if (initialData == null || initialData.Length == 0) {
            SilkMarshal.ThrowHResult(nativeDevice.CreateTexture2D(ref textureDesc, null, ref texture));
            return;
        }

        var subresources = ToSubresourceData(initialData);
        fixed (SubresourceData* subresourcePtr = subresources) {
            SilkMarshal.ThrowHResult(nativeDevice.CreateTexture2D(ref textureDesc,
                                                                  subresourcePtr,
                                                                  ref texture));
        }
    }

    private void CreateTexture(
        ref Texture3DDesc textureDesc,
        DataBox[] initialData,
        ref ID3D11Texture3D* texture
    ) {
        if (initialData == null || initialData.Length == 0) {
            SilkMarshal.ThrowHResult(nativeDevice.CreateTexture3D(ref textureDesc, null, ref texture));
            return;
        }

        var subresources = ToSubresourceData(initialData);
        fixed (SubresourceData* subresourcePtr = subresources) {
            SilkMarshal.ThrowHResult(nativeDevice.CreateTexture3D(ref textureDesc,
                                                                  subresourcePtr,
                                                                  ref texture));
        }
    }

    private static SubresourceData[] ToSubresourceData(DataBox[] initialData) {
        var subresources = new SubresourceData[initialData.Length];
        for (var i = 0; i < initialData.Length; i++)
            subresources[i] = new SubresourceData {
                PSysMem = initialData[i].DataPointer.ToPointer(),
                SysMemPitch = (uint)initialData[i].RowPitch,
                SysMemSlicePitch = (uint)initialData[i].SlicePitch
            };

        return subresources;
    }
}

public sealed unsafe class CommandList : IDisposable {
    private SilkD3D11CommandListPtr commandList;

    internal CommandList(SilkD3D11CommandListPtr commandList) {
        if (commandList.Handle == null) throw new ArgumentNullException(nameof(commandList));

        this.commandList = commandList;
    }

    public nint NativePointer => (nint)commandList.Handle;

    internal ID3D11CommandList* Handle => commandList.Handle;

    public bool IsDisposed { get; private set; }

    public void Dispose() {
        if (IsDisposed) return;

        commandList.Dispose();
        IsDisposed = true;
    }
}

public sealed unsafe class SilkD3DDeviceContext : IDisposable {
    private SilkD3D11ContextPtr nativeContext;

    // ponytail: cache until Silk exposes the base ID3D11DeviceContext getter.
    private D3DPrimitiveTopology primitiveTopology;

    public SilkD3DDeviceContext(SilkD3D11ContextPtr nativeContext, bool isDeferred) {
        if (nativeContext.Handle == null) throw new ArgumentNullException(nameof(nativeContext));

        this.nativeContext = nativeContext;
        IsDeferred = isDeferred;
    }

    public nint NativePointer => (nint)nativeContext.Handle;

    internal ID3D11DeviceContext* Handle => nativeContext.Handle;

    internal ref SilkD3D11ContextPtr NativeContext => ref nativeContext;

    public bool IsDeferred { get; }

    public bool IsDisposed { get; private set; }

    public D3DPrimitiveTopology PrimitiveTopology {
        get => primitiveTopology;
        set {
            nativeContext.IASetPrimitiveTopology(value);
            primitiveTopology = value;
        }
    }

    public void Dispose() {
        if (IsDisposed) return;

        nativeContext.Dispose();
        IsDisposed = true;
    }

    public void ClearState() {
        nativeContext.ClearState();
    }

    public void Flush() {
        nativeContext.Flush();
    }

    public CommandList FinishCommandList(bool restoreState) {
        if (!IsDeferred)
            throw new InvalidOperationException("Command lists can only be finished on deferred device contexts.");

        ID3D11CommandList* commandList = null;
        SilkMarshal.ThrowHResult(nativeContext.FinishCommandList(new Bool32(restoreState), ref commandList));
        return new CommandList(new SilkD3D11CommandListPtr(commandList));
    }

    public void ExecuteCommandList(CommandList commandList, bool restoreContextState) {
        if (IsDeferred)
            throw new InvalidOperationException("Command lists can only be executed on the immediate device context.");
        commandList.AssertArgumentNotNull();
        if (commandList.IsDisposed) throw new ObjectDisposedException(nameof(CommandList));

        nativeContext.ExecuteCommandList(commandList.Handle, new Bool32(restoreContextState));
    }

    public void Draw(uint vertexCount, uint startVertexLocation) {
        nativeContext.Draw(vertexCount, startVertexLocation);
    }

    public void DrawAuto() {
        nativeContext.DrawAuto();
    }

    public void DrawIndexed(uint indexCount, uint startIndexLocation, int baseVertexLocation) {
        nativeContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
    }

    public void DrawIndexedInstanced(
        uint indexCountPerInstance,
        uint instanceCount,
        uint startIndexLocation,
        int baseVertexLocation,
        uint startInstanceLocation
    ) {
        nativeContext.DrawIndexedInstanced(indexCountPerInstance,
                                           instanceCount,
                                           startIndexLocation,
                                           baseVertexLocation,
                                           startInstanceLocation);
    }

    public void DrawInstanced(
        uint vertexCountPerInstance,
        uint instanceCount,
        uint startVertexLocation,
        uint startInstanceLocation
    ) {
        nativeContext.DrawInstanced(vertexCountPerInstance,
                                    instanceCount,
                                    startVertexLocation,
                                    startInstanceLocation);
    }

    public void DrawInstancedIndirect(Buffer buffer, uint alignedByteOffsetForArgs) {
        nativeContext.DrawInstancedIndirect(buffer.BufferHandle, alignedByteOffsetForArgs);
    }

    public void Dispatch(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ) {
        nativeContext.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
    }

    internal void SetInputLayout(InputLayout inputLayout) {
        nativeContext.IASetInputLayout(inputLayout?.Handle);
    }

    public void SetIndexBuffer(Buffer buffer, Format format, int offset) {
        nativeContext.IASetIndexBuffer(buffer?.BufferHandle, format, (uint)offset);
    }

    public void SetVertexBuffer(int slot, VertexBufferBinding binding) {
        var bufferPtr = binding.Buffer?.BufferHandle;
        var stride = (uint)binding.Stride;
        var offset = (uint)binding.Offset;
        nativeContext.IASetVertexBuffers((uint)slot, 1, &bufferPtr, &stride, &offset);
    }

    public void SetVertexBuffers(int startSlot, VertexBufferBinding[] bindings) {
        if (bindings == null || bindings.Length == 0) return;

        var bufferPtrs = stackalloc ID3D11Buffer*[bindings.Length];
        var strides = stackalloc uint[bindings.Length];
        var offsets = stackalloc uint[bindings.Length];
        for (var i = 0; i < bindings.Length; i++) {
            bufferPtrs[i] = bindings[i].Buffer?.BufferHandle;
            strides[i] = (uint)bindings[i].Stride;
            offsets[i] = (uint)bindings[i].Offset;
        }

        nativeContext.IASetVertexBuffers((uint)startSlot,
                                         (uint)bindings.Length,
                                         bufferPtrs,
                                         strides,
                                         offsets);
    }

    internal void SetShader(IShaderHandle shader) {
        if (shader != null) SetShader(shader.StageIndex, shader);
    }

    internal void SetShader(int shaderStage, IShaderHandle shader) {
        var shaderHandle = shader == null ? null : shader.NativeHandle;
        switch (shaderStage) {
            case Constants.VertexIdx:
                nativeContext.VSSetShader((ID3D11VertexShader*)shaderHandle, null, 0);
                break;
            case Constants.HullIdx:
                nativeContext.HSSetShader((ID3D11HullShader*)shaderHandle, null, 0);
                break;
            case Constants.DomainIdx:
                nativeContext.DSSetShader((ID3D11DomainShader*)shaderHandle, null, 0);
                break;
            case Constants.GeometryIdx:
                nativeContext.GSSetShader((ID3D11GeometryShader*)shaderHandle, null, 0);
                break;
            case Constants.PixelIdx:
                nativeContext.PSSetShader((ID3D11PixelShader*)shaderHandle, null, 0);
                break;
            case Constants.ComputeIdx:
                nativeContext.CSSetShader((ID3D11ComputeShader*)shaderHandle, null, 0);
                break;
        }
    }

    public void SetConstantBuffer(int shaderStage, int slot, Buffer buffer) {
        if (slot < 0) return;

        var bufferPtr = buffer?.BufferHandle;
        SetConstantBuffers(shaderStage, slot, 1, &bufferPtr);
    }

    public void SetConstantBuffers(int shaderStage, int slot, Buffer[] buffers) {
        if (slot < 0 || buffers == null || buffers.Length == 0) return;

        var bufferPtrs = stackalloc ID3D11Buffer*[buffers.Length];
        for (var i = 0; i < buffers.Length; i++) bufferPtrs[i] = buffers[i]?.BufferHandle;

        SetConstantBuffers(shaderStage, slot, (uint)buffers.Length, bufferPtrs);
    }

    public void SetViewport(float x, float y, float width, float height, float minZ, float maxZ) {
        var viewport = new Viewport(x, y, width, height, minZ, maxZ);
        nativeContext.RSSetViewports(1, ref viewport);
    }

    public void SetScissorRectangle(int left, int top, int right, int bottom) {
        var rectangle = new Box2D<int>(left, top, right, bottom);
        nativeContext.RSSetScissorRects(1, ref rectangle);
    }

    public DataBox MapSubresource(Resource resource, int subresource, MapMode mode, MapFlags flags) {
        if (resource == null) return default;

        MappedSubresource mapped = default;
        SilkMarshal.ThrowHResult(nativeContext.Map(resource.Handle,
                                                   (uint)subresource,
                                                   mode.ToSilkMap(),
                                                   flags.ToSilkMapFlags(),
                                                   ref mapped));
        return mapped.ToDataBox();
    }

    public DataBox MapSubresource(
        Resource resource,
        int subresource,
        MapMode mode,
        MapFlags flags,
        out DataStream stream
    ) {
        var dataBox = MapSubresource(resource, subresource, mode, flags);
        stream = new DataStream(dataBox.DataPointer,
                                0,
                                mode == MapMode.Read || mode == MapMode.ReadWrite,
                                mode != MapMode.Read);
        return dataBox;
    }

    public void UnmapSubresource(Resource resource, int subresource) {
        if (resource == null) return;

        nativeContext.Unmap(resource.Handle, (uint)subresource);
    }

    public void UpdateSubresource(
        Resource resource,
        int subresource,
        ResourceRegion? region,
        nint sourceData,
        int rowPitch,
        int depthPitch
    ) {
        if (resource == null || sourceData == nint.Zero) return;

        if (region.HasValue) {
            var box = region.Value.ToSilkBox();
            nativeContext.UpdateSubresource(resource.Handle,
                                            (uint)subresource,
                                            ref box,
                                            sourceData.ToPointer(),
                                            (uint)rowPitch,
                                            (uint)depthPitch);
        } else {
            nativeContext.UpdateSubresource(resource.Handle,
                                            (uint)subresource,
                                            (Box*)null,
                                            sourceData.ToPointer(),
                                            (uint)rowPitch,
                                            (uint)depthPitch);
        }
    }

    public void CopyResource(Resource source, Resource destination) {
        if (source == null || destination == null) return;

        nativeContext.CopyResource(destination.Handle, source.Handle);
    }

    public void CopySubresourceRegion(
        Resource source,
        int sourceSubresource,
        ResourceRegion? sourceRegion,
        Resource destination,
        int destinationSubResource,
        int dstX,
        int dstY,
        int dstZ
    ) {
        if (source == null || destination == null) return;

        if (sourceRegion.HasValue) {
            var box = sourceRegion.Value.ToSilkBox();
            nativeContext.CopySubresourceRegion(destination.Handle,
                                                (uint)destinationSubResource,
                                                (uint)dstX,
                                                (uint)dstY,
                                                (uint)dstZ,
                                                source.Handle,
                                                (uint)sourceSubresource,
                                                ref box);
        } else {
            nativeContext.CopySubresourceRegion(destination.Handle,
                                                (uint)destinationSubResource,
                                                (uint)dstX,
                                                (uint)dstY,
                                                (uint)dstZ,
                                                source.Handle,
                                                (uint)sourceSubresource,
                                                (Box*)null);
        }
    }

    public void ResolveSubresource(
        Resource source,
        int sourceSubresource,
        Resource destination,
        int destinationSubresource,
        Format format
    ) {
        if (source == null || destination == null) return;

        nativeContext.ResolveSubresource(destination.Handle,
                                         (uint)destinationSubresource,
                                         source.Handle,
                                         (uint)sourceSubresource,
                                         format);
    }

    public void CopyStructureCount(
        Buffer destination,
        int destinationAlignedByteOffset,
        UnorderedAccessView source
    ) {
        if (destination == null || source == null) return;

        nativeContext.CopyStructureCount(destination.BufferHandle,
                                         (uint)destinationAlignedByteOffset,
                                         source.Handle);
    }

    public void GenerateMips(ShaderResourceView shaderResourceView) {
        if (shaderResourceView == null) return;

        nativeContext.GenerateMips(shaderResourceView.Handle);
    }

    public void SetShaderResource(int shaderStage, int slot, ShaderResourceView shaderResourceView) {
        if (slot < 0) return;

        var viewPtr = shaderResourceView?.Handle;
        SetShaderResources(shaderStage, slot, 1, &viewPtr);
    }

    public void SetShaderResources(int shaderStage, int slot, ShaderResourceView[] shaderResourceViews) {
        if (slot < 0 || shaderResourceViews == null || shaderResourceViews.Length == 0) return;

        var viewPtrs = stackalloc ID3D11ShaderResourceView*[shaderResourceViews.Length];
        for (var i = 0; i < shaderResourceViews.Length; i++) viewPtrs[i] = shaderResourceViews[i]?.Handle;

        SetShaderResources(shaderStage, slot, (uint)shaderResourceViews.Length, viewPtrs);
    }

    public void SetSampler(int shaderStage, int slot, SamplerState samplerState) {
        if (slot < 0) return;

        var statePtr = samplerState?.Handle;
        SetSamplers(shaderStage, slot, 1, &statePtr);
    }

    public void SetSamplers(int shaderStage, int slot, SamplerState[] samplerStates) {
        if (slot < 0 || samplerStates == null || samplerStates.Length == 0) return;

        var statePtrs = stackalloc ID3D11SamplerState*[samplerStates.Length];
        for (var i = 0; i < samplerStates.Length; i++) statePtrs[i] = samplerStates[i]?.Handle;

        SetSamplers(shaderStage, slot, (uint)samplerStates.Length, statePtrs);
    }

    public void SetUnorderedAccessView(
        int slot,
        UnorderedAccessView unorderedAccessView,
        int initialCount = -1
    ) {
        if (slot < 0) return;

        var viewPtr = unorderedAccessView?.Handle;
        var count = unchecked((uint)initialCount);
        nativeContext.CSSetUnorderedAccessViews((uint)slot, 1, &viewPtr, &count);
    }

    public void SetUnorderedAccessViews(
        int slot,
        UnorderedAccessView[] unorderedAccessViews,
        int[]? initialCounts = null
    ) {
        if (slot < 0 || unorderedAccessViews == null || unorderedAccessViews.Length == 0) return;

        var viewPtrs = stackalloc ID3D11UnorderedAccessView*[unorderedAccessViews.Length];
        var counts = stackalloc uint[unorderedAccessViews.Length];
        for (var i = 0; i < unorderedAccessViews.Length; i++) {
            viewPtrs[i] = unorderedAccessViews[i]?.Handle;
            counts[i] = initialCounts == null || i >= initialCounts.Length
                            ? unchecked((uint)-1)
                            : unchecked((uint)initialCounts[i]);
        }

        nativeContext.CSSetUnorderedAccessViews((uint)slot,
                                                (uint)unorderedAccessViews.Length,
                                                viewPtrs,
                                                counts);
    }

    private void SetShaderResources(
        int shaderStage,
        int slot,
        uint count,
        ID3D11ShaderResourceView** shaderResourceViews
    ) {
        switch (shaderStage) {
            case Constants.VertexIdx:
                nativeContext.VSSetShaderResources((uint)slot, count, shaderResourceViews);
                break;
            case Constants.HullIdx:
                nativeContext.HSSetShaderResources((uint)slot, count, shaderResourceViews);
                break;
            case Constants.DomainIdx:
                nativeContext.DSSetShaderResources((uint)slot, count, shaderResourceViews);
                break;
            case Constants.GeometryIdx:
                nativeContext.GSSetShaderResources((uint)slot, count, shaderResourceViews);
                break;
            case Constants.PixelIdx:
                nativeContext.PSSetShaderResources((uint)slot, count, shaderResourceViews);
                break;
            case Constants.ComputeIdx:
                nativeContext.CSSetShaderResources((uint)slot, count, shaderResourceViews);
                break;
        }
    }

    private void SetConstantBuffers(int shaderStage, int slot, uint count, ID3D11Buffer** constantBuffers) {
        switch (shaderStage) {
            case Constants.VertexIdx:
                nativeContext.VSSetConstantBuffers((uint)slot, count, constantBuffers);
                break;
            case Constants.HullIdx:
                nativeContext.HSSetConstantBuffers((uint)slot, count, constantBuffers);
                break;
            case Constants.DomainIdx:
                nativeContext.DSSetConstantBuffers((uint)slot, count, constantBuffers);
                break;
            case Constants.GeometryIdx:
                nativeContext.GSSetConstantBuffers((uint)slot, count, constantBuffers);
                break;
            case Constants.PixelIdx:
                nativeContext.PSSetConstantBuffers((uint)slot, count, constantBuffers);
                break;
            case Constants.ComputeIdx:
                nativeContext.CSSetConstantBuffers((uint)slot, count, constantBuffers);
                break;
        }
    }

    private void SetSamplers(int shaderStage, int slot, uint count, ID3D11SamplerState** samplerStates) {
        switch (shaderStage) {
            case Constants.VertexIdx:
                nativeContext.VSSetSamplers((uint)slot, count, samplerStates);
                break;
            case Constants.HullIdx:
                nativeContext.HSSetSamplers((uint)slot, count, samplerStates);
                break;
            case Constants.DomainIdx:
                nativeContext.DSSetSamplers((uint)slot, count, samplerStates);
                break;
            case Constants.GeometryIdx:
                nativeContext.GSSetSamplers((uint)slot, count, samplerStates);
                break;
            case Constants.PixelIdx:
                nativeContext.PSSetSamplers((uint)slot, count, samplerStates);
                break;
            case Constants.ComputeIdx:
                nativeContext.CSSetSamplers((uint)slot, count, samplerStates);
                break;
        }
    }

    public void SetRasterState(RasterizerState rasterizerState) {
        nativeContext.RSSetState(rasterizerState?.Handle);
    }

    public void SetDepthStencilState(DepthStencilState depthStencilState, int stencilRef) {
        nativeContext.OMSetDepthStencilState(depthStencilState?.Handle, unchecked((uint)stencilRef));
    }

    public void SetBlendState(BlendState blendState, Color4? blendFactor, uint sampleMask) {
        if (blendFactor.HasValue) {
            var factor = blendFactor.Value;
            var factors = stackalloc float[4] {
                factor.X,
                factor.Y,
                factor.Z,
                factor.W
            };
            nativeContext.OMSetBlendState(blendState?.Handle, factors, sampleMask);
        } else {
            nativeContext.OMSetBlendState(blendState?.Handle, (float*)null, sampleMask);
        }
    }

    public void SetStreamOutputTarget(Buffer buffer, int offset) {
        var bufferPtr = buffer?.BufferHandle;
        var offsetValue = (uint)offset;
        nativeContext.SOSetTargets(1, &bufferPtr, &offsetValue);
    }

    public void SetStreamOutputTargets(Buffer[] buffers) {
        if (buffers == null || buffers.Length == 0) {
            nativeContext.SOSetTargets(0, null, (uint*)null);
            return;
        }

        var bufferPtrs = stackalloc ID3D11Buffer*[buffers.Length];
        var offsets = stackalloc uint[buffers.Length];
        for (var i = 0; i < buffers.Length; i++) {
            bufferPtrs[i] = buffers[i]?.BufferHandle;
            offsets[i] = 0;
        }

        nativeContext.SOSetTargets((uint)buffers.Length, bufferPtrs, offsets);
    }

    public void SetRenderTargets(DepthStencilView depthStencilView, RenderTargetView renderTargetView) {
        var renderTargetViewPtr = renderTargetView?.Handle;
        nativeContext.OMSetRenderTargets(1, &renderTargetViewPtr, depthStencilView?.Handle);
    }

    public void SetRenderTargets(DepthStencilView depthStencilView, RenderTargetView[] renderTargetViews) {
        if (renderTargetViews == null || renderTargetViews.Length == 0) {
            nativeContext.OMSetRenderTargets(0, null, depthStencilView?.Handle);
            return;
        }

        var renderTargetViewPtrs = stackalloc ID3D11RenderTargetView*[renderTargetViews.Length];
        for (var i = 0; i < renderTargetViews.Length; i++)
            renderTargetViewPtrs[i] = renderTargetViews[i]?.Handle;

        nativeContext.OMSetRenderTargets((uint)renderTargetViews.Length,
                                         renderTargetViewPtrs,
                                         depthStencilView?.Handle);
    }

    public void ClearRenderTargetView(RenderTargetView renderTargetView, Color4 color) {
        if (renderTargetView == null) return;

        var clearColor = stackalloc float[4] {
            color.X,
            color.Y,
            color.Z,
            color.W
        };
        nativeContext.ClearRenderTargetView(renderTargetView.Handle, clearColor);
    }

    public void ClearDepthStencilView(
        DepthStencilView depthStencilView,
        DepthStencilClearFlags clearFlags,
        float depth,
        byte stencil
    ) {
        if (depthStencilView == null) return;

        nativeContext.ClearDepthStencilView(depthStencilView.Handle, (uint)clearFlags, depth, stencil);
    }

    public void ClearRenderTargetBindings() {
        nativeContext.OMSetRenderTargets(0, null, (ID3D11DepthStencilView*)null);
    }

    public void GetDepthStencilView(out DepthStencilView depthStencilView) {
        ID3D11DepthStencilView* depthStencilViewPtr = null;
        nativeContext.OMGetRenderTargets(0, null, &depthStencilViewPtr);
        depthStencilView = depthStencilViewPtr == null
                               ? null
                               : new DepthStencilView(new SilkD3D11DepthStencilViewPtr(depthStencilViewPtr));
    }

    public RenderTargetView[] GetRenderTargets(int numViews) {
        return GetRenderTargets(numViews, out _);
    }

    public RenderTargetView[] GetRenderTargets(int numViews, out DepthStencilView depthStencilView) {
        if (numViews <= 0) {
            GetDepthStencilView(out depthStencilView);
            return [];
        }

        var renderTargetViewPtrs = stackalloc ID3D11RenderTargetView*[numViews];
        ID3D11DepthStencilView* depthStencilViewPtr = null;
        nativeContext.OMGetRenderTargets((uint)numViews, renderTargetViewPtrs, &depthStencilViewPtr);

        var renderTargetViews = new RenderTargetView[numViews];
        for (var i = 0; i < numViews; i++)
            renderTargetViews[i] = renderTargetViewPtrs[i] == null
                                       ? null
                                       : new RenderTargetView(
                                           new SilkD3D11RenderTargetViewPtr(renderTargetViewPtrs[i]));

        depthStencilView = depthStencilViewPtr == null
                               ? null
                               : new DepthStencilView(new SilkD3D11DepthStencilViewPtr(depthStencilViewPtr));
        return renderTargetViews;
    }

    public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessView, Int4 values) {
        if (unorderedAccessView == null) return;

        var clearValues = stackalloc uint[4] {
            unchecked((uint) values.X),
            unchecked((uint) values.Y),
            unchecked((uint) values.Z),
            unchecked((uint) values.W)
        };
        nativeContext.ClearUnorderedAccessViewUint(unorderedAccessView.Handle, clearValues);
    }

    public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessView, Vector4 values) {
        if (unorderedAccessView == null) return;

        var clearValues = stackalloc float[4] {
            values.X,
            values.Y,
            values.Z,
            values.W
        };
        nativeContext.ClearUnorderedAccessViewFloat(unorderedAccessView.Handle, clearValues);
    }

    public void SetOutputUnorderedAccessView(int slot, UnorderedAccessView unorderedAccessView) {
        var unorderedAccessViewPtr = unorderedAccessView?.Handle;
        var initialCount = unchecked((uint)-1);
        nativeContext.OMSetRenderTargetsAndUnorderedAccessViews(uint.MaxValue,
                                                                null,
                                                                (ID3D11DepthStencilView*)null,
                                                                (uint)slot,
                                                                1,
                                                                &unorderedAccessViewPtr,
                                                                &initialCount);
    }

    public void SetOutputUnorderedAccessViews(int startSlot, UnorderedAccessView[] unorderedAccessViews) {
        if (unorderedAccessViews == null || unorderedAccessViews.Length == 0) return;

        var unorderedAccessViewPtrs = stackalloc ID3D11UnorderedAccessView*[unorderedAccessViews.Length];
        var initialCounts = stackalloc uint[unorderedAccessViews.Length];
        for (var i = 0; i < unorderedAccessViews.Length; i++) {
            unorderedAccessViewPtrs[i] = unorderedAccessViews[i]?.Handle;
            initialCounts[i] = unchecked((uint)-1);
        }

        nativeContext.OMSetRenderTargetsAndUnorderedAccessViews(uint.MaxValue,
                                                                null,
                                                                (ID3D11DepthStencilView*)null,
                                                                (uint)startSlot,
                                                                (uint)unorderedAccessViews.Length,
                                                                unorderedAccessViewPtrs,
                                                                initialCounts);
    }

    public UnorderedAccessView[] GetUnorderedAccessViews(int startSlot, int count) {
        if (count <= 0)
            return [];

        var unorderedAccessViewPtrs = stackalloc ID3D11UnorderedAccessView*[count];
        nativeContext.OMGetRenderTargetsAndUnorderedAccessViews(0,
                                                                null,
                                                                null,
                                                                (uint)startSlot,
                                                                (uint)count,
                                                                unorderedAccessViewPtrs);

        var unorderedAccessViews = new UnorderedAccessView[count];
        for (var i = 0; i < count; i++)
            unorderedAccessViews[i] = unorderedAccessViewPtrs[i] == null
                                          ? null
                                          : new UnorderedAccessView(
                                              new SilkD3D11UnorderedAccessViewPtr(unorderedAccessViewPtrs[i]));

        return unorderedAccessViews;
    }
}
