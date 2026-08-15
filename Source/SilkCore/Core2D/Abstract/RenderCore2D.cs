/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Core2D.Abstract;
/// <summary>
/// </summary>
public abstract class RenderCore2D : DisposeObject {
    /// <summary>
    ///     Gets a value indicating whether this instance is empty.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is empty; otherwise, <c>false</c>.
    /// </value>
    public bool IsEmpty { get; } = false;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is rendering.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is rendering; otherwise, <c>false</c>.
    /// </value>
    public bool IsRendering { get; set; } = true;

    public IRenderHost? RenderHost { get; private set; }

    /// <summary>
    ///     Absolute layout rectangle cooridnate for renderable
    /// </summary>
    public RectangleF LayoutBound {
        get;
        set {
            if (SetAffectsRender(ref field, value))
                OnLayoutBoundChanged(value);
        }
    }

    /// <summary>
    ///     Gets or sets the layout clipping bound, includes border.
    /// </summary>
    /// <value>
    ///     The layout clipping bound.
    /// </value>
    public RectangleF LayoutClippingBound {
        get;
        set => SetAffectsRender(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the transform. <see cref="RenderCore2D.Transform" />
    /// </summary>
    /// <value>
    ///     The transform.
    /// </value>
    public Matrix3X2 Transform {
        get;
        set => SetAffectsRender(ref field, value);
    } = Matrix3X2.Identity;

    /// <summary>
    ///     Gets or sets the local transform. This only transform local position. Same as RenderTransform
    /// </summary>
    /// <value>
    ///     The local transform.
    /// </value>
    public Matrix3X2 LocalTransform {
        get;
        set => SetAffectsRender(ref field, value);
    } = Matrix3X2.Identity;

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is mouse over.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is mouse over; otherwise, <c>false</c>.
    /// </value>
    public bool IsMouseOver {
        get;
        set => SetAffectsRender(ref field, value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance is attached.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is attached; otherwise, <c>false</c>.
    /// </value>
    public bool IsAttached { get; private set; }

    /// <summary>
    ///     Occurs when [on invalidate renderer].
    /// </summary>
    public event EventHandler<EventArgs>? InvalidateRender;

    /// <summary>
    ///     Attaches the specified host.
    /// </summary>
    /// <param name="host">The host.</param>
    [MemberNotNull(nameof(RenderHost))]
    public void Attach(IRenderHost host) {
        if (IsAttached) {
            RenderHost.AssertNotNull("Host must be already present");
            return;
        }
        
        RenderHost = host.AssertNotNull();
        IsAttached = OnAttach(host);
    }

    /// <summary>
    ///     Called when [attach].
    /// </summary>
    /// <param name="host">The target.</param>
    /// <returns></returns>
    protected virtual bool OnAttach(IRenderHost host) => true;

    /// <summary>
    ///     Detaches this instance.
    /// </summary>
    public void Detach() {
        if (!IsAttached) 
            return;
        
        OnDetach();
        IsAttached = false;
    }

    /// <summary>
    ///     Called when [detach].
    /// </summary>
    protected virtual void OnDetach() { }

    /// <summary>
    /// </summary>
    /// <param name="layoutBound"></param>
    protected virtual void OnLayoutBoundChanged(RectangleF layoutBound) { }

    /// <summary>
    ///     Renders the specified context.
    /// </summary>
    /// <param name="context">The context.</param>
    public abstract void Render(RenderContext2D context);

    /// <summary>
    ///     Invalidates the renderer.
    /// </summary>
    protected void InvalidateRenderer() => InvalidateRender?.Invoke(this, EventArgs.Empty);

    
    /// <summary>
    ///     Sets the backing field value and invalidates the renderer if the value has changed.
    /// </summary>
    /// <typeparam name="T">The type of the backing field.</typeparam>
    /// <param name="backingField">The backing field to update.</param>
    /// <param name="value">The new value to set.</param>
    /// <returns>
    ///     <c>true</c> if the value was changed and renderer was invalidated; otherwise, <c>false</c>.
    /// </returns>
    protected bool SetAffectsRender<T>(ref T backingField, T value) {
        if (EqualityComparer<T>.Default.Equals(backingField, value)) 
            return false;

        backingField = value;
        InvalidateRenderer();
        return true;
    }

    /// <summary>
    /// Updates the specified backing field with a new value and invalidates the renderer if the value changes.
    /// </summary>
    /// <typeparam name="T">The type of the field.</typeparam>
    /// <param name="backingField">The reference to the backing field to update.</param>
    /// <param name="newValue">The new value to assign to the backing field.</param>
    /// <returns>
    /// The previous value of the backing field, or null if the new value is equal to the current value.
    /// </returns>
    protected T? SetAffectsRender2<T>(ref T? backingField, T? newValue) where T : class {
        if (EqualityComparer<T>.Default.Equals(backingField, newValue))
            return null;

        var copy = backingField;
        
        backingField = newValue;
        InvalidateRenderer();
        return copy;
    }

    protected void SetDispose<T>(ref T? backingField, T? newValue) where T : IDisposable {
        if(EqualityComparer<T>.Default.Equals(backingField, newValue))
            backingField?.Dispose();

        backingField = newValue;
    }
    
    protected override void OnDispose(bool disposeManagedResources) {
        Detach();
        base.OnDispose(disposeManagedResources);
    }
}
