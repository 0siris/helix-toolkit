// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Element3D;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Materials;

namespace InstancingDemo;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using DemoCore;
using Color = System.Windows.Media.Color;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Colors = System.Windows.Media.Colors;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public MeshGeometry3D Model { get; private set; }
    public LineGeometry3D Lines { get; private set; }
    public LineGeometry3D Grid { get; private set; } = new();
    public Matrix[] ModelInstances { get; private set; } = [];

    public Matrix[] SelectedLineInstances { private set; get; } = [];

    public InstanceParameter[] InstanceParam { get; private set; } = [];

    public BillboardSingleImage3D? BillboardModel { private set; get; }
    public Matrix[] BillboardInstances { private set; get; } = [];

    public BillboardInstanceParameter[] BillboardInstanceParams { private set; get; } = [];

    public PhongMaterial ModelMaterial { get; private set; }
    public Transform3D ModelTransform { get; private set; }

    public Vector3D DirectionalLightDirection { get; private set; }
    public Color DirectionalLightColor { get; private set; }
    public Color AmbientLightColor { get; private set; }
    public TextureModel? Texture { private set; get; }
    public bool EnableAnimation { set; get; }

    private DispatcherTimer timer = new();
    private Random rnd = new();
    private float aniX;
    private float aniY;
    private float aniZ;
    private bool aniDir = true;

    public MainViewModel() {
        Title = "Instancing Demo";
        EffectsManager = new DefaultEffectsManager();
        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(40, 40, 40),
            LookDirection = new Vector3D(-40, -40, -40),
            UpDirection = new Vector3D(0, 1, 0)
        };

        // setup lighting
        AmbientLightColor = Colors.DarkGray;
        DirectionalLightColor = Colors.White;
        DirectionalLightDirection = new Vector3D(-2, -5, -2);

        // scene model3d
        var b1 = new MeshBuilder(true, true, true);
        b1.AddBox(new Vector3(0, 0, 0), 1, 1, 1, BoxFaces.All);
        Model = b1.ToMeshGeometry3D();
        if (Model.TextureCoordinates is { } textureCoordinates) {
            for (var i = 0; i < textureCoordinates.Count; ++i) {
                var tex = textureCoordinates[i];
                textureCoordinates[i] = new Vector2(tex.X * 0.5f, tex.Y * 0.5f);
            }
        }

        var l1 = new LineBuilder();
        l1.AddBox(new Vector3(0, 0, 0), 1.1, 1.1, 1.1);
        Lines = l1.ToLineGeometry3D();
        Lines.Colors = [.. Enumerable.Repeat(Colors.White.ToColor4(), Lines.Positions?.Count ?? 0)];
        // model trafo
        ModelTransform =
            Transform3D
                .Identity; // new Media3D.RotateTransform3D(new Media3D.AxisAngleRotation3D(new Vector3D(0, 0, 1), 45));

        // model material
        ModelMaterial = PhongMaterials.White;
        var diffuseMap = TextureModel.Create(
                             new Uri(@"TextureCheckerboard2.jpg", UriKind.RelativeOrAbsolute).ToString())
                         ?? throw new InvalidOperationException("The instancing diffuse texture is required.");
        ModelMaterial.DiffuseMap = diffuseMap;
        ModelMaterial.NormalMap =
            TextureModel.Create(new Uri(@"TextureCheckerboard2_dot3.jpg", UriKind.RelativeOrAbsolute)
                .ToString());

        BillboardModel = new BillboardSingleImage3D(diffuseMap, 20, 20);
        Texture = TextureModel.Create("Cubemap_Grandcanyon.dds");
        CreateModels();
        timer.Interval = TimeSpan.FromMilliseconds(30);
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private void Timer_Tick(object? sender, EventArgs e) {
        if (!EnableAnimation) {
            return;
        }

        CreateModels();
    }

    private const int Num = 40;
    private List<Matrix> instances = new(Num * 2);
    private List<InstanceParameter> parameters = new(Num * 2);

    private List<Matrix> billboardinstances = new(Num * 2);
    private List<BillboardInstanceParameter> billboardParams = new(Num * 2);

    private void CreateModels() {
        instances.Clear();
        parameters.Clear();

        if (aniDir) {
            aniX += 0.1f;
            aniY += 0.2f;
            aniZ += 0.3f;
        } else {
            aniX -= 0.1f;
            aniY -= 0.2f;
            aniZ -= 0.3f;
        }

        if (aniX > 15) {
            aniDir = false;
        } else if (aniX < -15) {
            aniDir = true;
        }

        for (var i = -Num - (int) aniX; i < Num + aniX; i++) {
            for (var j = -Num - (int) aniX; j < Num + aniX; j++) {
                var matrix = RotationAxis(new Vector3(0, 1, 0), aniX * Math.Sign(j))
                             * Translation(new Vector3(i * 1.2f + Math.Sign(i), j * 1.2f + Math.Sign(j), i * j / 2.0f));
                var color = new Color4(1,
                    1,
                    1,
                    1); //new Color4((float)Math.Abs(i) / num, (float)Math.Abs(j) / num, (float)Math.Abs(i + j) / (2 * num), 1);
                //  var emissiveColor = new Color4( rnd.NextFloat(0,1) , rnd.NextFloat(0, 1), rnd.NextFloat(0, 1), rnd.NextFloat(0, 0.2f));
                var k = Math.Abs(i + j) % 4;
                Vector2 offset;
                if (k == 0) {
                    offset = new Vector2(aniX, 0);
                } else if (k == 1) {
                    offset = new Vector2(0.5f + aniX, 0);
                } else if (k == 2) {
                    offset = new Vector2(0.5f + aniX, 0.5f);
                } else {
                    offset = new Vector2(aniX, 0.5f);
                }

                parameters.Add(new InstanceParameter() {
                    DiffuseColor = color,
                    TexCoordOffset = offset
                });
                instances.Add(matrix);
            }
        }

        InstanceParam = [.. parameters];
        ModelInstances = [.. instances];
        SubTitle = "Number of Instances: " + parameters.Count.ToString();

        if (billboardinstances.Count == 0) {
            for (var i = 0; i < 2 * Num; ++i) {
                billboardParams.Add(new BillboardInstanceParameter() {
                    TexCoordOffset = new Vector2(1f / 6 * rnd.Next(0, 6), 1f / 6 * rnd.Next(0, 6)),
                    TexCoordScale = new Vector2(1f / 6, 1f / 6)
                });
                billboardinstances.Add(Scaling(NextFloat(0.5f, 4f), NextFloat(0.5f, 3f), NextFloat(0.5f, 3f))
                                       * Translation(
                                           new Vector3(NextFloat(0, 100), NextFloat(0, 100), NextFloat(-50, 50))));
            }

            BillboardInstanceParams = [.. billboardParams];
            BillboardInstances = [.. billboardinstances];
        } else {
            for (var i = 0; i < billboardinstances.Count; ++i) {
                var current = billboardinstances[i];
                current.M41 += i % 3 == 0
                    ? aniX / 50
                    : -aniX / 50;
                current.M42 += i % 4 == 0
                    ? aniY / 50
                    : -aniY / 30;
                current.M43 += i % 5 == 0
                    ? aniZ / 100
                    : -aniZ / 50;
                billboardinstances[i] = current;
            }

            BillboardInstances = [.. billboardinstances];
        }
    }

    public void OnMouseLeftButtonDownHandler(object sender, System.Windows.Input.MouseButtonEventArgs e) {
        if (EnableAnimation) {
            return;
        }

        var viewport = sender as Viewport3DX;
        if (viewport == null) {
            return;
        }

        var point = e.GetPosition(viewport);
        var hitTests = viewport.FindHits(point);
        if (hitTests.Count > 0) {
            foreach (var hit in hitTests) {
                if (hit is {ModelHit: InstancingMeshGeometryModel3D, Tag: int index}) {
                    InstanceParam[index].EmissiveColor = InstanceParam[index].EmissiveColor != Colors.Yellow.ToColor4()
                        ? Colors.Yellow.ToColor4()
                        : Colors.Black.ToColor4();
                    InstanceParam = (InstanceParameter[]) InstanceParam.Clone();
                    break;
                } else if (hit is {ModelHit: LineGeometryModel3D, Tag: int lineIndex}) {
                    SelectedLineInstances = [ModelInstances[lineIndex]];
                    break;
                }
            }
        }
    }

    protected override void Dispose(bool disposing) {
        timer.Stop();
        timer.Tick -= Timer_Tick;
        base.Dispose(disposing);
    }

    private static Matrix RotationAxis(Vector3 axis, float angle) {
        var m = System.Numerics.Matrix4x4.CreateFromAxisAngle(new System.Numerics.Vector3(axis.X, axis.Y, axis.Z),
            angle);
        return ToMatrix(m);
    }

    private static Matrix Scaling(float x, float y, float z) {
        var m = System.Numerics.Matrix4x4.CreateScale(x, y, z);
        return ToMatrix(m);
    }

    private static Matrix Translation(Vector3 value) {
        var m = System.Numerics.Matrix4x4.CreateTranslation(value.X, value.Y, value.Z);
        return ToMatrix(m);
    }

    private static Matrix ToMatrix(System.Numerics.Matrix4x4 m) => new(m.M11,
        m.M12,
        m.M13,
        m.M14,
        m.M21,
        m.M22,
        m.M23,
        m.M24,
        m.M31,
        m.M32,
        m.M33,
        m.M34,
        m.M41,
        m.M42,
        m.M43,
        m.M44);

    private float NextFloat(float min, float max) => min + (max - min) * (float) rnd.NextDouble();
}