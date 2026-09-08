using DemoCore.Automation;
using System.Windows;

namespace RenderTechniqueImportExport;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private DemoAutomationHost? automationHost;

    public MainWindow() {
        InitializeComponent();
        ListMenu.DataContext = DataContext;
        DataContextChanged += MainWindow_DataContextChanged;
        if (DataContext is MainViewModel viewModel && viewModel.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(View, effectsManager);
        }
        Closed += (_, _) => {
            DemoBootstrapper.Detach(automationHost);
            automationHost = null;
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }

    private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
        ListMenu.DataContext = e.NewValue;
    }
}
