using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

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
