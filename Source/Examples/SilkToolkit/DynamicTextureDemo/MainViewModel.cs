using System;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using DemoCore;
using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Material;
using Color = System.Windows.Media.Color;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Colors = System.Windows.Media.Colors;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace DynamicTextureDemo;

public class MainViewModel : BaseViewModel {
    public Vector3D Light1Direction {
        set {
            if (field != value) {
                field = value;
                OnPropertyChanged();
            }
        }
        get;
    } = new();

    public FillMode FillMode {
        set {
            field = value;
            OnPropertyChanged();
        }
        get;
    } = FillMode.Solid;

    public bool ShowWireframe {
        set {
            field = value;
            OnPropertyChanged();
            if (field) {
                FillMode = FillMode.Wireframe;
            } else {
                FillMode = FillMode.Solid;
            }
        }
        get;
    } = false;

    public Color Light1Color { get; set; }
    public PhongMaterial ModelMaterial { get; set; }

    public PhongMaterial InnerModelMaterial { get; set; }

    //public PhongMaterial OtherMaterial { set; get; }
    public MeshGeometry3D Model { get; private set; }
    public MeshGeometry3D InnerModel { get; private set; }
    public PointGeometry3D PointModel { private set; get; }
    public LineGeometry3D LineModel { private set; get; }

    public LineMaterial LineMaterial { private set; get; }

    //public MeshGeometry3D Other { get; private set; }
    public Color AmbientLightColor { get; set; }
    private DispatcherTimer timer = new();

    public bool DynamicTexture { set; get; } = true;
    public bool DynamicVertices { set; get; } = false;
    public bool DynamicTriangles { set; get; } = false;
    public bool DynamicPointColor { set; get; } = true;
    public bool AnimateUvOffset { set; get; } = true;
    public bool ReverseInnerRotation { set; get; } = false;

    public Vector3D CamLookDir {
        set {
            if (field != value) {
                field = value;
                OnPropertyChanged();
                Light1Direction = value;
            }
        }
        get;
    } = new(-10, -10, -10);

    private readonly Vector3Collection initialPosition;
    private readonly IntCollection initialIndicies;
    private Random rnd = new();
    private bool isRemoving = true;
    private int removedIndex;
    private readonly CancellationTokenSource cts = new();

    private readonly SynchronizationContext context = SynchronizationContext.Current
                                                      ?? throw new InvalidOperationException(
                                                          "The dynamic texture demo requires a synchronization context.");

    private int counter;

    public MainViewModel() {
        // titles
        Title = "DynamicTexture Demo";
        SubTitle = "WPF & SharpDX";
        EffectsManager = new DefaultEffectsManager();
        Camera = new PerspectiveCamera {
            Position = new Point3D(10, 10, 10),
            LookDirection = new Vector3D(-10, -10, -10),
            UpDirection = new Vector3D(0, 1, 0)
        };
        Light1Color = Colors.White;
        Light1Direction = new Vector3D(-10, -10, -10);
        AmbientLightColor = Colors.Black;

        var b2 = new MeshBuilder(true, true, true);
        b2.AddSphere(new Vector3(0f, 0f, 0f), 4, 64, 64);
        Model = b2.ToMeshGeometry3D();
        Model.IsDynamic = true;
        var modelIndices = Model.Indices ?? throw new InvalidOperationException("Model indices are required.");
        var modelPositions = Model.Positions ?? throw new InvalidOperationException("Model positions are required.");
        InnerModel = new MeshGeometry3D() {
            Indices = modelIndices,
            Positions = modelPositions,
            Normals = Model.Normals,
            TextureCoordinates = Model.TextureCoordinates,
            Tangents = Model.Tangents,
            BiTangents = Model.BiTangents,
            IsDynamic = true
        };

        var image = TextureModel.Create(new Uri(@"test.png", UriKind.RelativeOrAbsolute).ToString());
        ModelMaterial = new PhongMaterial {
            AmbientColor = Colors.Gray.ToColor4(),
            DiffuseColor = Colors.White.ToColor4(),
            SpecularColor = Colors.White.ToColor4(),
            SpecularShininess = 100f,
            DiffuseAlphaMap = image,
            DiffuseMap =
                TextureModel.Create(new Uri(@"TextureCheckerboard2.dds", UriKind.RelativeOrAbsolute)
                    .ToString()),
            NormalMap = TextureModel.Create(
                new Uri(@"TextureCheckerboard2_dot3.dds", UriKind.RelativeOrAbsolute).ToString()),
        };

        InnerModelMaterial = new PhongMaterial {
            AmbientColor = Colors.Gray.ToColor4(),
            DiffuseColor = new Color4(0.75f, 0.75f, 0.75f, 1.0f),
            SpecularColor = Colors.White.ToColor4(),
            SpecularShininess = 100f,
            DiffuseAlphaMap = image,
            DiffuseMap =
                TextureModel.Create(new Uri(@"TextureNoise1.jpg", UriKind.RelativeOrAbsolute).ToString()),
            NormalMap = ModelMaterial.NormalMap
        };


        initialPosition = modelPositions;
        initialIndicies = modelIndices;

        #region Point Model

        PointModel = new PointGeometry3D() {
            IsDynamic = true,
            Positions = modelPositions
        };
        var count = modelPositions.Count;
        var colors = new Color4Collection(count);
        for (var i = 0; i < count / 2; ++i) {
            colors.Add(new Color4(0, 1, 1, 1));
        }

        for (var i = 0; i < count / 2; ++i) {
            colors.Add(new Color4(0, 0, 0, 0));
        }

        PointModel.Colors = colors;

        #endregion

        #region Line Model

        LineModel = new LineGeometry3D() {
            IsDynamic = true,
            Positions = [.. PointModel.Positions]
        };
        LineModel.Positions.Add(Vector3.Zero);
        var indices = new IntCollection(count * 2);
        for (var i = 0; i < count; ++i) {
            indices.Add(count);
            indices.Add(i);
        }

        LineModel.Indices = indices;
        colors = new Color4Collection(LineModel.Positions.Count);
        for (var i = 0; i < count; ++i) {
            colors.Add(new Color4((float) i / count, 1 - (float) i / count, 0, 1));
        }

        colors.Add(Colors.Blue.ToColor4());
        LineModel.Colors = colors;
        LineMaterial = new LineArrowHeadMaterial() {
            Color = Colors.White,
            Thickness = 0.5,
            ArrowSize = 0.02
        };

        #endregion

        var token = cts.Token;
        Task.Run(() => {
                while (!token.IsCancellationRequested) {
                    Timer_Tick();
                    Task.Delay(16)
                        .Wait();
                }
            },
            token);
        //timer.Interval = TimeSpan.FromMilliseconds(16);
        //timer.Tick += Timer_Tick;
        //timer.Start();
    }

    public static Stream ToStream(System.Drawing.Image image, ImageFormat format) {
        var stream = new MemoryStream();
        image.Save(stream, format);
        stream.Position = 0;
        return stream;
    }

    private void Timer_Tick() {
        ++counter;
        counter %= 128;
        if (DynamicTexture) {
            if (!AnimateUvOffset) {
                if (Model.TextureCoordinates is { } modelTextureCoordinates) {
                    var texture = new Vector2Collection(modelTextureCoordinates);
                    var t0 = texture[0];
                    for (var i = 1; i < texture.Count; ++i) {
                        texture[i - 1] = texture[i];
                    }

                    texture[texture.Count - 1] = t0;
                    context.Send((_) => {
                        Model.TextureCoordinates = texture;
                        if (ReverseInnerRotation) {
                            var texture1 = new Vector2Collection(texture);
                            texture1.Reverse();
                            InnerModel.TextureCoordinates = texture1;
                        } else {
                            InnerModel.TextureCoordinates = texture;
                        }
                    }, null);
                }
            } else {
                context.Send((_) => {
                    ModelMaterial.UvTransform = new UvTransform(0,
                        Vector2.One,
                        ModelMaterial.UvTransform.Translation +
                        new Vector2(0.005f, -0.01f));
                    InnerModelMaterial.UvTransform = new UvTransform(0,
                        Vector2.One,
                        InnerModelMaterial.UvTransform.Translation +
                        new Vector2(-0.01f, 0.005f));
                }, null);
            }
        }

        if (DynamicVertices) {
            var positions = new Vector3Collection(initialPosition);
            for (var i = 0; i < positions.Count; ++i) {
                var off = (float) Math.Sin(Math.PI * (float) (counter + i) / 64);
                var p = positions[i];
                p *= 0.8f + off * 0.2f;
                positions[i] = p;
            }

            var linePositions = new Vector3Collection(positions) {
                Vector3.Zero
            };
            //var normals =  MeshGeometryHelper.CalculateNormals(positions, initialIndicies);
            //var innerNormals =  new Vector3Collection(normals.Select(x => { return x * -1; }));
            context.Send((_) => {
                    //Model.Normals = normals;
                    //InnerModel.Normals = innerNormals;
                    //Model.Positions = positions;
                    //InnerModel.Positions = positions;
                    PointModel.Positions = positions;
                    LineModel.Positions = linePositions;
                },
                null);
        }

        if (DynamicTriangles) {
            var indices = new IntCollection(initialIndicies);
            if (isRemoving) {
                removedIndex += 3 * 8;
                if (removedIndex >= initialIndicies.Count) {
                    removedIndex = initialIndicies.Count;
                    isRemoving = false;
                }
            } else {
                removedIndex -= 3 * 8;
                if (removedIndex <= 0) {
                    isRemoving = true;
                    removedIndex = 0;
                }
            }

            indices.RemoveRange(0, removedIndex);
            context.Send((_) => {
                    Model.Indices = indices;
                    InnerModel.Indices = indices;
                },
                null);
        }

        if (DynamicTexture) {
            if (PointModel.Colors is not { } pointColors || LineModel.Colors is not { } lineColorsSource)
                return;
            var colors = new Color4Collection(pointColors);
            for (var k = 0; k < 10; ++k) {
                var c = colors[colors.Count - 1];
                for (var i = colors.Count - 1; i > 0; --i) {
                    colors[i] = colors[i - 1];
                }

                colors[0] = c;
            }

            var lineColors = new Color4Collection(lineColorsSource);
            for (var k = 0; k < 10; ++k) {
                var c = lineColors[colors.Count - 2];
                for (var i = lineColors.Count - 2; i > 0; --i) {
                    lineColors[i] = lineColors[i - 1];
                }

                lineColors[0] = c;
            }

            context.Send((_) => {
                    PointModel.Colors = colors;
                    LineModel.Colors = lineColors;
                },
                null);
        }
    }

    protected override void Dispose(bool disposing) {
        //timer.Stop();
        //timer.Tick -= Timer_Tick;
        cts.Cancel(true);
        cts.Dispose();
        base.Dispose(disposing);
    }
}