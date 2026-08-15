/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.ComponentModel;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

namespace HelixToolkit.SharpDX.Core.Model.Components;
public sealed class GeometryBoundManager : IDisposable {
    public delegate bool OnCheckGeometryDelegate(Geometry3D? geometry);

    private readonly WeakReference<GeometryNode> elementCore;
    public OnCheckGeometryDelegate? OnCheckGeometry;

    public GeometryBoundManager(GeometryNode core) {
        elementCore = new WeakReference<GeometryNode>(core);
        core.TransformChanged += OnTransformChanged;
    }

    private void OnGeometryPropertyChangedPrivate(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(Geometry3D.Positions) ||
            e.PropertyName == Geometry3D.VertexBuffer)
            GeometryValid = OnCheckGeometry?.Invoke(geometry) ?? CheckGeometry();
        else if (e.PropertyName == nameof(Geometry3D.Bound))
            UpdateBoundingBox();
        else if (e.PropertyName == nameof(Geometry3D.BoundingSphere)) UpdateBoundingSphere();
    }

    /// <summary>
    ///     <para>Check geometry validity.</para>
    ///     Return false if (this.geometryInternal == null || this.geometryInternal.Positions == null ||
    ///     this.geometryInternal.Positions.Count == 0 || this.geometryInternal.Indices == null ||
    ///     this.geometryInternal.Indices.Count == 0)
    /// </summary>
    /// <returns>
    /// </returns>
    private bool CheckGeometry() => Geometry?.Positions is { Count: > 0 };

    private void OnTransformChanged(object? sender, TransformArgs e) {
        var oldBound = BoundsWithTransform;
        BoundsWithTransform = Bounds.Transform(e);
        RaiseOnTransformBoundChanged(BoundsWithTransform, oldBound);
        var oldSphere = BoundsSphereWithTransform;
        BoundsSphereWithTransform = BoundsSphere.TransformBoundingSphere(e);
        RaiseOnTransformBoundSphereChanged(BoundsSphereWithTransform, oldSphere);
    }

    private void UpdateBoundingBox() {
        var geometry = Geometry;
        if (!GeometryValid || geometry is null) {
            Bounds = DefaultBound;
            BoundsWithTransform = DefaultBound;
        } else {
            if (!elementCore.TryGetTarget(out var target)) return;
            BoundingBox oldBound;
            if (Instances is not { Count: > 0 } instances) {
                oldBound = Bounds;
                Bounds = geometry.Bound;
                RaiseOnBoundChanged(Bounds, oldBound);
                oldBound = BoundsWithTransform;
                BoundsWithTransform = Bounds.Transform(target.TotalModelMatrixInternal);
                RaiseOnTransformBoundChanged(BoundsWithTransform, oldBound);
            } else {
                var bound = geometry.Bound.Transform(instances[0]);
                foreach (var instance in instances) {
                    var b = geometry.Bound.Transform(instance);
                    BoundingBox.Merge(ref bound, ref b, out bound);
                }

                oldBound = Bounds;
                Bounds = bound;
                RaiseOnBoundChanged(Bounds, oldBound);
                oldBound = BoundsWithTransform;
                BoundsWithTransform = Bounds.Transform(target.TotalModelMatrixInternal);
                RaiseOnTransformBoundChanged(BoundsWithTransform, oldBound);
            }
        }
    }

    private void UpdateBoundingSphere() {
        var geometry = Geometry;
        if (!GeometryValid || geometry is null) {
            BoundsSphere = DefaultBoundSphere;
            BoundsSphereWithTransform = DefaultBoundSphere;
        } else {
            if (!elementCore.TryGetTarget(out var target)) return;
            BoundingSphere oldSphere;
            if (Instances is not { Count: > 0 } instances) {
                oldSphere = BoundsSphere;
                BoundsSphere = geometry.BoundingSphere;
                RaiseOnBoundSphereChanged(BoundsSphere, oldSphere);
                oldSphere = BoundsSphereWithTransform;
                BoundsSphereWithTransform =
                    BoundsSphere.TransformBoundingSphere(target.TotalModelMatrixInternal);
                RaiseOnTransformBoundSphereChanged(BoundsSphereWithTransform, oldSphere);
            } else {
                var boundSphere = geometry.BoundingSphere.TransformBoundingSphere(instances[0]);
                foreach (var instance in instances) {
                    var bs = geometry.BoundingSphere.TransformBoundingSphere(instance);
                    BoundingSphereExtensions.Merge(ref boundSphere, ref bs, out boundSphere);
                }

                oldSphere = BoundsSphere;
                BoundsSphere = boundSphere;
                RaiseOnBoundSphereChanged(BoundsSphere, oldSphere);
                oldSphere = BoundsSphereWithTransform;
                BoundsSphereWithTransform =
                    BoundsSphere.TransformBoundingSphere(target.TotalModelMatrixInternal);
                RaiseOnTransformBoundSphereChanged(BoundsSphereWithTransform, oldSphere);
            }
        }
    }

    private void UpdateBounds() {
        GeometryValid = OnCheckGeometry?.Invoke(geometry) ?? CheckGeometry();
        UpdateBoundingBox();
        UpdateBoundingSphere();
    }

    public void DisposeAndClear() {
        Geometry = null;
    }

    #region Properties

    private Geometry3D? geometry;

    /// <summary>
    /// </summary>
    public Geometry3D? Geometry {
        get => geometry;
        set {
            if (geometry == value) return;
            var old = geometry;
            geometry = value;
            if (geometry != null && geometry.Bound.Maximum == Vector3.Zero &&
                geometry.Bound.Minimum == Vector3.Zero) geometry.UpdateBounds();
            old?.PropertyChanged -= OnGeometryPropertyChangedPrivate;
            if (geometry != null) {
                geometry.PropertyChanged += OnGeometryPropertyChangedPrivate;
                OriginalBounds = geometry.Bound;
                OriginalBoundsSphere = geometry.BoundingSphere;
            } else {
                OriginalBounds = DefaultBound;
                OriginalBoundsSphere = DefaultBoundSphere;
            }

            UpdateBounds();
        }
    }

    private IList<Matrix>? instances;

    public IList<Matrix>? Instances {
        get => instances;
        set {
            if (instances == value) return;
            instances = value;
            UpdateBounds();
        }
    }

    public bool HasInstances => instances is { Count: > 0 };

    public bool GeometryValid { get; private set; }

    #region Bounds

    public static readonly BoundingBox DefaultBound = new();
    public static readonly BoundingSphere DefaultBoundSphere = new();

    /// <summary>
    ///     Gets the original bound from the geometry. Same as <see cref="Geometry3D.Bound" />
    /// </summary>
    /// <value>
    ///     The original bound.
    /// </value>
    public BoundingBox OriginalBounds;

    /// <summary>
    ///     Gets the original bound sphere from the geometry. Same as <see cref="Geometry3D.BoundingSphere" />
    /// </summary>
    /// <value>
    ///     The original bound sphere.
    /// </value>
    public BoundingSphere OriginalBoundsSphere;

    /// <summary>
    ///     Gets the bounds. Usually same as <see cref="OriginalBounds" />. If have instances, the bound will enclose all
    ///     instances.
    /// </summary>
    /// <value>
    ///     The bounds.
    /// </value>
    public BoundingBox Bounds = DefaultBound;

    /// <summary>
    ///     Gets the bounds with transform. Usually same as <see cref="Bounds" />. If have transform, the bound is the
    ///     transformed <see cref="Bounds" />
    /// </summary>
    /// <value>
    ///     The bounds with transform.
    /// </value>
    public BoundingBox BoundsWithTransform = DefaultBound;

    /// <summary>
    ///     Gets the bounds sphere. Usually same as <see cref="OriginalBoundsSphere" />. If have instances, the bound sphere
    ///     will enclose all instances.
    /// </summary>
    /// <value>
    ///     The bounds sphere.
    /// </value>
    public BoundingSphere BoundsSphere = DefaultBoundSphere;

    /// <summary>
    ///     Gets the bounds sphere with transform. If have transform, the bound is the transformed <see cref="BoundsSphere" />
    /// </summary>
    /// <value>
    ///     The bounds sphere with transform.
    /// </value>
    public BoundingSphere BoundsSphereWithTransform = DefaultBoundSphere;

    public bool HasBound { get; set; } = true;

    #endregion

    #endregion

    #region Events and Delegates

    public event EventHandler<BoundChangeArgs<BoundingBox>>? OnBoundChanged;

    public event EventHandler<BoundChangeArgs<BoundingBox>>? OnTransformBoundChanged;

    public event EventHandler<BoundChangeArgs<BoundingSphere>>? OnBoundSphereChanged;

    public event EventHandler<BoundChangeArgs<BoundingSphere>>? OnTransformBoundSphereChanged;

    private void RaiseOnTransformBoundChanged(BoundingBox newBound, BoundingBox oldBound) {
        OnTransformBoundChanged?.Invoke(elementCore,
                                        new BoundChangeArgs<BoundingBox>(ref newBound, ref oldBound));
    }

    private void RaiseOnBoundChanged(BoundingBox newBound, BoundingBox oldBound) {
        OnBoundChanged?.Invoke(elementCore, new BoundChangeArgs<BoundingBox>(ref newBound, ref oldBound));
    }


    private void RaiseOnTransformBoundSphereChanged(
        BoundingSphere newBoundSphere,
        BoundingSphere oldBoundSphere
    ) {
        OnTransformBoundSphereChanged?.Invoke(elementCore,
                                              new BoundChangeArgs<BoundingSphere>(
                                                  ref newBoundSphere,
                                                  ref oldBoundSphere));
    }


    private void RaiseOnBoundSphereChanged(BoundingSphere newBoundSphere, BoundingSphere oldBoundSphere) {
        OnBoundSphereChanged?.Invoke(elementCore,
                                     new BoundChangeArgs<BoundingSphere>(
                                         ref newBoundSphere,
                                         ref oldBoundSphere));
    }

    #endregion

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    private void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                geometry?.PropertyChanged -= OnGeometryPropertyChangedPrivate;
                if (elementCore.TryGetTarget(out var target)) target.TransformChanged -= OnTransformChanged;
                OnBoundChanged = null;
                OnTransformBoundChanged = null;
                OnBoundSphereChanged = null;
                OnTransformBoundSphereChanged = null;
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~GeometryBoundManager() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose() {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }

    #endregion
}
