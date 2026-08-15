// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Workitem1349;

using System.Linq;
using DemoCore;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using MeshGeometry3D = MeshGeometry3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

public class MainViewModel : BaseViewModel {
    public MeshGeometry3D Model { get; private set; }
    public LineGeometry3D Grid { get; private set; }

    public HelixToolkit.SharpDX.Core.Color GridColor { get; private set; }

    public Color DirectionalLightColor { get; private set; }
    public Color AmbientLightColor { get; private set; }

    public BillboardText3D Text3D { get; set; } = new() {
        IsDynamic = true
    };

    public MainViewModel() {
        // titles
        Title = "Bug? BillboardText3D not visible until origin comes in sight.";
        SubTitle =
            "Please move or rotate the view and see how the BillboardText suddenly appears when the origin (right front edge of the grid) comes into view.";

        // camera setup
        Camera = new PerspectiveCamera {
            Position = new Point3D(4.4, 2.2, -4.4),
            LookDirection = new Vector3D(0, -4, 10),
            UpDirection = new Vector3D(0, 1, 0)
        };

        EffectsManager = new DefaultEffectsManager();

        // setup lighting
        AmbientLightColor = Colors.Black;
        DirectionalLightColor = Colors.White;

        // floor plane grid
        Grid = LineBuilder.GenerateGrid();
        GridColor = HelixToolkit.SharpDX.Core.Color.Black;

        // scene model3d
        var b1 = new MeshBuilder();
        b1.AddSphere(new Vector3(0, 0, 0), 0.05);

        var meshGeometry = b1.ToMeshGeometry3D();
        meshGeometry.Colors = meshGeometry.TextureCoordinates is { } textureCoordinates
            ? [.. textureCoordinates.Select(x => x.ToColor4())]
            : [];
        Model = meshGeometry;

        // Create Billboard Text
        float offset = 4.5f;
        float scale = 0.8f;
        Text3D.TextInfo.Add(new TextInfo("2", new Vector3(2, 0, 0)) {
            Foreground = HelixToolkit.SharpDX.Core.Color.Blue,
            Scale = scale * 2
        });
        Text3D.TextInfo.Add(new TextInfo("3", new Vector3(3, 0, 3)) {
            Foreground = HelixToolkit.SharpDX.Core.Color.Blue,
            Scale = scale * 2
        });
        Text3D.TextInfo.Add(new TextInfo("4", new Vector3(4, 0, 3)) {
            Foreground = HelixToolkit.SharpDX.Core.Color.Blue,
            Scale = scale * 2
        });
        Text3D.TextInfo.Add(new TextInfo("5", new Vector3(5, 0, 3)) {
            Foreground = HelixToolkit.SharpDX.Core.Color.Blue,
            Scale = scale * 2
        });
    }
}