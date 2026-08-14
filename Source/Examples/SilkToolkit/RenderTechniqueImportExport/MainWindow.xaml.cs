using System.Windows;

namespace RenderTechniqueImportExport;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        listMenu.DataContext = DataContext;
        DataContextChanged += MainWindow_DataContextChanged;
    }

    private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
        listMenu.DataContext = e.NewValue;
    }
}
