using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using System.Runtime.InteropServices;

namespace HelixToolkit.SharpDX.Core.Native;

/// Supplies application vertex channels and technique selection to the existing native mesh renderer.
public interface ID3D12MeshShaderProvider
{
    /// <summary>
    ///     Reads the current geometry's per-vertex curvature and shading hints.
    /// </summary>
    /// <param name="geometry">The actual geometry retained by the scene node.</param>
    /// <returns>One value per position, or an empty array when no additional stream is needed.</returns>
    D3D12MeshVertexData[] GetVertexData(Geometry3D geometry);

    /// <summary>
    ///     Resolves an application material while retaining explicit node technique overrides.
    /// </summary>
    /// <param name="node">The actual scene node.</param>
    /// <param name="selectedTechnique">The technique selected by the existing node contract.</param>
    /// <returns>The registered technique used by the native renderer.</returns>
    string GetTechniqueName(SceneNode node, string selectedTechnique);

    /// <summary>
    ///     Preserves an application's original transparency pass selection when it differs from native defaults.
    /// </summary>
    /// <param name="node">The actual scene node.</param>
    /// <param name="depthPeeling">Whether depth peeling is active.</param>
    /// <param name="initialize">Whether the min/max initialization stage is active.</param>
    /// <returns>The application pass, or <see langword="null"/> to retain the existing native selection.</returns>
    string? GetTransparencyPassName(SceneNode node, bool depthPeeling, bool initialize) => null;

}

/// Contains the two float4 channels consumed by extended mesh vertex shaders in input slot three.
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct D3D12MeshVertexData
{
    /// Contains mean, Gaussian, minimum and maximum curvature.
    public Vector4 Curvature;

    /// Contains border, shape-index, curvedness and organic-mask hints.
    public Vector4 ShadingHints;
}
