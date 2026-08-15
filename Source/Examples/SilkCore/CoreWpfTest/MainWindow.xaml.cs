// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

namespace CoreWpfTest;

using System.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        DataContext = new MainViewModel();
        view.AddHandler(Element3D.MouseDown3DEvent,
            new RoutedEventHandler((_, e) => {
                if (e is not MouseDown3DEventArgs {HitTestResult: { } hitTestResult}) {
                    return;
                }

                if (hitTestResult.ModelHit is SceneNode node && node.Tag is AttachedNodeViewModel vm) {
                    vm.Selected = !vm.Selected;
                }
            }));
    }
}