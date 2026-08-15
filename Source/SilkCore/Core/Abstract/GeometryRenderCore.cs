/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core;

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
