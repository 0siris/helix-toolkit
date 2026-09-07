/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.Abstract;

/// <summary>
/// </summary>
public abstract class GeometryRenderCore : RenderCore, IGeometryRenderCore {
    private RasterizerStateDescription rasterDescription = new() {
        FillMode = FillMode.Solid,
        CullMode = CullMode.None
    };

    /// <summary>
    ///     Initializes a new instance of the <see cref="GeometryRenderCore" /> class.
    /// </summary>
    public GeometryRenderCore() : base(RenderType.Opaque) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GeometryRenderCore" /> class.
    /// </summary>
    /// <param name="renderType">Type of the render.</param>
    public GeometryRenderCore(RenderType renderType) : base(renderType) { }

    /// <summary>
    /// </summary>
    public IElementsBufferModel InstanceBuffer {
        get;
        set {
            var old = field;
            if (SetAffectsCanRenderFlag(ref field, value)) {
                old?.ElementChanged -= OnElementChanged;
                if (field != null)
                    field.ElementChanged += OnElementChanged;
                else
                    field = MatrixInstanceBufferModel.Empty;
            }
        }
    } = MatrixInstanceBufferModel.Empty;

    /// <summary>
    /// </summary>
    public IAttachableBufferModel? GeometryBuffer {
        get;
        set {
            if (SetAffectsCanRenderFlag(ref field, value))
                OnGeometryBufferChanged(value);
        }
    }

    /// <summary>
    /// </summary>
    public RasterizerStateDescription RasterDescription {
        get => rasterDescription;
        set => SetAffectsRender(ref rasterDescription, value);
    }

    /// <summary>
    ///     Called when [geometry buffer changed].
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    protected virtual void OnGeometryBufferChanged(IAttachableBufferModel? buffer) { }

    /// <summary>
    ///     Called when [update can render flag].
    /// </summary>
    /// <returns></returns>
    protected override bool OnUpdateCanRenderFlag()
        => base.OnUpdateCanRenderFlag() && GeometryBuffer != null;

    /// <summary>
    ///     Binds and draws a default mesh through the Direct3D 12 command context.
    /// </summary>
    /// <param name="context">The Direct3D 12 command context.</param>
    /// <param name="buffers">The default mesh buffers.</param>
    /// <param name="instanceCount">The number of mesh instances.</param>
    /// <param name="vertexBufferStartSlot">The first vertex input slot.</param>
    /// <param name="topology">The pass topology overriding the geometry topology.</param>
    /// <returns>The first free vertex input slot after the mesh streams.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint DrawIndexed(
        SilkD3D12CommandContext context,
        SilkD3D12DefaultMeshBuffers buffers,
        uint instanceCount = 1,
        uint vertexBufferStartSlot = 0,
        PrimitiveTopology topology = PrimitiveTopology.Undefined
    ) {
        context.GuardNotNull();
        buffers.GuardNotNull();
        var nextVertexSlot = buffers.Bind(context, vertexBufferStartSlot);
        if (topology != PrimitiveTopology.Undefined) context.SetPrimitiveTopology(topology);
        context.DrawIndexedInstanced(buffers.IndexCount, instanceCount);
        return nextVertexSlot;
    }

    /// <summary>
    ///     Binds and draws an instanced default mesh through the Direct3D 12 command context.
    /// </summary>
    /// <typeparam name="T">The unmanaged instance element type.</typeparam>
    /// <param name="context">The Direct3D 12 command context.</param>
    /// <param name="buffers">The default mesh buffers.</param>
    /// <param name="instances">The instance vertex stream.</param>
    /// <param name="vertexBufferStartSlot">The first vertex input slot.</param>
    /// <param name="topology">The pass topology overriding the geometry topology.</param>
    /// <returns>The first free vertex input slot after the mesh and instance streams.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint DrawIndexed<T>(
        SilkD3D12CommandContext context,
        SilkD3D12DefaultMeshBuffers buffers,
        SilkD3D12ElementsBuffer<T> instances,
        uint vertexBufferStartSlot = 0,
        PrimitiveTopology topology = PrimitiveTopology.Undefined
    ) where T : unmanaged {
        context.GuardNotNull();
        buffers.GuardNotNull();
        instances.GuardNotNull();
        var instanceSlot = buffers.Bind(context, vertexBufferStartSlot);
        instances.Bind(context, instanceSlot);
        if (topology != PrimitiveTopology.Undefined) context.SetPrimitiveTopology(topology);
        context.DrawIndexedInstanced(buffers.IndexCount, instances.ElementCount);
        return instanceSlot + 1;
    }

    /// <summary>
    ///     Binds and draws default line or point geometry through the Direct3D 12 command context.
    /// </summary>
    /// <param name="context">The Direct3D 12 command context.</param>
    /// <param name="buffers">The default line or point buffers.</param>
    /// <param name="instanceCount">The number of geometry instances.</param>
    /// <param name="vertexBufferSlot">The vertex input slot.</param>
    /// <param name="topology">The pass topology overriding the geometry topology.</param>
    /// <returns>The first free vertex input slot.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Draw(
        SilkD3D12CommandContext context,
        SilkD3D12PointLineBuffers buffers,
        uint instanceCount = 1,
        uint vertexBufferSlot = 0,
        PrimitiveTopology topology = PrimitiveTopology.Undefined
    ) {
        context.GuardNotNull();
        buffers.GuardNotNull();
        buffers.Bind(context, vertexBufferSlot);
        if (topology != PrimitiveTopology.Undefined) context.SetPrimitiveTopology(topology);
        if (buffers.VertexCount == 0 && buffers.IndexCount == 0) return vertexBufferSlot + 1;
        if (buffers.IndexCount > 0)
            context.DrawIndexedInstanced(buffers.IndexCount, instanceCount);
        else
            context.DrawInstanced(buffers.VertexCount, instanceCount);
        return vertexBufferSlot + 1;
    }

    /// <summary>
    ///     Binds and draws instanced default line or point geometry through the Direct3D 12 command context.
    /// </summary>
    /// <typeparam name="T">The unmanaged instance element type.</typeparam>
    /// <param name="context">The Direct3D 12 command context.</param>
    /// <param name="buffers">The default line or point buffers.</param>
    /// <param name="instances">The instance vertex stream.</param>
    /// <param name="vertexBufferSlot">The geometry vertex input slot.</param>
    /// <param name="topology">The pass topology overriding the geometry topology.</param>
    /// <returns>The first free vertex input slot.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Draw<T>(
        SilkD3D12CommandContext context,
        SilkD3D12PointLineBuffers buffers,
        SilkD3D12ElementsBuffer<T> instances,
        uint vertexBufferSlot = 0,
        PrimitiveTopology topology = PrimitiveTopology.Undefined
    ) where T : unmanaged {
        context.GuardNotNull();
        buffers.GuardNotNull();
        instances.GuardNotNull();
        buffers.Bind(context, vertexBufferSlot);
        instances.Bind(context, vertexBufferSlot + 1);
        if (topology != PrimitiveTopology.Undefined) context.SetPrimitiveTopology(topology);
        if (buffers.VertexCount == 0 && buffers.IndexCount == 0) return vertexBufferSlot + 2;
        if (buffers.IndexCount > 0)
            context.DrawIndexedInstanced(buffers.IndexCount, instances.ElementCount);
        else
            context.DrawInstanced(buffers.VertexCount, instances.ElementCount);
        return vertexBufferSlot + 2;
    }

    /// <summary>
    ///     Records one supported existing geometry core through the productive Direct3D 12 resource path.
    /// </summary>
    /// <param name="context">The open Direct3D 12 command context.</param>
    /// <param name="resources">The render-host resource manager.</param>
    /// <param name="pass">The Direct3D 12 shader pass.</param>
    /// <param name="resourceTable">The optional CBV/SRV/UAV table start.</param>
    /// <param name="samplerTable">The optional sampler table start.</param>
    /// <returns>Whether supported geometry was recorded.</returns>
    internal bool TryRenderD3D12(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        ShaderPass pass,
        SilkD3D12Descriptor? resourceTable = null,
        SilkD3D12Descriptor? samplerTable = null
    ) {
        context.GuardNotNull();
        resources.GuardNotNull();
        pass.GuardNotNull();
        if (!CanRenderFlag || pass.IsNull) return false;
        if (!pass.IsD3D12)
            throw new ArgumentException("The pass must own a Direct3D 12 pipeline.", nameof(pass));
        if ((resourceTable is null) != (samplerTable is null))
            throw new ArgumentException("Graphics resource and sampler tables must be supplied together.");

        var instanceBuffer = InstanceBuffer is IElementsBufferModel<Matrix> matrixInstances
            ? resources.GetOrCreate(matrixInstances)
            : null;
        pass.BindShader(context);
        if (resourceTable is not null && samplerTable is not null)
            context.SetGraphicsDescriptorTables(resourceTable, samplerTable);
        switch (GeometryBuffer) {
            case DefaultMeshGeometryBufferModel mesh:
                var meshBuffers = resources.GetOrCreate(mesh);
                if (instanceBuffer is null)
                    DrawIndexed(context, meshBuffers, topology: pass.Topology);
                else
                    DrawIndexed(context, meshBuffers, instanceBuffer, topology: pass.Topology);
                return true;
            case DefaultLineGeometryBufferModel line:
                var lineBuffers = resources.GetOrCreate(line);
                if (instanceBuffer is null)
                    Draw(context, lineBuffers, topology: pass.Topology);
                else
                    Draw(context, lineBuffers, instanceBuffer, topology: pass.Topology);
                return true;
            case DefaultPointGeometryBufferModel point:
                var pointBuffers = resources.GetOrCreate(point);
                if (instanceBuffer is null)
                    Draw(context, pointBuffers, topology: pass.Topology);
                else
                    Draw(context, pointBuffers, instanceBuffer, topology: pass.Topology);
                return true;
            case DefaultBillboardBufferModel billboard:
                var billboardBuffers = resources.GetOrCreate(billboard);
                if (instanceBuffer is null)
                    Draw(context, billboardBuffers, topology: pass.Topology);
                else
                    Draw(context, billboardBuffers, instanceBuffer, topology: pass.Topology);
                return true;
            default:
                return false;
        }
    }

    protected void OnElementChanged(object? sender, EventArgs e) {
        UpdateCanRenderFlag();
        RaiseInvalidateRender();
    }

    protected void OnInvalidateRendererEvent(object? sender, EventArgs e) => RaiseInvalidateRender();
}
