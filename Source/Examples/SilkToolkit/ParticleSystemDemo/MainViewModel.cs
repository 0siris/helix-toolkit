using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DemoCore;
using HelixToolkit.Wpf.SharpDX;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Media = System.Windows.Media;
using Media3D = System.Windows.Media.Media3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace ParticleSystemDemo;

public class MainViewModel : BaseViewModel {
    public MeshGeometry3D Model { private set; get; }

    public Media3D.Transform3D EmitterTransform {
        get { return field; }
        set {
            SetValue(ref field, value);
            EmitterLocation = new Media3D.Point3D(value.Value.OffsetX, value.Value.OffsetY, value.Value.OffsetZ);
        }
    } = new Media3D.MatrixTransform3D(new Media3D.Matrix3D(0.5, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0.5, 0, 0, -4, 0, 1));

    public double EmitterRadius {
        set {
            if (SetValue(ref field, value)) {
                var matrix = EmitterTransform.Value;
                matrix.M11 = matrix.M22 = matrix.M33 = value;
                EmitterTransform = new Media3D.MatrixTransform3D(matrix);
            }
        }
        get { return field; }
    } = 0.5;

    public Media3D.Point3D EmitterLocation {
        set { SetValue(ref field, value); }
        get { return field; }
    } = new Media3D.Point3D(0, -4, 0);


    public Media3D.Transform3D ConsumerTransform {
        get { return field; }
        set {
            SetValue(ref field, value);
            ConsumerLocation = new Media3D.Point3D(value.Value.OffsetX, value.Value.OffsetY, value.Value.OffsetZ);
        }
    } = new Media3D.MatrixTransform3D(new Media3D.Matrix3D(0.5, 0, 0, 0, 0, 0.5, 0, 0, 0, 0, 0.5, 0, 0, 4, 0, 1));

    public Media3D.Point3D ConsumerLocation {
        set { SetValue(ref field, value); }
        get { return field; }
    } = new Media3D.Point3D(0, 4, 0);

    public double ConsumerRadius {
        set {
            if (SetValue(ref field, value)) {
                var matrix = ConsumerTransform.Value;
                matrix.M11 = matrix.M22 = matrix.M33 = value;
                ConsumerTransform = new Media3D.MatrixTransform3D(matrix);
            }
        }
        get { return field; }
    } = 0.5;

    public Material EmitterMaterial { get; } = new PhongMaterial() { DiffuseColor = new Color4(1, 0, 1, 1) };

    public Material ConsumerMaterial { get; } = new PhongMaterial() { DiffuseColor = new Color4(0.5f, 1f, 0.5f, 1) };

    public Stream ParticleTexture {
        set { SetValue(ref field, value); }
        get { return field; }
    }

    public Media3D.Vector3D Acceleration {
        set { SetValue(ref field, value); }
        get { return field; }
    } = new Media3D.Vector3D(0, 1, 0);

    public int AccelerationX {
        set {
            if (SetValue(ref field, value)) {
                UpdateAcceleration();
            }
        }
        get { return field; }
    } = 0;

    public Size ParticleSize {
        set { SetValue(ref field, value); }
        get { return field; }
    } = new Size(0.1, 0.1);

    public int SizeSlider {
        set {
            if (SetValue(ref field, value)) {
                ParticleSize = new Size(((double)value) / 100, ((double)value) / 100);
            }
        }
        get { return field; }
    } = 10;

    public int AccelerationY {
        set {
            if (SetValue(ref field, value)) {
                UpdateAcceleration();
            }
        }
        get { return field; }
    } = 100;

    public int AccelerationZ {
        set {
            if (SetValue(ref field, value)) {
                UpdateAcceleration();
            }
        }
        get { return field; }
    } = 0;

    private const int DefaultBoundScale = 10;
    public LineGeometry3D BoundingLines { private set; get; }

    public Media3D.ScaleTransform3D BoundingLineTransform { private set; get; } =
        new Media3D.ScaleTransform3D(DefaultBoundScale, DefaultBoundScale, DefaultBoundScale);

    public Media3D.Rect3D ParticleBounds {
        set { SetValue(ref field, value); }
        get { return field; }
    } = new Media3D.Rect3D(0, 0, 0, DefaultBoundScale, DefaultBoundScale, DefaultBoundScale);

    public int BoundScale {
        set {
            if (SetValue(ref field, value)) {
                ParticleBounds = new Media3D.Rect3D(0, 0, 0, value, value, value);
                BoundingLineTransform.ScaleX = BoundingLineTransform.ScaleY = BoundingLineTransform.ScaleZ = value;
            }
        }
        get { return field; }
    } = DefaultBoundScale;

    public Media.Color BlendColor {
        set {
            if (SetValue(ref field, value)) {
                BlendColorBrush = new Media.SolidColorBrush(value);
            }
        }
        get { return field; }
    } = Media.Colors.White;

    public int RedValue {
        set {
            if (SetValue(ref field, value)) {
                BlendColor = Media.Color.FromRgb((byte)RedValue, (byte)GreenValue, (byte)BlueValue);
            }
        }
        get { return field; }
    } = 255;

    public int GreenValue {
        set {
            if (SetValue(ref field, value)) {
                BlendColor = Media.Color.FromRgb((byte)RedValue, (byte)GreenValue, (byte)BlueValue);
            }
        }
        get { return field; }
    } = 255;

    public int BlueValue {
        set {
            if (SetValue(ref field, value)) {
                BlendColor = Media.Color.FromRgb((byte)RedValue, (byte)GreenValue, (byte)BlueValue);
            }
        }
        get { return field; }
    } = 255;

    public Media.SolidColorBrush BlendColorBrush {
        set { SetValue(ref field, value); }
        get { return field; }
    } = new Media.SolidColorBrush(Media.Colors.White);

    public int NumTextureRows {
        set { SetValue(ref field, value); }
        get { return field; }
    }

    public int NumTextureColumns {
        set { SetValue(ref field, value); }
        get { return field; }
    }

    public int SelectedTextureIndex {
        set {
            if (SetValue(ref field, value)) {
                LoadTexture(value);
            }
        }
        get { return field; }
    } = 0;

    public Array BlendOperationArray { get; } = Enum.GetValues(typeof(BlendOperation));

    public Array BlendOptionArray { get; } = Enum.GetValues(typeof(BlendOption));

    public BlendOption SourceBlendOption {
        set { SetValue(ref field, value); }
        get { return field; }
    } = BlendOption.One;

    public BlendOption SourceAlphaBlendOption {
        set { SetValue(ref field, value); }
        get { return field; }
    } = BlendOption.One;

    public BlendOption DestBlendOption {
        set { SetValue(ref field, value); }
        get { return field; }
    } = BlendOption.One;

    public BlendOption DestAlphaBlendOption {
        set { SetValue(ref field, value); }
        get { return field; }
    } = BlendOption.Zero;

    public Media.Color BlendFactorColor {
        set {
            if (SetValue(ref field, value)) {
                BlendFactorColorBrush = new Media.SolidColorBrush(value);
            }
        }
        get { return field; }
    } = Media.Colors.White;

    public int RedFactorValue {
        set {
            if (SetValue(ref field, value)) {
                BlendFactorColor =
                    Media.Color.FromRgb((byte)RedFactorValue, (byte)GreenFactorValue, (byte)BlueFactorValue);
            }
        }
        get { return field; }
    } = 255;

    public int GreenFactorValue {
        set {
            if (SetValue(ref field, value)) {
                BlendFactorColor =
                    Media.Color.FromRgb((byte)RedFactorValue, (byte)GreenFactorValue, (byte)BlueFactorValue);
            }
        }
        get { return field; }
    } = 255;

    public int BlueFactorValue {
        set {
            if (SetValue(ref field, value)) {
                BlendFactorColor =
                    Media.Color.FromRgb((byte)RedFactorValue, (byte)GreenFactorValue, (byte)BlueFactorValue);
            }
        }
        get { return field; }
    } = 255;

    public Media.SolidColorBrush BlendFactorColorBrush {
        set { SetValue(ref field, value); }
        get { return field; }
    } = new Media.SolidColorBrush(Media.Colors.White);

    public IList<Matrix> Instances { private set; get; }

    public readonly Tuple<int, int>[] TextureColumnsRows = [new Tuple<int, int>(1, 1), new Tuple<int, int>(4, 4), new Tuple<int, int>(4, 4), new Tuple<int, int>(6, 5)];

    public readonly string[] Textures = [@"Snowflake.png", @"FXT_Explosion_Fireball_Atlas_d.png", @"FXT_Sparks_01_Atlas_d.png", @"Smoke30Frames_0.png"];

    public readonly int[] DefaultParticleSizes = [20, 90, 40, 90];

    public Media.Color Light1Color { get; set; } = Media.Colors.White;

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();
        var lineBuilder = new LineBuilder();
        lineBuilder.AddBox(new Vector3(), 1, 1, 1);
        BoundingLines = lineBuilder.ToLineGeometry3D();
        LoadTexture(SelectedTextureIndex);
        var meshBuilder = new MeshBuilder();
        meshBuilder.AddSphere(new Vector3(0, 0, 0), 0.5, 16, 16);
        Model = meshBuilder.ToMesh();
        Camera = new PerspectiveCamera() {
            Position = new Media3D.Point3D(0, 0, 20), UpDirection = new Media3D.Vector3D(0, 1, 0),
            LookDirection = new Media3D.Vector3D(0, 0, -20)
        };
        Instances = [
            Matrix.Identity, Scaling(1, -1, 1) * Translation(10, 0, 10), Translation(-10, 0, 10),
            Translation(10, 0, -10),
            RotationAxis(new Vector3(1, 0, 0), 90) * Translation(-10, 0, -10),
        ];
    }

    private void LoadTexture(int index) {
        using (var file = new FileStream(new Uri(Textures[index], UriKind.RelativeOrAbsolute).ToString(),
                                         FileMode.Open)) {
            var mem = new MemoryStream();
            file.CopyTo(mem);
            ParticleTexture = mem;
        }

        NumTextureColumns = TextureColumnsRows[index].Item1;
        NumTextureRows = TextureColumnsRows[index].Item2;
        SizeSlider = DefaultParticleSizes[index];
        switch (index) {
            case 3:
                SourceBlendOption = BlendOption.SourceAlpha;
                SourceAlphaBlendOption = BlendOption.Zero;
                DestBlendOption = BlendOption.BlendFactor;
                DestAlphaBlendOption = BlendOption.Zero;
                break;
            default:
                SourceBlendOption = BlendOption.One;
                SourceAlphaBlendOption = BlendOption.One;
                DestBlendOption = BlendOption.One;
                DestAlphaBlendOption = BlendOption.Zero;
                break;
        }
    }

    private void UpdateAcceleration() {
        Acceleration = new Media3D.Vector3D((double)AccelerationX / 100,
                                            (double)AccelerationY / 100,
                                            (double)AccelerationZ / 100);
    }

    private static Matrix RotationAxis(Vector3 axis, float angle) {
        var m = System.Numerics.Matrix4x4.CreateFromAxisAngle(new System.Numerics.Vector3(axis.X, axis.Y, axis.Z),
                                                              angle);
        return ToMatrix(m);
    }

    private static Matrix Scaling(float x, float y, float z) {
        return ToMatrix(System.Numerics.Matrix4x4.CreateScale(x, y, z));
    }

    private static Matrix Translation(float x, float y, float z) {
        return ToMatrix(System.Numerics.Matrix4x4.CreateTranslation(x, y, z));
    }

    private static Matrix ToMatrix(System.Numerics.Matrix4x4 m) {
        return new Matrix(m.M11,
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
    }
}
