using System.Windows;

namespace MaterialDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Closed(object? sender, EventArgs e) {
        if (DataContext is IDisposable d) {
            d.Dispose();
        }
    }
}