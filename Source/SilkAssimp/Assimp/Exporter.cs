/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Text;
using System.Diagnostics.CodeAnalysis;
using Assimp;
using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.SharpDX.Core.Assimp;

public partial class Exporter : IDisposable {
    private const string ToUpperDictString = @"..\";
    private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;
    protected readonly Dictionary<Geometry3D, int> GeometryCollection = [];
    protected readonly Dictionary<MaterialCore, int> MaterialCollection = [];
    protected readonly Dictionary<ulong, MeshInfo> MeshInfos = [];
    private IList<Animations.Animation>? animations;

    private int materialIndexForNoName;
    private int meshIndexForNoName;

    static Exporter() {
        using (var temp = new AssimpContext()) {
            SupportedFormats = [.. temp.GetSupportedExportFormats()];
        }

        var builder = new StringBuilder();
        foreach (var s in SupportedFormats)
            builder.Append($"{s.Description} (*.{s.FileExtension})|*.{s.FileExtension}|");
        SupportedFormatsString = builder.ToString(0, builder.Length - 1);
    }

    public event EventHandler<Exception>? AssimpExceptionOccurred;

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
        AssimpContext? exporter;
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
                Logger.Error("Export failed. FilePath: {Value0}; Format: {Value1}", [filePath, formatId]);
                return ErrorCode.Failed;
            }

            return ErrorCode.Succeed;
        } catch (Exception ex) {
            Logger.Error(ex.Message);
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
    public ErrorCode ExportToBlob(Model.Scene.SceneNode root, string formatId, out ExportDataBlob? blob) {
        Clear();
        AssimpContext? exporter = null;
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
            Logger.Error(ex.Message);
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
        foreach (var material in MaterialCollection.OrderBy(x => x.Value))
            scene.Materials.Add(OnCreateAssimpMaterial(material.Key));
        scene.RootNode = ConstructAssimpNode(root, null);
        scene.Meshes.AddRange(MeshInfos.Select(x => x.Value.AssimpMesh
            ?? throw new InvalidOperationException("Assimp mesh was not created.")));
        AddAnimationsToScene(scene);
        return scene;
    }

    private Node ConstructAssimpNode(Model.Scene.SceneNode current, Node? parent) {
        var node = new Node(string.IsNullOrEmpty(current.Name)
            ? "Node"
            : current.Name, parent) {
            Transform = current.ModelMatrix.ToAssimpMatrix(configuration.ToSourceMatrixColumnMajor)
        };
        if (current is Model.Scene.GroupNodeBase group) {
            foreach (var s in group.Items)
                if (s is Model.Scene.GeometryNode geo) {
                    var key = GetMaterialGeoKey(geo, out var materialIndex, out var geoIndex);
                    if (MeshInfos.TryGetValue(key, out var meshInfo)) node.MeshIndices.Add(meshInfo.MeshIndex);
                } else if (s is Model.Scene.GroupNodeBase) {
                    node.Children.Add(ConstructAssimpNode(s, node));
                } else {
                    Logger.Warn("Current node type does not support yet. Type: {Value0}", s.GetType()
                        .Name);
                }

            foreach (var metadata in group.Metadata.ToAssimpMetadata())
                node.Metadata.Add(metadata.Key, metadata.Value);
        } else if (current is Model.Scene.GeometryNode geo) {
            var key = GetMaterialGeoKey(geo, out var materialIndex, out var geoIndex);
            if (MeshInfos.TryGetValue(key, out var meshInfo)) node.MeshIndices.Add(meshInfo.MeshIndex);
        } else {
            Logger.Warn("Current node type does not support yet. Type: {Value0}", current.GetType()
                .Name);
        }

        return node;
    }

    private void CollectAllGeometriesAndMaterials(Model.Scene.SceneNode root) {
        // Collect all geometries and materials
        foreach (var node in root.Traverse()) {
            if (GetMaterialFromNode(node, out var material) && material is { } materialValue
                && !MaterialCollection.ContainsKey(materialValue))
                MaterialCollection.Add(materialValue, MaterialCollection.Count);
            if (GetGeometryFromNode(node, out var geometry) && geometry is { } geometryValue
                && !GeometryCollection.ContainsKey(geometryValue))
                GeometryCollection.Add(geometryValue, GeometryCollection.Count);
        }

        foreach (var node in root.Traverse())
            if (node is Model.Scene.GeometryNode geo) {
                var info = OnCreateMeshInfo(geo);
                if (info == null) {
                    Logger.Warn("Create Mesh info failed. Node Name: {Value0}", geo.Name);
                    continue;
                }

                if (!MeshInfos.ContainsKey(info.MaterialMeshKey)) MeshInfos.Add(info.MaterialMeshKey, info);
            }

        if (configuration.EnableParallelProcessing)
            Parallel.ForEach(MeshInfos, info => { info.Value.AssimpMesh = OnCreateAssimpMesh(info.Value); });
        else
            foreach (var info in MeshInfos)
                info.Value.AssimpMesh = OnCreateAssimpMesh(info.Value);
    }

    protected virtual void Clear() {
        GeometryCollection.Clear();
        MaterialCollection.Clear();
        MeshInfos.Clear();
        materialIndexForNoName = meshIndexForNoName = 0;
    }

    #region Inner Classes

    /// <summary>
    /// </summary>
    protected sealed class HelixInternalScene {
        /// <summary>
        ///     The animations
        /// </summary>
        public List<Animations.Animation> Animations = [];

        /// <summary>
        ///     The assimp scene
        /// </summary>
        public Scene AssimpScene = new();

        /// <summary>
        ///     The materials
        /// </summary>
        public Tuple<Material, MaterialCore>[] Materials = [];

        /// <summary>
        ///     The meshes
        /// </summary>
        public MeshInfo[] Meshes = [];
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
    [AllowNull]
    public ExportConfiguration Configuration {
        get => configuration;
        set => configuration = value ?? new ExportConfiguration();
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
