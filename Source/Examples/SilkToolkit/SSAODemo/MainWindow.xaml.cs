using System.Windows;

namespace SSAODemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    [Obsolete("Obsolete")]
    private MainWindowViewModel vm = new();

    public MainWindow() {
        InitializeComponent();
        DataContext = vm;
        Closed += (_, _) => { vm.Dispose(); };
    }
}
