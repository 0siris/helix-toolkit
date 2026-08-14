using System.Windows;

namespace BoneSkinDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        Closed += (s, e) => {
            if (DataContext is IDisposable) {
                (DataContext as IDisposable).Dispose();
            }
        };
    }
}
