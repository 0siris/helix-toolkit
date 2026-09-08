using DemoCore.Automation;
using System.Windows;

namespace MemoryLeakTester;

/// <summary>
/// Interaction logic for TestWindow.xaml
/// </summary>
public partial class TestWindow : Window {
    private DemoAutomationHost? automationHost;

    public TestWindow() {
        InitializeComponent();
        DataContext = new TestWindowViewModel();
        if (DataContext is TestWindowViewModel vm && vm.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(View, effectsManager);
        }
        Closed += TestWindow_Closed;
    }

    private void TestWindow_Closed(object? sender, EventArgs e) {
        DemoBootstrapper.Detach(automationHost);
        automationHost = null;
        (DataContext as IDisposable)?.Dispose();
    }
}