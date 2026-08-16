using System.Windows;

namespace VolumeRendering;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
    public App() {
        _ = NvOptimusEnabler.Enable();
    }
}
