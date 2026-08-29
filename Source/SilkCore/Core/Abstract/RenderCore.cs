/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.CompilerServices;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core.Abstract;

/// <summary>
/// </summary>
public abstract class RenderCore : DisposeObject, IGuid, IThrowingShadow {
    private readonly List<CoreComponent> components = [];

    public event EventHandler<EventArgs>? InvalidateRender;

    /// <summary>
    ///     <see cref="IGuid.Guid" />
    /// </summary>
    public Guid Guid { get; } = Guid.NewGuid();

    /// <summary>
    ///     Gets or sets the type of the render.
    /// </summary>
    /// <value>
    ///     The type of the render.
    /// </value>
    public RenderType RenderType {
        get;
        set => SetAffectsRender(ref field, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance can be rendered. Update this flag using
    ///     <see cref="UpdateCanRenderFlag" />
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance can render; otherwise, <c>false</c>.
    /// </value>
    internal bool CanRenderFlag;

    /// <summary>
    ///     <see cref="IThrowingShadow.IsThrowingShadow" />
    /// </summary>
    public bool IsThrowingShadow {
        get;
        set => SetAffectsRender(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the default state binding.
    /// </summary>
    /// <value>
    ///     The default state binding.
    /// </value>
    public StateType DefaultStateBinding { get; set; } = StateType.BlendState | StateType.DepthStencilState;

    /// <summary>
    ///     Gets or sets the default state binding.
    /// </summary>
    /// <value>
    ///     The default state binding.
    /// </value>
    public StateType ShadowStateBinding { get; set; } = StateType.BlendState | StateType.DepthStencilState;

    /// <summary>
    ///     Model matrix
    /// </summary>
    public Matrix ModelMatrix = Matrix.Identity;

    /// <summary>
    ///     Is render core has been attached
    /// </summary>
    public bool IsAttached { get; private set; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RenderCore" /> class.
    /// </summary>
    /// <param name="renderType">Type of the render.</param>
    public RenderCore(RenderType renderType)
        => RenderType = renderType;

    protected T AddComponent<T>(T component) where T : CoreComponent {
        components.Add(component);
        component.InvalidateRender += (_, _) => RaiseInvalidateRender();
        return component;
    }

    /// <summary>
    ///     Attaches this render core to the renderer.
    /// </summary>
    internal void Attach() {
        if (IsAttached) return;
        IsAttached = OnAttachD3D12();
        UpdateCanRenderFlag();
    }

    /// <summary>
    ///     Creates package-specific Direct3D 12 state during attachment.
    /// </summary>
    /// <returns>Whether attachment succeeded.</returns>
    protected virtual bool OnAttachD3D12() => true;

    /// <summary>
    ///     Detach render core. Release all resources
    /// </summary>
    public void Detach() {
        if (!IsAttached) return;
        OnDetachD3D12();
        IsAttached = false;
        UpdateCanRenderFlag();
    }

    /// <summary>
    ///     Releases package-specific Direct3D 12 state during detachment.
    /// </summary>
    protected virtual void OnDetachD3D12() { }

    /// <summary>
    ///     Updates the can render flag.
    /// </summary>
    public void UpdateCanRenderFlag() {
        var flag = OnUpdateCanRenderFlag();
        if (CanRenderFlag != flag) {
            CanRenderFlag = flag;
            RaiseInvalidateRender();
        }
    }

    /// <summary>
    ///     Called when [update can render flag].
    /// </summary>
    /// <returns></returns>
    protected virtual bool OnUpdateCanRenderFlag() => IsAttached;

    /// <summary>
    ///     Resets the invalidate handler.
    /// </summary>
    public void ResetInvalidateHandler()
        => InvalidateRender = null;

    /// <summary>
    ///     Invalidates the renderer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void RaiseInvalidateRender()
        => InvalidateRender?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="backingField"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool SetAffectsRender<T>(ref T backingField, T value) {
        if (EqualityComparer<T>.Default.Equals(backingField, value))
            return false;

        backingField = value;
        RaiseInvalidateRender();
        return true;
    }

    /// <summary>
    ///     Sets the affects can render flag. This will also invalidate renderer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="backingField">The backing field.</param>
    /// <param name="value">The value.</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected bool SetAffectsCanRenderFlag<T>(ref T backingField, T value) {
        if (EqualityComparer<T>.Default.Equals(backingField, value))
            return false;

        backingField = value;
        UpdateCanRenderFlag();
        RaiseInvalidateRender();
        return true;
    }

    protected override void OnDispose(bool disposeManagedResources) {
        if (disposeManagedResources) {
            Detach();
            foreach (var comp in components)
                comp.Dispose();
        }

        base.OnDispose(disposeManagedResources);
    }


}
