/*
The MIT License (MIT)
Copyright (c) 2021 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.Logger;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core {
    namespace Utilities {
        public sealed class TextureModelRepository : ITextureModelRepository {
            private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

            private readonly ConditionalWeakTable<string, WeakReference<TextureModel>> fileDict = [];

            private readonly ConditionalWeakTable<Stream, WeakReference<TextureModel>> streamDict = [];

            public TextureModel Create(Stream stream) {
                if (stream == null) return null;
                lock (streamDict) {
                    if (streamDict.TryGetValue(stream, out var tex)) {
                        if (tex.TryGetTarget(out var target)) {
                            if (Logger.IsEnabled(LogLevel.Debug))
                                Logger.Debug("Reuse existing TextureModel. Guid: {Value0}", target.Guid);
                            return target;
                        }

                        streamDict.Remove(stream);
                    }

                    var newTexModel = new TextureModel(stream);
                    streamDict.Add(stream, new WeakReference<TextureModel>(newTexModel));
                    if (Logger.IsEnabled(LogLevel.Debug))
                        Logger.Debug("Created new TextureModel. Guid: {Value0}", newTexModel.Guid);
                    return newTexModel;
                }
            }

            public TextureModel Create(string texturePath) {
                if (string.IsNullOrEmpty(texturePath)) return null;
                lock (fileDict) {
                    if (fileDict.TryGetValue(texturePath, out var tex)) {
                        if (tex.TryGetTarget(out var target)) {
                            if (Logger.IsEnabled(LogLevel.Debug))
                                Logger.Debug("Reuse existing TextureModel. Guid: {Value0}", target.Guid);
                            return target;
                        }

                        fileDict.Remove(texturePath);
                    }

                    var newTexModel = new TextureModel(texturePath);
                    fileDict.Add(texturePath, new WeakReference<TextureModel>(newTexModel));
                    if (Logger.IsEnabled(LogLevel.Debug))
                        Logger.Debug("Created new TextureModel. Guid: {Value0}", newTexModel.Guid);
                    return newTexModel;
                }
            }
        }
    }
}
