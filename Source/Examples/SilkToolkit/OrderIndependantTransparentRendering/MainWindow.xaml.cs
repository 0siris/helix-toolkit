using System.Windows;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

namespace OrderIndependentTransparentRendering;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private GeometryModel3D? selectedModel;

    public MainWindow() {
        InitializeComponent();
        View.AddHandler(Element3D.MouseDown3DEvent,
            new RoutedEventHandler((_, e) => {
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