using System.Windows;

namespace XRayDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        DataContext = new MainViewModel();
        Closed += (s, e) => {
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }
}