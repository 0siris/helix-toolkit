using System.Windows;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D;
using DemoCore.Automation;

namespace GroupElementTester;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private GroupModel3D? tempGroup;
    private DemoAutomationHost? automationHost;

    public MainWindow() {
        InitializeComponent();
        if (DataContext is MainViewModel vm && vm.EffectsManager is { } effectsManager) {
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

    private void AttachGroupButton_Click(object sender, RoutedEventArgs e) {
        if (tempGroup is { } group) {
            View1.Items.Add(group);
            tempGroup = null;
        }
    }

    private void DetachGroupButton_Click(object sender, RoutedEventArgs e) {
        if (tempGroup == null) {
            tempGroup = Group1;
            View1.Items.Remove(Group1);
        }
    }
}