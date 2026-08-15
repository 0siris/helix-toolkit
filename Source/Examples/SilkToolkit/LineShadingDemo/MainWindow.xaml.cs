// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Interaction logic for MainWindow.xaml
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Linq;
using System.Windows;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

namespace LineShadingDemo;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        var viewModel = new MainViewModel();
        DataContext = viewModel;

        // mouse events            
        view1.MouseDown += (_, e) => {
            var hits = ViewportExtensions.FindHits(view1, e.GetPosition(view1));
            if (hits.Count > 0) {
                foreach (var hit in hits.Where(h => h.IsValid)) {
                    if (hit.ModelHit is Element3D element3D) {
                        element3D.RaiseEvent(
                            new MouseDown3DEventArgs(hit.ModelHit, hit, e.GetPosition(view1), null, e));
                        if (e.Handled) {
                            break;
                        }
                    }
                }
            }
        };
    }
}