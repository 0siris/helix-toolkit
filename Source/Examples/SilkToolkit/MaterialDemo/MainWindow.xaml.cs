using DemoCore.Automation;
using System.Windows;

namespace MaterialDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private DemoAutomationHost? automationHost;

    public MainWindow() {
        InitializeComponent();
        if (DataContext is MainViewModel vm && vm.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(View1, effectsManager);
        }
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object? sender, EventArgs e) {
        DemoBootstrapper.Detach(automationHost);
        automationHost = null;
        if (DataContext is IDisposable d) {
            d.Dispose();
        }
    }
}