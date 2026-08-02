using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Render {
        public partial class DeviceContextProxy {
            /// <summary>
            ///     Sets the state of the raster.
            /// </summary>
            /// <param name="rasterState">State of the raster.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetRasterState(RasterizerStateProxy rasterState) {
                if (AutoSkipRedundantStateSetting && currRasterState == rasterState) return;
                NativeContext.SetRasterState(rasterState?.State);
                currRasterState = rasterState;
            }

            /// <summary>
            ///     Sets the state of the depth stencil.
            /// </summary>
            /// <param name="depthStencilState">State of the depth stencil.</param>
            /// <param name="stencilRef">The stencil reference.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetDepthStencilState(DepthStencilStateProxy depthStencilState, int stencilRef = 0) {
                if (AutoSkipRedundantStateSetting && currDepthStencilState == depthStencilState &&
                    currStencilRef == stencilRef) return;
                NativeContext.SetDepthStencilState(depthStencilState?.State, stencilRef);
                currDepthStencilState = depthStencilState;
                currStencilRef = stencilRef;
            }

            /// <summary>
            ///     Sets the state of the blend.
            /// </summary>
            /// <param name="blendState">State of the blend.</param>
            /// <param name="blendFactor">The blend factor.</param>
            /// <param name="sampleMask">The sample mask.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetBlendState(BlendStateProxy blendState, Color4? blendFactor = null, int sampleMask = -1) {
                var mask = sampleMask == -1 ? uint.MaxValue : unchecked((uint)sampleMask);
                if (AutoSkipRedundantStateSetting && currBlendState == blendState && blendFactor == currBlendFactor &&
                    currSampleMask == mask) return;
                NativeContext.SetBlendState(blendState?.State, blendFactor, mask);
                currBlendState = blendState;
                currBlendFactor = blendFactor;
                currSampleMask = mask;
            }

            /// <summary>
            ///     Sets the state of the blend.
            /// </summary>
            /// <param name="blendState">State of the blend.</param>
            /// <param name="blendFactor">The blend factor.</param>
            /// <param name="sampleMask">The sample mask.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void SetBlendState(
                BlendStateProxy blendState,
                Color4? blendFactor = null,
                uint sampleMask = uint.MaxValue
            ) {
                if (AutoSkipRedundantStateSetting && currBlendState == blendState && blendFactor == currBlendFactor &&
                    currSampleMask == sampleMask) return;
                NativeContext.SetBlendState(blendState?.State, blendFactor, sampleMask);
                currBlendState = blendState;
                currBlendFactor = blendFactor;
                currSampleMask = sampleMask;
            }
        }
    }
}
