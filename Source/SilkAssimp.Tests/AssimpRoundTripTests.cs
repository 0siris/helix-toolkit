using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Utilities;
using Xunit;
using HxImporter = HelixToolkit.SharpDX.Core.Assimp.Importer;

namespace SilkAssimp.Tests;

/// <summary>
///     Import round-trip checks pinning the pre-migration baseline from
///     <c>specs/001-assimpnet-to-sharpassimp/baseline.md</c>.
/// </summary>
public class AssimpRoundTripTests {
    public static TheoryData<string, int, int> SmallFixtures => new() {
        { @"Models\obj\bunny\bunny.obj", 1, 2503 },
        { @"Models\glTF\robot nier\scene.gltf", 8, 31047 },
        { @"Models\stl\cube.stl", 1, 18 },
        { @"Models\ply\tetra_ascii.ply", 1, 4 },
        { @"Models\FBX\Solus The Knight\Solus_The_Knight.fbx", 1, 1816 }
    };

    [Theory]
    [MemberData(nameof(SmallFixtures))]
    [Trait("Category", "Unit")]
    public void SmallFixtureMatchesBaseline(string relativePath, int meshes, int vertices) {
        using var importer = new HxImporter();
        importer.Configuration = new ImporterConfiguration { BuildOctree = false };

        var code = importer.Load(RepoPath(relativePath), out var scene);

        Assert.Equal(ErrorCode.Succeed, code);
        var imported = scene ?? throw new InvalidOperationException("Import must return a scene.");
        var nodes = imported.Root.Traverse().OfType<MeshNode>().ToList();
        Assert.Equal(meshes, nodes.Count);
        Assert.Equal(vertices, nodes.Sum(m => (m.Geometry?.Positions?.Count).GetValueOrDefault()));
    }

    public static TheoryData<string> LargeFixtures => new() {
        { @"Models\ply\stanford_bunny_ascii.ply" },
        { @"Models\ply\stanford_armadillo_binary_big_endian.ply" },
        { @"Models\ply\stanford_dragon_full_ascii.ply" }
    };

    [Theory]
    [MemberData(nameof(LargeFixtures))]
    [Trait("Category", "LargeModels")]
    public void LargeFixtureLoadsWithoutError(string relativePath) {
        using var importer = new HxImporter();
        importer.Configuration = new ImporterConfiguration { BuildOctree = false };

        var code = importer.Load(RepoPath(relativePath), out var scene);

        Assert.Equal(ErrorCode.Succeed, code);
        Assert.NotNull(scene);
        Assert.NotEmpty(scene.Root.Traverse().OfType<MeshNode>());
    }

    private static string RepoPath(string relativePath) {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Models")))
            directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."),
            relativePath);
    }
}
