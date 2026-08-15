using System.Windows;

namespace BoneSkinDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        Closed += (_, _) => {
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }
}