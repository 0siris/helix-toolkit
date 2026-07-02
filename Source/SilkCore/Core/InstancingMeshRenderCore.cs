/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;

namespace HelixToolkit.SharpDX.Core
{
    namespace Core
    {
        public class InstancingMeshRenderCore : MeshRenderCore
        {
            private IElementsBufferModel parameterBufferModel;

            public IElementsBufferModel ParameterBuffer
            {
                get => parameterBufferModel;
                set
                {
                    var old = parameterBufferModel;
                    if (SetAffectsCanRenderFlag(ref parameterBufferModel, value))
                    {
                        if (old != null) old.ElementChanged -= OnElementChanged;
                        if (parameterBufferModel != null) parameterBufferModel.ElementChanged += OnElementChanged;
                    }
                }
            }

            protected override bool OnAttach(IRenderTechnique technique)
            {
                return base.OnAttach(technique);
            }

            protected override bool OnUpdateCanRenderFlag()
            {
                return base.OnUpdateCanRenderFlag() && InstanceBuffer != null && InstanceBuffer.HasElements;
            }

            protected override void OnUpdatePerModelStruct(RenderContext context)
            {
                base.OnUpdatePerModelStruct(context);
                modelStruct.HasInstanceParams = ParameterBuffer != null && ParameterBuffer.HasElements ? 1 : 0;
            }

            protected override bool OnAttachBuffers(DeviceContextProxy context, ref int vertStartSlot)
            {
                if (base.OnAttachBuffers(context, ref vertStartSlot))
                {
                    ParameterBuffer?.AttachBuffer(context, ref vertStartSlot);
                    return true;
                }

                return false;
            }
        }
    }
}