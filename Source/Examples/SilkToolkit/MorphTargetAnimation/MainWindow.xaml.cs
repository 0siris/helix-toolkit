// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace MorphTargetAnimationDemo;

using System;
using System.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private MainViewModel mvm;

    public MainWindow() {
        InitializeComponent();
        mvm = new MainViewModel();
        DataContext = mvm;
        Closed += (_, _) => {
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }
}