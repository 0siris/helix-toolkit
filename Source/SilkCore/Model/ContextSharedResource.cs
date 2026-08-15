/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Model;

public sealed class ContextSharedResource : IDisposable {
    public ShaderResourceViewProxy? ShadowView { get; set; }

    public ShaderResourceViewProxy? EnvironementMap { get; set; }

    public ShaderResourceViewProxy? SsaoMap { get; set; }

    public int EnvironmentMapMipLevels { get; set; }

#region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    private void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                ShadowView = null;
                EnvironementMap = null;
                SsaoMap = null;
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~ContextSharedResource() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose() {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }

#endregion
}