/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
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
    public RasterizerStateProxy? RasterState {
        get;
        private set {
            if(field != value)
                field?.Dispose();
            field = value;

        }
    }

    public RasterizerStateProxy? InvertCullModeState {
        get;
        set {
            if(field != value)
                field?.Dispose();
            field = value;
        }
    }

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
        set {
            if (SetAffectsRender(ref rasterDescription, value) && IsAttached) 
                CreateRasterState(value, false);
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="description"></param>
    /// <param name="force"></param>
    /// <returns></returns>
    protected virtual bool CreateRasterState(RasterizerStateDescription description, bool force) {
        if (EffectTechnique is not { EffectsManager: { } effectsManager })
            return false;

        var newRasterState = effectsManager.StateManager.Register(description);
        var invCull = description;
        if (description.CullMode != CullMode.None)
            invCull.CullMode = description.CullMode == CullMode.Back ? CullMode.Front : CullMode.Back;
        var newInvertCullModeState = effectsManager.StateManager.Register(invCull);

        RasterState = newRasterState;
        InvertCullModeState = newInvertCullModeState;
        return true;
    }

    /// <summary>
    /// </summary>
    /// <param name="technique"></param>
    /// <returns></returns>
    protected override bool OnAttach(IRenderTechnique technique) {
        CreateRasterState(rasterDescription, true);
        return true;
    }

    protected override void OnDetach() {
        RasterState = null;
        InvertCullModeState = null;
    }

    /// <summary>
    ///     Called when [geometry buffer changed].
    /// </summary>
    /// <param name="buffer">The buffer.</param>
    protected virtual void OnGeometryBufferChanged(IAttachableBufferModel? buffer) { }

    /// <summary>
    ///     Set all necessary states and buffers
    /// </summary>
    /// <param name="context"></param>
    /// <param name="isInvertCullMode"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void OnBindRasterState(DeviceContextProxy context, bool isInvertCullMode)
        => context.SetRasterState(isInvertCullMode ? InvertCullModeState : RasterState);

    /// <summary>
    ///     Attach vertex buffer routine
    /// </summary>
    /// <param name="context"></param>
    /// <param name="vertStartSlot"></param>
    protected virtual bool OnAttachBuffers(DeviceContextProxy context, ref int vertStartSlot) {
        if (GeometryBuffer is not { } geometryBuffer || EffectTechnique is not { } technique)
            return false;

        var geoAttached = geometryBuffer.AttachBuffers(context, ref vertStartSlot, technique.EffectsManager);
        if (geoAttached) {
            InstanceBuffer.AttachBuffer(context, ref vertStartSlot);
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Called when [update can render flag].
    /// </summary>
    /// <returns></returns>
    protected override bool OnUpdateCanRenderFlag() 
        => base.OnUpdateCanRenderFlag() && GeometryBuffer != null;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawIndexed(
        DeviceContextProxy context,
        IElementsBufferProxy indexBuffer,
        IElementsBufferModel instanceModel
    ) {
        if (!instanceModel.HasElements || instanceModel.Buffer is not { } buffer)
            context.DrawIndexed(indexBuffer.ElementCount, 0, 0);
        else
            context.DrawIndexedInstanced(indexBuffer.ElementCount, buffer.ElementCount, 0, 0, 0);
    }

    /// <summary>
    ///     Binds and draws a default mesh through the Direct3D 12 command context.
    /// </summary>
    /// <param name="context">The Direct3D 12 command context.</param>
    /// <param name="buffers">The default mesh buffers.</param>
    /// <param name="instanceCount">The number of mesh instances.</param>
    /// <param name="vertexBufferStartSlot">The first vertex input slot.</param>
    /// <returns>The first free vertex input slot after the mesh streams.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint DrawIndexed(
        SilkD3D12CommandContext context,
        SilkD3D12DefaultMeshBuffers buffers,
        uint instanceCount = 1,
        uint vertexBufferStartSlot = 0
    ) {
        context.AssertArgumentNotNull();
        buffers.AssertArgumentNotNull();
        var nextVertexSlot = buffers.Bind(context, vertexBufferStartSlot);
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
    /// <returns>The first free vertex input slot after the mesh and instance streams.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint DrawIndexed<T>(
        SilkD3D12CommandContext context,
        SilkD3D12DefaultMeshBuffers buffers,
        SilkD3D12ElementsBuffer<T> instances,
        uint vertexBufferStartSlot = 0
    ) where T : unmanaged {
        context.AssertArgumentNotNull();
        buffers.AssertArgumentNotNull();
        instances.AssertArgumentNotNull();
        var instanceSlot = buffers.Bind(context, vertexBufferStartSlot);
        instances.Bind(context, instanceSlot);
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
    /// <returns>The first free vertex input slot.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Draw(
        SilkD3D12CommandContext context,
        SilkD3D12PointLineBuffers buffers,
        uint instanceCount = 1,
        uint vertexBufferSlot = 0
    ) {
        context.AssertArgumentNotNull();
        buffers.AssertArgumentNotNull();
        buffers.Bind(context, vertexBufferSlot);
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
    /// <returns>The first free vertex input slot.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static uint Draw<T>(
        SilkD3D12CommandContext context,
        SilkD3D12PointLineBuffers buffers,
        SilkD3D12ElementsBuffer<T> instances,
        uint vertexBufferSlot = 0
    ) where T : unmanaged {
        context.AssertArgumentNotNull();
        buffers.AssertArgumentNotNull();
        instances.AssertArgumentNotNull();
        buffers.Bind(context, vertexBufferSlot);
        instances.Bind(context, vertexBufferSlot + 1);
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
        context.AssertArgumentNotNull();
        resources.AssertArgumentNotNull();
        pass.AssertArgumentNotNull();
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
                    DrawIndexed(context, meshBuffers);
                else
                    DrawIndexed(context, meshBuffers, instanceBuffer);
                return true;
            case DefaultLineGeometryBufferModel line:
                var lineBuffers = resources.GetOrCreate(line);
                if (instanceBuffer is null)
                    Draw(context, lineBuffers);
                else
                    Draw(context, lineBuffers, instanceBuffer);
                return true;
            case DefaultPointGeometryBufferModel point:
                var pointBuffers = resources.GetOrCreate(point);
                if (instanceBuffer is null)
                    Draw(context, pointBuffers);
                else
                    Draw(context, pointBuffers, instanceBuffer);
                return true;
            case DefaultBillboardBufferModel billboard:
                var billboardBuffers = resources.GetOrCreate(billboard);
                if (instanceBuffer is null)
                    Draw(context, billboardBuffers);
                else
                    Draw(context, billboardBuffers, instanceBuffer);
                return true;
            default:
                return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawPoints(
        DeviceContextProxy context,
        IElementsBufferProxy vertexBuffer,
        IElementsBufferModel instanceModel
    ) {
        if (!instanceModel.HasElements || instanceModel.Buffer is not { } buffer)
            context.Draw(vertexBuffer.ElementCount, 0);
        else
            context.DrawInstanced(vertexBuffer.ElementCount, buffer.ElementCount, 0, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool PreRender(RenderContext context, DeviceContextProxy deviceContext) {
        var vertStartSlot = 0;
        if (!OnAttachBuffers(deviceContext, ref vertStartSlot))
            return false;
        
        OnBindRasterState(deviceContext, context.IsInvertCullMode);
        return CanRenderFlag;
    }

    /// <summary>
    ///     Trigger OnRender function delegate if CanRender()==true
    /// </summary>
    /// <param name="context"></param>
    /// <param name="deviceContext"></param>
    public sealed override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        if (PreRender(context, deviceContext)) 
            OnRender(context, deviceContext);
    }


    public sealed override void RenderShadow(RenderContext context, DeviceContextProxy deviceContext) {
        if (PreRender(context, deviceContext)) 
            OnRenderShadow(context, deviceContext);
    }

    public sealed override void RenderCustom(RenderContext context, DeviceContextProxy deviceContext) {
        if (PreRender(context, deviceContext)) 
            OnRenderCustom(context, deviceContext);
    }

    public sealed override void RenderDepth(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderPass? customPass
    ) {
        if (PreRender(context, deviceContext)) 
            OnRenderDepth(context, deviceContext, customPass);
    }

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    protected abstract void OnRender(RenderContext context, DeviceContextProxy deviceContext);

    /// <summary>
    ///     Render function for custom shader pass. Used to do special effects
    /// </summary>
    protected abstract void OnRenderCustom(RenderContext context, DeviceContextProxy deviceContext);

    /// <summary>
    ///     Called when [render shadow].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext"></param>
    protected abstract void OnRenderShadow(RenderContext context, DeviceContextProxy deviceContext);

    /// <summary>
    ///     Called when [render depth].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    /// <param name="customPass">Custom depth pass</param>
    protected abstract void OnRenderDepth(
        RenderContext context,
        DeviceContextProxy deviceContext,
        ShaderPass? customPass
    );

    protected void OnElementChanged(object? sender, EventArgs e) {
        UpdateCanRenderFlag();
        RaiseInvalidateRender();
    }

    protected void OnInvalidateRendererEvent(object? sender, EventArgs e) => RaiseInvalidateRender();
}
