using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Model;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace CustomShaderDemo.Materials;

public class CustomPointMaterialVariable : PointMaterialVariable {
    private readonly ConstantBufferComponent customConstantBuffer;
    private Vector3 colorChanges = new(1, 1, 1);

    public CustomPointMaterialVariable(
        IEffectsManager manager,
        IRenderTechnique technique,
        PointMaterialCore materialCore,
        string pointPassName = "CustomPointPass"
    )
        : base(manager, technique, materialCore, pointPassName) {
        customConstantBuffer =
            new ConstantBufferComponent(new ConstantBufferDescription("CustomBuffer", Marshal.SizeOf<Vector4>()));
        customConstantBuffer.Attach(technique);
    }

    protected override void OnInitialPropertyBindings() {
        base.OnInitialPropertyBindings();
    }

    public override bool BindMaterialResources(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderPass shaderPass
    ) {
        colorChanges += new Vector3(0.1f, 0.3f, 0.7f);
        colorChanges.X %= 100;
        colorChanges.Y %= 100;
        colorChanges.Z %= 100;
        customConstantBuffer.WriteValueByName("random_color",
                                              new Vector3(colorChanges.X / 100,
                                                          colorChanges.Y / 100,
                                                          colorChanges.Z / 100));
        customConstantBuffer.Upload(deviceContext);
        return base.BindMaterialResources(context, deviceContext, shaderPass);
    }

    protected override void OnDispose(bool disposeManagedResources) {
        customConstantBuffer.Detach();
        customConstantBuffer.Dispose();
        base.OnDispose(disposeManagedResources);
    }
}
