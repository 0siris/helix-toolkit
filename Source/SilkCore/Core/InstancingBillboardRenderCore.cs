/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core.Core;

public class InstancingBillboardRenderCore : PointLineRenderCore {
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
        => base.OnUpdateCanRenderFlag() && InstanceBuffer is {HasElements: true};

    protected override void OnUpdatePerModelStruct() {
        base.OnUpdatePerModelStruct();
        ModelStruct.HasInstanceParams = ParameterBuffer is {HasElements: true} ? 1 : 0;
    }

    protected override bool OnAttachBuffers(DeviceContextProxy context, ref int vertStartSlot) {
        if (base.OnAttachBuffers(context, ref vertStartSlot)) {
            ParameterBuffer?.AttachBuffer(context, ref vertStartSlot);
            return true;
        }

        return false;
    }
}