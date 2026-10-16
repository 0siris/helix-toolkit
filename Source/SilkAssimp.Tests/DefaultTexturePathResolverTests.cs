using Xunit;

namespace HelixToolkit.SharpDX.Core;

/// <summary>Verifies texture fallback paths against isolated real files without loading or presenting images.</summary>
public sealed class DefaultTexturePathResolverTests {
    /// <summary>
    ///     Resolves the parent sibling path and retains rooted, model-relative, stripped-parent and filename fallbacks.
    /// </summary>
    /// <param name="texturePath">The texture reference stored in the model.</param>
    /// <param name="filePath">The actual fixture file relative to the temporary root.</param>
    [Theory]
    [InlineData("textures/albedo.png", "textures/albedo.png")]
    [InlineData("textures/albedo.png", "scene/textures/albedo.png")]
    [InlineData("..\\textures\\albedo.png", "scene/textures/albedo.png")]
    [InlineData("unavailable/albedo.png", "scene/albedo.png")]
    [InlineData("rooted", "absolute/albedo.png")]
    [Trait("Category", "Unit")]
    public void ResolveExistingFallbackTest(string texturePath, string filePath) {
        var directory = Path.Combine(Path.GetTempPath(), $"SilkAssimpMaterial-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try {
            var modelPath = Path.Combine(directory, "scene", "model.obj");
            Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
            File.WriteAllText(modelPath, "# synthetic model");
            var expectedPath = Path.GetFullPath(Path.Combine(directory, filePath));
            Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
            File.WriteAllBytes(expectedPath, [1, 2, 3]);
            var reference = texturePath == "rooted" ? expectedPath : texturePath;
            var resolver = new DefaultTexturePathResolver();

            var resolved = resolver.Resolve(modelPath, reference);

            Assert.Equal(expectedPath, resolved);
        } finally {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>Returns no path for a missing texture when the model directory has no parent.</summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void ResolveRootWithoutParentTest() {
        var root = Path.GetPathRoot(Path.GetTempPath())!;
        var modelPath = Path.Combine(root, "model.obj");
        var resolver = new DefaultTexturePathResolver();
        var missing = $"missing-{Guid.NewGuid():N}/albedo-{Guid.NewGuid():N}.png";

        var resolved = resolver.Resolve(modelPath, missing);

        Assert.Null(resolved);
    }
}
