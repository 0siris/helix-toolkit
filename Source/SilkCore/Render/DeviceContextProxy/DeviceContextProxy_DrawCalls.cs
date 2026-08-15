using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Native;
using Buffer = HelixToolkit.SharpDX.Core.Native.Buffer;

namespace HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
public partial class DeviceContextProxy {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Flush() {
        NativeContext.Flush();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int ResetDrawCalls() {
        var total = NumberOfDrawCalls;
        NumberOfDrawCalls = 0;
        return total;
    }

    #region DrawCall

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Draw(int vertexCount, int startVertexLocation) {
        ++NumberOfDrawCalls;
        NativeContext.Draw((uint)vertexCount, (uint)startVertexLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawAuto() {
        ++NumberOfDrawCalls;
        NativeContext.DrawAuto();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawIndexed(int indexCount, int startIndexLocation, int baseVertexLocation) {
        ++NumberOfDrawCalls;
        NativeContext.DrawIndexed((uint)indexCount, (uint)startIndexLocation, baseVertexLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawIndexedInstanced(
        int indexCountPerInstance,
        int instanceCount,
        int startIndexLocation,
        int baseVertexLocation,
        int startInstanceLocation
    ) {
        ++NumberOfDrawCalls;
        NativeContext.DrawIndexedInstanced((uint)indexCountPerInstance,
                                                 (uint)instanceCount,
                                                 (uint)startIndexLocation,
                                                 baseVertexLocation,
                                                 (uint)startInstanceLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawIndexedInstancedIndirect(nint bufferForArgsRef, int alignedByteOffsetForArgs) {
        throw new NotSupportedException("Indirect draw calls require the native buffer wrapper migration.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawInstanced(
        int vertexCountPerInstance,
        int instanceCount,
        int startVertexLocation,
        int startInstanceLocation
    ) {
        ++NumberOfDrawCalls;
        NativeContext.DrawInstanced((uint)vertexCountPerInstance,
                                          (uint)instanceCount,
                                          (uint)startVertexLocation,
                                          (uint)startInstanceLocation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DrawInstancedIndirect(Buffer bufferForArgs, int alignedByteOffsetForArgs) {
        NativeContext.DrawInstancedIndirect(bufferForArgs, (uint)alignedByteOffsetForArgs);
    }

    #endregion DrawCall

    #region Dispatch

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispatch(int threadGroupCountX, int threadGroupCountY, int threadGroupCountZ) {
        ++NumberOfDrawCalls;
        NativeContext.Dispatch((uint)threadGroupCountX,
                                     (uint)threadGroupCountY,
                                     (uint)threadGroupCountZ);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void DispatchIndirect(nint bufferForArgsRef, int alignedByteOffsetForArgs) {
        throw new NotSupportedException("Indirect dispatch calls require the native buffer wrapper migration.");
    }

    #endregion Dispatch

    #region CommandList

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CommandList FinishCommandList(bool restoreState) => NativeContext.FinishCommandList(restoreState);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ExecuteCommandList(CommandList commandList, bool restoreContextState) {
        NativeContext.ExecuteCommandList(commandList, restoreContextState);
    }

    #endregion CommandList
}
