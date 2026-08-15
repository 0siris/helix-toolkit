// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StLReader.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Provides an importer for StereoLithography .StL files.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HelixToolkit.SharpDX.Core.Model;


namespace HelixToolkit.SharpDX.Core;

using FileFormatException = Exception;
using Mesh3DGroup = List<Object3D>;
using Point3D = Color3;

/// <summary>
///     Provides an importer for StereoLithography .StL files.
/// </summary>
/// <remarks>
///     The format is documented on <a href="http://en.wikipedia.org/wiki/STL_(file_format)">Wikipedia</a>.
/// </remarks>
[Obsolete("Suggest to use HelixToolkit.SharpDX.Assimp")]
public class StLReader : ModelReader {
    /// <summary>
    ///     The regular expression used to parse normal vectors.
    /// </summary>
    private static readonly Regex NormalRegex = new(@"normal\s*(\S*)\s*(\S*)\s*(\S*)", RegexOptions.Compiled);

    /// <summary>
    ///     The regular expression used to parse vertices.
    /// </summary>
    private static readonly Regex VertexRegex = new(@"vertex\s*(\S*)\s*(\S*)\s*(\S*)", RegexOptions.Compiled);

    /// <summary>
    ///     The index.
    /// </summary>
    private int index;

    /// <summary>
    ///     The last color.
    /// </summary>
    private Color lastColor;

    /// <summary>
    ///     Initializes a new instance of the <see cref="StLReader" /> class.
    /// </summary>
    public StLReader() {
        Meshes = [];
        Materials = [];
    }

    /// <summary>
    ///     Gets the file header.
    /// </summary>
    /// <value>
    ///     The header.
    /// </value>
    public string? Header { get; private set; }

    /// <summary>
    ///     Gets the materials.
    /// </summary>
    /// <value> The materials. </value>
    public IList<MaterialCore> Materials { get; }

    /// <summary>
    ///     Gets the meshes.
    /// </summary>
    /// <value> The meshes. </value>
    public IList<MeshBuilder> Meshes { get; }

    /// <summary>
    ///     Reads the model from the specified stream.
    /// </summary>
    /// <param name="stream">The stream.</param>
    /// <param name="info"></param>
    /// <returns>The model.</returns>
    public override Mesh3DGroup? Read(Stream stream, ModelInfo info = default) {
        // Try to read in BINARY format
        var success = TryReadBinary(stream);
        if (!success) {
            // Reset position of stream
            stream.Position = 0;

            // Read in ASCII format
            success = TryReadAscii(stream);
        }

        if (success) return ToModel3D();

        return null;
    }

    /// <summary>
    ///     Builds the model.
    /// </summary>
    /// <returns>The model.</returns>
    public Mesh3DGroup ToModel3D() {
        var modelGroup = new Mesh3DGroup();
        var i = 0;
        foreach (var mesh in Meshes) {
            var gm = new Object3D {
                Geometry = mesh.ToMesh(),
                Material = Materials[i]
            };
            modelGroup.Add(gm);
            i++;
        }

        return modelGroup;
    }

    /// <summary>
    ///     Parses the ID and values from the specified line.
    /// </summary>
    /// <param name="line">
    ///     The line.
    /// </param>
    /// <param name="id">
    ///     The id.
    /// </param>
    /// <param name="values">
    ///     The values.
    /// </param>
    private static void ParseLine(string line, out string id, out string values) {
        line = line.Trim();
        var idx = line.IndexOf(' ');
        if (idx == -1) {
            id = line.ToLowerInvariant();
            values = string.Empty;
        } else {
            id = line.Substring(0, idx).ToLowerInvariant();
            values = line.Substring(idx + 1);
        }
    }

    /// <summary>
    ///     Parses a normal string.
    /// </summary>
    /// <param name="input">
    ///     The input string.
    /// </param>
    /// <returns>
    ///     The normal vector.
    /// </returns>
    private static Color3 ParseNormal(string input) {
        input = input.ToLowerInvariant();
        input = input.Replace("nan", "NaN");
        var match = NormalRegex.Match(input);
        if (!match.Success) throw new FileFormatException("Unexpected line.");

        var x = double.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var y = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var z = double.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);
        return new Color3((float)x, (float)y, (float)z);
    }

    /// <summary>
    ///     Reads a float (4 byte)
    /// </summary>
    /// <param name="reader">
    ///     The reader.
    /// </param>
    /// <returns>
    ///     The float.
    /// </returns>
    private static float ReadFloat(BinaryReader reader) {
        var bytes = reader.ReadBytes(4);
        return BitConverter.ToSingle(bytes, 0);
    }

    /// <summary>
    ///     Reads a line from the stream reader.
    /// </summary>
    /// <param name="reader">
    ///     The stream reader.
    /// </param>
    /// <param name="token">
    ///     The expected token ID.
    /// </param>
    /// <exception cref="FileFormatException">
    ///     The expected token ID was not matched.
    /// </exception>
    private static void ReadLine(StreamReader reader, string token) {
        ArgumentNullException.ThrowIfNull(token);

        var line = reader.ReadLine()
            ?? throw new System.IO.EndOfStreamException("Unexpected end of STL file.");
        string id, values;
        ParseLine(line, out id, out values);

        if (!string.Equals(token, id, StringComparison.OrdinalIgnoreCase))
            throw new FileFormatException("Unexpected line.");
    }

    /// <summary>
    ///     Reads a 16-bit unsigned integer.
    /// </summary>
    /// <param name="reader">
    ///     The reader.
    /// </param>
    /// <returns>
    ///     The unsigned integer.
    /// </returns>
    private static ushort ReadUInt16(BinaryReader reader) {
        var bytes = reader.ReadBytes(2);
        return BitConverter.ToUInt16(bytes, 0);
    }

    /// <summary>
    ///     Reads a 32-bit unsigned integer.
    /// </summary>
    /// <param name="reader">
    ///     The reader.
    /// </param>
    /// <returns>
    ///     The unsigned integer.
    /// </returns>
    private static uint ReadUInt32(BinaryReader reader) {
        var bytes = reader.ReadBytes(4);
        return BitConverter.ToUInt32(bytes, 0);
    }

    /// <summary>
    ///     Tries to parse a vertex from a string.
    /// </summary>
    /// <param name="line">
    ///     The input string.
    /// </param>
    /// <param name="point">
    ///     The vertex point.
    /// </param>
    /// <returns>
    ///     True if parsing was successful.
    /// </returns>
    private static bool TryParseVertex(string line, out Point3D point) {
        line = line.ToLowerInvariant();
        var match = VertexRegex.Match(line);
        if (!match.Success) {
            point = new Point3D();
            return false;
        }

        var x = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
        var y = float.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);
        var z = float.Parse(match.Groups[3].Value, CultureInfo.InvariantCulture);

        point = new Point3D(x, y, z);
        return true;
    }

    /// <summary>
    ///     Reads a facet.
    /// </summary>
    /// <param name="reader">
    ///     The stream reader.
    /// </param>
    /// <param name="normal">
    ///     The normal.
    /// </param>
    private void ReadFacet(StreamReader reader, string normal) {
#pragma warning disable 168
        var n = ParseNormal(normal);
#pragma warning restore 168
        var points = new List<Point3D>();
        ReadLine(reader, "outer");
        while (true) {
            var line = reader.ReadLine();
            if (string.IsNullOrEmpty(line)) continue;

            line = line.Trim();

            Point3D point;
            if (TryParseVertex(line, out point)) {
                points.Add(point);
                continue;
            }

            string id, values;
            ParseLine(line, out id, out values);

            if (id == "endloop") break;
        }

        ReadLine(reader, "endfacet");

        if (Materials.Count < index + 1) Materials.Add(DefaultMaterial);

        if (Meshes.Count < index + 1) Meshes.Add(new MeshBuilder(true));

        Meshes[index].AddPolygon(points);

        // todo: add normals
    }

    /// <summary>
    ///     Reads a triangle from a binary STL file.
    /// </summary>
    /// <param name="reader">
    ///     The reader.
    /// </param>
    private void ReadTriangle(BinaryReader reader) {
        var ni = ReadFloat(reader);
        var nj = ReadFloat(reader);
        var nk = ReadFloat(reader);

#pragma warning disable 168
        var n = new Color3(ni, nj, nk);
#pragma warning restore 168

        var x1 = ReadFloat(reader);
        var y1 = ReadFloat(reader);
        var z1 = ReadFloat(reader);
        var v1 = new Point3D(x1, y1, z1);

        var x2 = ReadFloat(reader);
        var y2 = ReadFloat(reader);
        var z2 = ReadFloat(reader);
        var v2 = new Point3D(x2, y2, z2);

        var x3 = ReadFloat(reader);
        var y3 = ReadFloat(reader);
        var z3 = ReadFloat(reader);
        var v3 = new Point3D(x3, y3, z3);

        var attrib = Convert.ToString(ReadUInt16(reader), 2).PadLeft(16, '0').ToCharArray();
        var hasColor = attrib[0].Equals('1');

        if (hasColor) {
            var blue = attrib[15].Equals('1') ? 1 : 0;
            blue = attrib[14].Equals('1') ? blue + 2 : blue;
            blue = attrib[13].Equals('1') ? blue + 4 : blue;
            blue = attrib[12].Equals('1') ? blue + 8 : blue;
            blue = attrib[11].Equals('1') ? blue + 16 : blue;
            var b = blue * 8;

            var green = attrib[10].Equals('1') ? 1 : 0;
            green = attrib[9].Equals('1') ? green + 2 : green;
            green = attrib[8].Equals('1') ? green + 4 : green;
            green = attrib[7].Equals('1') ? green + 8 : green;
            green = attrib[6].Equals('1') ? green + 16 : green;
            var g = green * 8;

            var red = attrib[5].Equals('1') ? 1 : 0;
            red = attrib[4].Equals('1') ? red + 2 : red;
            red = attrib[3].Equals('1') ? red + 4 : red;
            red = attrib[2].Equals('1') ? red + 8 : red;
            red = attrib[1].Equals('1') ? red + 16 : red;
            var r = red * 8;
            var currentColor = new Color(r / 255f, g / 255f, b / 255f);
            if (!Equals(lastColor, currentColor)) {
                lastColor = currentColor;
                index++;
            }

            if (Materials.Count < index + 1)
                Materials.Add(new PhongMaterialCore { DiffuseColor = currentColor.ToColor4() });
        } else {
            if (Materials.Count < index + 1) Materials.Add(DefaultMaterial);
        }

        if (Meshes.Count < index + 1) Meshes.Add(new MeshBuilder(true));

        Meshes[index].AddTriangle(v1, v2, v3);

        // todo: add normal
    }

    /// <summary>
    ///     Reads the model in ASCII format from the specified stream.
    /// </summary>
    /// <param name="stream">
    ///     The stream.
    /// </param>
    /// <returns>
    ///     True if the model was loaded successfully.
    /// </returns>
    private bool TryReadAscii(Stream stream) {
        var reader = new StreamReader(stream);
        Meshes.Add(new MeshBuilder(true));
        Materials.Add(DefaultMaterial);

        while (!reader.EndOfStream) {
            var line = reader.ReadLine();
            if (line == null) continue;

            line = line.Trim();
            if (line.Length == 0 || line.StartsWith("\0") || line.StartsWith("#") || line.StartsWith("!")
                || line.StartsWith("$"))
                continue;

            string id, values;
            ParseLine(line, out id, out values);
            switch (id) {
                case "solid":
                    Header = values.Trim();
                    break;
                case "facet":
                    ReadFacet(reader, values);
                    break;
                case "endsolid":
                    break;
            }
        }

        return true;
    }

    /// <summary>
    ///     Reads the model from the specified binary stream.
    /// </summary>
    /// <param name="stream">
    ///     The stream.
    /// </param>
    /// <returns>
    ///     True if the file was read successfully.
    /// </returns>
    private bool TryReadBinary(Stream stream) {
        var length = stream.Length;
        if (length < 84) throw new FileFormatException("Incomplete file");

        var reader = new BinaryReader(stream);
        Header = Encoding.ASCII.GetString(reader.ReadBytes(80)).Trim();
        var numberTriangles = ReadUInt32(reader);

        if (length - 84 != numberTriangles * 50) return false;

        index = 0;
        Meshes.Add(new MeshBuilder(true));
        Materials.Add(DefaultMaterial);

        for (var i = 0; i < numberTriangles; i++) ReadTriangle(reader);

        return true;
    }
}
