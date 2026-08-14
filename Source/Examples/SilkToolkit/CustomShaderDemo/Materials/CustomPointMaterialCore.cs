namespace CustomShaderDemo.Materials;

public class CustomPointMaterialCore : PointMaterialCore {
    public override MaterialVariable CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique) => new CustomPointMaterialVariable(manager, technique, this);
}
