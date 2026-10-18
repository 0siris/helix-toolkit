using HelixToolkit.SharpDX.Core.Model;
using System.Text;
using SharpAssimp;
using System.Numerics;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Utilities;
using Xunit;
using HxImporter = HelixToolkit.SharpDX.Core.Assimp.Importer;

namespace HelixToolkit.SharpDX.Core.Assimp;

/// <summary>Verifies independent PBR factors through actual scene and native glTF imports without presentation.</summary>
public sealed class AssimpMaterialImportTests {
    /// <summary>
    ///     Imports literal properties independently, including a roughness property without a metallic property.
    /// </summary>
    /// <param name="prefix">The literal property namespace emitted by the producer.</param>
    /// <param name="hasMetallic">Whether the source contains a metallic factor.</param>
    [Theory]
    [InlineData("$mat.gltf.pbrMetallicRoughness.", true)]
    [InlineData("$mat.gltf.pbrMetallicRoughness.", false)]
    [InlineData("$mat.", true)]
    [InlineData("$mat.", false)]
    [Trait("Category", "Unit")]
    public void LoadIndependentFactorsTest(string prefix, bool hasMetallic) {
        var material = new Material { Name = "Literal factors" };
        if (hasMetallic)
            material.AddProperty(new MaterialProperty(prefix + "metallicFactor", 0.2f));
        material.AddProperty(new MaterialProperty(prefix + "roughnessFactor", 0.8f));
        var source = CreateTriangleScene(material);
        using var importer = new HxImporter {
            Configuration = new ImporterConfiguration { BuildOctree = false }
        };

        var result = importer.Load(source, "literal.gltf", out var imported);

        Assert.Equal(ErrorCode.Succeed, result);
        Assert.NotNull(imported);
        using var root = imported.Root;
        var meshes = root.Traverse().OfType<MeshNode>().ToArray();
        var mesh = Assert.Single(meshes);
        var pbr = Assert.IsType<PBRMaterialCore>(mesh.Material);
        Assert.Equal(hasMetallic ? 0.2f : 0f, pbr.MetallicFactor, 5);
        Assert.Equal(0.8f, pbr.RoughnessFactor, 5);
    }

    /// <summary>
    ///     Parses a self-contained synthetic glTF through AssimpNet's native importer and the toolkit Load path.
    /// </summary>
    /// <param name="hasMetallic">Whether glTF explicitly supplies metallic or uses its default of one.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Trait("Category", "Unit")]
    public void LoadNativeGltfFactorsTest(bool hasMetallic) {
        var metallic = hasMetallic ? "\"metallicFactor\":0.2," : string.Empty;
        var json = $$$"""
            {
              "asset":{"version":"2.0"},
              "buffers":[{"byteLength":42,"uri":"data:application/octet-stream;base64,AAAAAAAAAAAAAAAAAACAPwAAAAAAAAAAAAAAAAAAgD8AAAAAAAABAAIA"}],
              "bufferViews":[{"buffer":0,"byteOffset":0,"byteLength":36},{"buffer":0,"byteOffset":36,"byteLength":6}],
              "accessors":[{"bufferView":0,"componentType":5126,"count":3,"type":"VEC3","min":[0,0,0],"max":[1,1,0]},
                           {"bufferView":1,"componentType":5123,"count":3,"type":"SCALAR"}],
              "materials":[{"name":"Literal factors","pbrMetallicRoughness":{ {{{metallic}}} "roughnessFactor":0.8}}],
              "meshes":[{"primitives":[{"attributes":{"POSITION":0},"indices":1,"material":0}]}],
              "nodes":[{"mesh":0}],"scenes":[{"nodes":[0]}],"scene":0
            }
            """;
        using var nativeStream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var context = new AssimpContext();
        var nativeScene = context.ImportFileFromStream(nativeStream, PostProcessSteps.Triangulate, "gltf");
        var nativeMaterial = nativeScene.Materials.Single(material => material.Name == "Literal factors");
        var nativeProperty = nativeMaterial.GetNonTextureProperty("$mat.gltf.pbrMetallicRoughness.roughnessFactor");
        Assert.NotNull(nativeProperty);
        var nativeRoughness = nativeProperty.GetFloatValue();
        Assert.Equal(0.8f, nativeRoughness, 5);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        using var importer = new HxImporter {
            Configuration = new ImporterConfiguration { BuildOctree = false }
        };

        var result = importer.Load(stream, "literal.gltf", "gltf", out var imported);

        Assert.Equal(ErrorCode.Succeed, result);
        Assert.NotNull(imported);
        using var root = imported.Root;
        var meshes = root.Traverse().OfType<MeshNode>().ToArray();
        var mesh = Assert.Single(meshes);
        var pbr = Assert.IsType<PBRMaterialCore>(mesh.Material);
        Assert.Equal(hasMetallic ? 0.2f : 1f, pbr.MetallicFactor, 5);
        Assert.Equal(0.8f, pbr.RoughnessFactor, 5);
    }

    /// <summary>
    ///     Constructs a literal triangle whose material reaches the existing material conversion path.
    /// </summary>
    /// <param name="material">The independent source material.</param>
    /// <returns>The scene containing one material and one triangle.</returns>
    private static Scene CreateTriangleScene(Material material) {
        var scene = new Scene { RootNode = new Node("Triangle") };
        var mesh = new Mesh("Triangle", PrimitiveType.Triangle) { MaterialIndex = 0 };
        mesh.Vertices.AddRange([new(0, 0, 0), new(1, 0, 0), new(0, 1, 0)]);
        mesh.Faces.Add(new Face([0, 1, 2]));
        scene.Meshes.Add(mesh);
        scene.Materials.Add(material);
        scene.RootNode.MeshIndices.Add(0);
        return scene;
    }
}
