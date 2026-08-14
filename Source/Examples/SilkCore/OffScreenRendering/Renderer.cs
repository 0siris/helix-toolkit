using System;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Cameras;
using HelixToolkit.SharpDX.Core.Controls;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace OffScreenRendering;

internal class Renderer {
    private readonly ViewportCore viewport = new() { EffectsManager = new DefaultEffectsManager() };
    private readonly Random random = new((int)Stopwatch.GetTimestamp());

    private readonly DirectionalLightNode lightNode = new() { Direction = new Vector3(-1, -1, 0), Color = new Color4(1, 1, 1, 1) };

    private GroupNode currentScene;

    public Renderer() {
        viewport.CameraCore = new OrthographicCameraCore() {
            LookDirection = new Vector3(-100, 0, 0),
            UpDirection = new Vector3(0, 1, 0),
            Position = new Vector3(100, 0, 0),
            Width = 100,
            NearPlaneDistance = 0.1f,
            FarPlaneDistance = 500
        };
        viewport.FxaaLevel = FxaaLevel.Medium;
        viewport.ShowViewCube = false;
        viewport.Items.AddChildNode(lightNode);
        viewport.StartD3D(100, 100);
    }

    /// <summary>
    /// Must be called from same thread created the viewport object.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void Resize(int width, int height) {
        viewport.Resize(width, height);
    }

    /// <summary>
    /// Can be called by different thread.
    /// </summary>
    /// <returns></returns>
    public BitmapSource Render() {
        lock (viewport) {
            // Remove existing scene and create an new scene
            currentScene?.RemoveSelf();
            currentScene = new GroupNode();
            GenerateSomeMesh(currentScene);
            viewport.Items.AddChildNode(currentScene);
            viewport.Render();
            using var bitmapStream = viewport.RenderToBitmapStream();
            bitmapStream.Position = 0;
            var frame = BitmapFrame.Create(bitmapStream,
                                           BitmapCreateOptions.IgnoreImageCache,
                                           BitmapCacheOption.OnLoad);
            return frame;
        }
    }

    private void GenerateSomeMesh(GroupNode root) {
        var builder = new MeshBuilder();
        builder.AddSphere(Vector3.Zero, 5);
        var mesh = builder.ToMesh();
        var numSphere = random.Next(50, 100);
        for (int i = 0; i < numSphere; ++i) {
            var meshNode = new MeshNode() {
                Geometry = mesh,
                Material = new PhongMaterialCore() { DiffuseColor = random.NextColor() },
                ModelMatrix = Translation(random.NextVector3(new Vector3(-50, -50, -50), new Vector3(50, 50, 50)))
            };
            root.AddChildNode(meshNode);
        }
    }

    private static Matrix Translation(Vector3 value) {
        var matrix = Matrix.Identity;
        matrix.M41 = value.X;
        matrix.M42 = value.Y;
        matrix.M43 = value.Z;
        return matrix;
    }
}

internal static class RandomExtensions {
    public static Color4 NextColor(this Random random) => new((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), 1);

    public static Vector3 NextVector3(this Random random, Vector3 min, Vector3 max) => new(NextFloat(random, min.X, max.X),
        NextFloat(random, min.Y, max.Y),
        NextFloat(random, min.Z, max.Z));

    private static float NextFloat(Random random, float min, float max) => min + (max - min) * (float)random.NextDouble();
}
