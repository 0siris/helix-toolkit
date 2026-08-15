using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Material;

namespace HelixToolkit.Wpf.SharpDX.Material;

/// <summary>
///     Render color by mesh vertex color
/// </summary>
public sealed class VertColorMaterial : Material {
    public VertColorMaterial() { }

    public VertColorMaterial(ColorMaterialCore core) : base(core) { }

    protected override MaterialCore OnCreateCore() => ColorMaterialCore.Core;

    protected override Freezable CreateInstanceCore() => new VertColorMaterial {
        Name = Name
    };
}
