using DemoCore.Automation;
using System.Windows;

namespace SSAODemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    [Obsolete("Obsolete")]
    private MainWindowViewModel vm = new();

    private DemoAutomationHost? automationHost;

    public MainWindow() {
        InitializeComponent();
        DataContext = vm;
        if (DataContext is MainWindowViewModel viewModel && viewModel.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(Viewport, effectsManager);
        }
        Closed += (_, _) => {
            DemoBootstrapper.Detach(automationHost);
            automationHost = null;
            vm.Dispose();
        };
    }
}
