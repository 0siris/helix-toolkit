/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System;
using System.Collections.Generic;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.Wpf.SharpDX {
    namespace Model {
        /// <summary>
        ///     External Wrapper core to be used for different platform
        /// </summary>
        public abstract class Element3DCore : FrameworkContentElement, IDisposable {
            /// <summary>
            /// </summary>
            public Guid GUID => SceneNode.GUID;

            /// <summary>
            /// </summary>
            public Matrix TotalModelMatrix => SceneNode.TotalModelMatrix;


            public bool Visible => SceneNode.Visible;

            public bool IsAttached => SceneNode.IsAttached;

            #region Events

            /// <summary>
            ///     Occurs when [on scene node created]. Make sure to hook up this event at the top of constructor of class, otherwise
            ///     may miss the event.
            /// </summary>
            public event EventHandler<SceneNodeCreatedEventArgs> OnSceneNodeCreated;

            #endregion

            #region Hit Test

            /// <summary>
            ///     Hits the test.
            /// </summary>
            /// <param name="context">The context.</param>
            /// <param name="hits">The hits.</param>
            /// <returns></returns>
            public virtual bool HitTest(HitTestContext context, ref List<HitTestResult> hits) {
                return SceneNode.HitTest(context, ref hits);
            }

            #endregion

            public void InvalidateRender() {
                SceneNode.InvalidateRender();
            }

            public static explicit operator SceneNode(Element3DCore core) {
                return core.SceneNode;
            }

            public sealed class SceneNodeCreatedEventArgs : EventArgs {
                public SceneNodeCreatedEventArgs(SceneNode node) {
                    Node = node;
                }

                public SceneNode Node { get; private set; }
            }

            #region Scene Node

            private readonly object sceneNodeLock = new();
            private SceneNode sceneNode;

            public SceneNode SceneNode {
                get {
                    if (sceneNode == null)
                        lock (sceneNodeLock) {
                            if (sceneNode == null) {
                                sceneNode = OnCreateSceneNode();
                                AssignDefaultValuesToSceneNode(sceneNode);
                                sceneNode.WrapperSource = this;
                                OnSceneNodeCreated?.Invoke(this, new SceneNodeCreatedEventArgs(sceneNode));
                            }
                        }

                    return sceneNode;
                }
            }

            /// <summary>
            ///     Called when [create scene node].
            /// </summary>
            /// <returns></returns>
            protected abstract SceneNode OnCreateSceneNode();

            protected virtual void AssignDefaultValuesToSceneNode(SceneNode node) { }

            public string SceneNodeName {
                get => SceneNode.Name;
                set => SceneNode.Name = value;
            }

            #endregion

            #region IBoundable

            /// <summary>
            ///     Gets the bounds.
            /// </summary>
            /// <value>
            ///     The bounds.
            /// </value>
            public BoundingBox Bounds => SceneNode.Bounds;

            /// <summary>
            ///     Gets the bounds with transform.
            /// </summary>
            /// <value>
            ///     The bounds with transform.
            /// </value>
            public BoundingBox BoundsWithTransform => SceneNode.BoundsWithTransform;

            /// <summary>
            ///     Gets the bounds sphere.
            /// </summary>
            /// <value>
            ///     The bounds sphere.
            /// </value>
            public BoundingSphere BoundsSphere => SceneNode.BoundsSphere;

            /// <summary>
            ///     Gets the bounds sphere with transform.
            /// </summary>
            /// <value>
            ///     The bounds sphere with transform.
            /// </value>
            public BoundingSphere BoundsSphereWithTransform => SceneNode.BoundsSphereWithTransform;

            #endregion

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
                    if (disposing) Disposer.RemoveAndDispose(ref sceneNode);
                    // TODO: dispose managed state (managed objects).
                    // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                    // TODO: set large fields to null.

                    disposedValue = true;
                }
            }

            // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
            // ~Element3DCore() {
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
    }
}
