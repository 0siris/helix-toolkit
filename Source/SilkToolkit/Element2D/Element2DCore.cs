/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

#if DEBUG
//#define DEBUGDRAWING
//#define DISABLEBITMAPCACHE
#endif

using System;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene2D;

namespace HelixToolkit.Wpf.SharpDX.Core2D;

/// <summary>
///     External Wrapper core to be used for different platform
/// </summary>
public abstract class Element2DCore : FrameworkContentElement, IDisposable {
    public sealed class SceneNode2DCreatedEventArgs : EventArgs {
        public SceneNode2DCreatedEventArgs(SceneNode2D node) {
            Node = node;
        }

        public SceneNode2D Node { get; private set; }
    }

    /// <summary>
    ///     Gets the unique identifier.
    /// </summary>
    /// <value>
    ///     The unique identifier.
    /// </value>
    public Guid GUID => SceneNode.Guid;


    public bool IsAttached => SceneNode.IsAttached;

    #region Scene Node

    private readonly object sceneNodeLock = new();

    public SceneNode2D SceneNode {
        get {
            if (field == null)
                lock (sceneNodeLock) {
                    if (field == null) {
                        field = OnCreateSceneNode();
                        AssignDefaultValuesToSceneNode(field);
                        field.WrapperSource = this;
                        field.Attached += SceneNode_OnAttached;
                        field.Detached += SceneNode_OnDetached;
                        field.UpdateRequested += SceneNode_OnUpdate;
                        OnSceneNodeCreated?.Invoke(this, new SceneNode2DCreatedEventArgs(field));
                    }
                }

            return field;
        }
    }

    private void SceneNode_OnUpdate(object sender, SceneNode2D.UpdateEventArgs e) {
        OnUpdate(e.Context);
    }

    private void SceneNode_OnDetached(object sender, EventArgs e) {
        if (Dispatcher != null && Dispatcher.Thread.IsAlive) {
            if (Dispatcher.CheckAccess())
                OnDetached();
            else
                Dispatcher.Invoke(OnDetached);
        }
    }

    private void SceneNode_OnAttached(object sender, EventArgs e) {
        OnAttached();
    }

    protected virtual void OnAttached() { }

    protected virtual void OnDetached() { }

    protected virtual void OnUpdate(RenderContext2D context) { }

    /// <summary>
    ///     Called when [create scene node].
    /// </summary>
    /// <returns></returns>
    protected abstract SceneNode2D OnCreateSceneNode();

    protected virtual void AssignDefaultValuesToSceneNode(SceneNode2D node) { }

    #endregion

    #region Events

    /// <summary>
    ///     Occurs when [on scene node created]. Make sure to hook up this event at the top of constructor of class, otherwise
    ///     may miss the event.
    /// </summary>
    public event EventHandler<SceneNode2DCreatedEventArgs> OnSceneNodeCreated;

    #endregion

    public virtual bool HitTest(Vector2 mousePoint, out HitTest2DResult hitResult) => SceneNode.HitTest(mousePoint, out hitResult);

    public void InvalidateRender() {
        SceneNode.InvalidateRender();
    }
    public void InvalidateMeasure() {
        SceneNode.InvalidateMeasure();
    }

    public void InvalidateArrange() {
        SceneNode.InvalidateArrange();
    }


    public static implicit operator SceneNode2D(Element2DCore e) => e.SceneNode;

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls        

    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">
    ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
    ///     unmanaged resources.
    /// </param>
    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~Element2DCore() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.        
    /// <summary>
    ///     Releases unmanaged and - optionally - managed resources.
    /// </summary>
    public void Dispose() {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
