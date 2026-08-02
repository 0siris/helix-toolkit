// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Base ViewModel for Demo Applications?
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace DemoCore;

using System;
using System.Collections.Generic;
using System.IO;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;

/// <summary>
/// Base ViewModel for Demo Applications?
/// </summary>
public abstract class BaseViewModel : ObservableObject, IDisposable {
    public const string Orthographic = "Orthographic Camera";

    public const string Perspective = "Perspective Camera";

    private string cameraModel = null!;

    public string Title {
        get { return field; }
        set { SetValue(ref field, value, "Title"); }
    } = null!;

    public string SubTitle {
        get { return field; }
        set { SetValue(ref field, value, "SubTitle"); }
    } = null!;

    public List<string> CameraModelCollection { get; private set; } = [];

    public string CameraModel {
        get { return cameraModel; }
        set {
            if (SetValue(ref cameraModel, value, "CameraModel")) {
                OnCameraModelChanged();
            }
        }
    }

    public Camera Camera {
        get { return field; }

        protected set {
            SetValue(ref field, value, "Camera");
            CameraModel = value is PerspectiveCamera
                              ? Perspective
                              : value is OrthographicCamera
                                  ? Orthographic
                              : null!;
        }
    } = null!;

    public IEffectsManager EffectsManager {
        get { return field; }
        protected set { SetValue(ref field, value); }
    } = null!;

    protected OrthographicCamera defaultOrthographicCamera = new OrthographicCamera {
        Position = new System.Windows.Media.Media3D.Point3D(0, 0, 5),
        LookDirection = new System.Windows.Media.Media3D.Vector3D(-0, -0, -5),
        UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0), NearPlaneDistance = 1, FarPlaneDistance = 100
    };

    protected PerspectiveCamera defaultPerspectiveCamera = new PerspectiveCamera {
        Position = new System.Windows.Media.Media3D.Point3D(0, 0, 5),
        LookDirection = new System.Windows.Media.Media3D.Vector3D(-0, -0, -5),
        UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0), NearPlaneDistance = 0.5,
        FarPlaneDistance = 150
    };

    public event EventHandler? CameraModelChanged;

    protected BaseViewModel() {
        // camera models
        CameraModelCollection = [
            Orthographic,
            Perspective,
        ];

        // on camera changed callback
        CameraModelChanged += (s, e) => {
            if (cameraModel == Orthographic) {
                if (!(Camera is OrthographicCamera))
                    Camera = defaultOrthographicCamera;
            } else if (cameraModel == Perspective) {
                if (!(Camera is PerspectiveCamera))
                    Camera = defaultPerspectiveCamera;
            } else {
                throw new InvalidOperationException("Camera Model Error.");
            }
        };

        // default camera model
        CameraModel = Perspective;

        Title = "Demo (HelixToolkitDX)";
        SubTitle = "Default Base View Model";
    }

    protected virtual void OnCameraModelChanged() {
        CameraModelChanged?.Invoke(this, EventArgs.Empty);
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    protected virtual void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing) {
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.
            if (EffectsManager != null) {
                var effectManager = EffectsManager as IDisposable;
                Disposer.RemoveAndDispose(ref effectManager);
            }

            disposedValue = true;
            GC.SuppressFinalize(this);
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    ~BaseViewModel() {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose() {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }

    #endregion
}
