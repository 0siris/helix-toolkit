using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PolygonTriangulationDemo;

using System;
using HelixToolkit.Wpf.SharpDX;
using Vector2 = Silk.NET.Maths.Vector2D<float>;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    /// <summary>
    /// List of Polygon Points to display
    /// </summary>
    private List<Vector2> mPolygonPoints = [];

    /// <summary>
    /// The ViewModel
    /// </summary>
    private MainViewModel mViewModel;

    /// <summary>
    /// Constructor for the MainWindow
    /// </summary>
    public MainWindow() {
        // Initialize all Components
        InitializeComponent();

        // Setup the ViewModel
        mViewModel = new MainViewModel();
        DataContext = mViewModel;

        // Setup the Line Drawing Handler
        mViewModel.PropertyChanged += ((s, e) => {
            // Switch the Line Geometry
            if (e.PropertyName == "ShowTriangleLines") {
                if (mViewModel.ShowTriangleLines) {
                    lineTriangulatedPolygon.Geometry = mViewModel.LineGeometry;
                } else {
                    lineTriangulatedPolygon.Geometry = null;
                }
            }
        });
    }

    /// <summary>
    /// Generate a simple Polygon and then triangulate it.
    /// The Result is then Displayed.
    /// </summary>
    /// <param name="sender">The Sender (i.e. the Button)</param>
    /// <param name="e">The routet Event Args</param>
    private void generatePolygonButton_Click(object sender, RoutedEventArgs e) {
        // Generate random Polygon
        var random = new Random();
        var cnt = mViewModel.PointCount;
        mPolygonPoints = [];
        var angle = 0f;
        var angleDiff = 2f * (Single) Math.PI / cnt;
        var radius = 4f;
        // Random Radii for the Polygon
        var radii = new List<float>();
        var innerRadii = new List<float>();
        for (int i = 0; i < cnt; i++) {
            radii.Add(NextFloat(random, radius * 0.9f, radius * 1.1f));
            innerRadii.Add(NextFloat(random, radius * 0.2f, radius * 0.3f));
        }

        var hole1 = new List<Vector2>();
        var hole2 = new List<Vector2>();
        var holeDistance = 2f;
        var holeAngle = NextFloat(random, 0, (float) Math.PI * 2);
        var cos = (float) Math.Cos(holeAngle);
        var sin = (float) Math.Sin(holeAngle);
        var offset1 = new Vector2(holeDistance * cos, holeDistance * sin);
        var offset2 = new Vector2(-holeDistance * cos, -holeDistance * sin);
        for (int i = 0; i < cnt; i++) {
            // Flatten a bit
            var radiusUse = radii[i];
            mPolygonPoints.Add(new Vector2(radii[i] * (Single) Math.Cos(angle), radii[i] * (Single) Math.Sin(angle)));
            hole1.Add(offset1 + new Vector2(innerRadii[i] * (Single) Math.Cos(-angle),
                innerRadii[i] * (Single) Math.Sin(-angle)));
            hole2.Add(offset2 + new Vector2(innerRadii[i] * (Single) Math.Cos(-angle),
                innerRadii[i] * (Single) Math.Sin(-angle)));
            angle += angleDiff;
        }

        var holes = new List<List<Vector2>>() {
            hole1,
            hole2
        };

        // Triangulate and measure the Time needed for the Triangulation
        var before = DateTime.Now;
        var sLti = SweepLinePolygonTriangulator.Triangulate(mPolygonPoints, holes)
                   ?? throw new InvalidOperationException("Polygon triangulation returned no result.");
        var after = DateTime.Now;

        // Generate the Output
        var geometry = new HelixToolkit.SharpDX.Core.MeshGeometry3D {
            Positions = [],
            Normals = []
        };
        var positions = geometry.Positions
                        ?? throw new InvalidOperationException("The generated polygon has no positions.");
        var normals = geometry.Normals
                      ?? throw new InvalidOperationException("The generated polygon has no normals.");
        foreach (var point in mPolygonPoints.Union(holes.SelectMany(h => h))) {
            positions.Add(new Vector3(point.X, 0, point.Y + 5));
            normals.Add(new Vector3(0, 1, 0));
        }

        geometry.Indices = [.. sLti];
        triangulatedPolygon.Geometry = geometry;

        var lb = new LineBuilder();
        for (int i = 0; i < sLti.Count; i += 3) {
            lb.AddLine(positions[sLti[i]], positions[sLti[i + 1]]);
            lb.AddLine(positions[sLti[i + 1]], positions[sLti[i + 2]]);
            lb.AddLine(positions[sLti[i + 2]], positions[sLti[i]]);
        }

        mViewModel.LineGeometry = lb.ToLineGeometry3D();

        // Set the Lines if activated
        if (mViewModel.ShowTriangleLines) {
            lineTriangulatedPolygon.Geometry = mViewModel.LineGeometry;
        } else {
            lineTriangulatedPolygon.Geometry = null;
        }

        // Set the InfoLabel Text
        var timeNeeded = (after - before).TotalMilliseconds;
        infoLabel.Content = String.Format("Last triangulation of {0} Points took {1:0.##} Milliseconds!",
            positions.Count,
            timeNeeded);
    }

    /// <summary>
    /// Handle the selection Change of the Material
    /// </summary>
    /// <param name="sender">Sender (i.e. the Combobox)</param>
    /// <param name="e">Event Args</param>
    private void ComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
        // Set the ViewModel's Material and the Polygon-Material
        if (e.AddedItems.Count > 0 && e.AddedItems[0]
                ?.ToString() is { } materialName) {
            mViewModel.Material = PhongMaterials.GetMaterial(materialName);
        }
    }

    private static float NextFloat(Random random, float min, float max)
        => min + (max - min) * (float) random.NextDouble();
}