/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core;

/// <summary>
///     Use for texture resource sharing between models. It uses texture stream as key for each texture.
///     <para>Call Register to get(if already exists) or create a new shared texture.</para>
///     <para>
///         Call Unregister to detach the texture from model. Call detach from SharedTextureResourceProxy achieves the
///         same result.
///     </para>
/// </summary>
public sealed class TextureResourceManager : IDisposable, ITextureResourceManager {
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    private readonly object device;
    private readonly Dictionary<Guid, ShaderResourceViewProxy> resourceDictionaryMipMaps = [];
    private readonly Dictionary<Guid, ShaderResourceViewProxy> resourceDictionaryNoMipMaps = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="TextureResourceManager" /> class.
    /// </summary>
    /// <param name="device">The device.</param>
    public TextureResourceManager(object device) {
        this.device = device;
    }

    public int Count => resourceDictionaryMipMaps.Count + resourceDictionaryNoMipMaps.Count;

    /// <summary>
    ///     Registers the specified texture model. This creates mipmaps automatically
    /// </summary>
    /// <param name="textureStream">The texture model.</param>
    /// <returns></returns>
    public ShaderResourceViewProxy? Register(TextureModel? textureStream) => Register(textureStream, true);

    /// <summary>
    ///     Registers the specified material unique identifier.
    /// </summary>
    /// <param name="textureModel">The texture model.</param>
    /// <param name="enableAutoGenMipMap">Enable generate mipmaps automatically</param>
    /// <returns></returns>
    public ShaderResourceViewProxy? Register(TextureModel? textureModel, bool enableAutoGenMipMap) {
        if (textureModel == null) return null;
        var targetDict = enableAutoGenMipMap ? resourceDictionaryMipMaps : resourceDictionaryNoMipMaps;
        lock (targetDict) {
            if (targetDict.TryGetValue(textureModel.Guid, out var view)) {
                if (Logger.IsEnabled(LogLevel.Debug)) Logger.Debug("Re-using existing texture resource");
                view.IncRef();
                return view;
            }

            if (Logger.IsEnabled(LogLevel.Debug)) Logger.Debug("Creating new texture resource");
            var proxy = new ShaderResourceViewProxy(device);
            proxy.CreateView(textureModel, true, enableAutoGenMipMap);
            proxy.Guid = textureModel.Guid;
            proxy.Disposed += (_, _) => {
                lock (targetDict) {
                    targetDict.Remove(proxy.Guid);
                }
            };
            targetDict.Add(textureModel.Guid, proxy);
            return proxy;
        }
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    private void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                lock (resourceDictionaryMipMaps) {
                    foreach (var resource in resourceDictionaryMipMaps.Values.ToArray()) resource.ForceDispose();
                    resourceDictionaryMipMaps.Clear();
                }

                lock (resourceDictionaryNoMipMaps) {
                    foreach (var resource in resourceDictionaryNoMipMaps.Values.ToArray()) resource.ForceDispose();
                    resourceDictionaryNoMipMaps.Clear();
                }
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~TextureResourceManager() {
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
