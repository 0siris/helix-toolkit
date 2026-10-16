using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Supplies application mesh shaders with constants and resources through the existing D3D12 draw bindings.
/// </summary>
/// <remarks>Implementations retain CPU parameters only; the renderer owns all native allocations and their lifetime.</remarks>
public interface ID3D12MeshMaterial
{
    /// Gets the registered technique containing the material's default and transparency passes.
    string TechniqueName { get; }

    /// Gets whether the material samples the renderer-owned shadow map.
    bool RenderShadowMap { get; }

    /// <summary>
    ///     Applies the shared b1 material fields without replacing the caller's model transform.
    /// </summary>
    /// <param name="material">The initialized shared mesh payload.</param>
    void ApplyMeshParameters(ref PhongPbrMaterialStruct material);

    /// <summary>
    ///     Writes the complete application b2 payload, including deterministic padding, for the current geometry.
    /// </summary>
    /// <param name="destination">The cleared 256-byte constant-buffer payload.</param>
    /// <param name="geometry">The currently drawn geometry, when available.</param>
    /// <param name="node">The actual owning scene node, when drawn through the scene renderer.</param>
    void WriteMaterialConstants(Span<byte> destination, Geometry3D? geometry, SceneNode? node);

    /// <summary>
    ///     Gets the texture currently bound to an application material register.
    /// </summary>
    /// <param name="shaderRegister">The t-register, excluding renderer-owned environment and pass resources.</param>
    /// <returns>The texture, or <see langword="null" /> to clear the register.</returns>
    TextureModel? GetTexture(int shaderRegister);

    /// <summary>
    ///     Gets the sampler currently bound to an application material register.
    /// </summary>
    /// <param name="shaderRegister">The s-register.</param>
    /// <returns>The sampler, or <see langword="null" /> for the renderer default.</returns>
    SamplerStateDescription? GetSampler(int shaderRegister);
}
