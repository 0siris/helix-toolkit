using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Material;

namespace HelixToolkit.Wpf.SharpDX.Material;

/// <summary>
///     Render color by triangle normal
/// </summary>
public sealed class NormalMaterial : Material {
    public NormalMaterial() { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="NormalMaterial" /> class.
    /// </summary>
    /// <param name="core">The core.</param>
    public NormalMaterial(NormalMaterialCore core) : base(core) { }

    /// <summary>
    ///     Called when [create core].
    /// </summary>
    /// <returns></returns>
    protected override MaterialCore OnCreateCore() => NormalMaterialCore.Core;

    protected override Freezable CreateInstanceCore() => new NormalMaterial {
        Name = Name
    };
}
