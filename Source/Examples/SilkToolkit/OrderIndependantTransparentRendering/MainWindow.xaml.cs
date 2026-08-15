using System.Windows;
using HelixToolkit.Wpf.SharpDX;

namespace OrderIndependentTransparentRendering;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private GeometryModel3D? selectedModel;

    public MainWindow() {
        InitializeComponent();
        view.AddHandler(Element3D.MouseDown3DEvent,
            new RoutedEventHandler((s, e) => {
                if (e is not MouseDown3DEventArgs {HitTestResult: { } hitTestResult}) {
                    return;
                }

                //if (selectedModel != null)
                //{
                //    selectedModel.PostEffects = null;
                //    selectedModel = null;
                //}
                selectedModel = hitTestResult.ModelHit as GeometryModel3D;
                selectedModel?.PostEffects =
                    string.IsNullOrEmpty(selectedModel.PostEffects)
                        ? $"highlight[color:#FFFF00]"
                        : null;
            }));
    }
}