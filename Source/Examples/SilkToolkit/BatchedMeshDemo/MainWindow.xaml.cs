using System.Windows;

namespace BatchedMeshDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
    }

    private void BatchedMeshGeometryModel3D_Mouse3DDown(
        object? sender,
        HelixToolkit.Wpf.SharpDX.MouseDown3DEventArgs e
    ) {
        if (e.HitTestResult is {Geometry: { } geometry}) {
            viewModel.SelectedGeometry = geometry;
        }
    }
}