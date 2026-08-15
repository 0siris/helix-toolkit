// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Windows;

namespace LightingDemo;

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

    private void Button_Click(object? sender, RoutedEventArgs e) {
        MultiViewport viewportWin = new MultiViewport() {
            DataContext = DataContext
        };
        viewportWin.Show();
    }
}