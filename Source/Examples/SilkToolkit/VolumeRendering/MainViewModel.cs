using System.IO;
using System.Windows.Input;
using DemoCore;
using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.ImportExport;
using HelixToolkit.Wpf.SharpDX.Camera;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Material;
using HelixToolkit.Wpf.SharpDX.Model.Materials;
using Color4 = Silk.NET.Maths.Vector4D<float>;
using Colors = System.Windows.Media.Colors;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;
using Media3D = System.Windows.Media.Media3D;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;


namespace VolumeRendering;

public class MainViewModel : BaseViewModel {
    public Material? VolumeMaterial {
        set => SetValue(ref field, value);
        get;
    }

    public Media3D.Transform3D? Transform {
        set => SetValue(ref field, value);
        get;
    }

    public Geometry3D MeshModel { get; }

    public Material MeshMaterial { get; }

    public LineGeometry3D AxisModel { get; }

    public Material AxisModelMaterial { get; }

    public bool IsLoading {
        private set => SetValue(ref field, value);
        get;
    } = false;

    public ICommand LoadTeapotCommand { get; }
    public ICommand LoadSkullCommand { get; }
    public ICommand LoadCloudCommand { get; }
    public ICommand LoadBeetleCommand { get; }

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();
        Camera = new OrthographicCamera() {
            Position = new Point3D(0, 0, -5),
            LookDirection = new Vector3D(0, 0, 5),
            UpDirection = new Vector3D(0, 1, 0)
        };
        LoadTeapotCommand = new RelayCommand((_) => { Load(0); });
        LoadSkullCommand = new RelayCommand((_) => { Load(1); });
        LoadCloudCommand = new RelayCommand((_) => { Load(2); });
        LoadBeetleCommand = new RelayCommand((_) => { Load(3); });
        var builder = new MeshBuilder();
        //builder.AddBox(new Vector3(0, 0, 0), 2, 2, 0.001);
        builder.AddSphere(Vector3.Zero, 0.1);
        builder.AddBox(new Vector3(1, 0, 0), 0.2, 0.2, 0.2);
        MeshModel = builder.ToMesh();
        MeshMaterial = PhongMaterials.Yellow;

        var lineBuilder = new LineBuilder();
        lineBuilder.AddLine(Vector3.Zero, new Vector3(1.5f, 0, 0));
        lineBuilder.AddLine(Vector3.Zero, new Vector3(0, 1.5f, 0));
        lineBuilder.AddLine(Vector3.Zero, new Vector3(0, 0, 1.5f));
        var axisModel = lineBuilder.ToLineGeometry3D();
        var positions = axisModel.Positions
                        ?? throw new InvalidOperationException("The generated axis has no positions.");
        axisModel.Colors = new Color4Collection(positions.Count) {
            Colors.Red.ToColor4(),
            Colors.Red.ToColor4(),
            Colors.Green.ToColor4(),
            Colors.Green.ToColor4(),
            Colors.Blue.ToColor4(),
            Colors.Blue.ToColor4()
        };
        AxisModel = axisModel;
        AxisModelMaterial = new LineArrowHeadMaterial() {
            Color = Colors.White,
            ArrowSize = 0.05
        };
        //Load(0);
    }

    private void Load(int idx) {
        if (IsLoading) {
            return;
        }

        IsLoading = true;
        Task.Run<Tuple<Material, Media3D.Transform3D>>(() => {
                switch (idx) {
                    case 0:
                        return LoadTeapot();
                    case 1:
                        return LoadSkull();
                    case 2:
                        return LoadNoise();
                    case 3:
                        return LoadBeetle();
                }

                throw new ArgumentOutOfRangeException(nameof(idx));
            })
            .ContinueWith((result) => {
                    VolumeMaterial = result.Result.Item1.Clone() is Material material
                        ? material
                        : throw new InvalidOperationException("The loaded volume material could not be cloned.");
                    Transform = result.Result.Item2;
                    IsLoading = false;
                },
                TaskScheduler.FromCurrentSynchronizationContext());
    }

    private Tuple<Material, Media3D.Transform3D> LoadTeapot() {
        //var m = new VolumeTextureRawDataMaterial();
        //m.Texture = VolumeTextureRawDataMaterialCore.LoadRAWFile("teapot256x256x178.raw", 256, 256, 178);
        var m = new VolumeTextureDiffuseMaterial();
        var data = VolumeTextureRawDataMaterialCore.LoadRawFile("teapot256x256x178.raw", 256, 256, 178);
        m.Texture = ProcessData(data.VolumeTextures, data.Width, data.Height, data.Depth, out var transferMap);
        m.Color = new Color4(1, 1, 1, 0.4f);
        m.TransferMap = transferMap;
        m.Freeze();
        var scale = Scaling(2, 2, 178 / 256f * 2);
        var rotate = RotationAxis(new Vector3(1, 0, 0), (float) Math.PI);
        var t = new Media3D.MatrixTransform3D((scale * rotate).ToMatrix3D());
        t.Freeze();
        return new Tuple<Material, Media3D.Transform3D>(m, t);
    }

    private Tuple<Material, Media3D.Transform3D> LoadSkull() {
        var m = new VolumeTextureDiffuseMaterial();
        var data = VolumeTextureRawDataMaterialCore.LoadRawFile("male128x256x256.raw", 128, 256, 256);
        m.Texture = ProcessData(data.VolumeTextures, data.Width, data.Height, data.Depth, out var transferMap);
        m.Color = new Color4(0.6f, 0.6f, 0.6f, 1f);
        m.TransferMap = transferMap;
        m.Freeze();
        var rotate = RotationAxis(new Vector3(1, 0, 0), (float) Math.PI);
        var transform = new Media3D.MatrixTransform3D(rotate.ToMatrix3D());
        transform.Freeze();
        return new Tuple<Material, Media3D.Transform3D>(m, transform);
    }

    private Tuple<Material, Media3D.Transform3D> LoadNoise() {
        var m = new VolumeTextureDds3DMaterial {
            Texture = TextureModel.Create("NoiseVolume.dds")
                      ?? throw new InvalidOperationException("The volume texture could not be loaded."),
            Color = new Color4(1, 1, 1, 0.01f)
        };
        m.Freeze();
        var transform = new Media3D.ScaleTransform3D(1, 1, 1);
        transform.Freeze();
        return new Tuple<Material, Media3D.Transform3D>(m, transform);
    }

    private Tuple<Material, Media3D.Transform3D> LoadBeetle() {
        var data = ReadDat("stagbeetle208x208x123.dat", out var width, out var height, out var depth);
        var m = new VolumeTextureDiffuseMaterial();
        var max = data.Max();
        var histogram = new uint[max + 1];

        var fdata = new float[data.Length];

        for (var i = 0; i < data.Length; ++i) {
            fdata[i] = (float) data[i] / max;
            histogram[data[i]]++;
        }

        GetTransferFunction(histogram, data.Length, 1);
        var gradients = VolumeDataHelper.GenerateGradients(fdata, width, height, depth, 1);
        VolumeDataHelper.FilterNxNxN(gradients, width, height, depth, 3);
        m.Texture = new VolumeTextureGradientParams(gradients, width, height, depth);
        m.Color = new Color4(0, 1, 0, 0.4f);
        var transform = new Media3D.ScaleTransform3D(1, 1, 1);
        transform.Freeze();
        m.Freeze();
        return new Tuple<Material, Media3D.Transform3D>(m, transform);
    }

    private VolumeTextureGradientParams ProcessData(
        byte[] data,
        int width,
        int height,
        int depth,
        out Color4[] transferMap
    ) {
        var histogram = new uint[256];

        var fdata = new float[data.Length];
        for (var i = 0; i < data.Length; ++i) {
            fdata[i] = (float) data[i] / byte.MaxValue;
            histogram[data[i]]++;
        }

        transferMap = GetTransferFunction(histogram, data.Length);
        var gradients = VolumeDataHelper.GenerateGradients(fdata, width, height, depth, 1);
        VolumeDataHelper.FilterNxNxN(gradients, width, height, depth, 3);
        return new VolumeTextureGradientParams(gradients, width, height, depth);
    }

    private float[] Normalize(byte[] data) {
        var fdata = new float[data.Length];
        for (var i = 0; i < data.Length; ++i) {
            fdata[i] = (float) data[i] / byte.MaxValue;
        }

        return fdata;
    }

    private static readonly Color4[] ColorCandidates = [
        Colors.LightPink.ToColor4(),
        Colors.DarkGray.ToColor4(),
        Colors.Yellow.ToColor4(),
        Colors.Red.ToColor4(),
        Colors.Green.ToColor4(),
    ];

    /// <summary>
    /// Gets the transfer function. Please create your own color transfer map.
    /// </summary>
    /// <param name="histogram">The histogram.</param>
    /// <param name="total">The total.</param>
    /// <returns></returns>
    public static Color4[] GetTransferFunction(
        uint[] histogram,
        int total,
        float maxPercent = 0.003f,
        float minPercent = 0.0001f
    ) {
        var percentage = new float[histogram.Length];
        for (var i = 0; i < histogram.Length; ++i) {
            percentage[i] = (float) histogram[i] / total;
            if (percentage[i] > maxPercent || percentage[i] < minPercent) {
                percentage[i] = 0;
            }
        }

        var ret = new Color4[histogram.Length];
        var counter = 0;
        var isZero = true;
        for (var i = 0; i < percentage.Length; ++i) {
            if (percentage[i] > 0) {
                ret[i] = ColorCandidates[counter];
                isZero = false;
            }

            if (!isZero && percentage[i] == 0) {
                counter = (counter + 1) % ColorCandidates.Length;
                isZero = true;
            }
        }

        return ret;
    }

    private static ushort[] ReadDat(string file, out int width, out int height, out int depth) {
        using var f = File.OpenRead(file);
        using var stream = new BinaryReader(f);
        width = stream.ReadUInt16();
        height = stream.ReadUInt16();
        depth = stream.ReadUInt16();
        return stream.ReadUInt16(width * height * depth);
    }

    private static Matrix RotationAxis(Vector3 axis, float angle) => ToMatrix(
        System.Numerics.Matrix4x4.CreateFromAxisAngle(new System.Numerics.Vector3(axis.X, axis.Y, axis.Z), angle));

    private static Matrix Scaling(float x, float y, float z)
        => ToMatrix(System.Numerics.Matrix4x4.CreateScale(x, y, z));

    private static Matrix ToMatrix(System.Numerics.Matrix4x4 matrix) => new(matrix.M11,
        matrix.M12,
        matrix.M13,
        matrix.M14,
        matrix.M21,
        matrix.M22,
        matrix.M23,
        matrix.M24,
        matrix.M31,
        matrix.M32,
        matrix.M33,
        matrix.M34,
        matrix.M41,
        matrix.M42,
        matrix.M43,
        matrix.M44);
}