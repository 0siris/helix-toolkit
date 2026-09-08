using System.Windows;
using DemoCore.Automation;

namespace BillboardDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private DemoAutomationHost? automationHost;

    public MainWindow() {
        InitializeComponent();
        if (DataContext is MainViewModel viewModel && viewModel.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(View1, effectsManager);
        }
        Closed += (_, _) => {
            DemoBootstrapper.Detach(automationHost);
            automationHost = null;
        };
    }
}
