/*
The MIT License(MIT)
Copyright(c) 2018 Helix Toolkit contributors
*/


using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;

namespace HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
public abstract class ParametricSurface3DNode : MeshNode {
    private CancellationTokenSource? cancelToken = new();

    private Task? tesselationTask;

    public int MeshSizeU {
        get;
        set {
            if (Set(ref field, value)) TessellateAsync();
        }
    } = 120;

    public int MeshSizeV {
        get;
        set {
            if (Set(ref field, value)) TessellateAsync();
        }
    } = 120;

    public bool IsTessellating {
        get;
        private set => Set(ref field, value);
    }

    protected override bool OnAttach(IEffectsManager effectsManager) {
        cancelToken = new CancellationTokenSource();
        return base.OnAttach(effectsManager);
    }

    protected override void OnDetach() {
        cancelToken?.Cancel(true);
        RemoveAndDispose(ref cancelToken);
        base.OnDetach();
    }

    protected void TessellateAsync() {
        cancelToken?.Cancel(true);
        RemoveAndDispose(ref cancelToken);
        cancelToken = new CancellationTokenSource();
        IsTessellating = true;
        var token = cancelToken.Token;
        tesselationTask = Task.Run(() => {
            var mesh = OnTesselatingAsync(token);
            mesh.Normals = mesh.CalculateNormals();
            mesh.UpdateOctree();
            mesh.UpdateBounds();
            return mesh;
        },
                                   token).ContinueWith(result => {
                                       IsTessellating = false;
                                       if (result.IsCompleted) Geometry = result.Result;
                                   },
                                                       TaskScheduler.FromCurrentSynchronizationContext());
    }

    protected virtual MeshGeometry3D OnTesselatingAsync(CancellationToken token) {
        var mesh = new MeshGeometry3D {
            Positions = [],
            TextureCoordinates = [],
            Indices = []
        };
        if (mesh.Positions is not { } positions || mesh.TextureCoordinates is not { } textureCoordinates
            || mesh.TriangleIndices is not { })
            throw new InvalidOperationException("The parametric mesh buffers were not initialized.");

        var n = MeshSizeU;
        var m = MeshSizeV;
        var p = new Vector3[m * n];
        var tc = new Vector2[m * n];

        // todo: use MeshBuilder

        // todo: parallel execution...
        // Parallel.For(0, n, (i) =>
        for (var i = 0; i < n && !token.IsCancellationRequested; i++) {
            var u = 1.0 * i / (n - 1);

            for (var j = 0; j < m; j++) {
                var v = 1.0 * j / (m - 1);
                var ij = i * m + j;
                p[ij] = Evaluate(u, v, out tc[ij]);
            }
        }

        // );
        var idx = 0;
        for (var i = 0; i < n && !token.IsCancellationRequested; i++)
            for (var j = 0; j < m; j++) {
                positions.Add(p[idx]);
                textureCoordinates.Add(tc[idx]);
                idx++;
            }

        for (var i = 0; i + 1 < n && !token.IsCancellationRequested; i++)
            for (var j = 0; j + 1 < m; j++) {
                var x0 = i * m;
                var x1 = (i + 1) * m;
                var y0 = j;
                var y1 = j + 1;
                AddTriangle(mesh, x0 + y0, x1 + y0, x0 + y1);
                AddTriangle(mesh, x1 + y0, x1 + y1, x0 + y1);
            }

        return mesh;
    }

    /// <summary>
    ///     The add triangle.
    /// </summary>
    /// <param name="mesh">
    ///     The mesh.
    /// </param>
    /// <param name="i1">
    ///     The i 1.
    /// </param>
    /// <param name="i2">
    ///     The i 2.
    /// </param>
    /// <param name="i3">
    ///     The i 3.
    /// </param>
    private static void AddTriangle(MeshGeometry3D mesh, int i1, int i2, int i3) {
        if (mesh.Positions is not { } positions) return;
        var p1 = positions[i1];
        if (!IsDefined(p1)) return;

        var p2 = positions[i2];
        if (!IsDefined(p2)) return;

        var p3 = positions[i3];
        if (!IsDefined(p3)) return;

        if (mesh.TriangleIndices is not { } triangleIndices) return;
        triangleIndices.Add(i1);
        triangleIndices.Add(i2);
        triangleIndices.Add(i3);
    }

    /// <summary>
    ///     Evaluates the surface at the specified u,v parameters.
    /// </summary>
    /// <param name="u">
    ///     The u parameter.
    /// </param>
    /// <param name="v">
    ///     The v parameter.
    /// </param>
    /// <param name="textureCoord">
    ///     The texture coordinates.
    /// </param>
    /// <returns>
    ///     The evaluated <see cref="Vector3" />.
    /// </returns>
    protected abstract Vector3 Evaluate(double u, double v, out Vector2 textureCoord);

    /// <summary>
    ///     Determines whether the specified point is defined.
    /// </summary>
    /// <param name="point">
    ///     The point.
    /// </param>
    /// <returns>
    ///     The is defined.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDefined(Vector3 point) => !double.IsNaN(point.X) && !double.IsNaN(point.Y) && !double.IsNaN(point.Z);
}
