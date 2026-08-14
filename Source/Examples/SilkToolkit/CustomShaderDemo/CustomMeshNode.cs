namespace CustomShaderDemo;

public class CustomMeshNode : MeshNode {
    public float HeightScale {
        set => (RenderCore as CustomMeshCore).DataHeightScale = value;
        get => (RenderCore as CustomMeshCore).DataHeightScale;
    }

    protected override RenderCore OnCreateRenderCore() => new CustomMeshCore();

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[CustomShaderNames.DataSampling];
}
