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
using DemoCore.Automation;

namespace ExampleBrowser.Workitems.Workitem10051;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
// old issue: [Example("Issue 10051", "SharpDX: Line shader issues.")]
[Example("Issue 1074-1", "ManipulationBindings: TwoFingerPan-Rotate, Pan-Pan, Pinch-Zoom.")]
public partial class MainWindow : Window {
    private DemoAutomationHost? automationHost;

    public MainWindow() {
        InitializeComponent();
        if (DataContext is MainViewModel vm && vm.EffectsManager is { } effectsManager) {
            automationHost = DemoBootstrapper.Attach(Viewport, effectsManager);
        }

        Closed += (_, _) => {
            DemoBootstrapper.Detach(automationHost);
            automationHost = null;
            if (DataContext is IDisposable disposable) {
                disposable.Dispose();
            }
        };
    }
}
