using System.Windows;

namespace MemoryLeakTester;

/// <summary>
/// Interaction logic for TestWindow.xaml
/// </summary>
public partial class TestWindow : Window {
    public TestWindow() {
        InitializeComponent();
        DataContext = new TestWindowViewModel();
        Closed += TestWindow_Closed;
    }

    private void TestWindow_Closed(object sender, EventArgs e) {
        (DataContext as IDisposable)?.Dispose();
    }
}
