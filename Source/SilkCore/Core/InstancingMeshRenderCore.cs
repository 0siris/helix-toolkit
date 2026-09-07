/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Core;

public class InstancingMeshRenderCore : MeshRenderCore {
    public IElementsBufferModel? ParameterBuffer {
        get;
        set {
            var old = field;
            if (SetAffectsCanRenderFlag(ref field, value)) {
                old?.ElementChanged -= OnElementChanged;
                field?.ElementChanged += OnElementChanged;
            }
        }
    }

    protected override bool OnUpdateCanRenderFlag()
        => base.OnUpdateCanRenderFlag() && InstanceBuffer is { HasElements: true };

    protected override void OnUpdatePerModelStruct(RenderContext context) {
        base.OnUpdatePerModelStruct(context);
        ModelStruct.HasInstanceParams = ParameterBuffer is { HasElements: true } ? 1 : 0;
    }

}
