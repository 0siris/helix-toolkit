/*
The MIT License (MIT)
Copyright (c) 2021 Helix Toolkit contributors
*/

using HelixToolkit.Logger;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core {
    namespace Utilities {
        public class TextureFileLoader : ITextureInfoLoader {
            private static readonly ILogger logger = LogManager.Create<TextureFileLoader>();

            private Stream fileStream = Stream.Null;

            public TextureFileLoader(string filePath) {
                FilePath = filePath;
            }

            public string FilePath { get; }

            public void Complete(Guid id, TextureInfo info, bool succeeded) {
                if (logger.IsEnabled(LogLevel.Debug)) logger.LogDebug("Disposing file stream: {0}.", FilePath);
                fileStream.Dispose();
            }

            public TextureInfo Load(Guid id) {
                logger.LogInformation("Loading texture file: {0}", FilePath);

#if WINDOWS_UWP
                try
                {
                    Windows.Storage.StorageFile.GetFileFromPathAsync(FilePath).AsTask().GetAwaiter().GetResult();
                    var folder =
 Windows.Storage.StorageFile.GetFileFromPathAsync(FilePath).AsTask().GetAwaiter().GetResult();
                    fileStream = folder.OpenStreamForReadAsync().GetAwaiter().GetResult();
                    return new TextureInfo(fileStream);
                }
                catch (Exception) { }
                return TextureInfo.Null;
#else
                if (!File.Exists(FilePath)) return TextureInfo.Null;
                fileStream = File.OpenRead(FilePath);
                return new TextureInfo(fileStream);
#endif
            }
        }
    }
}
