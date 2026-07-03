/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Core {
        public sealed class BoneUploaderCore : RenderCore {
            private static readonly Matrix[] empty = new Matrix[0];
            private Matrix[] boneMatrices = empty;
            public StructuredBufferProxy boneSkinSB;
            private bool matricesChanged = true;

            public BoneUploaderCore() : base(RenderType.None) {
                NeedUpdate = false;
            }

            public Matrix[] BoneMatrices {
                get => boneMatrices;
                set {
                    if (SetAffectsRender(ref boneMatrices, value)) {
                        matricesChanged = true;
                        if (value == null) boneMatrices = empty;
                        BoneChanged?.Invoke(this, EventArgs.Empty);
                    }
                }
            }

            public StructuredBufferProxy BoneSkinSB => boneSkinSB;
            public event EventHandler BoneChanged;

            public override void Render(RenderContext context, DeviceContextProxy deviceContext) { }

            protected override void OnUpdate(RenderContext context, DeviceContextProxy deviceContext) {
                if (matricesChanged && BoneSkinSB != null) {
                    BoneSkinSB.UploadDataToBuffer(deviceContext, boneMatrices, boneMatrices.Length);
                    matricesChanged = false;
                }
            }

            protected override bool OnAttach(IRenderTechnique technique) {
                boneSkinSB = new StructuredBufferProxy(SilkMath.MatrixSizeInBytes, false);
                return true;
            }

            protected override void OnDetach() {
                RemoveAndDispose(ref boneSkinSB);
            }

            public void BindBuffer(DeviceContextProxy deviceContext, int slot) {
                if (BoneSkinSB != null) deviceContext.SetShaderResource(VertexShader.Type, slot, BoneSkinSB);
            }

            public void InvalidateBoneMatrices() {
                matricesChanged = true;
            }

            protected override void OnDispose(bool disposeManagedResources) {
                if (disposeManagedResources) BoneChanged = null;
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}
