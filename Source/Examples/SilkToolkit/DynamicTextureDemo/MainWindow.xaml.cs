using System;
using System.Windows;

namespace DynamicTextureDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        DataContext = new MainViewModel();
        Closed += (_, _) => {
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }
}