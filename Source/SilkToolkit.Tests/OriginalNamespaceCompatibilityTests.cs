using System.Xaml;
using System.Xaml.Schema;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using Xunit;

namespace HelixToolkit.SilkToolkit.Tests;

/// <summary>Checks original public CLR and XAML names without creating a viewport or native device.</summary>
public sealed class OriginalNamespaceCompatibilityTests {
    /// <summary>Preserves representative original public type identities and native Silk ownership.</summary>
    /// <param name="type">The actual public type.</param>
    /// <param name="expectedName">The source-compatible fully qualified name.</param>
    [Theory]
    [InlineData(typeof(Viewport3DX), "HelixToolkit.Wpf.SharpDX.Viewport3DX")]
    [InlineData(typeof(PhongMaterial), "HelixToolkit.Wpf.SharpDX.PhongMaterial")]
    [InlineData(typeof(PBRMaterial), "HelixToolkit.Wpf.SharpDX.PBRMaterial")]
    [InlineData(typeof(MeshGeometry3D), "HelixToolkit.SharpDX.Core.MeshGeometry3D")]
    [InlineData(typeof(RenderContext), "HelixToolkit.SharpDX.Core.RenderContext")]
    [InlineData(typeof(MSAALevel), "HelixToolkit.SharpDX.Core.MSAALevel")]
    [InlineData(typeof(PBRMaterialCore), "HelixToolkit.SharpDX.Core.Model.PBRMaterialCore")]
    [InlineData(typeof(SceneNode), "HelixToolkit.SharpDX.Core.Model.Scene.SceneNode")]
    [InlineData(typeof(D3D12ScenePassCatalog), "HelixToolkit.SharpDX.Core.Native.D3D12ScenePassCatalog")]
    public void PublicTypeNameTest(Type type, string expectedName) => Assert.Equal(expectedName, type.FullName);

    /// <summary>Resolves unchanged Helix XAML type names without running dependency-object constructors.</summary>
    /// <param name="name">The original XAML type name.</param>
    /// <param name="expectedType">The corresponding restored toolkit type.</param>
    [Theory]
    [InlineData("Viewport3DX", typeof(Viewport3DX))]
    [InlineData("Material", typeof(Material))]
    [InlineData("PhongMaterial", typeof(PhongMaterial))]
    [InlineData("PBRMaterial", typeof(PBRMaterial))]
    public void OriginalXamlTypeResolutionTest(string name, Type expectedType) {
        var context = new XamlSchemaContext([typeof(Viewport3DX).Assembly]);
        var requestedName = new XamlTypeName("http://helix-toolkit.org/wpf/SharpDX", name);
        var resolvedType = context.GetXamlType(requestedName);
        Assert.NotNull(resolvedType);
        Assert.Equal(expectedType, resolvedType.UnderlyingType);
    }

    /// <summary>Retains the original palette member while preserving independently mutable factory results.</summary>
    /// <returns>The headless WPF palette verification.</returns>
    [Fact]
    public Task OriginalVrmlPaletteNameTest() => global::SilkToolkit.Tests.StaThread.RunAsync(() => {
        var original = PhongMaterials.DefaultVRML;
        var current = PhongMaterials.DefaultVrml;
        Assert.Equal("DefaultVRML", original.Name);
        Assert.Equal(current.DiffuseColor, original.DiffuseColor);
        Assert.NotSame(original, current);
    });
}
