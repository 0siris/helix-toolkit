// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Windows;
using System.Windows.Media.Media3D;
using DemoCore;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.Wpf.SharpDX.Extensions;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

namespace ExampleBrowser.Workitems.Workitem10048;

public class MainViewModel : BaseViewModel {
    private static readonly Point3D NoHit = new(double.NaN, double.NaN, double.NaN);

    public Point3D PointHit {
        get;

        set {
            if (field != value) {
                field = value;
                OnPropertyChanged(nameof(PointHit));
            }
        }
    } = NoHit;

    public MainViewModel() {
        // titles
        Title = "Simple Demo (Workitem 10048 and 10052)";
        SubTitle =
            "Select lines with left mouse button.\nRotate or zoom around a point on a line if the cursor is above one.";

        EffectsManager = new DefaultEffectsManager();
    }

    public void OnMouseDown3D(object sender, RoutedEventArgs e) {
        PointHit = (e as MouseDown3DEventArgs)?.HitTestResult?.PointHit.ToPoint3D() ?? NoHit;
    }
}
