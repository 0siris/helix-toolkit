// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using DemoCore.Automation;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

namespace FileLoadDemo;

using System;
using System.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        DataContext = new MainViewModel(this);

        View.AddHandler(Element3D.MouseDown3DEvent,
            new RoutedEventHandler((_, e) => {
                if (e is not MouseDown3DEventArgs {HitTestResult: { } hitTestResult}) {
                    return;
                }

                if (hitTestResult.ModelHit is SceneNode {Tag: AttachedNodeViewModel vm}) {
                    vm.Selected = !vm.Selected;
                }
            }));

        Closed += (_, _) => {
            DemoBootstrapper.Detach((DataContext as MainViewModel)?.AutomationHost);
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }
}
