/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
Reference: https://graphicsrunner.blogspot.com/search/label/Volume%20Rendering
*/

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class VolumeRenderCore : RenderCore {
    private static readonly MeshGeometry3D BoxMesh;
    private int backTexSlot;

    private VolumeCubeBufferModel? buffer;

    private ShaderPass? cubeBackPass;
    private ShaderPass? meshFrontPass;
    private ModelMatrices modelMatrices;
    private ShaderPass? volumePass;

    /// <summary>
    ///     Gets or sets the material supplied by the DX12 scene-node boundary.
    /// </summary>
    internal MaterialCore? D3D12Material { get; set; }

    /// <summary>
    ///     Gets the existing productive volume pass selected by the current material.
    /// </summary>
    internal string D3D12MaterialPassName => D3D12Material switch {
        VolumeTextureDiffuseMaterialCore => DefaultPassNames.Diffuse,
        VolumeTextureRawDataMaterialCore or VolumeTextureDds3DMaterialCore => DefaultPassNames.Default,
        null => throw new InvalidOperationException("A DX12 volume material is required for pass selection."),
        _ => throw new NotSupportedException($"DX12 volume material '{D3D12Material.GetType().Name}' is not supported.")
    };

    static VolumeRenderCore() {
        BoxMesh = new MeshGeometry3D {
            Positions = [
                new Vector3(-0.5f, -0.5f, -0.5f),
                new Vector3(0.5f, -0.5f, -0.5f),
                new Vector3(-0.5f, 0.5f, -0.5f),
                new Vector3(0.5f, 0.5f, -0.5f),
                new Vector3(-0.5f, -0.5f, 0.5f),
                new Vector3(0.5f, -0.5f, 0.5f),
                new Vector3(-0.5f, 0.5f, 0.5f),
                new Vector3(0.5f, 0.5f, 0.5f)
            ],
            Indices = [
                0, 2, 3,
                3, 1, 0,
                4, 5, 7,
                7, 6, 4,
                0, 1, 5,
                5, 4, 0,
                1, 3, 7,
                7, 5, 1,
                3, 2, 6,
                6, 7, 3,
                2, 0, 4,
                4, 6, 2
            ]
        };
    }

    public VolumeRenderCore()
        : base(RenderType.Particle) {
    }

    /// <summary>
    ///     Updates the existing volume material, camera, and model resources for a DX12 draw.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="resources">The shared render-host resources.</param>
    /// <param name="bindings">The per-core volume bindings.</param>
    /// <param name="volumeResources">The shared volume cube and offscreen resources.</param>
    /// <param name="transforms">The current camera transforms.</param>
    /// <returns>Whether the core has a complete supported volume material.</returns>
    internal bool PrepareD3D12(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        SilkD3D12VolumeBindings bindings,
        SilkD3D12VolumeResources volumeResources,
        in GlobalTransformStruct transforms
    ) => bindings.Update(context,
        resources,
        in transforms,
        in ModelMatrix,
        D3D12Material,
        volumeResources.BackPositions);

    /// <summary>
    ///     Records one indexed volume-cube pass using the prepared shared-root bindings.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="pass">The selected existing volume pass.</param>
    /// <param name="bindings">The prepared per-core bindings.</param>
    /// <param name="volumeResources">The shared volume cube.</param>
    internal static void DrawD3D12(
        SilkD3D12CommandContext context,
        ShaderPass pass,
        SilkD3D12VolumeBindings bindings,
        SilkD3D12VolumeResources volumeResources
    ) {
        pass.BindShader(context);
        bindings.Bind(context);
        volumeResources.DrawCube(context, pass.Topology);
    }

    /// <inheritdoc />
    protected override bool OnUpdateCanRenderFlag()
        => base.OnUpdateCanRenderFlag() && D3D12Material is IVolumeTextureMaterial;

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct ModelMatrices {
        public Matrix ModelMatrix;
        public Matrix ModelMatrixInv;

        public void Update(ref Matrix modelMatrix) {
            if (ModelMatrix != modelMatrix) {
                ModelMatrix = modelMatrix;
                ModelMatrixInv = modelMatrix.Inverted();
            }
        }
    }

    /// <summary>
    /// </summary>
    private sealed class VolumeCubeBufferModel : MeshGeometryBufferModel<Vector3> {
        public VolumeCubeBufferModel() : base(SilkMath.Vector3SizeInBytes) {
            Topology = PrimitiveTopology.TriangleList;
        }
    }
}
