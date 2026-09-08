using DemoCore.Automation;
using System.Windows;

namespace VolumeRendering;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private DemoAutomationHost? automationHost;

    public MainWindow() {
        InitializeComponent();
        if (Resources["ViewModel"] is MainViewModel vm && vm.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(View, effectsManager);
        }
        Closed += (_, _) => {
            DemoBootstrapper.Detach(automationHost);
            automationHost = null;
        };
    }
}
