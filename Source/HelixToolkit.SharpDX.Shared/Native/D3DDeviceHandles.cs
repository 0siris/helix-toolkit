/*
The MIT License (MIT)
Copyright (c) 2026 Helix Toolkit contributors
*/

using System;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.Maths;
using SilkD3D11BufferPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Buffer>;
using SilkD3D11ContextPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DeviceContext>;
using SilkD3D11DevicePtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Device>;
using SilkD3D11DepthStencilViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11DepthStencilView>;
using SilkD3D11RenderTargetViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11RenderTargetView>;
using SilkD3D11ShaderResourceViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11ShaderResourceView>;
using SilkD3D11Texture1DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture1D>;
using SilkD3D11Texture2DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture2D>;
using SilkD3D11Texture3DPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11Texture3D>;
using SilkD3D11UnorderedAccessViewPtr = Silk.NET.Core.Native.ComPtr<Silk.NET.Direct3D11.ID3D11UnorderedAccessView>;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Native
    {
        internal enum SilkDriverType
        {
            Unknown = 0,
            Hardware,
            Warp,
            Reference,
            Software
        }

        internal enum SilkFeatureLevel
        {
            Unknown = 0,
            Level_9_1,
            Level_9_2,
            Level_9_3,
            Level_10_0,
            Level_10_1,
            Level_11_0,
            Level_11_1
        }

        internal unsafe sealed class SilkD3DDevice : IDisposable
        {
            private SilkD3D11DevicePtr nativeDevice;

            public SilkD3DDevice(SilkD3D11DevicePtr nativeDevice, SilkDriverType driverType, SilkFeatureLevel featureLevel)
            {
                if (nativeDevice.Handle == null)
                {
                    throw new ArgumentNullException(nameof(nativeDevice));
                }

                this.nativeDevice = nativeDevice;
                DriverType = driverType;
                FeatureLevel = featureLevel;
            }

            public IntPtr NativePointer => (IntPtr)nativeDevice.Handle;

            public ID3D11Device* Handle => nativeDevice.Handle;

            public ref SilkD3D11DevicePtr NativeDevice => ref nativeDevice;

            public SilkDriverType DriverType { get; }

            public SilkFeatureLevel FeatureLevel { get; }

            public bool IsDisposed { get; private set; }

            public SilkD3D11BufferPtr CreateBuffer(BufferDescription description)
            {
                var bufferDesc = description.ToSilkDesc();
                ID3D11Buffer* buffer = null;
                SilkMarshal.ThrowHResult(nativeDevice.CreateBuffer(ref bufferDesc, (SubresourceData*)null, ref buffer));
                return new SilkD3D11BufferPtr(buffer);
            }

            public SilkD3D11BufferPtr CreateBuffer(BufferDescription description, IntPtr initialData)
            {
                if (initialData == IntPtr.Zero)
                {
                    return CreateBuffer(description);
                }

                var bufferDesc = description.ToSilkDesc();
                var subresource = new SubresourceData
                {
                    PSysMem = initialData.ToPointer()
                };

                ID3D11Buffer* buffer = null;
                SilkMarshal.ThrowHResult(nativeDevice.CreateBuffer(ref bufferDesc, ref subresource, ref buffer));
                return new SilkD3D11BufferPtr(buffer);
            }

            public Texture1D CreateTexture1D(Texture1DDescription description, DataBox[] initialData = null)
            {
                var textureDesc = description.ToSilkDesc();
                ID3D11Texture1D* texture = null;
                CreateTexture(ref textureDesc, initialData, ref texture);
                return new Texture1D(new SilkD3D11Texture1DPtr(texture), this, description);
            }

            public Texture2D CreateTexture2D(Texture2DDescription description, DataBox[] initialData = null)
            {
                var textureDesc = description.ToSilkDesc();
                ID3D11Texture2D* texture = null;
                CreateTexture(ref textureDesc, initialData, ref texture);
                return new Texture2D(new SilkD3D11Texture2DPtr(texture), this, description);
            }

            public Texture3D CreateTexture3D(Texture3DDescription description, DataBox[] initialData = null)
            {
                var textureDesc = description.ToSilkDesc();
                ID3D11Texture3D* texture = null;
                CreateTexture(ref textureDesc, initialData, ref texture);
                return new Texture3D(new SilkD3D11Texture3DPtr(texture), this, description);
            }

            public RenderTargetView CreateRenderTargetView(Resource resource, RenderTargetViewDescription? description = null)
            {
                if (resource == null)
                {
                    return null;
                }

                ID3D11RenderTargetView* view = null;
                if (description.HasValue)
                {
                    var viewDesc = description.Value.ToSilkDesc();
                    SilkMarshal.ThrowHResult(nativeDevice.CreateRenderTargetView(resource.Handle, ref viewDesc, ref view));
                    return new RenderTargetView(new SilkD3D11RenderTargetViewPtr(view));
                }

                SilkMarshal.ThrowHResult(nativeDevice.CreateRenderTargetView(resource.Handle, (RenderTargetViewDesc*)null, ref view));
                return new RenderTargetView(new SilkD3D11RenderTargetViewPtr(view));
            }

            public DepthStencilView CreateDepthStencilView(Resource resource, DepthStencilViewDescription? description = null)
            {
                if (resource == null)
                {
                    return null;
                }

                ID3D11DepthStencilView* view = null;
                if (description.HasValue)
                {
                    var viewDesc = description.Value.ToSilkDesc();
                    SilkMarshal.ThrowHResult(nativeDevice.CreateDepthStencilView(resource.Handle, ref viewDesc, ref view));
                    return new DepthStencilView(new SilkD3D11DepthStencilViewPtr(view));
                }

                SilkMarshal.ThrowHResult(nativeDevice.CreateDepthStencilView(resource.Handle, (DepthStencilViewDesc*)null, ref view));
                return new DepthStencilView(new SilkD3D11DepthStencilViewPtr(view));
            }

            public ShaderResourceView CreateShaderResourceView(Resource resource, ShaderResourceViewDescription? description = null)
            {
                if (resource == null)
                {
                    return null;
                }

                ID3D11ShaderResourceView* view = null;
                if (description.HasValue)
                {
                    var viewDesc = description.Value.ToSilkDesc();
                    SilkMarshal.ThrowHResult(nativeDevice.CreateShaderResourceView(resource.Handle, ref viewDesc, ref view));
                    return new ShaderResourceView(new SilkD3D11ShaderResourceViewPtr(view), description.Value);
                }

                SilkMarshal.ThrowHResult(nativeDevice.CreateShaderResourceView(resource.Handle, (ShaderResourceViewDesc*)null, ref view));
                return new ShaderResourceView(new SilkD3D11ShaderResourceViewPtr(view));
            }

            public UnorderedAccessView CreateUnorderedAccessView(Resource resource, UnorderedAccessViewDescription? description = null)
            {
                if (resource == null)
                {
                    return null;
                }

                ID3D11UnorderedAccessView* view = null;
                if (description.HasValue)
                {
                    var viewDesc = description.Value.ToSilkDesc();
                    SilkMarshal.ThrowHResult(nativeDevice.CreateUnorderedAccessView(resource.Handle, ref viewDesc, ref view));
                    return new UnorderedAccessView(new SilkD3D11UnorderedAccessViewPtr(view), description.Value);
                }

                SilkMarshal.ThrowHResult(nativeDevice.CreateUnorderedAccessView(resource.Handle, (UnorderedAccessViewDesc*)null, ref view));
                return new UnorderedAccessView(new SilkD3D11UnorderedAccessViewPtr(view));
            }

            private void CreateTexture(ref Texture1DDesc textureDesc, DataBox[] initialData, ref ID3D11Texture1D* texture)
            {
                if (initialData == null || initialData.Length == 0)
                {
                    SilkMarshal.ThrowHResult(nativeDevice.CreateTexture1D(ref textureDesc, (SubresourceData*)null, ref texture));
                    return;
                }

                var subresources = ToSubresourceData(initialData);
                fixed (SubresourceData* subresourcePtr = subresources)
                {
                    SilkMarshal.ThrowHResult(nativeDevice.CreateTexture1D(ref textureDesc, subresourcePtr, ref texture));
                }
            }

            private void CreateTexture(ref Texture2DDesc textureDesc, DataBox[] initialData, ref ID3D11Texture2D* texture)
            {
                if (initialData == null || initialData.Length == 0)
                {
                    SilkMarshal.ThrowHResult(nativeDevice.CreateTexture2D(ref textureDesc, (SubresourceData*)null, ref texture));
                    return;
                }

                var subresources = ToSubresourceData(initialData);
                fixed (SubresourceData* subresourcePtr = subresources)
                {
                    SilkMarshal.ThrowHResult(nativeDevice.CreateTexture2D(ref textureDesc, subresourcePtr, ref texture));
                }
            }

            private void CreateTexture(ref Texture3DDesc textureDesc, DataBox[] initialData, ref ID3D11Texture3D* texture)
            {
                if (initialData == null || initialData.Length == 0)
                {
                    SilkMarshal.ThrowHResult(nativeDevice.CreateTexture3D(ref textureDesc, (SubresourceData*)null, ref texture));
                    return;
                }

                var subresources = ToSubresourceData(initialData);
                fixed (SubresourceData* subresourcePtr = subresources)
                {
                    SilkMarshal.ThrowHResult(nativeDevice.CreateTexture3D(ref textureDesc, subresourcePtr, ref texture));
                }
            }

            private static SubresourceData[] ToSubresourceData(DataBox[] initialData)
            {
                var subresources = new SubresourceData[initialData.Length];
                for (var i = 0; i < initialData.Length; i++)
                {
                    subresources[i] = new SubresourceData
                    {
                        PSysMem = initialData[i].DataPointer.ToPointer(),
                        SysMemPitch = (uint)initialData[i].RowPitch,
                        SysMemSlicePitch = (uint)initialData[i].SlicePitch
                    };
                }

                return subresources;
            }

            public void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }

                nativeDevice.Dispose();
                IsDisposed = true;
            }
        }

        internal unsafe sealed class SilkD3DDeviceContext : IDisposable
        {
            private SilkD3D11ContextPtr nativeContext;

            public SilkD3DDeviceContext(SilkD3D11ContextPtr nativeContext, bool isDeferred)
            {
                if (nativeContext.Handle == null)
                {
                    throw new ArgumentNullException(nameof(nativeContext));
                }

                this.nativeContext = nativeContext;
                IsDeferred = isDeferred;
            }

            public IntPtr NativePointer => (IntPtr)nativeContext.Handle;

            public ID3D11DeviceContext* Handle => nativeContext.Handle;

            public ref SilkD3D11ContextPtr NativeContext => ref nativeContext;

            public bool IsDeferred { get; }

            public bool IsDisposed { get; private set; }

            public void ClearState()
            {
                nativeContext.ClearState();
            }

            public void Flush()
            {
                nativeContext.Flush();
            }

            public void Draw(uint vertexCount, uint startVertexLocation)
            {
                nativeContext.Draw(vertexCount, startVertexLocation);
            }

            public void DrawAuto()
            {
                nativeContext.DrawAuto();
            }

            public void DrawIndexed(uint indexCount, uint startIndexLocation, int baseVertexLocation)
            {
                nativeContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);
            }

            public void DrawIndexedInstanced(uint indexCountPerInstance, uint instanceCount, uint startIndexLocation, int baseVertexLocation, uint startInstanceLocation)
            {
                nativeContext.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
            }

            public void DrawInstanced(uint vertexCountPerInstance, uint instanceCount, uint startVertexLocation, uint startInstanceLocation)
            {
                nativeContext.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);
            }

            public void Dispatch(uint threadGroupCountX, uint threadGroupCountY, uint threadGroupCountZ)
            {
                nativeContext.Dispatch(threadGroupCountX, threadGroupCountY, threadGroupCountZ);
            }

            public D3DPrimitiveTopology PrimitiveTopology
            {
                get
                {
                    nativeContext.IAGetPrimitiveTopology(out D3DPrimitiveTopology topology);
                    return topology;
                }
                set
                {
                    nativeContext.IASetPrimitiveTopology(value);
                }
            }

            public void SetViewport(float x, float y, float width, float height, float minZ, float maxZ)
            {
                var viewport = new Viewport(x, y, width, height, minZ, maxZ);
                nativeContext.RSSetViewports(1, ref viewport);
            }

            public void SetScissorRectangle(int left, int top, int right, int bottom)
            {
                var rectangle = new Box2D<int>(left, top, right, bottom);
                nativeContext.RSSetScissorRects(1, ref rectangle);
            }

            public DataBox MapSubresource(Resource resource, int subresource, MapMode mode, MapFlags flags)
            {
                if (resource == null)
                {
                    return default;
                }

                MappedSubresource mapped = default;
                SilkMarshal.ThrowHResult(nativeContext.Map(resource.Handle, (uint)subresource, mode.ToSilkMap(), flags.ToSilkMapFlags(), ref mapped));
                return mapped.ToDataBox();
            }

            public DataBox MapSubresource(Resource resource, int subresource, MapMode mode, MapFlags flags, out DataStream stream)
            {
                var dataBox = MapSubresource(resource, subresource, mode, flags);
                stream = new DataStream(dataBox.DataPointer, 0, mode == MapMode.Read || mode == MapMode.ReadWrite, mode != MapMode.Read);
                return dataBox;
            }

            public void UnmapSubresource(Resource resource, int subresource)
            {
                if (resource == null)
                {
                    return;
                }

                nativeContext.Unmap(resource.Handle, (uint)subresource);
            }

            public void UpdateSubresource(Resource resource, int subresource, ResourceRegion? region, IntPtr sourceData, int rowPitch, int depthPitch)
            {
                if (resource == null || sourceData == IntPtr.Zero)
                {
                    return;
                }

                if (region.HasValue)
                {
                    var box = region.Value.ToSilkBox();
                    nativeContext.UpdateSubresource(resource.Handle, (uint)subresource, ref box, sourceData.ToPointer(), (uint)rowPitch, (uint)depthPitch);
                }
                else
                {
                    nativeContext.UpdateSubresource(resource.Handle, (uint)subresource, (Box*)null, sourceData.ToPointer(), (uint)rowPitch, (uint)depthPitch);
                }
            }

            public void CopyResource(Resource source, Resource destination)
            {
                if (source == null || destination == null)
                {
                    return;
                }

                nativeContext.CopyResource(destination.Handle, source.Handle);
            }

            public void CopySubresourceRegion(Resource source, int sourceSubresource, ResourceRegion? sourceRegion, Resource destination, int destinationSubResource, int dstX, int dstY, int dstZ)
            {
                if (source == null || destination == null)
                {
                    return;
                }

                if (sourceRegion.HasValue)
                {
                    var box = sourceRegion.Value.ToSilkBox();
                    nativeContext.CopySubresourceRegion(
                        destination.Handle,
                        (uint)destinationSubResource,
                        (uint)dstX,
                        (uint)dstY,
                        (uint)dstZ,
                        source.Handle,
                        (uint)sourceSubresource,
                        ref box);
                }
                else
                {
                    nativeContext.CopySubresourceRegion(
                        destination.Handle,
                        (uint)destinationSubResource,
                        (uint)dstX,
                        (uint)dstY,
                        (uint)dstZ,
                        source.Handle,
                        (uint)sourceSubresource,
                        (Box*)null);
                }
            }

            public void ResolveSubresource(Resource source, int sourceSubresource, Resource destination, int destinationSubresource, Format format)
            {
                if (source == null || destination == null)
                {
                    return;
                }

                nativeContext.ResolveSubresource(destination.Handle, (uint)destinationSubresource, source.Handle, (uint)sourceSubresource, format);
            }

            public void CopyStructureCount(Buffer destination, int destinationAlignedByteOffset, UnorderedAccessView source)
            {
                if (destination == null || source == null)
                {
                    return;
                }

                nativeContext.CopyStructureCount(destination.BufferHandle, (uint)destinationAlignedByteOffset, source.Handle);
            }

            public void GenerateMips(ShaderResourceView shaderResourceView)
            {
                if (shaderResourceView == null)
                {
                    return;
                }

                nativeContext.GenerateMips(shaderResourceView.Handle);
            }

            public void SetShaderResource(int shaderStage, int slot, ShaderResourceView shaderResourceView)
            {
                if (slot < 0)
                {
                    return;
                }

                var viewPtr = shaderResourceView?.Handle;
                SetShaderResources(shaderStage, slot, 1, &viewPtr);
            }

            public void SetShaderResources(int shaderStage, int slot, ShaderResourceView[] shaderResourceViews)
            {
                if (slot < 0 || shaderResourceViews == null || shaderResourceViews.Length == 0)
                {
                    return;
                }

                var viewPtrs = stackalloc ID3D11ShaderResourceView*[shaderResourceViews.Length];
                for (var i = 0; i < shaderResourceViews.Length; i++)
                {
                    viewPtrs[i] = shaderResourceViews[i]?.Handle;
                }

                SetShaderResources(shaderStage, slot, (uint)shaderResourceViews.Length, viewPtrs);
            }

            public void SetUnorderedAccessView(int slot, UnorderedAccessView unorderedAccessView, int initialCount = -1)
            {
                if (slot < 0)
                {
                    return;
                }

                var viewPtr = unorderedAccessView?.Handle;
                var count = unchecked((uint)initialCount);
                nativeContext.CSSetUnorderedAccessViews((uint)slot, 1, &viewPtr, &count);
            }

            public void SetUnorderedAccessViews(int slot, UnorderedAccessView[] unorderedAccessViews, int[] initialCounts = null)
            {
                if (slot < 0 || unorderedAccessViews == null || unorderedAccessViews.Length == 0)
                {
                    return;
                }

                var viewPtrs = stackalloc ID3D11UnorderedAccessView*[unorderedAccessViews.Length];
                var counts = stackalloc uint[unorderedAccessViews.Length];
                for (var i = 0; i < unorderedAccessViews.Length; i++)
                {
                    viewPtrs[i] = unorderedAccessViews[i]?.Handle;
                    counts[i] = initialCounts == null || i >= initialCounts.Length ? unchecked((uint)-1) : unchecked((uint)initialCounts[i]);
                }

                nativeContext.CSSetUnorderedAccessViews((uint)slot, (uint)unorderedAccessViews.Length, viewPtrs, counts);
            }

            private void SetShaderResources(int shaderStage, int slot, uint count, ID3D11ShaderResourceView** shaderResourceViews)
            {
                switch (shaderStage)
                {
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

            public void SetStreamOutputTarget(Buffer buffer, int offset)
            {
                var bufferPtr = buffer?.BufferHandle;
                var offsetValue = (uint)offset;
                nativeContext.SOSetTargets(1, &bufferPtr, &offsetValue);
            }

            public void SetStreamOutputTargets(Buffer[] buffers)
            {
                if (buffers == null || buffers.Length == 0)
                {
                    nativeContext.SOSetTargets(0, (ID3D11Buffer**)null, (uint*)null);
                    return;
                }

                var bufferPtrs = stackalloc ID3D11Buffer*[buffers.Length];
                var offsets = stackalloc uint[buffers.Length];
                for (var i = 0; i < buffers.Length; i++)
                {
                    bufferPtrs[i] = buffers[i]?.BufferHandle;
                    offsets[i] = 0;
                }

                nativeContext.SOSetTargets((uint)buffers.Length, bufferPtrs, offsets);
            }

            public void SetRenderTargets(DepthStencilView depthStencilView, RenderTargetView renderTargetView)
            {
                var renderTargetViewPtr = renderTargetView?.Handle;
                nativeContext.OMSetRenderTargets(1, &renderTargetViewPtr, depthStencilView?.Handle);
            }

            public void SetRenderTargets(DepthStencilView depthStencilView, RenderTargetView[] renderTargetViews)
            {
                if (renderTargetViews == null || renderTargetViews.Length == 0)
                {
                    nativeContext.OMSetRenderTargets(0, (ID3D11RenderTargetView**)null, depthStencilView?.Handle);
                    return;
                }

                var renderTargetViewPtrs = stackalloc ID3D11RenderTargetView*[renderTargetViews.Length];
                for (var i = 0; i < renderTargetViews.Length; i++)
                {
                    renderTargetViewPtrs[i] = renderTargetViews[i]?.Handle;
                }

                nativeContext.OMSetRenderTargets((uint)renderTargetViews.Length, renderTargetViewPtrs, depthStencilView?.Handle);
            }

            public void ClearRenderTargetView(RenderTargetView renderTargetView, Color4 color)
            {
                if (renderTargetView == null)
                {
                    return;
                }

                var clearColor = stackalloc float[4]
                {
                    color.X,
                    color.Y,
                    color.Z,
                    color.W
                };
                nativeContext.ClearRenderTargetView(renderTargetView.Handle, clearColor);
            }

            public void ClearDepthStencilView(DepthStencilView depthStencilView, DepthStencilClearFlags clearFlags, float depth, byte stencil)
            {
                if (depthStencilView == null)
                {
                    return;
                }

                nativeContext.ClearDepthStencilView(depthStencilView.Handle, (uint)clearFlags, depth, stencil);
            }

            public void ClearRenderTargetBindings()
            {
                nativeContext.OMSetRenderTargets(0, (ID3D11RenderTargetView**)null, (ID3D11DepthStencilView*)null);
            }

            public void GetDepthStencilView(out DepthStencilView depthStencilView)
            {
                ID3D11DepthStencilView* depthStencilViewPtr = null;
                nativeContext.OMGetRenderTargets(0, (ID3D11RenderTargetView**)null, &depthStencilViewPtr);
                depthStencilView = depthStencilViewPtr == null ? null : new DepthStencilView(new SilkD3D11DepthStencilViewPtr(depthStencilViewPtr));
            }

            public RenderTargetView[] GetRenderTargets(int numViews)
            {
                return GetRenderTargets(numViews, out _);
            }

            public RenderTargetView[] GetRenderTargets(int numViews, out DepthStencilView depthStencilView)
            {
                if (numViews <= 0)
                {
                    GetDepthStencilView(out depthStencilView);
                    return Array.Empty<RenderTargetView>();
                }

                var renderTargetViewPtrs = stackalloc ID3D11RenderTargetView*[numViews];
                ID3D11DepthStencilView* depthStencilViewPtr = null;
                nativeContext.OMGetRenderTargets((uint)numViews, renderTargetViewPtrs, &depthStencilViewPtr);

                var renderTargetViews = new RenderTargetView[numViews];
                for (var i = 0; i < numViews; i++)
                {
                    renderTargetViews[i] = renderTargetViewPtrs[i] == null ? null : new RenderTargetView(new SilkD3D11RenderTargetViewPtr(renderTargetViewPtrs[i]));
                }

                depthStencilView = depthStencilViewPtr == null ? null : new DepthStencilView(new SilkD3D11DepthStencilViewPtr(depthStencilViewPtr));
                return renderTargetViews;
            }

            public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessView, Int4 values)
            {
                if (unorderedAccessView == null)
                {
                    return;
                }

                var clearValues = stackalloc uint[4]
                {
                    unchecked((uint)values.X),
                    unchecked((uint)values.Y),
                    unchecked((uint)values.Z),
                    unchecked((uint)values.W)
                };
                nativeContext.ClearUnorderedAccessViewUint(unorderedAccessView.Handle, clearValues);
            }

            public void ClearUnorderedAccessView(UnorderedAccessView unorderedAccessView, Vector4 values)
            {
                if (unorderedAccessView == null)
                {
                    return;
                }

                var clearValues = stackalloc float[4]
                {
                    values.X,
                    values.Y,
                    values.Z,
                    values.W
                };
                nativeContext.ClearUnorderedAccessViewFloat(unorderedAccessView.Handle, clearValues);
            }

            public void SetOutputUnorderedAccessView(int slot, UnorderedAccessView unorderedAccessView)
            {
                var unorderedAccessViewPtr = unorderedAccessView?.Handle;
                var initialCount = unchecked((uint)-1);
                nativeContext.OMSetRenderTargetsAndUnorderedAccessViews(
                    uint.MaxValue,
                    (ID3D11RenderTargetView**)null,
                    (ID3D11DepthStencilView*)null,
                    (uint)slot,
                    1,
                    &unorderedAccessViewPtr,
                    &initialCount);
            }

            public void SetOutputUnorderedAccessViews(int startSlot, UnorderedAccessView[] unorderedAccessViews)
            {
                if (unorderedAccessViews == null || unorderedAccessViews.Length == 0)
                {
                    return;
                }

                var unorderedAccessViewPtrs = stackalloc ID3D11UnorderedAccessView*[unorderedAccessViews.Length];
                var initialCounts = stackalloc uint[unorderedAccessViews.Length];
                for (var i = 0; i < unorderedAccessViews.Length; i++)
                {
                    unorderedAccessViewPtrs[i] = unorderedAccessViews[i]?.Handle;
                    initialCounts[i] = unchecked((uint)-1);
                }

                nativeContext.OMSetRenderTargetsAndUnorderedAccessViews(
                    uint.MaxValue,
                    (ID3D11RenderTargetView**)null,
                    (ID3D11DepthStencilView*)null,
                    (uint)startSlot,
                    (uint)unorderedAccessViews.Length,
                    unorderedAccessViewPtrs,
                    initialCounts);
            }

            public UnorderedAccessView[] GetUnorderedAccessViews(int startSlot, int count)
            {
                if (count <= 0)
                {
                    return Array.Empty<UnorderedAccessView>();
                }

                var unorderedAccessViewPtrs = stackalloc ID3D11UnorderedAccessView*[count];
                nativeContext.OMGetRenderTargetsAndUnorderedAccessViews(
                    0,
                    (ID3D11RenderTargetView**)null,
                    (ID3D11DepthStencilView**)null,
                    (uint)startSlot,
                    (uint)count,
                    unorderedAccessViewPtrs);

                var unorderedAccessViews = new UnorderedAccessView[count];
                for (var i = 0; i < count; i++)
                {
                    unorderedAccessViews[i] = unorderedAccessViewPtrs[i] == null ? null : new UnorderedAccessView(new SilkD3D11UnorderedAccessViewPtr(unorderedAccessViewPtrs[i]));
                }

                return unorderedAccessViews;
            }

            public void Dispose()
            {
                if (IsDisposed)
                {
                    return;
                }

                nativeContext.Dispose();
                IsDisposed = true;
            }
        }
    }
}
