using System.Windows;
using DemoCore.Automation;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

namespace BatchedMeshDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private DemoAutomationHost? automationHost;

    public MainWindow() {
        InitializeComponent();
        if (DataContext is MainViewModel viewModel && viewModel.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(View, effectsManager);
        }
        Closed += (_, _) => {
            DemoBootstrapper.Detach(automationHost);
            automationHost = null;
        };
    }

    private void BatchedMeshGeometryModel3D_Mouse3DDown(
        object? sender,
        MouseDown3DEventArgs e
    ) {
        if (e.HitTestResult is {Geometry: { } geometry}) {
            ViewModel.SelectedGeometry = geometry;
        }
    }
}