using System.Reflection;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Native;

namespace SilkCore.Tests;

/// <summary>Checks billboard preparation and descriptor contracts without native devices or submission.</summary>
public sealed class D3D12BillboardPreparationTests {
    /// <summary>Fresh geometry generations must prepare their own texture before the core binds t0.</summary>
    [Fact]
    public void CorePreparesBillboardBeforeReadingTextureAndBinding() {
        var source = ReadSource("Core/PointLineRenderCore.cs");
        var prepare = source.IndexOf("PrepareForD3D12()", StringComparison.Ordinal);
        var bind = source.IndexOf("bindings.Update(context", StringComparison.Ordinal);
        Assert.True(prepare >= 0 && prepare < bind, "Billboard preparation must precede texture binding");
        Assert.Contains("PrepareForD3D12().Texture", source);
    }

    /// <summary>A missing replacement texture must overwrite the same t0 destination with a null SRV.</summary>
    [Fact]
    public void MissingTextureWritesNullDescriptorAtT0() {
        var source = ReadSource("Native/D3D12Bindings.cs");
        var start = source.IndexOf("internal sealed class SilkD3D12PointLineBindings", StringComparison.Ordinal);
        var end = source.IndexOf("private TextureModel? GetMaterialTexture()", start, StringComparison.Ordinal);
        var update = source[start..end];
        Assert.Contains("} else\n            device.CreateNullShaderResourceView(bindings.ShaderResource(0))",
            update.Replace("\r\n", "\n", StringComparison.Ordinal));
        Assert.Contains("device.CreateShaderResourceView(texture.Resource, bindings.ShaderResource(0)", update);
        Assert.Contains("bindings.GetTexture(context, resources, textureModel, 0, textureSource)", update);
    }

    /// <summary>Frame cleanup clears a registered descriptor before relinquishing its resource ownership.</summary>
    [Fact]
    public void ReleasedRegistrationClearsDescriptorBeforeDisposal() {
        var source = ReadSource("Native/D3D12Bindings.cs");
        var start = source.IndexOf("internal void ReleaseTexture(int register)", StringComparison.Ordinal);
        var end = source.IndexOf("textures[register] = null", start, StringComparison.Ordinal);
        var release = source[start..end];
        var clear = release.IndexOf("CreateNullShaderResourceView(ShaderResource(register))", StringComparison.Ordinal);
        var dispose = release.IndexOf("Dispose()", StringComparison.Ordinal);
        Assert.True(clear >= 0 && clear < dispose, "Clear the registered SRV before releasing its target");
    }

    /// <summary>Early preparation preserves failures for missing geometry, resources and disposed owners.</summary>
    [Fact]
    public void PreparationPreservesInvalidStateFailures() {
        using var model = new DefaultBillboardBufferModel();
        Assert.Throws<InvalidOperationException>(() => model.PrepareForD3D12());
        model.Geometry = new BillboardSingleText3D();
        Assert.Throws<InvalidOperationException>(() => model.PrepareForD3D12());
        model.Dispose();
        Assert.Throws<ObjectDisposedException>(() => model.PrepareForD3D12());
    }

    /// <summary>G1 to fresh G2 and then empty geometry never reuse the previous geometry's texture.</summary>
    [Fact]
    public void ReplacementPreparesOwnTextureAndEmptyGeometryStaysNull() {
        var first = new CpuBillboard();
        var second = new CpuBillboard();
        using var model = new DefaultBillboardBufferModel { Geometry = first };
        Assert.Null(first.Texture);
        var firstVertices = Prepare(model);
        var firstTexture = first.Texture;
        model.Geometry = second;
        Assert.Null(second.Texture);
        var secondVertices = Prepare(model);
        var secondTexture = second.Texture;
        var repeatedVertices = Prepare(model);
        var empty = new CpuBillboard { Empty = true };
        model.Geometry = empty;
        var emptyVertices = Prepare(model);

        Assert.Single(firstVertices);
        Assert.Single(secondVertices);
        Assert.Single(repeatedVertices);
        Assert.NotNull(firstTexture);
        Assert.NotNull(secondTexture);
        Assert.NotSame(firstTexture, secondTexture);
        Assert.Equal(1, first.GenerationCount);
        Assert.Equal(1, second.GenerationCount);
        Assert.Null(empty.Texture);
        Assert.Empty(emptyVertices);
    }

    /// <summary>Invokes the actual CPU preparation entry without constructing a native resource.</summary>
    private static BillboardVertex[] Prepare(DefaultBillboardBufferModel model) {
        var method = typeof(SilkD3D12PointLineBuffers).GetMethod("Prepare",
            BindingFlags.NonPublic | BindingFlags.Static, [typeof(DefaultBillboardBufferModel)]);
        return (BillboardVertex[])method!.Invoke(null, [model])!;
    }

    /// <summary>Uses the source-contract location convention of D3D12CutoverContractTests.</summary>
    private static string ReadSource(string relativePath) {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "SilkCore")))
            directory = directory.Parent;
        return File.ReadAllText(Path.Combine(directory!.FullName, "SilkCore", relativePath));
    }

    /// <summary>Supplies deterministic texture generations without DirectWrite, WPF rasterization or a device.</summary>
    private sealed class CpuBillboard : BillboardSingleText3D {
        /// <summary>Gets or sets whether preparation produces empty text.</summary>
        internal bool Empty { get; init; }
        /// <summary>Gets the number of actual texture generations.</summary>
        internal int GenerationCount { get; private set; }
        /// <summary>Prepares one CPU-only generation, retaining the ordinary initialized contract.</summary>
        internal override bool TryPrepareVerticesForD3D12() {
            if (IsInitialized) return true;
            GenerationCount++;
            if (!Empty) {
                Texture = new TextureModel(new MemoryStream([1]));
                BillboardVertices.Add(default);
            }
            IsInitialized = true;
            return true;
        }
    }
}
