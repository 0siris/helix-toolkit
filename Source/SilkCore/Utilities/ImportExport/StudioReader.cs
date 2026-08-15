using System.Text;
using HelixToolkit.SharpDX.Core.Geometry;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;

namespace HelixToolkit.SharpDX.Core.Utilities.ImportExport;
using FileFormatException = Exception;
using Object3DGroup = List<Object3D>;
using PhongMaterial = PhongMaterialCore;

    /// <summary>
    ///     Ported from HelixToolkit.Wpf
    /// </summary>
[Obsolete("Suggest to use HelixToolkit.SharpDX.Assimp")]
public class StudioReader : IModelReader {
    private readonly Dictionary<string, MaterialCore> materials = [];

    private enum ChunkId {
        //// Primary chunk

        Main3Ds = 0x4D4D,

        // Main Chunks
        Edit3Ds = 0x3D3D, // this is the start of the editor config
        Keyf3Ds = 0xB000, // this is the start of the keyframer config
        Version = 0x0002,
        Meshversion = 0x3D3E,

        // sub defines of EDIT3DS
        EditMaterial = 0xAFFF,
        EditConfig1 = 0x0100,
        EditConfig2 = 0x3E3D,
        EditViewP1 = 0x7012,
        EditViewP2 = 0x7011,
        EditViewP3 = 0x7020,
        EditView1 = 0x7001,
        EditBackgr = 0x1200,
        EditAmbient = 0x2100,
        EditObject = 0x4000,
        EditUnknw01 = 0x1100,
        EditUnknw02 = 0x1201,
        EditUnknw03 = 0x1300,
        EditUnknw04 = 0x1400,
        EditUnknw05 = 0x1420,
        EditUnknw06 = 0x1450,
        EditUnknw07 = 0x1500,
        EditUnknw08 = 0x2200,
        EditUnknw09 = 0x2201,
        EditUnknw10 = 0x2210,
        EditUnknw11 = 0x2300,
        EditUnknw12 = 0x2302,
        EditUnknw13 = 0x3000,
        EditUnknw14 = 0xAFFF,

        // sub defines of EDIT_MATERIAL
        MatName01 = 0xA000,
        MatLuminance = 0xA010,
        MatDiffuse = 0xA020,
        MatSpecular = 0xA030,
        MatShininess = 0xA040,
        MatMap = 0xA200,
        MatMapfile = 0xA300,

        //  MAT_AMBIENT=
        MatTransparency = 0xA050,

        // sub defines of EDIT_OBJECT
        ObjTrimesh = 0x4100,
        ObjLight = 0x4600,
        ObjCamera = 0x4700,
        ObjUnknwn01 = 0x4010,
        ObjUnknwn02 = 0x4012, // Could be shadow

        // sub defines of OBJ_CAMERA
        CamUnknwn01 = 0x4710,
        CamUnknwn02 = 0x4720,

        // sub defines of OBJ_LIGHT
        LitOff = 0x4620,
        LitSpot = 0x4610,
        LitUnknwn01 = 0x465A,

        // sub defines of OBJ_TRIMESH
        TriVertexl = 0x4110,
        TriFacel2 = 0x4111,
        TriFacel1 = 0x4120,
        TriFacemat = 0x4130,
        TriTexcoord = 0x4140,
        TriSmooth = 0x4150,
        TriLocal = 0x4160,
        TriVisible = 0x4165,

        // sub defs of KEYF3DS
        KeyfUnknwn01 = 0xB009,
        KeyfUnknwn02 = 0xB00A,
        KeyfFrames = 0xB008,
        KeyfObjdes = 0xB002,
        KeyfHierarchy = 0xB030,
        Kfname = 0xB010,

        // these define the different color chunk types
        ColRgb = 0x0010,
        ColTru = 0x0011, // RGB24
        ColUnk = 0x0013,

        // defines for viewport chunks
        Top = 0x0001,
        Bottom = 0x0002,
        Left = 0x0003,
        Right = 0x0004,
        Front = 0x0005,
        Back = 0x0006,
        User = 0x0007,
        Camera = 0x0008, // = 0xFFFF is the actual code read from file
        Light = 0x0009,
        Disabled = 0x0010,
        Bogus = 0x0011,
        // ReSharper restore UnusedMember.Local
        // ReSharper restore InconsistentNaming
        Percentw = 0x0030,
        Percentf = 0x0031,
        Percentd = 0x0032
    }


        /// <summary>
        ///     Helper class to create objects
        /// </summary>
    public Object3DGroup ObGroup = [];

        /// <summary>
        ///     Gets or sets the directory
        /// </summary>
    public string? Directory { get; set; }

        /// <summary>
        ///     Gets or sets the texture path.
        /// </summary>
        /// <value>The texture path.</value>
    public string? TexturePath {
        get => Directory;

        set => Directory = value;
    }

    public Object3DGroup Read(string path, ModelInfo info = default) {
        Directory = Path.GetDirectoryName(path);
        using (var s = File.OpenRead(path)) {
            return Read(s);
        }

        ;
    }

    public Object3DGroup Read(Stream s, ModelInfo info = default) {
        using (var reader = new BinaryReader(s)) {
            var headerId = ReadChunkId(reader);
            if (headerId != ChunkId.Main3Ds) throw new FileFormatException("Unknown file");
            ReadChunkSize(reader);
            //if (headerSize != length)
            //{
            //    throw new FileFormatException("Incomplete file (file length does not match header)");
            //}
            while (reader.BaseStream.Position < reader.BaseStream.Length) {
                var id = ReadChunkId(reader);
                var size = ReadChunkSize(reader);
                switch (id) {
                    case ChunkId.EditMaterial:
                        ReadMaterial(reader, size);
                        break;
                    case ChunkId.EditObject:
                        ReadObject(reader, size);
                        break;
                    case ChunkId.Edit3Ds:
                    case ChunkId.ObjCamera:
                    case ChunkId.ObjLight:
                    case ChunkId.ObjTrimesh:

                        // don't read the whole chunk, read the sub-defines...
                        break;

                    default:

                        // download the whole chunk
                        ReadData(reader, size - 6);
                        break;
                }
            }
        }

        return ObGroup;
    }

        /// <summary>
        ///     Read a chunk id.
        /// </summary>
        /// <param name="reader">
        ///     The reader.
        /// </param>
        /// <returns>
        ///     The chunk ID.
        /// </returns>
    private ChunkId ReadChunkId(BinaryReader reader) => (ChunkId)reader.ReadUInt16();

    /// <summary>
        ///     Read a chunk size.
        /// </summary>
        /// <param name="reader">
        ///     The reader.
        /// </param>
        /// <returns>
        ///     The read chunk size.
        /// </returns>
    private int ReadChunkSize(BinaryReader reader) => (int)reader.ReadUInt32();


    /// <summary>
        ///     reads the Material of a chunck
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="chunkSize"></param>
    private void ReadMaterial(BinaryReader reader, int chunkSize) {
        var total = 6;
        string? name = null;
        var luminance = Color.Transparent; //SharpDX.Color not System.Windows.Media.Color
        var diffuse = Color.Transparent;
        var specular = Color.Transparent;
        double opacity = 0;
        string? texture = null;
        float specularPower = 100; //check if we can find this somewhere instead of just setting it to 100
        while (total < chunkSize) {
            var id = ReadChunkId(reader);
            var size = ReadChunkSize(reader);
            total += size;

            switch (id) {
                case ChunkId.MatName01:
                    name = ReadString(reader);
                    break;
                case ChunkId.MatTransparency:
                    // skip the first 6 bytes
                    ReadData(reader, 6);
                    // read the percent value as 16Bit Uint
                    var data = ReadData(reader, 2);
                    opacity = (100 - BitConverter.ToUInt16(data, 0)) / 100.0;
                    break;
                case ChunkId.MatLuminance:
                    luminance = ReadColor(reader);
                    break;
                case ChunkId.MatDiffuse:
                    diffuse = ReadColor(reader);
                    break;
                case ChunkId.MatSpecular:
                    specular = ReadColor(reader);
                    break;
                case ChunkId.MatShininess:
                    //byte[] bytes = this.ReadData(reader, size - 6);
                    specularPower = ReadPercent(reader, size - 6);
                    break;
                case ChunkId.MatMap:
                    texture = ReadMatMap(reader, size - 6);
                    break;
                case ChunkId.MatMapfile:
                    ReadData(reader, size - 6);
                    break;

                default:
                    ReadData(reader, size - 6);
                    break;
            }
        }

        var image = ReadBitmapSoure(texture, diffuse);

        if (Math.Abs(opacity) > 0.001) {
            diffuse.A = (byte)(opacity * 255);
            luminance.A = (byte)(opacity * 255);
        }

        var material = new PhongMaterialCore {
            DiffuseColor = diffuse,
            AmbientColor = luminance, //not really sure about this, lib3ds uses 0xA010 as AmbientColor
            SpecularColor = specular,
            SpecularShininess = specularPower
        };
        if (image != null) material.NormalMap = image;
        if (name != null) materials[name] = material;
    }

        /// <summary>
        ///     Reads an object
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="chunkSize"></param>
    private void ReadObject(BinaryReader reader, int chunkSize) {
        var total = 6;
        var objectName = ReadString(reader);
        total += objectName.Length + 1;
        while (total < chunkSize) {
            var id = ReadChunkId(reader);
            var size = ReadChunkSize(reader);
            total += size;
            switch (id) {
                case ChunkId.ObjTrimesh:
                    ReadTriangularMesh(reader, size);
                    break;
                default: {
                        ReadData(reader, size - 6);
                        break;
                    }
            }
        }
    }

        /// <summary>
        ///     Reads a triangular mesh.
        /// </summary>
        /// <param name="reader">
        ///     The reader.
        /// </param>
        /// <param name="chunkSize">
        ///     The chunk size.
        /// </param>
    private void ReadTriangularMesh(BinaryReader reader, int chunkSize) {
        var bytesRead = 6;
        Vector3Collection? positions = null;
        IntCollection? faces = null;
        Vector2Collection? textureCoordinates = null;
        List<FaceSet>? facesets = null;
        IntCollection? triangleIndices = null;
        Vector3Collection? normals = null;
        //Matrix matrix = Matrix.Identity;
        Vector3Collection? tangents = null;
        Vector3Collection? bitangents = null;
        var transforms = new List<Matrix>();
        while (bytesRead < chunkSize) {
            var id = ReadChunkId(reader);
            var size = ReadChunkSize(reader);
            bytesRead += size;
            switch (id) {
                case ChunkId.TriVertexl:
                    positions = ReadVertexList(reader);
                    break;
                case ChunkId.TriFacel1:
                    faces = ReadFaceList(reader);
                    size -= faces.Count / 3 * 8 + 2;
                    facesets = ReadFaceSets(reader, size - 6);
                    break;
                case ChunkId.TriTexcoord:
                    textureCoordinates = ReadTexCoords(reader);
                    break;
                case ChunkId.TriLocal:
                    transforms.Add(ReadTransformation(reader));
                    break;
                default:
                    ReadData(reader, size - 6);
                    break;
            }
        }

        if (faces == null)
            //no faces defined?? return...
            return;

        if (positions is null)
            return;

        if (facesets == null || facesets.Count == 0) {
            triangleIndices = faces;
            CreateMesh(positions,
                       textureCoordinates,
                       triangleIndices,
                       transforms,
                       out normals,
                       out tangents,
                       out bitangents,
                       new PhongMaterial {
                           Name = "Gray",
                           AmbientColor = new Color4(0.1f, 0.1f, 0.1f, 1.0f),
                           DiffuseColor = new Color4(0.254902f, 0.254902f, 0.254902f, 1.0f),
                           SpecularColor = new Color4(0.0225f, 0.0225f, 0.0225f, 1.0f),
                           EmissiveColor = new Color4(0.0f, 0.0f, 0.0f, 1.0f),
                           SpecularShininess = 12.8f
                       });
            //Add default get and setter
        } else {
            foreach (var fm in facesets) {
                triangleIndices = ConvertFaceIndices(fm.Faces, faces);
                MaterialCore? mat = null;
                if (materials.ContainsKey(fm.Name)) mat = materials[fm.Name];
                CreateMesh(positions,
                           textureCoordinates,
                           triangleIndices,
                           transforms,
                           out normals,
                           out tangents,
                           out bitangents,
                           mat);
            }
        }
    }


        /// <summary>
        ///     Create a Mesh, with found props
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="textureCoordinates"></param>
        /// <param name="triangleIndices"></param>
        /// <param name="normals"></param>
        /// <param name="tangents"></param>
        /// <param name="bitangents"></param>
        /// <param name="material"></param>
        /// <param name="transforms"></param>
    private void CreateMesh(
        Vector3Collection positions,
        Vector2Collection? textureCoordinates,
        IntCollection triangleIndices,
        List<Matrix> transforms,
        out Vector3Collection normals,
        out Vector3Collection tangents,
        out Vector3Collection bitangents,
        MaterialCore? material
    ) {
        ComputeNormals(positions, triangleIndices, out normals);
        if (textureCoordinates == null) {
            textureCoordinates = [];
            foreach (var pos in positions) textureCoordinates.Add(Vector2.One);
        }

        MeshBuilder.ComputeTangents(positions,
                                    normals,
                                    textureCoordinates,
                                    triangleIndices,
                                    out tangents,
                                    out bitangents);
        var mesh = new MeshGeometry3D {
            Positions = positions,
            Normals = normals,
            TextureCoordinates = textureCoordinates,
            Indices = triangleIndices,
            Tangents = tangents,
            BiTangents = bitangents
        };
        var ob3D = new Object3D {
            Geometry = mesh,
            Material = material,
            Transform = transforms,
            Name = "Default"
        };
        ObGroup.Add(ob3D);
    }

        /// <summary>
        ///     Stolen from MeshBuilder class, maybe make this static method there public...
        /// </summary>
        /// <param name="positions"></param>
        /// <param name="triangleIndices"></param>
        /// <param name="normals"></param>
    private static void ComputeNormals(
        Vector3Collection positions,
        IntCollection triangleIndices,
        out Vector3Collection normals
    ) {
        normals = new Vector3Collection(positions.Count);
        normals.AddRange(Enumerable.Repeat(Vector3.Zero, positions.Count));

        for (var t = 0; t < triangleIndices.Count; t += 3) {
            var i1 = triangleIndices[t];
            var i2 = triangleIndices[t + 1];
            var i3 = triangleIndices[t + 2];

            var v1 = positions[i1];
            var v2 = positions[i2];
            var v3 = positions[i3];

            var p1 = v2 - v1;
            var p2 = v3 - v1;
            var n = SilkMath.Cross(p1, p2);
            // angle
            p1.Normalize();
            p2.Normalize();
            var a = (float)Math.Acos(SilkMath.Dot(p1, p2));
            n.Normalize();
            normals[i1] += a * n;
            normals[i2] += a * n;
            normals[i3] += a * n;
        }

        for (var i = 0; i < normals.Count; i++) {
            var n = normals[i];
            n.Normalize();
            normals[i] = n;
        }
    }

    private static IntCollection ConvertFaceIndices(List<int> subFaces, IList<int> faces) {
        var triangleIndices = new IntCollection(subFaces.Count * 3); // new List<int>(subFaces.Count * 3);
        foreach (var f in subFaces) {
            triangleIndices.Add(faces[f * 3]);
            triangleIndices.Add(faces[f * 3 + 1]);
            triangleIndices.Add(faces[f * 3 + 2]);
        }

        return triangleIndices;
    }

    private Vector2Collection ReadTexCoords(BinaryReader reader) {
        int size = reader.ReadUInt16();
        var pts = new Vector2Collection();
        for (var i = 0; i < size; i++) {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            pts.Add(new Vector2(x, 1 - y));
        }

        return pts;
    }

        /// <summary>
        ///     Reads face sets.
        /// </summary>
        /// <param name="reader">
        ///     The reader.
        /// </param>
        /// <param name="chunkSize">
        ///     The chunk size.
        /// </param>
        /// <returns>
        ///     A list of face sets.
        /// </returns>
    private List<FaceSet> ReadFaceSets(BinaryReader reader, int chunkSize) {
        var total = 6;
        var list = new List<FaceSet>();
        while (total < chunkSize) {
            var id = ReadChunkId(reader);
            var size = ReadChunkSize(reader);
            total += size;
            switch (id) {
                case ChunkId.TriFacemat: {
                        var name = ReadString(reader);
                        int n = reader.ReadUInt16();
                        var c = new List<int>();
                        for (var i = 0; i < n; i++) c.Add(reader.ReadUInt16());

                        var fm = new FaceSet { Name = name, Faces = c };
                        list.Add(fm);
                        break;
                    }

                case ChunkId.TriSmooth: {
                        ReadData(reader, size - 6);
                        break;
                    }

                default: {
                        ReadData(reader, size - 6);
                        break;
                    }
            }
        }

        return list;
    }

    private IntCollection ReadFaceList(BinaryReader reader) {
        int size = reader.ReadUInt16();
        var faces = new IntCollection();
        for (var i = 0; i < size; i++) {
            faces.Add(reader.ReadUInt16());
            faces.Add(reader.ReadUInt16());
            faces.Add(reader.ReadUInt16());
            reader.ReadUInt16();
        }

        return faces;
    }


        /// <summary>
        ///     Reads a vector.
        /// </summary>
        /// <param name="reader">
        ///     The reader.
        /// </param>
        /// <returns>
        ///     A vector.
        /// </returns>
    private Vector3 ReadVector(BinaryReader reader) => new(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

    /// <summary>
        ///     Reads a transformation.
        /// </summary>
        /// <param name="reader">
        ///     The reader.
        /// </param>
        /// <returns>
        ///     A transformation.
        /// </returns>
    private Matrix ReadTransformation(BinaryReader reader) {
        var localx = ReadVector(reader);
        var localy = ReadVector(reader);
        var localz = ReadVector(reader);
        var origin = ReadVector(reader);

        var matrix = new Matrix {
            M11 = localx.X,
            M21 = localx.Y,
            M31 = localx.Z,
            M12 = localy.X,
            M22 = localy.Y,
            M32 = localy.Z,
            M13 = localz.X,
            M23 = localz.Y,
            M33 = localz.Z,
            M41 = origin.X,
            M42 = origin.Y,
            M43 = origin.Z,
            M14 = 0,
            M24 = 0,
            M34 = 0,
            M44 = 1
        };

        return matrix;
    }

    private Vector3Collection ReadVertexList(BinaryReader reader) {
        int size = reader.ReadUInt16();
        var pts = new Vector3Collection();
        for (var i = 0; i < size; i++) {
            var x = reader.ReadSingle();
            var y = reader.ReadSingle();
            var z = reader.ReadSingle();
            pts.Add(new Vector3(x, y, z));
        }

        return pts;
    }

        /// <summary>
        ///     A bit hacky we use the give texture as normalMap, if not existant we create a BitMapSource in the fallbackColor
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="fallBackColor"></param>
        /// <returns></returns>
    private Stream? ReadBitmapSoure(string? texture, Color fallBackColor) {
        if (texture == null) return null;
        try {
            var ext = Path.GetExtension(texture).ToLowerInvariant();
            // TGA not supported - convert textures to .png
            if (ext == ".tga") texture = Path.ChangeExtension(texture, ".png");
            var actualTexturePath = TexturePath ?? string.Empty;
            var path = Path.GetFullPath(Path.Combine(actualTexturePath, texture));
            if (File.Exists(path)) {
                var stream = new MemoryStream();
                using var fileStream = File.OpenRead(path);
                fileStream.CopyTo(stream);
                return stream;
            }
            return null;
        } catch (Exception ex) //Not really nice
        {
            throw new FileFormatException(ex.Message);
        }
    }

        /// <summary>
        ///     Reads a material map.
        /// </summary>
        /// <param name="reader">
        ///     The reader.
        /// </param>
        /// <param name="size">
        ///     The size.
        /// </param>
        /// <returns>
        ///     The mat map.
        /// </returns>
    private string ReadMatMap(BinaryReader reader, int size) {
        ReadChunkId(reader);
        ReadChunkSize(reader);
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        size -= 14;
        var cname = ReadString(reader);
        size -= cname.Length + 1;
        ReadData(reader, size);
        return cname;
    }

        /// <summary>
        ///     Read a color.
        /// </summary>
        /// <param name="reader">
        ///     The reader.
        /// </param>
        /// <returns>
        ///     A color.
        /// </returns>
    private Color ReadColor(BinaryReader reader) {
        var type = ReadChunkId(reader);
        var csize = ReadChunkSize(reader);
        switch (type) {
            case ChunkId.ColRgb: {
                    var r = reader.ReadSingle();
                    var g = reader.ReadSingle();
                    var b = reader.ReadSingle();

                    return new Color(r, g, b); // .FromScRgb(1, r, g, b);
                }

            case ChunkId.ColTru: {
                    var r = reader.ReadByte();
                    var g = reader.ReadByte();
                    var b = reader.ReadByte();
                    return new Color(r, g, b);
                }

            default:
                ReadData(reader, csize);
                break;
        }

        return Color.White;
    }

    private float ReadPercent(BinaryReader reader, int size) {
        var type = ReadChunkId(reader);
        ReadChunkSize(reader);
        size -= 6;
        float percent = 1;
        switch (type) {
            case ChunkId.Percentw:
                percent = reader.ReadUInt16();
                break;
            case ChunkId.Percentf:
                percent = reader.ReadSingle();
                break;
            case ChunkId.Percentd:
                reader.ReadBytes(size);
                break;
        }

        return percent;
    }

        /// <summary>
        ///     Read data.
        /// </summary>
        /// <param name="reader">
        ///     The reader.
        /// </param>
        /// <param name="size">
        ///     Excluding header size
        /// </param>
        /// <returns>
        ///     The data.
        /// </returns>
    private byte[] ReadData(BinaryReader reader, int size) => reader.ReadBytes(size);

    /// <summary>
        ///     Reads a string.
        /// </summary>
        /// <param name="reader">
        ///     The reader.
        /// </param>
        /// <returns>
        ///     The string.
        /// </returns>
    private string ReadString(BinaryReader reader) {
        var sb = new StringBuilder();
        while (true) {
            var ch = (char)reader.ReadByte();
            if (ch == 0) break;

            sb.Append(ch);
        }

        return sb.ToString();
    }

        /// <summary>
        ///     Represents a set of faces that belongs to the same material.
        /// </summary>
    private class FaceSet {
            /// <summary>
            ///     Gets or sets Faces.
            /// </summary>
        public List<int> Faces { get; set; } = [];

            /// <summary>
            ///     Gets or sets the name of the material.
            /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}
