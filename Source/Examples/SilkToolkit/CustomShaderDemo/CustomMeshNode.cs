namespace CustomShaderDemo;

public class CustomMeshNode : MeshNode {
    public float HeightScale {
        set {
            if (RenderCore is CustomMeshCore core) {
                core.DataHeightScale = value;
            }
        }
        get => RenderCore is CustomMeshCore core
            ? core.DataHeightScale
            : 0;
    }

    protected override RenderCore OnCreateRenderCore() => new CustomMeshCore();

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager)
        => effectsManager[CustomShaderNames.DataSampling];
}