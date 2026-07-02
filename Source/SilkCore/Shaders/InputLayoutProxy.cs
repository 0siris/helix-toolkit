using System;

namespace HelixToolkit.SharpDX.Core
{
    namespace Shaders
    {
        using Native;

        public sealed class InputLayoutProxy : DisposeObject
        {
            private Native.InputLayout layout;

            internal Native.InputLayout Layout => layout;

            internal InputLayoutProxy(SilkD3DDevice device, byte[] vertexShaderByteCode, InputElement[] elements)
            {
                layout = device.CreateInputLayout(vertexShaderByteCode, elements);
            }

            protected override void OnDispose(bool disposeManagedResources)
            {
                RemoveAndDispose(ref layout);
                base.OnDispose(disposeManagedResources);
            }
        }
    }
}
