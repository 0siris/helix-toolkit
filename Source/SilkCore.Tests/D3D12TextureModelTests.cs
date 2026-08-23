using System.Runtime.InteropServices;
using System.Text;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using ToolkitImage = HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics.Image;
using ToolkitPixelFormat = HelixToolkit.SharpDX.Core.SharpDX.Toolkit.Graphics.PixelFormat;

namespace SilkCore.Tests;

/// <summary>
///     Verifies the existing texture-model contract at the Direct3D 12 upload boundary.
/// </summary>
public class D3D12TextureModelTests {
    /// <summary>
    ///     Verifies managed byte and color textures preserve their dimensions, format, pitch, and pixels.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void PreparesManagedTextureData() {
        var bytes = new byte[] {1, 2, 3, 4, 5, 6, 7, 8};
        var byteData = SilkD3D12TextureModelResource.PrepareUploadData(
            new TextureInfo(bytes, Format.FormatR8G8B8A8Unorm, 2, 1));
        var colors = new[] {new Color4(1, 0.5f, 0.25f, 1)};
        var colorData = SilkD3D12TextureModelResource.PrepareUploadData(new TextureInfo(colors, 1, 1));

        Assert.Equal(bytes, byteData.Pixels);
        Assert.Equal(2u, byteData.Width);
        Assert.Equal(1u, byteData.Height);
        Assert.Equal(8u, byteData.RowPitch);
        Assert.Equal(Format.FormatR8G8B8A8Unorm, byteData.Format);
        Assert.Equal(new[] {1f, 0.5f, 0.25f, 1f},
            MemoryMarshal.Cast<byte, float>(colorData.Pixels).ToArray());
        Assert.Equal(16u, colorData.RowPitch);
        Assert.Equal(Format.FormatR32G32B32A32Float, colorData.Format);
    }

    /// <summary>
    ///     Verifies pointer-backed texture data is copied before the caller releases its memory.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void CopiesPointerTextureData() {
        var expected = new byte[] {9, 8, 7, 6};
        var pointer = Marshal.AllocHGlobal(expected.Length);
        try {
            Marshal.Copy(expected, 0, pointer, expected.Length);

            var data = SilkD3D12TextureModelResource.PrepareUploadData(
                new TextureInfo(pointer, Format.FormatR8G8B8A8Unorm, 1, 1));

            Assert.Equal(expected, data.Pixels);
            Assert.Equal(4u, data.RowPitch);
        } finally {
            Marshal.FreeHGlobal(pointer);
        }
    }

    /// <summary>
    ///     Verifies raw streams are read exactly and seekable stream positions are restored.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ReadsRawTextureStreamAndRestoresPosition() {
        using var stream = new MemoryStream([1, 2, 3, 4]);
        stream.Position = 2;
        var info = new TextureInfo(stream, Format.FormatR8G8B8A8Unorm, 1, 1, false);

        var data = SilkD3D12TextureModelResource.PrepareUploadData(info);

        Assert.Equal([1, 2, 3, 4], data.Pixels);
        Assert.Equal(2, stream.Position);
    }

    /// <summary>
    ///     Verifies encoded image streams use the existing decoder and restore their original position.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DecodesTextureStreamAndRestoresPosition() {
        using var stream = CreateBmp([0, 255, 0, 255]);
        stream.Position = 2;

        var data = SilkD3D12TextureModelResource.PrepareUploadData(new TextureInfo(stream, false));

        Assert.Equal(1u, data.Width);
        Assert.Equal(1u, data.Height);
        Assert.Equal(4u, data.RowPitch);
        Assert.Equal(Format.FormatB8G8R8A8Unorm, data.Format);
        Assert.Equal([0, 255, 0, 255], data.Pixels);
        Assert.Equal(2, stream.Position);
    }

    /// <summary>
    ///     Verifies decoded arrays and cube maps preserve every mip subresource in native upload order.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void PreparesMipmappedArrayAndCubeSubresources() {
        using var arrayImage = ToolkitImage.New2D(4, 4, 2, ToolkitPixelFormat.R8G8B8A8.UNorm, 2);
        FillSubresources(arrayImage);
        using var cubeImage = ToolkitImage.NewCube(4, 2, ToolkitPixelFormat.Bc1.UNorm);
        FillSubresources(cubeImage);

        var arrayData = SilkD3D12TextureModelResource.PrepareImage(arrayImage);
        var cubeData = SilkD3D12TextureModelResource.PrepareImage(cubeImage);

        Assert.Equal(2, arrayData.ArraySize);
        Assert.Equal(2, arrayData.MipLevels);
        Assert.False(arrayData.IsCubeMap);
        Assert.Equal(4, arrayData.Subresources.Length);
        Assert.Equal([1, 2, 3, 4], arrayData.Subresources.Select(data => data.Data.Span[0]).ToArray());
        Assert.Equal([16u, 8u, 16u, 8u], arrayData.Subresources.Select(data => data.RowPitch).ToArray());
        Assert.Equal(6, cubeData.ArraySize);
        Assert.Equal(2, cubeData.MipLevels);
        Assert.True(cubeData.IsCubeMap);
        Assert.Equal(12, cubeData.Subresources.Length);
        Assert.Equal(Enumerable.Range(1, 12).Select(value => (byte) value),
            cubeData.Subresources.Select(data => data.Data.Span[0]));
        Assert.All(cubeData.Subresources, data => Assert.Equal(8u, data.SlicePitch));
    }

    /// <summary>
    ///     Verifies the encoded DDS bridge keeps BC1 array slices, mip levels, and cube faces intact.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DecodesMipmappedBc1ArrayAndCubeDds() {
        using var arrayStream = CreateBc1Dds(2, 2, false);
        using var cubeStream = CreateBc1Dds(1, 2, true);

        var arrayData = SilkD3D12TextureModelResource.PrepareUploadData(new TextureInfo(arrayStream, false));
        var cubeData = SilkD3D12TextureModelResource.PrepareUploadData(new TextureInfo(cubeStream, false));

        Assert.Equal((ushort) 2, arrayData.ArraySize);
        Assert.Equal((ushort) 2, arrayData.MipLevels);
        Assert.Equal(4, arrayData.Subresources.Length);
        Assert.Equal([1, 2, 3, 4], arrayData.Subresources.Select(data => data.Data.Span[0]).ToArray());
        Assert.Equal((ushort) 6, cubeData.ArraySize);
        Assert.Equal((ushort) 2, cubeData.MipLevels);
        Assert.True(cubeData.IsCubeMap);
        Assert.Equal(12, cubeData.Subresources.Length);
        Assert.Equal(12, cubeData.Subresources[^1].Data.Span[0]);
    }

    /// <summary>
    ///     Verifies the scene renderer accepts only cube textures and publishes their mip metadata to b3.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpPreparesSharedEnvironmentCubeMapMetadata() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 2, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 1, true);
        using var renderer = new SilkD3D12SceneRenderer(device, resourceHeap, samplerHeap);
        using var stream = CreateBc1Dds(1, 2, true);
        using var nonCubeStream = CreateBc1Dds(1, 2, false);
        var loader = new TrackingTextureLoader(new TextureInfo(stream, false));
        var nonCubeLoader = new TrackingTextureLoader(new TextureInfo(nonCubeStream, false));
        var model = new TextureModel(Guid.NewGuid(), loader);
        var nonCubeModel = new TextureModel(Guid.NewGuid(), nonCubeLoader);
        var lights = new HelixToolkit.SharpDX.Core.Model.Lights.LightsBufferModel();

        context.Reset();
        Assert.Same(model, renderer.PrepareEnvironment(context, lights, model));
        Assert.True(lights.HasEnvironmentMap);
        Assert.Equal(2, lights.EnvironmentMapMipLevels);
        Assert.Null(renderer.PrepareEnvironment(context, lights, nonCubeModel));
        Assert.False(lights.HasEnvironmentMap);
        Assert.Equal(0, lights.EnvironmentMapMipLevels);
        Assert.Equal(2, resourceHeap.Count);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        Assert.Equal(1, loader.LoadCount);
        Assert.Equal(1, loader.CompleteCount);
        Assert.True(loader.Succeeded);
        Assert.Equal(1, nonCubeLoader.LoadCount);
        Assert.Equal(1, nonCubeLoader.CompleteCount);
        Assert.True(nonCubeLoader.Succeeded);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Verifies unsupported dimensions, formats, and byte counts fail before native allocation.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void RejectsUnsupportedTextureData() {
        Assert.Throws<NotSupportedException>(() =>
            SilkD3D12TextureModelResource.PrepareUploadData(TextureInfo.Null));
        Assert.Throws<NotSupportedException>(() =>
            SilkD3D12TextureModelResource.PrepareUploadData(
                new TextureInfo([1], Format.FormatR8Unorm, 1)));
        Assert.Throws<NotSupportedException>(() =>
            SilkD3D12TextureModelResource.PrepareUploadData(
                new TextureInfo([1], Format.FormatBC1Unorm, 1, 1)));
        Assert.Throws<ArgumentException>(() =>
            SilkD3D12TextureModelResource.PrepareUploadData(
                new TextureInfo([1], Format.FormatR8G8B8A8Unorm, 1, 1)));

        using var oversized = new MemoryStream([1, 2, 3, 4, 5]);
        Assert.Throws<ArgumentException>(() =>
            SilkD3D12TextureModelResource.PrepareUploadData(
                new TextureInfo(oversized, Format.FormatR8G8B8A8Unorm, 1, 1)));
    }

    /// <summary>
    ///     Verifies a texture model uploads, samples, reads back, completes, and disposes through WARP.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpUploadsAndSamplesTextureModel() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var rootSignature = device.CreateDefaultRootSignature();
        using var pipeline = device.CreateGraphicsPipelineState(rootSignature,
            D3D12ShaderModule.Load("VS", "vsMeshOutlineScreenQuad"),
            D3D12ShaderModule.Load("PS", "psScreenDup"));
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 118, true);
        using var samplerHeap = device.CreateDescriptorHeap(DescriptorHeapType.Sampler, 10, true);
        using var sampler = samplerHeap.Allocate();
        using var renderTargetHeap = device.CreateDescriptorHeap(DescriptorHeapType.Rtv, 1);
        using var renderTargetView = renderTargetHeap.Allocate();
        using var renderTarget = device.CreateRenderTargetTexture2D(4, 4, Format.FormatR8G8B8A8Unorm);
        var reservedDescriptors = Enumerable.Range(0, 10).Select(_ => resourceHeap.Allocate()).ToArray();
        var loader = new TrackingTextureLoader(
            new TextureInfo([0, 255, 0, 255], Format.FormatR8G8B8A8Unorm, 1, 1, false));
        var model = new TextureModel(Guid.NewGuid(), loader);
        var footprint = device.GetCopyableFootprint(renderTarget, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);

        try {
            context.Reset();
            using var texture = SilkD3D12TextureModelResource.Create(device, context, resourceHeap, model);
            device.CreateSampler(sampler);
            device.CreateRenderTargetView(renderTarget, renderTargetView);
            Assert.Equal(10, texture.ShaderResourceView.Index);
            Assert.Equal(1, loader.LoadCount);
            Assert.Equal(1, loader.CompleteCount);
            Assert.True(loader.Succeeded);

            context.ClearRenderTarget(renderTarget, renderTargetView, [1, 0, 0, 1]);
            context.SetRenderTarget(renderTargetView);
            context.SetViewport(4, 4);
            context.SetGraphicsPipeline(rootSignature, pipeline);
            context.SetDescriptorHeaps(resourceHeap, samplerHeap);
            context.SetGraphicsDescriptorTables(reservedDescriptors[0], sampler);
            context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
            context.DrawInstanced(4);
            context.Transition(renderTarget, ResourceStates.CopySource);
            context.CopyTextureToBuffer(readback, renderTarget, in footprint);
            context.Close();
            queue.Execute(context);
            fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

            Assert.Equal([0, 255, 0, 255], readback.Read(4));
            texture.Dispose();
            Assert.True(texture.IsDisposed);
            Assert.True(texture.Resource.IsDisposed);
            Assert.True(texture.UploadResource.IsDisposed);
            Assert.True(texture.ShaderResourceView.IsDisposed);
            Assert.Equal(10, resourceHeap.Count);
            device.ThrowIfDeviceRemoved();
        } finally {
            foreach (var descriptor in reservedDescriptors) descriptor.Dispose();
        }
    }

    /// <summary>
    ///     Verifies WARP uploads and reads a non-base mip from a BC1 texture array.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpUploadsMipmappedBc1Array() {
        using var stream = CreateBc1Dds(2, 2, false);

        AssertWarpSubresourceUpload(stream, 3, 4, false);
    }

    /// <summary>
    ///     Verifies WARP creates a cube SRV and reads a non-base mip from its final face.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpUploadsMipmappedBc1Cube() {
        using var stream = CreateBc1Dds(1, 2, true);

        AssertWarpSubresourceUpload(stream, 11, 12, true);
    }

    /// <summary>
    ///     Verifies invalid multi-subresource uploads fail before native command recording.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpRejectsInvalidMultiSubresourceUploads() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var texture = device.CreateTexture2D(4,
            4,
            Format.FormatBC1Unorm,
            arraySize: 2,
            mipLevels: 2);
        var validBlock = new D3D12SubresourceData(new byte[8], 8, 8);

        Assert.Throws<ArgumentException>(() =>
            device.CreateTextureUploadBuffer(texture, [validBlock], out _));
        Assert.Throws<ArgumentException>(() =>
            device.CreateTextureUploadBuffer(texture,
                [validBlock, validBlock, validBlock, new D3D12SubresourceData(new byte[8], 4, 8)],
                out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => device.GetCopyableFootprint(texture, 4, out _));
    }

    /// <summary>
    ///     Verifies failed uploads report failure once and leave no descriptor allocation behind.
    /// </summary>
    [Fact]
    [Trait("Category", "Warp")]
    public void WarpCleansUpFailedTextureModelUpload() {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var context = device.CreateCommandContext();
        using var resourceHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1, true);
        using var invisibleHeap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1);
        var loader = new TrackingTextureLoader(TextureInfo.Null);
        var model = new TextureModel(Guid.NewGuid(), loader);
        context.Reset();

        Assert.Throws<ArgumentException>(() =>
            SilkD3D12TextureModelResource.Create(device, context, invisibleHeap, model));
        Assert.Equal(0, loader.LoadCount);
        Assert.Throws<NotSupportedException>(() =>
            SilkD3D12TextureModelResource.Create(device, context, resourceHeap, model));

        Assert.Equal(1, loader.LoadCount);
        Assert.Equal(1, loader.CompleteCount);
        Assert.False(loader.Succeeded);
        Assert.Equal(0, resourceHeap.Count);
    }

    /// <summary>
    ///     Creates one uncompressed 32-bit bitmap stream.
    /// </summary>
    /// <param name="pixel">The single BGRA pixel.</param>
    /// <returns>The bitmap stream positioned at its beginning.</returns>
    private static MemoryStream CreateBmp(byte[] pixel) {
        if (pixel.Length != 4) throw new ArgumentException("One BGRA pixel is required.", nameof(pixel));

        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) {
            writer.Write((ushort) 0x4D42);
            writer.Write(58);
            writer.Write(0);
            writer.Write(54);
            writer.Write(40);
            writer.Write(1);
            writer.Write(1);
            writer.Write((ushort) 1);
            writer.Write((ushort) 32);
            writer.Write(0);
            writer.Write(4);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(pixel);
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    ///     Fills every image subresource with its one-based native upload index.
    /// </summary>
    /// <param name="image">The image to fill.</param>
    private static void FillSubresources(ToolkitImage image) {
        var value = 1;
        for (var arrayIndex = 0; arrayIndex < image.Description.ArraySize; arrayIndex++)
        for (var mipIndex = 0; mipIndex < image.Description.MipLevels; mipIndex++) {
            var buffer = image.GetPixelBuffer(arrayIndex, mipIndex);
            Marshal.Copy(Enumerable.Repeat((byte) value++, buffer.BufferStride).ToArray(),
                0,
                buffer.DataPointer,
                buffer.BufferStride);
        }
    }

    /// <summary>
    ///     Creates a DX10-header BC1 DDS stream with deterministic array-major, mip-minor data.
    /// </summary>
    /// <param name="arraySize">The texture-array size or cube count.</param>
    /// <param name="mipLevels">The mip-level count.</param>
    /// <param name="isCubeMap">Whether each array item is a six-face cube.</param>
    /// <returns>The DDS stream positioned at its beginning.</returns>
    internal static MemoryStream CreateBc1Dds(int arraySize, int mipLevels, bool isCubeMap) {
        if (arraySize <= 0) throw new ArgumentOutOfRangeException(nameof(arraySize));
        if (mipLevels <= 0) throw new ArgumentOutOfRangeException(nameof(mipLevels));

        var subresourceCount = checked(arraySize * (isCubeMap ? 6 : 1) * mipLevels);
        var stream = new MemoryStream();
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true)) {
            writer.Write(0x20534444u);
            writer.Write(124u);
            writer.Write(0x000A1007u);
            writer.Write(4u);
            writer.Write(4u);
            writer.Write(8u);
            writer.Write(0u);
            writer.Write((uint) mipLevels);
            for (var index = 0; index < 11; index++) writer.Write(0u);
            writer.Write(32u);
            writer.Write(4u);
            writer.Write(0x30315844u);
            for (var index = 0; index < 5; index++) writer.Write(0u);
            writer.Write(mipLevels > 1 || arraySize > 1 || isCubeMap ? 0x00401008u : 0x00001000u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write(0u);
            writer.Write((uint) Format.FormatBC1Unorm);
            writer.Write(3u);
            writer.Write(isCubeMap ? 4u : 0u);
            writer.Write((uint) arraySize);
            writer.Write(0u);
            for (var subresource = 1; subresource <= subresourceCount; subresource++)
                writer.Write(Enumerable.Repeat((byte) subresource, 8).ToArray());
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    ///     Uploads one encoded texture model and reads a selected BC1 subresource through WARP.
    /// </summary>
    /// <param name="stream">The encoded DDS stream.</param>
    /// <param name="subresource">The subresource to read.</param>
    /// <param name="expectedValue">The expected repeated BC1 block byte.</param>
    /// <param name="isCubeMap">Whether a cube-map resource is expected.</param>
    private static void AssertWarpSubresourceUpload(
        Stream stream,
        uint subresource,
        byte expectedValue,
        bool isCubeMap
    ) {
        using var device = SilkD3D12DeviceFactory.CreateDefault(SilkFeatureLevel.Level110, SilkDriverType.Warp);
        using var queue = device.CreateCommandQueue();
        using var context = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var heap = device.CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, 1, true);
        var loader = new TrackingTextureLoader(new TextureInfo(stream, false));
        var model = new TextureModel(Guid.NewGuid(), loader);
        context.Reset();
        using var texture = SilkD3D12TextureModelResource.Create(device, context, heap, model);
        var footprint = device.GetCopyableFootprint(texture.Resource, subresource, out var totalBytes);
        using var readback = device.CreateBuffer(totalBytes, HeapType.Readback);
        context.Transition(texture.Resource, ResourceStates.CopySource);
        context.CopyTextureToBuffer(readback, texture.Resource, in footprint, subresource);
        context.Close();
        queue.Execute(context);
        fence.Wait(queue.Signal(fence), TimeSpan.FromSeconds(5));

        Assert.Equal(isCubeMap ? 6 : 2, texture.Resource.Description.DepthOrArraySize);
        Assert.Equal(2, texture.Resource.Description.MipLevels);
        Assert.Equal(Enumerable.Repeat(expectedValue, 8), readback.Read(8, footprint.Offset));
        Assert.True(loader.Succeeded);
        device.ThrowIfDeviceRemoved();
    }

    /// <summary>
    ///     Tracks the texture loader lifecycle used by the native bridge.
    /// </summary>
    private sealed class TrackingTextureLoader : ITextureInfoLoader {
        /// <summary>
        ///     Initializes a tracking loader.
        /// </summary>
        /// <param name="info">The texture information returned from loading.</param>
        public TrackingTextureLoader(TextureInfo info) {
            Info = info;
        }

        /// <summary>
        ///     Gets the texture information returned to the bridge.
        /// </summary>
        private TextureInfo Info { get; }

        /// <summary>
        ///     Gets the number of load calls.
        /// </summary>
        public int LoadCount { get; private set; }

        /// <summary>
        ///     Gets the number of completion calls.
        /// </summary>
        public int CompleteCount { get; private set; }

        /// <summary>
        ///     Gets the last reported completion result.
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        ///     Returns the configured texture information.
        /// </summary>
        /// <param name="id">The texture identifier.</param>
        /// <returns>The configured texture information.</returns>
        public TextureInfo Load(Guid id) {
            LoadCount++;
            return Info;
        }

        /// <summary>
        ///     Records completion of one upload attempt.
        /// </summary>
        /// <param name="id">The texture identifier.</param>
        /// <param name="info">The completed texture information.</param>
        /// <param name="succeeded">Whether the upload succeeded.</param>
        public void Complete(Guid id, TextureInfo info, bool succeeded) {
            CompleteCount++;
            Succeeded = succeeded;
        }
    }
}
