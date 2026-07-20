using Assimp;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Xunit;
using HxExporter = HelixToolkit.SharpDX.Core.Assimp.Exporter;
using HxImporter = HelixToolkit.SharpDX.Core.Assimp.Importer;

namespace SilkAssimp.Tests {
    public class AssimpImportExportTests {
        [Fact]
        [Trait("Category", "Unit")]
        public void EmptySceneProducesEmptyHelixRoot() {
            using var importer = new HxImporter();
            var source = new Scene {RootNode = new Node("Root")};

            var result = importer.ToHelixToolkitScene(source, out var scene);

            Assert.Equal(ErrorCode.Succeed, result);
            Assert.IsType<GroupNode>(scene.Root);
            Assert.Empty(scene.Root.Items);
        }

        [Theory]
        [InlineData(MaterialType.BlinnPhong, typeof(PhongMaterialCore))]
        [InlineData(MaterialType.PBR, typeof(PBRMaterialCore))]
        [InlineData(MaterialType.Diffuse, typeof(DiffuseMaterialCore))]
        [Trait("Category", "Unit")]
        public void InMemoryTriangleImportsRequestedMaterial(MaterialType materialType, Type expectedType) {
            using var importer = new HxImporter {
                Configuration = new ImporterConfiguration {
                    BuildOctree = false,
                    ImportMaterialType = materialType
                }
            };

            var result = importer.ToHelixToolkitScene(CreateTriangleScene(), out var scene);
            var mesh = Assert.Single(scene.Root.Traverse().OfType<MeshNode>());
            var geometry = Assert.IsType<MeshGeometry3D>(mesh.Geometry);

            Assert.Equal(ErrorCode.Succeed, result);
            Assert.IsType(expectedType, mesh.Material);
            Assert.Equal(3, geometry.Positions.Count);
            Assert.Equal(new[] {0, 1, 2}, geometry.Indices);
            Assert.Equal(3, geometry.Normals.Count);
            Assert.Equal(3, geometry.TextureCoordinates.Count);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void NodeAnimationIsImported() {
            var source = CreateTriangleScene();
            var channel = new NodeAnimationChannel {NodeName = "TriangleNode"};
            channel.PositionKeys.Add(new VectorKey(0, new Vector3D()));
            channel.RotationKeys.Add(new QuaternionKey(0, new Quaternion()));
            channel.ScalingKeys.Add(new VectorKey(0, new Vector3D(1, 1, 1)));
            var animation = new Animation {Name = "Move", DurationInTicks = 1, TicksPerSecond = 1};
            animation.NodeAnimationChannels.Add(channel);
            source.Animations.Add(animation);

            using var importer = new HxImporter();
            var result = importer.ToHelixToolkitScene(source, out var scene);

            Assert.Equal(ErrorCode.Succeed, result);
            var imported = Assert.Single(scene.Animations);
            Assert.Equal("Move", imported.Name);
            Assert.NotEmpty(imported.NodeAnimationCollection);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void HelixAssimpHelixRoundTripPreservesStructure() {
            var source = CreateRichTriangleScene();
            using var importer = new HxImporter {
                Configuration = new ImporterConfiguration {BuildOctree = false}
            };
            Assert.Equal(ErrorCode.Succeed, importer.ToHelixToolkitScene(source, out var first));
            var firstTransform = first.Root.Traverse().Single(x => x.Name == "TriangleNode").ModelMatrix;
            var firstMesh = Assert.Single(first.Root.Traverse().OfType<BoneSkinMeshNode>());
            var firstGeometry = Assert.IsType<BoneSkinnedMeshGeometry3D>(firstMesh.Geometry);
            var firstAnimation = Assert.Single(first.Animations);

            using var exporter = new HxExporter();
            Assert.Equal(ErrorCode.Succeed, exporter.ToAssimpScene(first.Root, out var assimp));
            Assert.Single(assimp.Meshes);
            Assert.Single(assimp.Materials);
            Assert.Single(assimp.Meshes[0].Bones);
            // ToAssimpScene converts the root graph; animations are scene-level data.
            assimp.Animations.Add(source.Animations.Single());

            Assert.Equal(ErrorCode.Succeed, importer.ToHelixToolkitScene(assimp, out var second));
            Assert.Single(importer.Animations);
            var secondTransform = second.Root.Traverse().Single(x => x.Name == "TriangleNode").ModelMatrix;
            var secondMesh = Assert.Single(second.Root.Traverse().OfType<BoneSkinMeshNode>());
            var secondGeometry = Assert.IsType<BoneSkinnedMeshGeometry3D>(secondMesh.Geometry);
            var secondAnimation = Assert.Single(second.Animations);

            Assert.Equal(firstTransform, secondTransform);
            Assert.Equal(firstGeometry.Tangents.ToArray(), secondGeometry.Tangents.ToArray());
            Assert.Equal(firstGeometry.BiTangents.ToArray(), secondGeometry.BiTangents.ToArray());
            Assert.Equal(firstGeometry.Colors.ToArray(), secondGeometry.Colors.ToArray());
            Assert.Equal(firstMesh.Bones.Select(x => x.Name), secondMesh.Bones.Select(x => x.Name));
            Assert.Equal(firstGeometry.VertexBoneIds.Select(x => x.Bone1),
                         secondGeometry.VertexBoneIds.Select(x => x.Bone1));
            Assert.Equal(firstGeometry.VertexBoneIds.Select(x => x.Weights),
                         secondGeometry.VertexBoneIds.Select(x => x.Weights));
            Assert.Equal(firstAnimation.Name, secondAnimation.Name);
            Assert.Equal(firstAnimation.NodeAnimationCollection.Single().KeyFrames.Select(x => x.Time),
                         secondAnimation.NodeAnimationCollection.Single().KeyFrames.Select(x => x.Time));
            Assert.Equal(firstAnimation.NodeAnimationCollection.Single().KeyFrames.Select(x => x.Translation),
                         secondAnimation.NodeAnimationCollection.Single().KeyFrames.Select(x => x.Translation));
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

                using var importer = new HxImporter {
                    Configuration = new ImporterConfiguration {
                        BuildOctree = false,
                        ImportMaterialType = MaterialType.BlinnPhong
                    }
                };
                var first = importer.Load(source);
                var firstMesh = Assert.Single(first.Root.Traverse().OfType<MeshNode>());
                Assert.Null(Assert.IsType<PhongMaterialCore>(firstMesh.Material).DiffuseMap);

                var exported = Path.Combine(directory, "exported.obj");
                using var exporter = new HxExporter();
                Assert.Equal(ErrorCode.Succeed, exporter.ExportToFile(exported, first, "obj"));
                Assert.True(File.Exists(exported));

                var second = importer.Load(exported);
                var secondMesh = Assert.Single(second.Root.Traverse().OfType<MeshNode>());
                Assert.Equal(3, secondMesh.Geometry.Positions.Count);
                Assert.Equal(3, secondMesh.Geometry.Indices.Count);
            } finally {
                Directory.Delete(directory, true);
            }
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void UnsupportedStreamFormatReturnsFailureWithoutThrowing() {
            using var importer = new HxImporter();
            using var stream = new MemoryStream(new byte[] {1, 2, 3});

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
                Configuration = new ImporterConfiguration {ExternalContext = context}
            };

            importer.Dispose();

            Assert.NotEmpty(context.GetSupportedImportFormats());
        }

        private static Scene CreateTriangleScene() {
            var source = new Scene {RootNode = new Node("Root")};
            var child = new Node("TriangleNode", source.RootNode);
            source.RootNode.Children.Add(child);

            var mesh = new Mesh("Triangle", PrimitiveType.Triangle) {MaterialIndex = 0};
            mesh.Vertices.AddRange(new[] {
                new Vector3D(0, 0, 0),
                new Vector3D(1, 0, 0),
                new Vector3D(0, 1, 0)
            });
            mesh.Normals.AddRange(Enumerable.Repeat(new Vector3D(0, 0, 1), 3));
            mesh.TextureCoordinateChannels[0] = new List<Vector3D> {
                new(0, 0, 0),
                new(1, 0, 0),
                new(0, 1, 0)
            };
            mesh.Faces.Add(new Face(new[] {0, 1, 2}));
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
            mesh.VertexColorChannels[0] = new List<Color4D> {
                new(1, 0, 0, 1),
                new(0, 1, 0, 1),
                new(0, 0, 1, 1)
            };
            var bone = new Bone {Name = "BoneNode", OffsetMatrix = Matrix4x4.Identity};
            bone.VertexWeights.Add(new VertexWeight(0, 0.75f));
            bone.VertexWeights.Add(new VertexWeight(1, 0.25f));
            mesh.Bones.Add(bone);

            var channel = new NodeAnimationChannel {NodeName = "BoneNode"};
            channel.PositionKeys.Add(new VectorKey(0, new Vector3D(0, 0, 0)));
            channel.PositionKeys.Add(new VectorKey(1, new Vector3D(1, 2, 3)));
            channel.RotationKeys.Add(new QuaternionKey(0, new Quaternion(1, 0, 0, 0)));
            channel.RotationKeys.Add(new QuaternionKey(1, new Quaternion(1, 0, 0, 0)));
            channel.ScalingKeys.Add(new VectorKey(0, new Vector3D(1, 1, 1)));
            channel.ScalingKeys.Add(new VectorKey(1, new Vector3D(1, 1, 1)));
            var animation = new Animation {Name = "BoneMove", DurationInTicks = 1, TicksPerSecond = 1};
            animation.NodeAnimationChannels.Add(channel);
            source.Animations.Add(animation);
            return source;
        }
    }
}
