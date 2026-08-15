using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Material.Variables;

namespace CustomShaderDemo.Materials;

public class CustomPointMaterialCore : PointMaterialCore {
    public override MaterialVariable CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique) => new CustomPointMaterialVariable(manager, technique, this);
}
