namespace HelixToolkit.SharpDX.Core;

/// <summary>Resolves model texture references using model-relative and local directory fallback paths.</summary>
public class DefaultTexturePathResolver : ITexturePathResolver {
    private const string ToUpperDictString = @"..\";
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

    public string? Resolve(string modelPath, string texturePath) => OnLoadTexture(modelPath, texturePath);

    /// <summary>
    ///     Called when [load texture].
    /// </summary>
    /// <param name="modelPath">The model path</param>
    /// <param name="texturePath">The path.</param>
    /// <returns></returns>
    protected virtual string? OnLoadTexture(string modelPath, string texturePath) {
        try {
            var dict = Path.GetDirectoryName(modelPath);
            if (string.IsNullOrEmpty(dict)) dict = Directory.GetCurrentDirectory();
            var p = Path.GetFullPath(Path.Combine(dict, texturePath));
            if (!FileExists(p))
                p = HandleTexturePathNotFound(dict, texturePath);
            if (!FileExists(p)) {
                Logger.Warn("Load Texture Failed. Texture Path = {Value0}", texturePath);
                return null;
            }

            return p;
        } catch (Exception ex) {
            Logger.Warn(ex, "Load Texture Exception. Texture Path = {Value0}", p1: texturePath);
        }

        return null;
    }

    /// <summary>
    ///     Tries stripped parent references, the model directory's parent, and the model directory's filename.
    /// </summary>
    /// <param name="dir">The model directory.</param>
    /// <param name="texturePath">The unresolved texture reference.</param>
    /// <returns>The existing fallback path, or an empty string when none exists.</returns>
    /// <remarks>A directory without a parent retains itself as the parent fallback base.</remarks>
    protected virtual string HandleTexturePathNotFound(string dir, string texturePath) {
        //If file not found in texture path dir, try to find the file in the same dir as the model file
        if (texturePath.StartsWith(ToUpperDictString)) {
            var t = texturePath[ToUpperDictString.Length..];
            var p = Path.GetFullPath(Path.Combine(dir, t));
            if (FileExists(p))
                return p;
        }

        //If still not found, try to go one upper level and find
        var upper = Directory.GetParent(dir)?.FullName ?? dir;
        try {
            upper = Path.GetFullPath(Path.Combine(upper, texturePath));
        } catch (NotSupportedException ex) {
            Logger.Warn(ex, "Texture path format is not supported");
        }

        if (FileExists(upper))
            return upper;

        var fileName = Path.GetFileName(texturePath);
        var currentPath = Path.Combine(dir, fileName);
        if (FileExists(currentPath))
            return currentPath;
        return string.Empty;
    }

    protected virtual bool FileExists(string path) => File.Exists(path);
}
