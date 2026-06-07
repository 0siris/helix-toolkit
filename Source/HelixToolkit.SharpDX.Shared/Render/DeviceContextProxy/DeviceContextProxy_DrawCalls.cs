using System;
using System.Runtime.CompilerServices;

#if !NETFX_CORE
namespace HelixToolkit.Wpf.SharpDX
#else
#if CORE
namespace HelixToolkit.SharpDX.Core
#else
namespace HelixToolkit.UWP
#endif
#endif
{
    namespace Render
    {
        public partial class DeviceContextProxy
        {
            #region DrawCall

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Draw(int vertexCount, int startVertexLocation)
            {
                ++NumberOfDrawCalls;
                nativeDeviceContext.Draw((uint)vertexCount, (uint)startVertexLocation);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DrawAuto()
            {
                ++NumberOfDrawCalls;
                nativeDeviceContext.DrawAuto();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DrawIndexed(int indexCount, int startIndexLocation, int baseVertexLocation)
            {
                ++NumberOfDrawCalls;
                nativeDeviceContext.DrawIndexed((uint)indexCount, (uint)startIndexLocation, baseVertexLocation);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount, int startIndexLocation, int baseVertexLocation, int startInstanceLocation)
            {
                ++NumberOfDrawCalls;
                nativeDeviceContext.DrawIndexedInstanced(
                    (uint)indexCountPerInstance,
                    (uint)instanceCount,
                    (uint)startIndexLocation,
                    baseVertexLocation,
                    (uint)startInstanceLocation);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DrawIndexedInstancedIndirect(IntPtr bufferForArgsRef, int alignedByteOffsetForArgs)
            {
                throw new NotSupportedException("Indirect draw calls require the native buffer wrapper migration.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DrawInstanced(int vertexCountPerInstance, int instanceCount, int startVertexLocation, int startInstanceLocation)
            {
                ++NumberOfDrawCalls;
                nativeDeviceContext.DrawInstanced(
                    (uint)vertexCountPerInstance,
                    (uint)instanceCount,
                    (uint)startVertexLocation,
                    (uint)startInstanceLocation);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DrawInstancedIndirect(IntPtr bufferForArgsRef, int alignedByteOffsetForArgs)
            {
                throw new NotSupportedException("Indirect draw calls require the native buffer wrapper migration.");
            }

            #endregion DrawCall

            #region Dispatch

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispatch(int threadGroupCountX, int threadGroupCountY, int threadGroupCountZ)
            {
                ++NumberOfDrawCalls;
                nativeDeviceContext.Dispatch((uint)threadGroupCountX, (uint)threadGroupCountY, (uint)threadGroupCountZ);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DispatchIndirect(IntPtr bufferForArgsRef, int alignedByteOffsetForArgs)
            {
                throw new NotSupportedException("Indirect dispatch calls require the native buffer wrapper migration.");
            }

            #endregion Dispatch

            #region CommandList

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public object FinishCommandList(bool restoreState)
            {
                throw new NotSupportedException("Deferred command lists require the native command list migration.");
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void ExecuteCommandList(object commandListRef, bool restoreContextState)
            {
                throw new NotSupportedException("Deferred command lists require the native command list migration.");
            }

            #endregion CommandList

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Flush()
            {
                nativeDeviceContext.Flush();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int ResetDrawCalls()
            {
                var total = NumberOfDrawCalls;
                NumberOfDrawCalls = 0;
                return total;
            }
        }
    }
}
