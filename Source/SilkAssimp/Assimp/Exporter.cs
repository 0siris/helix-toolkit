/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Text;
using Assimp;
using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core.Model;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core.Assimp;

public partial class Exporter : IDisposable {
    private const string ToUpperDictString = @"..\";
    private static readonly ILogger logger = LogManager.Create<Exporter>();
    protected readonly Dictionary<Geometry3D, int> geometryCollection = new();
    protected readonly Dictionary<MaterialCore, int> materialCollection = new();
    protected readonly Dictionary<ulong, MeshInfo> meshInfos = new();
    private IList<Animations.Animation>? animations;

    private int MaterialIndexForNoName;
    private int MeshIndexForNoName;

    static Exporter() {
        using (var temp = new AssimpContext()) {
            SupportedFormats = temp.GetSupportedExportFormats().ToArray();
        }

        var builder = new StringBuilder();
        foreach (var s in SupportedFormats)
            builder.Append($"{s.Description} (*.{s.FileExtension})|*.{s.FileExtension}|");
        SupportedFormatsString = builder.ToString(0, builder.Length - 1);
    }

    public event EventHandler<Exception> AssimpExceptionOccurred;

    /// <summary>
    ///     Exports to file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="scene">The scene.</param>
    /// <param name="formatId">The format identifier. <see cref="SupportedFormats" /></param>
    /// <returns></returns>
    public ErrorCode ExportToFile(string filePath, HelixToolkitScene? scene, string formatId) {
        if (scene == null) return ErrorCode.Failed;
        animations = scene.Animations;
        var code = ExportToFile(filePath, scene.Root, formatId);
        animations = null;
        return code;
    }

    /// <summary>
    ///     Exports to file.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <param name="root">The root.</param>
    /// <param name="formatId">The format identifier. <see cref="SupportedFormats" /></param>
    /// <returns></returns>
    public ErrorCode ExportToFile(string filePath, Model.Scene.SceneNode root, string formatId) {
        Clear();
        AssimpContext? exporter = null;
        var useExtern = false;
        if (Configuration.ExternalContext != null) {
            exporter = Configuration.ExternalContext;
            useExtern = true;
        } else {
            exporter = new AssimpContext();
        }

        if (!exporter.IsExportFormatSupported(Path.GetExtension(filePath)))
            return ErrorCode.Failed | ErrorCode.FileTypeNotSupported;
        var scene = CreateScene(root);
        var postProcessing = configuration.PostProcessing;
        if (configuration.FlipWindingOrder) postProcessing |= PostProcessSteps.FlipWindingOrder;
        try {
            if (!exporter.ExportFile(scene, filePath, formatId, postProcessing)) {
                logger.LogError("Export failed. FilePath: {0}; Format: {1}", filePath, formatId);
                return ErrorCode.Failed;
            }

            return ErrorCode.Succeed;
        } catch (Exception ex) {
            logger.LogError(ex.Message);
            AssimpExceptionOccurred?.Invoke(this, ex);
        } finally {
            if (!useExtern) exporter.Dispose();
        }

        return ErrorCode.Failed;
    }

    /// <summary>
    ///     Exports to BLOB.
    /// </summary>
    /// <param name="root">The root.</param>
    /// <param name="formatId">The format identifier.</param>
    /// <param name="blob">The BLOB.</param>
    /// <returns></returns>
    public ErrorCode ExportToBlob(Model.Scene.SceneNode root, string formatId, out ExportDataBlob blob) {
        Clear();
        AssimpContext exporter = null;
        var useExtern = false;
        if (Configuration.ExternalContext != null) {
            exporter = Configuration.ExternalContext;
            useExtern = true;
        } else {
            exporter = new AssimpContext();
        }

        var scene = CreateScene(root);
        var postProcessing = configuration.PostProcessing;
        if (configuration.FlipWindingOrder) postProcessing |= PostProcessSteps.FlipWindingOrder;
        blob = null;
        try {
            blob = exporter.ExportToBlob(scene, formatId, postProcessing);
            return ErrorCode.Succeed;
        } catch (Exception ex) {
            logger.LogError(ex.Message);
            AssimpExceptionOccurred?.Invoke(this, ex);
        } finally {
            if (!useExtern) exporter.Dispose();
        }

        return ErrorCode.Failed;
    }

    /// <summary>
    ///     Convert a HelixToolkit scene graph to the assimp scene.
    /// </summary>
    /// <param name="root">The HelixToolkit scene graph root node.</param>
    /// <param name="assimpScene">The assimp scene.</param>
    /// <returns></returns>
    public ErrorCode ToAssimpScene(Model.Scene.SceneNode root, out Scene assimpScene) {
        Clear();
        assimpScene = CreateScene(root);
        return ErrorCode.Succeed;
    }

    private Scene CreateScene(Model.Scene.SceneNode root) {
        CollectAllGeometriesAndMaterials(root);
        var scene = new Scene();
        //Adds material and meshes into the assimp scene
        foreach (var material in materialCollection.OrderBy(x => x.Value))
            scene.Materials.Add(OnCreateAssimpMaterial(material.Key));
        scene.RootNode = ConstructAssimpNode(root, null);
        scene.Meshes.AddRange(meshInfos.Select(x => x.Value.AssimpMesh));
        AddAnimationsToScene(scene);
        return scene;
    }

    private Node ConstructAssimpNode(Model.Scene.SceneNode current, Node parent) {
        var node = new Node(string.IsNullOrEmpty(current.Name) ? "Node" : current.Name, parent) {
            Transform = current.ModelMatrix.ToAssimpMatrix(configuration.ToSourceMatrixColumnMajor)
        };
        if (current is Model.Scene.GroupNodeBase group) {
            foreach (var s in group.Items)
                if (s is Model.Scene.GeometryNode geo) {
                    var key = GetMaterialGeoKey(geo, out var materialIndex, out var geoIndex);
                    if (meshInfos.TryGetValue(key, out var meshInfo)) node.MeshIndices.Add(meshInfo.MeshIndex);
                } else if (s is Model.Scene.GroupNodeBase) {
                    node.Children.Add(ConstructAssimpNode(s, node));
                } else {
                    logger.LogWarning("Current node type does not support yet. Type: {0}", s.GetType().Name);
                }

            if (group.Metadata != null)
                foreach (var metadata in group.Metadata.ToAssimpMetadata())
                    node.Metadata.Add(metadata.Key, metadata.Value);
        } else if (current is Model.Scene.GeometryNode geo) {
            var key = GetMaterialGeoKey(geo, out var materialIndex, out var geoIndex);
            if (meshInfos.TryGetValue(key, out var meshInfo)) node.MeshIndices.Add(meshInfo.MeshIndex);
        } else {
            logger.LogWarning("Current node type does not support yet. Type: {0}", current.GetType().Name);
        }

        return node;
    }

    private void CollectAllGeometriesAndMaterials(Model.Scene.SceneNode root) {
        // Collect all geometries and materials
        foreach (var node in root.Traverse()) {
            if (GetMaterialFromNode(node, out var material) && !materialCollection.ContainsKey(material))
                materialCollection.Add(material, materialCollection.Count);
            if (GetGeometryFromNode(node, out var geometry) && !geometryCollection.ContainsKey(geometry))
                geometryCollection.Add(geometry, geometryCollection.Count);
        }

        foreach (var node in root.Traverse())
            if (node is Model.Scene.GeometryNode geo) {
                var info = OnCreateMeshInfo(geo);
                if (info == null) {
                    logger.LogWarning("Create Mesh info failed. Node Name: {0}", geo.Name);
                    continue;
                }

                if (!meshInfos.ContainsKey(info.MaterialMeshKey)) meshInfos.Add(info.MaterialMeshKey, info);
            }

        if (configuration.EnableParallelProcessing)
            Parallel.ForEach(meshInfos, info => { info.Value.AssimpMesh = OnCreateAssimpMesh(info.Value); });
        else
            foreach (var info in meshInfos)
                info.Value.AssimpMesh = OnCreateAssimpMesh(info.Value);
    }

    protected virtual void Clear() {
        geometryCollection.Clear();
        materialCollection.Clear();
        meshInfos.Clear();
        MaterialIndexForNoName = MeshIndexForNoName = 0;
    }

#region Inner Classes

    /// <summary>
    /// </summary>
    protected sealed class HelixInternalScene {
        /// <summary>
        ///     The animations
        /// </summary>
        public List<Animations.Animation> Animations;

        /// <summary>
        ///     The assimp scene
        /// </summary>
        public Scene AssimpScene;

        /// <summary>
        ///     The materials
        /// </summary>
        public Tuple<Material, MaterialCore>[] Materials;

        /// <summary>
        ///     The meshes
        /// </summary>
        public MeshInfo[] Meshes;
    }

#endregion

#region Properties

    /// <summary>
    ///     Gets the supported formats.
    /// </summary>
    /// <value>
    ///     The supported formats.
    /// </value>
    public static ExportFormatDescription[] SupportedFormats { get; }

    /// <summary>
    ///     Gets the supported formats string.
    /// </summary>
    /// <value>
    ///     The supported formats string.
    /// </value>
    public static string SupportedFormatsString { get; }

    private ExportConfiguration configuration = new();

    /// <summary>
    ///     Gets or sets the configuration.
    /// </summary>
    /// <value>
    ///     The configuration.
    /// </value>
    public ExportConfiguration Configuration {
        get => configuration;
        set {
            configuration = value;
            if (value == null) configuration = new ExportConfiguration();
        }
    }

#endregion

#region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing)
                // TODO: dispose managed state (managed objects).
                Clear();

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~Importer() {
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
