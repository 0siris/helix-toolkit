// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2021 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.Wpf.SharpDX.Camera;
using Color = System.Windows.Media.Color;
using Colors = System.Windows.Media.Colors;
using Point3D = System.Windows.Media.Media3D.Point3D;
using Transform3D = System.Windows.Media.Media3D.Transform3D;
using TranslateTransform3D = System.Windows.Media.Media3D.TranslateTransform3D;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

namespace DynamicPointsAndLines;

public class MainViewModel : INotifyPropertyChanged, IDisposable {
    #region INotifyPropertyChanged Support

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? info = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));

    protected bool SetValue<T>(ref T backingField, T value, [CallerMemberName] string propertyName = "") {
        if (Equals(backingField, value)) {
            return false;
        }

        backingField = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.
            IDisposable? effectManager = EffectsManager;
            Disposer.RemoveAndDispose(ref effectManager);

            disposedValue = true;
            GC.SuppressFinalize(this);
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~MainViewModel() {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose() {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }

    #endregion

    public LineGeometry3D Lines { get; }
    public PointGeometry3D Points { get; }
    public Transform3D Lines1Transform { get; }
    public Transform3D Lines2Transform { get; }
    public Transform3D Points1Transform { get; }
    public Vector3D DirectionalLightDirection { get; }
    public Color DirectionalLightColor { get; }
    public Color AmbientLightColor { get; }
    public Stopwatch StopWatch { get; }

    public IEffectsManager EffectsManager { get; }
    public Camera Camera { get; }

    public int NumberOfPoints {
        get;
        set {
            StopWatch.Stop();

            SetValue(ref field, value);
            var indices = new IntCollection((field - 1) * 2);
            for (var i = 0; i < (field - 1) * 2; i++) {
                indices.Add(i);
            }

            Lines.Indices = indices;

            StopWatch.Start();
        }
    }

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();
        Camera = new PerspectiveCamera {
            Position = new Point3D(-90, 0, 95),
            LookDirection = new Vector3D(110, 0, -105),
            UpDirection = new Vector3D(0, 1, 0)
        };

        // setup lighting
        AmbientLightColor = Colors.DimGray;
        DirectionalLightColor = Colors.White;
        DirectionalLightDirection = new Vector3D(-2, -5, -2);

        // model trafos
        Lines1Transform = new TranslateTransform3D(0, 0, 45);
        Lines2Transform = new TranslateTransform3D(0, 0, -45);
        Points1Transform = new TranslateTransform3D(0, 0, 0);

        Lines = new LineGeometry3D {
            IsDynamic = true,
            Positions = []
        };
        Points = new PointGeometry3D {
            IsDynamic = true,
            Positions = []
        };

        StopWatch = new Stopwatch();
        StopWatch.Start();

        NumberOfPoints = 900;
    }

    public void UpdatePoints() {
        if (StopWatch.IsRunning && Points.Positions is { } pointPositions && Lines.Positions is { } linePositions) {
            pointPositions.Clear();
            pointPositions.AddRange(GeneratePoints(NumberOfPoints, StopWatch.ElapsedMilliseconds * 0.003));
            linePositions.Clear();
            linePositions.AddRange(pointPositions);
            Points.UpdateVertices();
            Lines.UpdateVertices();
        }
    }

    private static IEnumerable<Vector3> GeneratePoints(int n, double time) {
        const double r = 30;
        const double q = 5;
        for (var i = 0; i < n; i++) {
            var t = Math.PI * 2 * i / (n - 1);
            var u = (t * 24) + (time * 5);
            var pt = new Vector3((float) (Math.Cos(t) * (r + (q * Math.Cos(u)))),
                (float) (Math.Sin(t) * (r + (q * Math.Cos(u)))),
                (float) (q * Math.Sin(u)));
            yield return pt;
            if (i > 0 && i < n - 1) {
                yield return pt;
            }
        }
    }
}