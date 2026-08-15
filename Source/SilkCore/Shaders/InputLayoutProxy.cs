using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Shaders;

public sealed class InputLayoutProxy : DisposeObject {
    private InputLayout? layout;

    internal InputLayoutProxy(SilkD3DDevice device, byte[] vertexShaderByteCode, InputElement[] elements) {
        layout = device.CreateInputLayout(vertexShaderByteCode, elements);
    }

    internal InputLayout Layout => layout
        ?? throw new InvalidOperationException("The input layout has been disposed.");

    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref layout);
        base.OnDispose(disposeManagedResources);
    }
}
