/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using MeshGeometry3D = HelixToolkit.SharpDX.Core.MeshGeometry3D;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
///     Export the 3D visual tree to a Wavefront OBJ file
/// </summary>
/// <remarks>
///     http://en.wikipedia.org/wiki/Obj
///     http://www.martinreddy.net/gfx/3d/OBJ.spec
///     http://www.eg-models.de/formats/Format_Obj.html
/// </remarks>
public class ObjExporter : Exporter {
    /// <summary>
    ///     The directory.
    /// </summary>
    private readonly string directory;

    /// <summary>
    ///     The exported materials.
    /// </summary>
    private readonly Dictionary<MaterialCore, string> exportedMaterials = [];

    /// <summary>
    ///     The mwriter.
    /// </summary>
    private readonly StreamWriter mwriter;

    /// <summary>
    ///     The writer.
    /// </summary>
    private readonly StreamWriter writer;

    /// <summary>
    ///     Group index counter.
    /// </summary>
    private int groupNo = 1;

    /// <summary>
    ///     Material index counter.
    /// </summary>
    private int matNo = 1;

    /// <summary>
    ///     Normal index counter.
    /// </summary>
    private int normalIndex = 1;

    /// <summary>
    ///     Object index counter.
    /// </summary>
    private int objectNo = 1;

    /// <summary>
    ///     Texture index counter.
    /// </summary>
    private int textureIndex = 1;

    /// <summary>
    ///     Vertex index counter.
    /// </summary>
    private int vertexIndex = 1;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ObjExporter" /> class.
    /// </summary>
    /// <param name="outputFileName">
    ///     Name of the output file.
    /// </param>
    public ObjExporter(string outputFileName)
        : this(outputFileName, null) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ObjExporter" /> class.
    /// </summary>
    /// <param name="outputFileName">
    ///     Name of the output file.
    /// </param>
    /// <param name="comment">
    ///     The comment.
    /// </param>
    public ObjExporter(string outputFileName, string? comment) {
        SwitchYz = true;
        ExportNormals = false;

        var fullPath = Path.GetFullPath(outputFileName);
        var mtlPath = Path.ChangeExtension(outputFileName, ".mtl");
        var mtlFilename = Path.GetFileName(mtlPath);
        directory = Path.GetDirectoryName(fullPath) ?? string.Empty;

        writer = new StreamWriter(outputFileName);
        mwriter = new StreamWriter(mtlPath);

        if (!string.IsNullOrEmpty(comment)) writer.WriteLine("# {0}", comment);

        writer.WriteLine("mtllib ./" + mtlFilename);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether to export normals.
    /// </summary>
    public bool ExportNormals { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to use "d" for transparency (default is "Tr").
    /// </summary>
    public bool UseDissolveForTransparency { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether to switch Y and Z coordinates.
    /// </summary>
    public bool SwitchYz { get; set; }

    /// <summary>
    ///     Closes this exporter.
    /// </summary>
    public override void Close() {
        writer.Close();
        mwriter.Close();
        base.Close();
    }

    /// <summary>
    ///     The export model.
    /// </summary>
    /// <param name="model">
    ///     The model.
    /// </param>
    /// <param name="transform">
    ///     The transform.
    /// </param>
    protected override void ExportModel(MeshNode model, Transform3D transform) {
        if (!model.GeometryValid || model.Geometry is not MeshGeometry3D mesh || model.Material is not { } material)
            return;

            writer.WriteLine("o object{0}", objectNo++);
            writer.WriteLine("g group{0}", groupNo++);

            if (exportedMaterials.TryGetValue(model.Material, out var matName)) {
                writer.WriteLine("usemtl {0}", matName);
            } else {
                matName = string.Format(CultureInfo.InvariantCulture, "mat{0}", matNo++);
                writer.WriteLine("usemtl {0}", matName);
                ExportMaterial(matName, material);
                exportedMaterials.Add(material, matName);
            }

            if (model.HasInstances && model.Instances is { } instances) {
                var m = transform.ToMatrix();
                for (var i = 0; i < instances.Count; ++i) ExportMesh(mesh, instances[i] * m);
            } else {
                ExportMesh(mesh, transform.ToMatrix());
            }
    }

    /// <summary>
    ///     The export mesh.
    /// </summary>
    /// <param name="m">
    ///     The m.
    /// </param>
    /// <param name="t">
    ///     The t.
    /// </param>
    public void ExportMesh(MeshGeometry3D? m, Matrix t) {
        ArgumentNullException.ThrowIfNull(m);

        // mapping from local indices (0-based) to the obj file indices (1-based)
        var vertexIndexMap = new Dictionary<int, int>();
        var textureIndexMap = new Dictionary<int, int>();
        var normalIndexMap = new Dictionary<int, int>();

        var index = 0;
        if (m.Positions != null) {
            foreach (var v in m.Positions) {
                vertexIndexMap.Add(index++, vertexIndex++);
                var p = SilkMath.TransformCoordinate(v, t);
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                                               "v {0} {1} {2}",
                                               NormalizeZero(p.X),
                                               NormalizeZero(SwitchYz ? p.Z : p.Y),
                                               NormalizeZero(SwitchYz ? -p.Y : p.Z)));
            }

            writer.WriteLine("# {0} vertices", index);
        }

        if (m.TextureCoordinates != null) {
            index = 0;
            foreach (var vt in m.TextureCoordinates) {
                textureIndexMap.Add(index++, textureIndex++);
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "vt {0} {1}", vt.X, 1 - vt.Y));
            }

            writer.WriteLine("# {0} texture coordinates", index);
        }

        if (m.Normals != null && ExportNormals) {
            index = 0;
            foreach (var vn in m.Normals) {
                normalIndexMap.Add(index++, normalIndex++);
                writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "vn {0} {1} {2}", vn.X, vn.Y, vn.Z));
            }

            writer.WriteLine("# {0} normals", index);
        }

        Func<int, string> formatIndices = i0 => {
            var hasTextureIndex = textureIndexMap.ContainsKey(i0);
            var hasNormalIndex = normalIndexMap.ContainsKey(i0);
            if (hasTextureIndex && hasNormalIndex)
                return string.Format(CultureInfo.InvariantCulture,
                                     "{0}/{1}/{2}",
                                     vertexIndexMap[i0],
                                     textureIndexMap[i0],
                                     normalIndexMap[i0]);

            if (hasTextureIndex)
                return string.Format(CultureInfo.InvariantCulture, "{0}/{1}", vertexIndexMap[i0], textureIndexMap[i0]);

            if (hasNormalIndex)
                return string.Format(CultureInfo.InvariantCulture, "{0}//{1}", vertexIndexMap[i0], normalIndexMap[i0]);

            return vertexIndexMap[i0].ToString(CultureInfo.InvariantCulture);
        };

        if (m.Indices != null) {
            for (var i = 0; i < m.Indices.Count; i += 3) {
                var i0 = m.Indices[i];
                var i1 = m.Indices[i + 1];
                var i2 = m.Indices[i + 2];

                writer.WriteLine("f {0} {1} {2}", formatIndices(i0), formatIndices(i1), formatIndices(i2));
            }

            writer.WriteLine("# {0} faces", m.Indices.Count / 3);
        }

        writer.WriteLine();
    }

    /// <summary>
    ///     The export material.
    /// </summary>
    /// <param name="matName">
    ///     The mat name.
    /// </param>
    /// <param name="material">
    ///     The material.
    /// </param>
    private void ExportMaterial(string matName, MaterialCore material) {
        mwriter.WriteLine("newmtl {0}", matName);
        var pm = material as PhongMaterialCore;

        if (pm != null) {
            if (pm.DiffuseMap is not { } diffuseMap) {
                mwriter.WriteLine("Kd {0}", ToColorString(pm.DiffuseColor));

                if (UseDissolveForTransparency)
                    // Dissolve factor
                    mwriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "d {0:F4}", pm.DiffuseColor.W));
                else
                    // Transparency
                    mwriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "Tr {0:F4}", pm.DiffuseColor.W));
            } else {
                var textureFilename = matName + ".png";
                var texturePath = Path.Combine(directory, textureFilename);
                var texture = diffuseMap.TextureInfoLoader.Load(diffuseMap.Guid);
                if (texture.Texture.CanRead) {
                    // create .png bitmap file for the brush
                    RenderBrush(texturePath, texture.Texture);
                    mwriter.WriteLine("map_Ka {0}", textureFilename);
                    diffuseMap.TextureInfoLoader.Complete(diffuseMap.Guid, texture, true);
                } else {
                    diffuseMap.TextureInfoLoader.Complete(diffuseMap.Guid, texture, false);
                }
            }
        }

        // Illumination model 1
        // This is a diffuse illumination model using Lambertian shading. The
        // color includes an ambient constant term and a diffuse shading term for
        // each light source.  The formula is
        // color = KaIa + Kd { SUM j=1..ls, (N * Lj)Ij }
        var illum = 1; // Lambertian

        if (pm != null) {
            mwriter.WriteLine("Ks {0}",
                              ToColorString(pm.DiffuseMap == null
                                                ? pm.SpecularColor
                                                : new Color4(0.2f, 0.2f, 0.2f, 1.0f)));

            // Illumination model 2
            // This is a diffuse and specular illumination model using Lambertian
            // shading and Blinn's interpretation of Phong's specular illumination
            // model (BLIN77).  The color includes an ambient constant term, and a
            // diffuse and specular shading term for each light source.  The formula
            // is: color = KaIa + Kd { SUM j=1..ls, (N*Lj)Ij } + Ks { SUM j=1..ls, ((H*Hj)^Ns)Ij }
            illum = 2;

            // Specifies the specular exponent for the current material.  This defines the focus of the specular highlight.
            // "exponent" is the value for the specular exponent.  A high exponent results in a tight, concentrated highlight.  Ns values normally range from 0 to 1000.
            mwriter.WriteLine(string.Format(CultureInfo.InvariantCulture, "Ns {0:F4}", pm.SpecularShininess));
        }

        // roughness
        mwriter.WriteLine("Ns {0}", 2);

        // Optical density (index of refraction)
        mwriter.WriteLine("Ni {0}", 1);

        // Transmission filter
        mwriter.WriteLine("Tf {0} {1} {2}", 1, 1, 1);

        // Illumination model
        // Illumination    Properties that are turned on in the
        // model           Property Editor
        // 0		Color on and Ambient off
        // 1		Color on and Ambient on
        // 2		Highlight on
        // 3		Reflection on and Ray trace on
        // 4		Transparency: Glass on
        // Reflection: Ray trace on
        // 5		Reflection: Fresnel on and Ray trace on
        // 6		Transparency: Refraction on
        // Reflection: Fresnel off and Ray trace on
        // 7		Transparency: Refraction on
        // Reflection: Fresnel on and Ray trace on
        // 8		Reflection on and Ray trace off
        // 9		Transparency: Glass on
        // Reflection: Ray trace off
        // 10		Casts shadows onto invisible surfaces
        mwriter.WriteLine("illum {0}", illum);
    }

    /// <summary>
    ///     Converts a color to a string.
    /// </summary>
    /// <param name="color">
    ///     The color.
    /// </param>
    /// <returns>
    ///     The string.
    /// </returns>
    private string ToColorString(Color4 color) => string.Format(CultureInfo.InvariantCulture,
        "{0:F4} {1:F4} {2:F4}",
        color.X,
        color.Y,
        color.Z);

    private static float NormalizeZero(float value) => value == 0 ? 0 : value;
}
