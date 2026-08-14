namespace CustomShaderDemo;

public class CustomMeshCore : MeshRenderCore {
    private float dataHeightScale = 5;

    public float DataHeightScale {
        set => SetAffectsRender(ref dataHeightScale, value);
        get => dataHeightScale;
    }

    protected override void OnUpdatePerModelStruct(RenderContext context) {
        base.OnUpdatePerModelStruct(context);
        ModelStruct.Params.Y = dataHeightScale;
    }
}
