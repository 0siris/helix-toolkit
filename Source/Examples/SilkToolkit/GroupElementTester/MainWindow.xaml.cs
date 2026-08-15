using System.Windows;
using HelixToolkit.Wpf.SharpDX;

namespace GroupElementTester;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private GroupModel3D? tempGroup;

    public MainWindow() {
        InitializeComponent();
        Closed += (s, e) => {
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }

    private void AttachGroupButton_Click(object sender, RoutedEventArgs e) {
        if (tempGroup is { } group) {
            view1.Items.Add(group);
            tempGroup = null;
        }
    }

    private void DetachGroupButton_Click(object sender, RoutedEventArgs e) {
        if (tempGroup == null) {
            tempGroup = group1;
            view1.Items.Remove(group1);
        }
    }
}