using System.Windows;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

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
        MouseDown3DEventArgs e
    ) {
        if (e.HitTestResult is {Geometry: { } geometry}) {
            viewModel.SelectedGeometry = geometry;
        }
    }
}