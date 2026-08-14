using System.Windows;
using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Render color by mesh vertex position
/// </summary>
public sealed class PositionColorMaterial : Material {
    public PositionColorMaterial() { }

    public PositionColorMaterial(PositionMaterialCore core) : base(core) { }

    protected override MaterialCore OnCreateCore() => PositionMaterialCore.Core;

    protected override Freezable CreateInstanceCore() => new PositionColorMaterial {
        Name = Name
    };
}
