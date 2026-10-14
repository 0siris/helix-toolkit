using Assimp;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Utilities;
using Xunit;
using HxExporter = HelixToolkit.SharpDX.Core.Assimp.Exporter;
using HxImporter = HelixToolkit.SharpDX.Core.Assimp.Importer;

namespace SilkAssimp.Tests;

/// <summary>Verifies actual Assimp scene conversion, hierarchy, geometry and interchange roundtrips.</summary>
public class AssimpImportExportTests {
    /// <summary>Skipped geometry and shared geometry with distinct materials cannot leave invalid exported mesh indices.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ExportedMeshIndicesRemainContiguousAfterSkippedGeometryTest() {
        var builder = new HelixToolkit.SharpDX.Core.Geometry.MeshBuilder();
        builder.AddTriangle(new(0, 0, 0), new(1, 0, 0), new(0, 1, 0));
        using var root = new GroupNode();
        using var skipped = new MeshNode { Name = "No Material", Geometry = builder.ToMeshGeometry3D() };
        var shared = builder.ToMeshGeometry3D();
        using var first = new MeshNode { Name = "First", Geometry = shared, Material = new PhongMaterialCore() };
        using var second = new MeshNode { Name = "Second", Geometry = shared, Material = new PhongMaterialCore() };
        root.AddChildNode(skipped);
        root.AddChildNode(first);
        root.AddChildNode(second);
        using var exporter = new HxExporter();
        var result = exporter.ToAssimpScene(root, out var scene);
        Assert.Equal(ErrorCode.Succeed, result);
        Assert.Equal(2, scene!.MeshCount);
        var indices = scene.RootNode.Children.SelectMany(child => child.MeshIndices).ToArray();
        Assert.Equal(new[] { 0, 1 }, indices);
        Assert.Empty(scene.RootNode.Children[0].MeshIndices);
    }
    /// <summary>Direct mesh children retain their own local transforms below a nonidentity parent.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void DirectMeshChildRetainsTransformInExportedHierarchyTest() {
        using var root = new GroupNode {
            Name = "Parent", ModelMatrix = new(0, 1, 0, 0, -1, 0, 0, 0, 0, 0, 1, 0, 11, -2, 7, 1)
        };
        var builder = new HelixToolkit.SharpDX.Core.Geometry.MeshBuilder();
        builder.AddTriangle(new(0, 0, 0), new(1, 0, 0), new(0, 1, 0));
        using var mesh = new MeshNode {
            Name = "Child", Geometry = builder.ToMeshGeometry3D(), Material = new PhongMaterialCore(),
            ModelMatrix = new(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 3, 4, 5, 1)
        };
        root.AddChildNode(mesh);
        using var exporter = new HxExporter();
        var result = exporter.ToAssimpScene(root, out var exported);
        Assert.Equal(ErrorCode.Succeed, result);
        var child = Assert.Single(exported!.RootNode.Children);
        Assert.Equal("Child", child.Name);
        Assert.Single(child.MeshIndices);
        Assert.Empty(exported.RootNode.MeshIndices);
        Assert.Equal(mesh.ModelMatrix.ToAssimpMatrix(true), child.Transform);
        Assert.Equal(root.ModelMatrix.ToAssimpMatrix(true), exported.RootNode.Transform);
        Assert.Same(root, mesh.Parent);
    }
    [Fact]
    [Trait("Category", "Unit")]
    public void EmptySceneProducesEmptyHelixRoot() {
        using var importer = new HxImporter();
        var source = new Scene {
            RootNode = new Node("Root")
        };

        var result = importer.ToHelixToolkitScene(source, out var scene);

        Assert.Equal(ErrorCode.Succeed, result);
        var importedScene = scene ?? throw new InvalidOperationException("A successful import must return a scene.");
        Assert.IsType<GroupNode>(importedScene.Root);
        Assert.Empty(importedScene.Root.Items);
    }

    private static readonly int[] expected = [0, 1, 2];

    [Theory]
    [InlineData(MaterialType.BlinnPhong, typeof(PhongMaterialCore))]
    [InlineData(MaterialType.Pbr, typeof(PbrMaterialCore))]
    [InlineData(MaterialType.Diffuse, typeof(DiffuseMaterialCore))]
    [Trait("Category", "Unit")]
    public void InMemoryTriangleImportsRequestedMaterial(MaterialType materialType, Type expectedType) {
        var config = new ImporterConfiguration {
            BuildOctree = false,
            ImportMaterialType = materialType
        };
        using var importer = new HxImporter();
        importer.Configuration = config;

        var result = importer.ToHelixToolkitScene(CreateTriangleScene(), out var scene);
        Assert.Equal(ErrorCode.Succeed, result);
        var importedScene = scene ?? throw new InvalidOperationException("A successful import must return a scene.");
        var mesh = Assert.Single(importedScene.Root.Traverse()
            .OfType<MeshNode>());
        var geometry = Assert.IsType<MeshGeometry3D>(mesh.Geometry);

        Assert.IsType(expectedType, mesh.Material);
        Assert.Equal(3, (geometry.Positions ?? throw new InvalidOperationException()).Count);
        Assert.Equal(expected, geometry.Indices ?? throw new InvalidOperationException());
        Assert.Equal(3, (geometry.Normals ?? throw new InvalidOperationException()).Count);
        Assert.Equal(3, (geometry.TextureCoordinates ?? throw new InvalidOperationException()).Count);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void NodeAnimationIsImported() {
        var source = CreateTriangleScene();
        var channel = new NodeAnimationChannel {
            NodeName = "TriangleNode"
        };
        channel.PositionKeys.Add(new VectorKey(0, new Vector3D()));
        channel.RotationKeys.Add(new QuaternionKey(0, new Quaternion()));
        channel.ScalingKeys.Add(new VectorKey(0, new Vector3D(1, 1, 1)));
        var animation = new Animation {
            Name = "Move",
            DurationInTicks = 1,
            TicksPerSecond = 1
        };
        animation.NodeAnimationChannels.Add(channel);
        source.Animations.Add(animation);

        using var importer = new HxImporter();
        var result = importer.ToHelixToolkitScene(source, out var scene);

        Assert.Equal(ErrorCode.Succeed, result);
        var importedScene = scene ?? throw new InvalidOperationException("A successful import must return a scene.");
        var imported = Assert.Single(importedScene.Animations);
        Assert.Equal("Move", imported.Name);
        Assert.NotEmpty(imported.NodeAnimationCollection);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void HelixAssimpHelixRoundTripPreservesStructure() {
        var source = CreateRichTriangleScene();
        using var importer = new HxImporter();
        importer.Configuration = new ImporterConfiguration {
            BuildOctree = false
        };
        Assert.Equal(ErrorCode.Succeed, importer.ToHelixToolkitScene(source, out var first));
        var firstScene = first ?? throw new InvalidOperationException("A successful import must return a scene.");
        var firstTransform = firstScene.Root.Traverse()
            .Single(x => x.Name == "TriangleNode")
            .ModelMatrix;
        var firstMesh = Assert.Single(firstScene.Root.Traverse()
            .OfType<BoneSkinMeshNode>());
        var firstGeometry = Assert.IsType<BoneSkinnedMeshGeometry3D>(firstMesh.Geometry);
        var firstAnimation = Assert.Single(firstScene.Animations);

        using var exporter = new HxExporter();
        Assert.Equal(ErrorCode.Succeed, exporter.ToAssimpScene(firstScene.Root, out var assimp));
        var assimpScene = assimp ?? throw new InvalidOperationException("A successful export must return a scene.");
        Assert.Single(assimpScene.Meshes);
        Assert.Single(assimpScene.Materials);
        Assert.Single(assimpScene.Meshes[0].Bones);
        // ToAssimpScene converts the root graph; animations are scene-level data.
        assimpScene.Animations.Add(source.Animations.Single());

        Assert.Equal(ErrorCode.Succeed, importer.ToHelixToolkitScene(assimpScene, out var second));
        Assert.Single(importer.Animations);
        var secondScene = second ?? throw new InvalidOperationException("A successful import must return a scene.");
        var secondTransform = secondScene.Root.Traverse()
            .Single(x => x.Name == "TriangleNode")
            .ModelMatrix;
        var secondMesh = Assert.Single(secondScene.Root.Traverse()
            .OfType<BoneSkinMeshNode>());
        var secondGeometry = Assert.IsType<BoneSkinnedMeshGeometry3D>(secondMesh.Geometry);
        var secondAnimation = Assert.Single(secondScene.Animations);

        Assert.Equal(firstTransform, secondTransform);
        var firstTangents = firstGeometry.Tangents ?? throw new InvalidOperationException();
        var secondTangents = secondGeometry.Tangents ?? throw new InvalidOperationException();
        var firstBiTangents = firstGeometry.BiTangents ?? throw new InvalidOperationException();
        var secondBiTangents = secondGeometry.BiTangents ?? throw new InvalidOperationException();
        var firstColors = firstGeometry.Colors ?? throw new InvalidOperationException();
        var secondColors = secondGeometry.Colors ?? throw new InvalidOperationException();
        var firstBones = firstMesh.Bones ?? throw new InvalidOperationException();
        var secondBones = secondMesh.Bones ?? throw new InvalidOperationException();
        var firstBoneIds = firstGeometry.VertexBoneIds ?? throw new InvalidOperationException();
        var secondBoneIds = secondGeometry.VertexBoneIds ?? throw new InvalidOperationException();
        Assert.Equal(firstTangents.ToArray(), secondTangents.ToArray());
        Assert.Equal(firstBiTangents.ToArray(), secondBiTangents.ToArray());
        Assert.Equal(firstColors.ToArray(), secondColors.ToArray());
        Assert.Equal(firstBones.Select(x => x.Name), secondBones.Select(x => x.Name));
        Assert.Equal(firstBoneIds.Select(x => x.Bone1), secondBoneIds.Select(x => x.Bone1));
        Assert.Equal(firstBoneIds.Select(x => x.Weights), secondBoneIds.Select(x => x.Weights));
        Assert.Equal(firstAnimation.Name, secondAnimation.Name);
        Assert.Equal(firstAnimation.NodeAnimationCollection.Single()
                .KeyFrames.Select(x => x.Time),
            secondAnimation.NodeAnimationCollection.Single()
                .KeyFrames.Select(x => x.Time));
        Assert.Equal(firstAnimation.NodeAnimationCollection.Single()
                .KeyFrames.Select(x => x.Translation),
            secondAnimation.NodeAnimationCollection.Single()
                .KeyFrames.Select(x => x.Translation));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void LocalObjRoundTripPreservesMeshAndHandlesMissingTexture() {
        var directory = Path.Combine(Path.GetTempPath(), $"SilkAssimp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try {
            var source = Path.Combine(directory, "triangle.obj");
            File.WriteAllText(Path.Combine(directory, "triangle.mtl"),
                "newmtl Material\nKd 0.25 0.5 0.75\nmap_Kd missing.png\n");
            File.WriteAllText(source,
                "mtllib triangle.mtl\no Triangle\n"
                + "v 0 0 0\nv 1 0 0\nv 0 1 0\n"
                + "vt 0 0\nvt 1 0\nvt 0 1\n"
                + "usemtl Material\nf 1/1 2/2 3/3\n");

            using var importer = new HxImporter();
            importer.Configuration = new ImporterConfiguration {
                BuildOctree = false,
                ImportMaterialType = MaterialType.BlinnPhong
            };
            var first = importer.Load(source)
                        ?? throw new InvalidOperationException("A successful import must return a scene.");
            var firstMesh = Assert.Single(first.Root.Traverse()
                .OfType<MeshNode>());
            Assert.Null(Assert.IsType<PhongMaterialCore>(firstMesh.Material)
                .DiffuseMap);

            var exported = Path.Combine(directory, "exported.obj");
            using var exporter = new HxExporter();
            Assert.Equal(ErrorCode.Succeed, exporter.ExportToFile(exported, first, "obj"));
            Assert.True(File.Exists(exported));

            var second = importer.Load(exported)
                         ?? throw new InvalidOperationException("A successful import must return a scene.");
            var secondMesh = Assert.Single(second.Root.Traverse()
                .OfType<MeshNode>());
            Assert.Equal(3, (secondMesh.Geometry?.Positions ?? throw new InvalidOperationException()).Count);
            Assert.Equal(3, (secondMesh.Geometry.Indices ?? throw new InvalidOperationException()).Count);
        } finally {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void UnsupportedStreamFormatReturnsFailureWithoutThrowing() {
        using var importer = new HxImporter();
        using var stream = new MemoryStream([1, 2, 3]);

        var result = importer.Load(stream, "fixture.invalid", "not-a-format", out var scene);

        Assert.Null(scene);
        Assert.True(result.HasFlag(ErrorCode.Failed));
        Assert.True(result.HasFlag(ErrorCode.FileTypeNotSupported));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void MissingFileReportsFailureAndRaisesEvent() {
        using var importer = new HxImporter();
        Exception? reported = null;
        importer.AssimpExceptionOccurred += (_, exception) => reported = exception;

        var result = importer.Load(Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.obj"),
            out var scene);

        Assert.Equal(ErrorCode.Failed, result);
        Assert.Null(scene);
        Assert.NotNull(reported);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ImporterAndExporterDisposeAreIdempotent() {
        var importer = new HxImporter();
        var exporter = new HxExporter();

        importer.Dispose();
        importer.Dispose();
        exporter.Dispose();
        exporter.Dispose();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ExternalContextRemainsOwnedByCaller() {
        using var context = new AssimpContext();
        var importer = new HxImporter {
            Configuration = new ImporterConfiguration {
                ExternalContext = context
            }
        };

        importer.Dispose();

        Assert.NotEmpty(context.GetSupportedImportFormats());
    }

    private static Scene CreateTriangleScene() {
        var source = new Scene {
            RootNode = new Node("Root")
        };
        var child = new Node("TriangleNode", source.RootNode);
        source.RootNode.Children.Add(child);

        var mesh = new Mesh("Triangle", PrimitiveType.Triangle) {
            MaterialIndex = 0
        };
        mesh.Vertices.AddRange([
            new Vector3D(0, 0, 0),
            new Vector3D(1, 0, 0),
            new Vector3D(0, 1, 0)
        ]);
        mesh.Normals.AddRange(Enumerable.Repeat(new Vector3D(0, 0, 1), 3));
        mesh.TextureCoordinateChannels[0] = [
            new(0, 0, 0),
            new(1, 0, 0),
            new(0, 1, 0)
        ];
        mesh.Faces.Add(new Face([0, 1, 2]));
        source.Meshes.Add(mesh);
        source.Materials.Add(new Material {
            Name = "Material",
            ShadingMode = ShadingMode.Blinn,
            ColorDiffuse = new Color4D(0.25f, 0.5f, 0.75f, 1)
        });
        child.MeshIndices.Add(0);
        return source;
    }

    private static Scene CreateRichTriangleScene() {
        var source = CreateTriangleScene();
        var triangleNode = source.RootNode.Children.Single();
        triangleNode.Transform = new Matrix4x4(1,
            0,
            0,
            2,
            0,
            1,
            0,
            3,
            0,
            0,
            1,
            4,
            0,
            0,
            0,
            1);
        triangleNode.Children.Add(new Node("BoneNode", triangleNode));

        var mesh = source.Meshes.Single();
        mesh.Tangents.AddRange(Enumerable.Repeat(new Vector3D(1, 0, 0), 3));
        mesh.BiTangents.AddRange(Enumerable.Repeat(new Vector3D(0, 1, 0), 3));
        mesh.VertexColorChannels[0] = [
            new(1, 0, 0, 1),
            new(0, 1, 0, 1),
            new(0, 0, 1, 1)
        ];
        var bone = new Bone {
            Name = "BoneNode",
            OffsetMatrix = Matrix4x4.Identity
        };
        bone.VertexWeights.Add(new VertexWeight(0, 0.75f));
        bone.VertexWeights.Add(new VertexWeight(1, 0.25f));
        mesh.Bones.Add(bone);

        var channel = new NodeAnimationChannel {
            NodeName = "BoneNode"
        };
        channel.PositionKeys.Add(new VectorKey(0, new Vector3D(0, 0, 0)));
        channel.PositionKeys.Add(new VectorKey(1, new Vector3D(1, 2, 3)));
        channel.RotationKeys.Add(new QuaternionKey(0, new Quaternion(1, 0, 0, 0)));
        channel.RotationKeys.Add(new QuaternionKey(1, new Quaternion(1, 0, 0, 0)));
        channel.ScalingKeys.Add(new VectorKey(0, new Vector3D(1, 1, 1)));
        channel.ScalingKeys.Add(new VectorKey(1, new Vector3D(1, 1, 1)));
        var animation = new Animation {
            Name = "BoneMove",
            DurationInTicks = 1,
            TicksPerSecond = 1
        };
        animation.NodeAnimationChannels.Add(channel);
        source.Animations.Add(animation);
        return source;
    }
}
