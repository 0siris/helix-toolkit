extern alias WpfAssimpAssembly;

using HelixToolkit.Wpf.SharpDX.Model;
using HelixToolkit.Wpf.SharpDX.Model.Scene;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AssimpErrorCode = WpfAssimpAssembly::HelixToolkit.Wpf.SharpDX.Assimp.ErrorCode;
using HelixExporter = WpfAssimpAssembly::HelixToolkit.Wpf.SharpDX.Assimp.Exporter;
using Importer = WpfAssimpAssembly::HelixToolkit.Wpf.SharpDX.Assimp.Importer;
using ImporterConfiguration = WpfAssimpAssembly::HelixToolkit.Wpf.SharpDX.Assimp.ImporterConfiguration;
using MaterialType = WpfAssimpAssembly::HelixToolkit.Wpf.SharpDX.Assimp.MaterialType;

namespace HelixToolkit.Wpf.SharpDX.Tests.Importers
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class AssimpImportExportTests
    {
        [Test]
        public void ImportsAndExportsTexturedObj()
        {
            var directory = Path.Combine(Path.GetTempPath(), $"HelixAssimp-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            try
            {
                var source = CreateTexturedObj(directory);
                using var importer = new Importer
                {
                    Configuration = new ImporterConfiguration
                    {
                        ImportMaterialType = MaterialType.BlinnPhong
                    }
                };

                var scene = importer.Load(source);
                Assert.That(scene?.Root, Is.Not.Null);
                var mesh = scene.Root.Traverse().OfType<MeshNode>().Single();
                Assert.That(mesh.Geometry.Positions, Has.Count.EqualTo(3));
                Assert.That(mesh.Material, Is.TypeOf<PhongMaterialCore>());
                Assert.That(((PhongMaterialCore)mesh.Material).DiffuseMap, Is.Not.Null);

                var exported = Path.Combine(directory, "exported.obj");
                using var exporter = new HelixExporter();
                Assert.That(exporter.ExportToFile(exported, scene, "obj"), Is.EqualTo(AssimpErrorCode.Succeed));
                Assert.That(File.Exists(exported), Is.True);

                var roundTrip = importer.Load(exported);
                Assert.That(roundTrip?.Root.Traverse().OfType<MeshNode>(), Is.Not.Empty);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public void ImportsNodeAnimation()
        {
            var source = new global::Assimp.Scene
            {
                RootNode = new global::Assimp.Node("AnimatedNode")
            };
            var mesh = new global::Assimp.Mesh("Triangle", global::Assimp.PrimitiveType.Triangle)
            {
                MaterialIndex = 0
            };
            mesh.Vertices.AddRange(new[]
            {
                new global::Assimp.Vector3D(0, 0, 0),
                new global::Assimp.Vector3D(1, 0, 0),
                new global::Assimp.Vector3D(0, 1, 0)
            });
            mesh.Faces.Add(new global::Assimp.Face(new[] { 0, 1, 2 }));
            source.Meshes.Add(mesh);
            source.Materials.Add(new global::Assimp.Material { Name = "Material" });
            source.RootNode.MeshIndices.Add(0);

            var channel = new global::Assimp.NodeAnimationChannel { NodeName = "AnimatedNode" };
            channel.PositionKeys.Add(new global::Assimp.VectorKey(0, new global::Assimp.Vector3D()));
            channel.RotationKeys.Add(new global::Assimp.QuaternionKey(0, new global::Assimp.Quaternion()));
            channel.ScalingKeys.Add(new global::Assimp.VectorKey(0, new global::Assimp.Vector3D(1, 1, 1)));
            var animation = new global::Assimp.Animation
            {
                Name = "Move",
                DurationInTicks = 1,
                TicksPerSecond = 1
            };
            animation.NodeAnimationChannels.Add(channel);
            source.Animations.Add(animation);

            using var importer = new Importer();
            Assert.That(importer.ToHelixToolkitScene(source, out var scene), Is.EqualTo(AssimpErrorCode.Succeed));
            Assert.That(scene.HasAnimation, Is.True);
            Assert.That(scene.Animations.Single().Name, Is.EqualTo("Move"));
            Assert.That(scene.Animations.Single().NodeAnimationCollection, Is.Not.Empty);
        }

        private static string CreateTexturedObj(string directory)
        {
            var texture = Path.Combine(directory, "diffuse.png");
            var bitmap = BitmapSource.Create(
                1,
                1,
                96,
                96,
                PixelFormats.Bgra32,
                null,
                new byte[] { 0, 0, 255, 255 },
                4);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(texture))
            {
                encoder.Save(stream);
            }

            File.WriteAllText(Path.Combine(directory, "model.mtl"),
                "newmtl Material\nKd 1 1 1\nmap_Kd diffuse.png\n");
            var model = Path.Combine(directory, "model.obj");
            File.WriteAllText(model,
                "mtllib model.mtl\n" +
                "o Triangle\n" +
                "v 0 0 0\nv 1 0 0\nv 0 1 0\n" +
                "vt 0 0\nvt 1 0\nvt 0 1\n" +
                "usemtl Material\nf 1/1 2/2 3/3\n");
            return model;
        }
    }
}
