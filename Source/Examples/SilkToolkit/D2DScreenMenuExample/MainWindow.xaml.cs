using System.Windows;

namespace D2DScreenMenuExample;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
