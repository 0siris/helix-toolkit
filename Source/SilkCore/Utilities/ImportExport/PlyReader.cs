using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Geometry;

namespace HelixToolkit.SharpDX.Core.Utilities.ImportExport;

using Mesh3DGroup = List<Object3D>;

/// <summary>
///     Polygon File Format Reader.
/// </summary>
/// https://www.cc.gatech.edu/projects/large_models/ply.html
/// http://graphics.stanford.edu/data/3Dscanrep/
/// <remarks>
///     This reader only reads ascii ply formats.
///     This was initially meant to read models exported by Blender 3D Software.
/// </remarks>
[Obsolete("Suggest to use HelixToolkit.SharpDX.Assimp")]
public class PlyReader : ModelReader {
    /// <summary>
    ///     Initializes a new <see cref="PlyReader" />.
    /// </summary>
    public PlyReader() {
        InitializeProperties();
    }

    #region Public methods

    /// <summary>
    ///     Reads the model from the specified stream.
    /// </summary>
    /// <param name="s">The stream.</param>
    /// <param name="info"></param>
    /// <returns>A <see cref="Mesh3DGroup" /></returns>
    public override Mesh3DGroup Read(Stream s, ModelInfo info = default) {
        InitializeProperties();
        Load(s);
        return CreateModel3D();
    }

    /// <summary>
    ///     Reads the model from the specified path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The model.</returns>
    public Mesh3DGroup Read(string path) {
        InitializeProperties();
        plymodelUri = path;
        Load(path);
        using var s = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Read(s);
    }

    /// <summary>
    ///     Creates a mesh from the loaded file.
    /// </summary>
    /// <returns>
    ///     A <see cref="MeshGeometry3D" />.
    /// </returns>
    public MeshGeometry3D CreateMesh() {
        var mesh = new MeshGeometry3D {
            Positions = [],
            Indices = []
        };
        if (Vertices.Count > 0)
            foreach (var vert in Vertices)
                mesh.Positions.Add(vert);

        if (Faces.Count > 0)
            foreach (var face in Faces)
                mesh.Indices.AddRange((int[])face.Clone());

        if (TextureCoordinates is { Count: > 0 } textureCoordinates) {
            mesh.TextureCoordinates = [];
            foreach (var item in textureCoordinates)
                mesh.TextureCoordinates.Add(new Vector2(item.X, item.Y));
        }

        if (TextureCoordinates?.Count == 0) TextureCoordinates = null;
        return mesh;
    }

    /// <summary>
    ///     Creates a <see cref="MeshGeometry3D" /> object from the loaded file. Polygons are triangulated using triangle fans.
    /// </summary>
    /// <returns>
    ///     A <see cref="MeshGeometry3D" />.
    /// </returns>
    public MeshGeometry3D CreateMeshGeometry3D() {
        var mb = new MeshBuilder(true);

        if (Vertices.Count > 0)
            foreach (var p in Vertices)
                mb.Positions.Add(p);

        if (Faces.Count > 0)
            foreach (var face in Faces)
                mb.AddTriangleFan(face);

        if (Normals.Count > 0) {
            mb.CreateNormals = true;
            mb.Normals ??= [];
            foreach (var item in Normals) mb.Normals.Add(item);
        }

        if (Normals.Count == 0) mb.CreateNormals = false;

        if (TextureCoordinates is { Count: > 0 } textureCoordinates) {
            mb.CreateTextureCoordinates = true;
            mb.TextureCoordinates ??= [];
            foreach (var item in textureCoordinates) mb.TextureCoordinates.Add(item);
        }

        if (TextureCoordinates?.Count == 0) {
            TextureCoordinates = null;
            mb.TextureCoordinates = null;
            mb.CreateTextureCoordinates = false;
        }

        var mesh = mb.ToMesh();
        if (mesh.Normals == null || mesh.Normals.Count == 0) mesh.Normals = mesh.CalculateNormals();
        return mesh;
    }

    /// <summary>
    ///     Creates a <see cref="Mesh3DGroup" /> from the loaded file.
    /// </summary>
    /// <returns>A <see cref="Mesh3DGroup" />.</returns>
    public Mesh3DGroup CreateModel3D() {
        Mesh3DGroup? modelGroup = null;
        modelGroup = [];
        var g = CreateMeshGeometry3D();
        var gm = new Object3D {
            Geometry = g,
            Material = DefaultMaterial
        };
        modelGroup.Add(gm);
        return modelGroup;
    }

    /// <summary>
    ///     Loads a ply file from the <see cref="Stream" />.
    /// </summary>
    /// <param name="s">The stream containing the ply file.</param>
    public void Load(Stream s) {
        InitializeProperties();
        //Get the model format from the header in order for the correct strategy to be used
        using (var textReader = new StreamReader(s)) {
            var linCount = 0;
            //!textReader.EndOfStream
            while (!textReader.EndOfStream) {
                var lineTxt = textReader.ReadLine();
                if (lineTxt is null)
                    break;

                var initarr = lineTxt.Split(' ');
                if (initarr[0] == "format") {
                    if (initarr[1] == "ascii")
                        modelFormat = PlyFormatTypes.Ascii;

                    else if (initarr[1] == "binary_big_endian")
                        modelFormat = PlyFormatTypes.BinaryBigEndian;

                    else if (initarr[1] == "binary_little_endian") modelFormat = PlyFormatTypes.BinaryLittleEndian;
                } else {
                    continue;
                }

                linCount += 1;
            }
        }

        #region MyRegion

        switch (modelFormat) {
            case PlyFormatTypes.Ascii: {
                    if (plymodelUri.Length > 6)
                        using (var fs = new FileStream(plymodelUri, FileMode.Open, FileAccess.Read)) {
                            Load_ascii(fs);
                        }


                    break;
                }

            case PlyFormatTypes.BinaryBigEndian: {
                    if (plymodelUri.Length > 6)
                        using (var fs = new FileStream(plymodelUri, FileMode.Open, FileAccess.Read)) {
                            Load_binaryBE(fs);
                        }

                    break;
                }

            case PlyFormatTypes.BinaryLittleEndian: {
                    break;
                }
        }

        #endregion
    }

    public void Load(string filepath) {
        InitializeProperties();
        plymodelUri = filepath;
        using var fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
        Load(fs);
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Gets or sets the vertices of this ply model.
    /// </summary>
    public IList<Vector3> Vertices { get; private set; } = [];

    public IList<int[]> Faces { get; private set; } = [];

    /// <summary>
    ///     Gets or sets the normal vectors of this ply model.
    /// </summary>
    public IList<Vector3> Normals { get; private set; } = [];

    /// <summary>
    ///     Gets or sets the texture coordinates of the ply model.
    /// </summary>
    /// <remarks>S,T->X,Y</remarks>
    public IList<Vector2>? TextureCoordinates { get; private set; }

    /// <summary>
    ///     Gets or sets the number of vertices.
    /// </summary>
    public int VerticesNumber { get; private set; }

    /// <summary>
    ///     Gets or sets the number of faces.
    /// </summary>
    public int FacesNumber { get; private set; }

    /// <summary>
    ///     Contains information about the Ply file received.
    /// </summary>
    public Dictionary<string, double> ObjectInformation { get; private set; } = [];

    #endregion

    #region Initialized Variables

    /// <summary>
    ///     Stores the type of ply format in the <see cref="Stream" />.
    /// </summary>
    private PlyFormatTypes modelFormat = PlyFormatTypes.Ascii;

    /// <summary>
    ///     Tells us what the current element in the header is,
    ///     so we can pick its property values in the subsequent lines.
    /// </summary>
    /// <remarks>
    ///     Only useful in reading the ply header.
    /// </remarks>
    private PlyElements currentElement = PlyElements.None;

    /// <summary>
    ///     The line containing data about an element(It starts just after the "end_header" statement).
    /// </summary>
    private int elementLineCurrent;

    /* A cummulative sum of each element count in the current ply model.
     * This helps to provide a range of the element position as we move through the ascii Ply file.
     */
    //private int ElLine_SeqCount = 0;//[Deprecated].
    /// <summary>
    ///     Gets the number of lines that contains element data.
    /// </summary>
    private int elementLinesCount;

    private string plymodelUri = "";
    private Dictionary<string, PlyElement> elementsRange = [];

    #endregion

    #region private Types

    /// <summary>
    ///     Contains a list of ply formatted model types.
    /// </summary>
    private enum PlyFormatTypes {
        /// <summary>
        ///     ASCII ply format.
        /// </summary>
        Ascii,

        /// <summary>
        ///     Binary big endian ply format.
        /// </summary>
        BinaryBigEndian,

        /// <summary>
        ///     Binary little endian ply format.
        /// </summary>
        BinaryLittleEndian,


        /// <summary>
        ///     An unrecognized format.
        /// </summary>
        None
    }

    /// <summary>
    ///     Contains a list of supported ply elements.
    /// </summary>
    private enum PlyElements {
        /// <summary>
        ///     The vertex ply element.
        /// </summary>
        Vertex,

        /// <summary>
        ///     The face ply element.
        /// </summary>
        Face,

        /// <summary>
        ///     An unrecognized element.
        /// </summary>
        None
    }

    /// <summary>
    ///     A class that attempts to define ply elements.
    /// </summary>
    private class PlyElement {
        /// <summary>
        ///     Stores the property string with its index.
        /// </summary>
        public readonly Dictionary<string, int> PropertyWithIndex = [];

        public int PropertyIndex;

        /// <summary>
        ///     Initializes a new PLY element.
        /// </summary>
        /// <param name="_y1">The lower or start range.</param>
        /// <param name="_y2">The upper or end range.</param>
        /// <param name="hasNormals"></param>
        /// <param name="hasTextures"></param>
        public PlyElement(int y1 = 0, int y2 = 1, bool hasNormals = false, bool hasTextures = false) {
            PropertyIndex = 0;
            PropertyWithIndex = [];
            StartRange = y1;
            EndRange = y2;
            ContainsNormals = hasNormals;
            ContainsTextureCoordinates = hasTextures;
            ElementCount = EndRange - StartRange;
        }

        /// <summary>
        ///     Returns a value specifying whether the index: <paramref name="num" /> is in this <see cref="PlyElement" /> range.
        /// </summary>
        /// <param name="num">The index.</param>
        /// <returns></returns>
        public bool ContainsNumber(int num) {
            if (num >= StartRange && num <= EndRange) return true;

            return false;
        }

        #region Element class properties

        /// <summary>
        ///     The point from which the current element starts to get picked.
        /// </summary>
        public int StartRange { get; }

        /// <summary>
        ///     The point at which the current element stops to get picked.
        /// </summary>
        public int EndRange { get; }

        /// <summary>
        ///     Specifies whether or not the vertex element has a normal.
        /// </summary>
        public bool ContainsNormals { get; set; }

        /// <summary>
        ///     Whether or not the vertex element has a texture coordinate.
        /// </summary>
        /// <remarks>
        ///     for vertices only.
        /// </remarks>
        public bool ContainsTextureCoordinates { get; set; }

        /// <summary>
        ///     Stores the number of the specified element in the current Ply model.
        /// </summary>
        public int ElementCount { get; private set; }

        #endregion
    }

    #endregion

    #region Private methods

    private bool IsDigitChar(char input) {
        if (input == '-' || input == '+' || input == '0' || input == '1' || input == '2' || input == '3' ||
            input == '4' || input == '5' || input == '6' || input == '7' || input == '8' || input == '9' ||
            input == '.') return true;

        return false;
    }

    private void InitializeProperties() {
        #region public:

        Vertices = [];
        Faces = [];
        Normals = [];
        TextureCoordinates = [];
        FacesNumber = 0;
        VerticesNumber = 0;
        ObjectInformation = [];

        #endregion

        #region private:

        modelFormat = PlyFormatTypes.None;
        currentElement = PlyElements.None;
        elementLineCurrent = 0;
        elementLinesCount = 0;
        elementsRange = [];

        #endregion
    }

    /// <summary>
    ///     Loads an ascii format ply file.
    /// </summary>
    /// <param name="s"></param>
    private void Load_ascii(Stream s) {
        using var reader = new StreamReader(s);
        while (!reader.EndOfStream) {
            var curline = reader.ReadLine();
            if (curline is null)
                break;

            var strarr = curline.Split(' ');

            #region Heading

            //comment Line
            if (strarr[0] == "comment" || strarr[0] == "format" || strarr[0] == "ply") { }

            //obj_info Line
            else if (strarr[0] == "obj_info") {
                //ObjectInformation.Add(strarr[1], double.Parse(strarr[2]));
            }

            //element Line
            else if (strarr[0] == "element") {
                /* Supported elements
                 * vertex
                 * face
                 */

                if (strarr[1] == "vertex") {
                    VerticesNumber = int.Parse(strarr[2]);
                    //Add the vertex element to the dictionary, including its contextual range
                    elementsRange.Add("vertex",
                                       new PlyElement(elementLinesCount + 1, elementLinesCount + VerticesNumber));
                    elementLinesCount += int.Parse(strarr[2]);
                    //set the current element as a "vertex"element
                    currentElement = PlyElements.Vertex;
                } else if (strarr[1] == "face") {
                    FacesNumber = int.Parse(strarr[2]);

                    elementsRange.Add("face",
                                       new PlyElement(elementLinesCount + 1, elementLinesCount + FacesNumber));
                    elementLinesCount += int.Parse(strarr[2]);
                    currentElement = PlyElements.Face;
                } else {
                    var miscElementNumber = int.Parse(strarr[2]);

                    elementsRange.Add(strarr[1],
                                       new PlyElement(elementLinesCount, elementLinesCount + miscElementNumber));
                    elementLinesCount += int.Parse(strarr[2]);
                    currentElement = PlyElements.None;
                }
            }

            //property Line
            else if (strarr[0] == "property") {
                //Ignore the numerical data type for now

                switch (currentElement) {
                    case PlyElements.Vertex: {
                            var propInd = elementsRange["vertex"].PropertyIndex;
                            elementsRange["vertex"].PropertyWithIndex.Add(strarr[2], propInd);
                            //Increase the property index if there's an element which has/hasn't been supported.
                            elementsRange["vertex"].PropertyIndex += 1;
                            if (strarr[2] == "nx" || strarr[2] == "ny" || strarr[2] == "nz")
                                elementsRange["vertex"].ContainsNormals = true;
                            else if (strarr[2] == "s" || strarr[2] == "t")
                                elementsRange["vertex"].ContainsTextureCoordinates = true;

                            break;
                        }

                    case PlyElements.Face: {
                            //nothing to do yet

                            break;
                        }
                }
            } else if (strarr[0] == "end_header") {
                //end info, begin number collection.
                elementLineCurrent = 0;
            }

            #endregion

            /* We pick the elements and its properties by checking whether the current
             * element line is contained in the element range.
             * The picking occurs if the first character in the string indicates a number follows.
             */
            else if (IsDigitChar(strarr[0][0])) {
                elementLineCurrent++;
                if (elementsRange.ContainsKey("vertex"))
                    if (elementsRange["vertex"].ContainsNumber(elementLineCurrent)) {
                        //Get vertices
                        var xIndx = elementsRange["vertex"].PropertyWithIndex["x"];
                        var yIndx = elementsRange["vertex"].PropertyWithIndex["y"];
                        var zIndx = elementsRange["vertex"].PropertyWithIndex["z"];
                        var pt = new Vector3 {
                            X = float.Parse(strarr[xIndx]),
                            Y = float.Parse(strarr[yIndx]),
                            Z = float.Parse(strarr[zIndx])
                        };
                        Vertices.Add(pt);

                        if (elementsRange["vertex"].ContainsNormals) {
                            var nxIndx = elementsRange["vertex"].PropertyWithIndex["nx"];
                            var nyIndx = elementsRange["vertex"].PropertyWithIndex["ny"];
                            var nzIndx = elementsRange["vertex"].PropertyWithIndex["nz"];

                            var vect3D = new Vector3 {
                                X = float.Parse(strarr[nxIndx]),
                                Y = float.Parse(strarr[nyIndx]),
                                Z = float.Parse(strarr[nzIndx])
                            };
                            Normals.Add(vect3D);
                        }

                        if (elementsRange["vertex"].ContainsTextureCoordinates) {
                            var sIndx = elementsRange["vertex"].PropertyWithIndex["s"];
                            var tIndx = elementsRange["vertex"].PropertyWithIndex["t"];

                            var texpt = new Vector2 {
                                X = float.Parse(strarr[sIndx]),
                                Y = float.Parse(strarr[tIndx])
                            };
                            TextureCoordinates?.Add(texpt);
                        }
                    }

                if (elementsRange.ContainsKey("face"))
                    if (elementsRange["face"].ContainsNumber(elementLineCurrent)) {
                        var facepos = new List<int>();
                        for (var i = 1; i <= int.Parse(strarr[0]); i++) facepos.Add(int.Parse(strarr[i]));
                        Faces.Add([.. facepos]);
                    }

                //should increase the elementLine_Current iff
                //the line contains element data.
            }
        }
    }

    /// <summary>
    ///     Loads a binary_big_endian format ply file.
    /// </summary>
    /// <param name="s"></param>
    private void Load_binaryBE(Stream s) {
        using (var reader = new BinaryReader(s)) {
            while (reader.BaseStream.Position < reader.BaseStream.Length) {
                var curline = reader.ReadString();
                var strarr = curline.Split(' ');
                //comment Line
                if (strarr[0] == "comment") continue;

                //obj_info Line
                if (strarr[0] == "obj_info") ObjectInformation.Add(strarr[1], double.Parse(strarr[2]));

                //element Line
                if (strarr[0] == "element") {
                    /* Supported elements
                     * vertex
                     * face
                     */

                    if (strarr[1] == "vertex") {
                        VerticesNumber = int.Parse(strarr[2]);

                        //Add the vertex element to the dictionary, including its contextual range
                        elementsRange.Add("vertex",
                                           new PlyElement(elementLinesCount, elementLinesCount + VerticesNumber));
                        elementLinesCount += int.Parse(strarr[2]);
                        currentElement = PlyElements.Vertex;
                    } else if (strarr[1] == "face") {
                        FacesNumber = int.Parse(strarr[2]);

                        elementsRange.Add("face",
                                           new PlyElement(elementLinesCount, elementLinesCount + FacesNumber));
                        elementLinesCount += int.Parse(strarr[2]);
                        currentElement = PlyElements.Face;
                    } else {
                        var miscElementNumber = int.Parse(strarr[2]);

                        elementsRange.Add(strarr[1],
                                           new PlyElement(elementLinesCount, elementLinesCount + miscElementNumber));
                        elementLinesCount += int.Parse(strarr[2]);
                    }
                }

                //property Line
                if (strarr[0] == "property")
                    //Ignore the numerical data type for now
                    switch (currentElement) {
                        case PlyElements.Vertex: {
                                if (strarr[2] == "nx" || strarr[2] == "ny" || strarr[2] == "nz")
                                    elementsRange["vertex"].ContainsNormals = true;

                                var propInd = elementsRange["vertex"].PropertyIndex;
                                elementsRange["vertex"].PropertyWithIndex.Add(strarr[2], propInd);
                                //Increase the property index if there's an element which has/hasn't been supported.
                                elementsRange["vertex"].PropertyIndex += 1;
                                break;
                            }

                        case PlyElements.Face: {
                                //nothing to do yet
                                break;
                            }
                    }

                if (strarr[0] == "end_header")
                    //end info, begin number collection.
                    elementLineCurrent = 0;

                /* We pick the elements and its properties by checking whether the current
                 * element line is contained in the element range.
                 */
                if (elementsRange["vertex"].ContainsNumber(elementLineCurrent)) {
                    //Get vertices
                    var xIndx = elementsRange["vertex"].PropertyWithIndex["x"];
                    var yIndx = elementsRange["vertex"].PropertyWithIndex["y"];
                    var zIndx = elementsRange["vertex"].PropertyWithIndex["z"];
                    var pt = new Vector3 {
                        X = float.Parse(strarr[xIndx]),
                        Y = float.Parse(strarr[yIndx]),
                        Z = float.Parse(strarr[zIndx])
                    };
                    Vertices.Add(pt);

                    if (elementsRange["vertex"].ContainsNormals) {
                        var nxIndx = elementsRange["vertex"].PropertyWithIndex["nx"];
                        var nyIndx = elementsRange["vertex"].PropertyWithIndex["ny"];
                        var nzIndx = elementsRange["vertex"].PropertyWithIndex["nz"];
                    }

                    if (elementsRange["vertex"].ContainsTextureCoordinates) { }
                }

                if (elementsRange["face"].ContainsNumber(elementLineCurrent)) {
                    var facepos = new List<int>();
                    for (var i = 1; i <= int.Parse(strarr[0]); i++) facepos.Add(int.Parse(strarr[i]));
                    Faces.Add([.. facepos]);
                }


                elementLineCurrent++;
            }
        }

        var br = new BinaryReader(s);
        br.ReadString();
    }

    #endregion
}
