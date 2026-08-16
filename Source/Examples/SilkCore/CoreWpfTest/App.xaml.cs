using System.Windows;

namespace CoreWpfTest;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        _  = NvOptimusEnabler.Enable();
        base.OnStartup(e);
    }
    
}