using System.Windows;
using DemoCore.Automation;

namespace CrossSectionDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private DemoAutomationHost? automationHost;

    public MainWindow() {
        InitializeComponent();
        DataContext = new MainViewModel();
        if (DataContext is MainViewModel viewModel && viewModel.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(View1, effectsManager);
        }
        Closed += (_, _) => {
            DemoBootstrapper.Detach(automationHost);
            automationHost = null;
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }
}