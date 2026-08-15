// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace FileLoadDemo;

using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        DataContext = new MainViewModel(this);

        view.AddHandler(Element3D.MouseDown3DEvent,
            new RoutedEventHandler((s, e) => {
                if (e is not MouseDown3DEventArgs {HitTestResult: { } hitTestResult}) {
                    return;
                }

                if (hitTestResult.ModelHit is SceneNode node && node.Tag is AttachedNodeViewModel vm) {
                    vm.Selected = !vm.Selected;
                }
            }));
    }
}