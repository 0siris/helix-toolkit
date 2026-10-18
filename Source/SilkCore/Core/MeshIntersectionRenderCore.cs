using HelixToolkit.SharpDX.Core;
using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.SharpDX.Core.Core;
/// <summary>Identifies an original triangle pair and the caller's correlation ID in the source 16-byte layout.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct IntersectionCandidate {
    /// Gets the first original triangle index.
    public readonly int TriangleA;
    /// Gets the second original triangle index.
    public readonly int TriangleB;
    /// Gets the caller's pair identifier.
    public readonly int PairId;
    /// Keeps the source structured element aligned to sixteen bytes.
    private readonly int padding;
    /// <summary>
    ///     Creates an original triangle pair.
    /// </summary>
    /// <param name="triangleA">The first triangle index.</param>
    /// <param name="triangleB">The second triangle index.</param>
    /// <param name="pairId">The propagated correlation ID.</param>
    public IntersectionCandidate(int triangleA, int triangleB, int pairId = 0) {
        TriangleA = triangleA;
        TriangleB = triangleB;
        PairId = pairId;
        padding = 0;
    }
}
/// <summary>Matches the source append element: two world-space float3 endpoints followed by the pair ID.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct IntersectionSegment {
    /// The world-space segment origin.
    public Vector3 Origin;
    /// The world-space segment endpoint.
    public Vector3 End;
    /// The original candidate's correlation ID.
    public int PairId;
}
/// <summary>Matches the row-major mesh-transform constant payload consumed by both source kernels.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct IntersectionTransforms {
    /// The first row-major world matrix.
    internal Matrix WorldA;
    /// The second row-major world matrix.
    internal Matrix WorldB;
    /// The valid candidate count, excluding allocation capacity.
    internal uint CandidateCount;
    /// The source constant-buffer padding.
    private uint padding;
    /// The remaining source constant-buffer padding.
    private Vector2 padding2;
}
/// <summary>
///     Owns immutable original positions and triangle order for one GPU input generation.
/// </summary>
/// <remarks>A borrowed render owner is used only while its complete positions and indices match this snapshot.</remarks>
public sealed class IntersectionMeshSnapshot {
    /// The owned float4 position sidecar.
    internal readonly Vector4[] Positions;
    /// The owned original triangle index stream.
    internal readonly int[] Indices;

    /// The borrowed scene owner whose native default mesh buffer can be created during attachment.
    private readonly MeshNode? renderNode;
    /// Gets the actual current native render-buffer owner without creating a second rendering allocation.
    internal DefaultMeshGeometryBufferModel? RenderBuffer => renderNode is { IsDisposed: false,
        RenderCore: GeometryRenderCore { GeometryBuffer: DefaultMeshGeometryBufferModel buffer } } ? buffer : field;
    /// <summary>
    ///     Copies and validates original geometry without retaining mutable caller storage.
    /// </summary>
    /// <param name="positions">Mesh-local positions in original vertex order.</param>
    /// <param name="indices">Three original vertex indices per original triangle.</param>
    /// <param name="renderBuffer">The optional real rendering owner for shared-index dispatch.</param>
    /// <param name="renderNode">The optional borrowed scene owner, resolved after native attachment.</param>
    /// <exception cref="ArgumentException">Geometry is nonfinite, incomplete or contains invalid indices.</exception>
    public IntersectionMeshSnapshot(IEnumerable<Vector3> positions, IEnumerable<int> indices,
        DefaultMeshGeometryBufferModel? renderBuffer = null, MeshNode? renderNode = null) {
        Positions = positions.Select(position => new Vector4(position, 1)).ToArray();
        Indices = indices.ToArray();
        if (Indices.Length % 3 != 0 || Indices.Any(index => index < 0 || index >= Positions.Length) ||
            Positions.Any(position => !float.IsFinite(position.X) || !float.IsFinite(position.Y) || !float.IsFinite(position.Z)))
            throw new ArgumentException("Intersection geometry must contain finite positions and complete valid triangles");
        this.RenderBuffer = renderBuffer;
        this.renderNode = renderNode;
    }
    /// Gets the original triangle count.
    public int TriangleCount => Indices.Length / 3;
    /// <summary>
    ///     Checks both topology and position identity, including same-size geometry edits.
    /// </summary>
    /// <returns>Whether the borrowed live rendering owner still represents this exact immutable snapshot.</returns>
    internal bool CanShare() => RenderBuffer is { IsDisposed: false, Geometry: MeshGeometry3D geometry } &&
        geometry.Positions is { } positions && geometry.Indices is { } indices &&
        positions.Count == Positions.Length && indices.SequenceEqual(Indices) &&
        positions.Select(position => new Vector4(position, 1)).SequenceEqual(Positions);
}
/// <summary>Owns a validated immutable candidate/pose snapshot for a mesh-pair dispatch.</summary>
public sealed class IntersectionDispatchInput {
    /// Gets the first immutable source mesh.
    public IntersectionMeshSnapshot First { get; }
    /// Gets the second immutable source mesh.
    public IntersectionMeshSnapshot Second { get; }
    /// The owned source candidate storage.
    internal readonly IntersectionCandidate[] Candidates;
    /// Gets the first world matrix.
    public Matrix WorldA { get; }
    /// Gets the second world matrix.
    public Matrix WorldB { get; }
    /// <summary>
    ///     Copies candidates and validates their original triangle identities before publication.
    /// </summary>
    /// <param name="first">The first immutable mesh.</param>
    /// <param name="second">The second immutable mesh.</param>
    /// <param name="worldA">The first row-major world matrix.</param>
    /// <param name="worldB">The second row-major world matrix.</param>
    /// <param name="candidates">Explicit original triangle pairs; empty means no dispatch.</param>
    /// <exception cref="ArgumentOutOfRangeException">A candidate index or world matrix is invalid.</exception>
    public IntersectionDispatchInput(IntersectionMeshSnapshot first, IntersectionMeshSnapshot second,
        Matrix worldA, Matrix worldB, IEnumerable<IntersectionCandidate> candidates) {
        First = first;
        Second = second;
        WorldA = worldA;
        WorldB = worldB;
        Candidates = candidates.ToArray();
        if (Candidates.Any(candidate => candidate.TriangleA < 0 || candidate.TriangleA >= first.TriangleCount ||
            candidate.TriangleB < 0 || candidate.TriangleB >= second.TriangleCount) ||
            !Finite(worldA) || !Finite(worldB))
            throw new ArgumentOutOfRangeException(nameof(candidates), "Intersection candidates and transforms must be valid");
    }
    /// <summary>
    ///     Checks every matrix component at the concrete GPU serialization boundary.
    /// </summary>
    /// <param name="matrix">The row-major matrix.</param>
    /// <returns>Whether all sixteen components are finite.</returns>
    private static bool Finite(Matrix matrix) => MemoryMarshal.Cast<Matrix, float>(MemoryMarshal.CreateReadOnlySpan(ref matrix, 1))
        .ToArray().All(float.IsFinite);
}
/// <summary>
///     Publishes immutable selected-pair work for the existing native renderer's compute/indirect-line owner.
/// </summary>
/// <remarks>Core computations and compatibility line geometry remain owned by the application, outside this UI-free toolkit.</remarks>
public sealed class MeshIntersectionRenderCore : PointLineRenderCore {
    /// <summary>
    ///     Gets or sets whether native rendering uses the source transverse GPU kernel. Set to false to render
    ///     application-provided Core geometry, including coplanar cases outside the source kernel's scope.
    /// </summary>
    public bool UseGpu {
        get;
        set {
            if (field == value) return;
            field = value;
            RaiseInvalidateRender();
        }
    } = true;
    /// Gets the immutable current dispatch generation.
    internal IReadOnlyList<IntersectionDispatchInput> Inputs { get; private set; } = [];
    /// <summary>
    ///     Publishes a complete input generation after cancellation and validation have succeeded.
    /// </summary>
    /// <param name="inputs">The complete next immutable input sequence.</param>
    /// <param name="cancellationToken">Cancellation checked before publication.</param>
    /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
    public void SetInputs(IEnumerable<IntersectionDispatchInput> inputs, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = Array.AsReadOnly(inputs.ToArray());
        cancellationToken.ThrowIfCancellationRequested();
        Inputs = snapshot;
        RaiseInvalidateRender();
    }
    /// <summary>Creates the ordinary point/line material core with additive native compute behavior.</summary>
    public MeshIntersectionRenderCore() { }
}
