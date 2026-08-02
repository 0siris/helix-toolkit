/*
The MIT License (MIT)
Copyright (c) 2021 Helix Toolkit contributors
*/

using HelixToolkit.Logger;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core {
    namespace Utilities {
        public class TextureFileLoader : ITextureInfoLoader {
            private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

            private Stream fileStream = Stream.Null;

            public TextureFileLoader(string filePath) {
                FilePath = filePath;
            }

            public string FilePath { get; }

            public void Complete(Guid id, TextureInfo info, bool succeeded) {
                if (Logger.IsEnabled(LogLevel.Debug)) Logger.Debug("Disposing file stream: {Value0}.", FilePath);
                fileStream.Dispose();
            }

            public TextureInfo Load(Guid id) {
                Logger.Info("Loading texture file: {Value0}", FilePath);

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
