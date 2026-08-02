/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        public class InstancingBillboardRenderCore : PointLineRenderCore {
            private IElementsBufferModel parameterBufferModel;

            public IElementsBufferModel ParameterBuffer {
                get => parameterBufferModel;
                set {
                    var old = parameterBufferModel;
                    if (SetAffectsCanRenderFlag(ref parameterBufferModel, value)) {
                        old?.ElementChanged -= OnElementChanged;
                        parameterBufferModel?.ElementChanged += OnElementChanged;
                    }
                }
            }

            protected override bool OnUpdateCanRenderFlag() {
                return base.OnUpdateCanRenderFlag() && InstanceBuffer != null && InstanceBuffer.HasElements;
            }

            protected override void OnUpdatePerModelStruct() {
                base.OnUpdatePerModelStruct();
                modelStruct.HasInstanceParams = ParameterBuffer != null && ParameterBuffer.HasElements ? 1 : 0;
            }

            protected override bool OnAttachBuffers(DeviceContextProxy context, ref int vertStartSlot) {
                if (base.OnAttachBuffers(context, ref vertStartSlot)) {
                    ParameterBuffer?.AttachBuffer(context, ref vertStartSlot);
                    return true;
                }

                return false;
            }
        }
    }
}
