/*
The MIT License (MIT)
Copyright (c) 2021 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.Logger;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core
{
    namespace Utilities
    {
        public sealed class TextureModelRepository : ITextureModelRepository
        {
            private static readonly ILogger logger = LogManager.Create<TextureModelRepository>();

            private readonly ConditionalWeakTable<string, WeakReference<TextureModel>> fileDict = new();

            private readonly ConditionalWeakTable<Stream, WeakReference<TextureModel>> streamDict = new();

            public TextureModel Create(Stream stream)
            {
                if (stream == null) return null;
                lock (streamDict)
                {
                    if (streamDict.TryGetValue(stream, out var tex))
                    {
                        if (tex.TryGetTarget(out var target))
                        {
                            if (logger.IsEnabled(LogLevel.Debug))
                                logger.LogDebug("Reuse existing TextureModel. Guid: {0}", target.Guid);
                            return target;
                        }

                        streamDict.Remove(stream);
                    }

                    var newTexModel = new TextureModel(stream);
                    streamDict.Add(stream, new WeakReference<TextureModel>(newTexModel));
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug("Created new TextureModel. Guid: {0}", newTexModel.Guid);
                    return newTexModel;
                }
            }

            public TextureModel Create(string texturePath)
            {
                if (string.IsNullOrEmpty(texturePath)) return null;
                lock (fileDict)
                {
                    if (fileDict.TryGetValue(texturePath, out var tex))
                    {
                        if (tex.TryGetTarget(out var target))
                        {
                            if (logger.IsEnabled(LogLevel.Debug))
                                logger.LogDebug("Reuse existing TextureModel. Guid: {0}", target.Guid);
                            return target;
                        }

                        fileDict.Remove(texturePath);
                    }

                    var newTexModel = new TextureModel(texturePath);
                    fileDict.Add(texturePath, new WeakReference<TextureModel>(newTexModel));
                    if (logger.IsEnabled(LogLevel.Debug))
                        logger.LogDebug("Created new TextureModel. Guid: {0}", newTexModel.Guid);
                    return newTexModel;
                }
            }
        }
    }
}