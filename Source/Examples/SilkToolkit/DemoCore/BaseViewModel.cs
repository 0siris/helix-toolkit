// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BaseViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   Base ViewModel for Demo Applications?
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.Wpf.SharpDX.Camera;

namespace DemoCore;

using System;
using System.Collections.Generic;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;

/// <summary>
/// Base ViewModel for Demo Applications?
/// </summary>
public abstract class BaseViewModel : ObservableObject, IDisposable {
    public const string Orthographic = "Orthographic Camera";

    public const string Perspective = "Perspective Camera";

    private string cameraModel = Perspective;

    public string Title {
        get;
        set => SetValue(ref field, value, "Title");
    } = "Demo (HelixToolkitDX)";

    public string SubTitle {
        get;
        set => SetValue(ref field, value, "SubTitle");
    } = "Default Base View Model";

    public List<string> CameraModelCollection { get; private set; } = [];

    public string CameraModel {
        get => cameraModel;
        set {
            if (SetValue(ref cameraModel, value, "CameraModel")) {
                OnCameraModelChanged();
            }
        }
    }

    public Camera Camera {
        get;

        protected set {
            SetValue(ref field, value, "Camera");
            CameraModel = value is PerspectiveCamera
                ? Perspective
                : value is OrthographicCamera
                    ? Orthographic
                    : throw new InvalidOperationException("Unsupported camera type.");
        }
    }

    public IEffectsManager? EffectsManager {
        get;
        protected set => SetValue(ref field, value);
    }

    protected OrthographicCamera DefaultOrthographicCamera = new() {
        Position = new System.Windows.Media.Media3D.Point3D(0, 0, 5),
        LookDirection = new System.Windows.Media.Media3D.Vector3D(-0, -0, -5),
        UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0), NearPlaneDistance = 1, FarPlaneDistance = 100
    };

    protected PerspectiveCamera DefaultPerspectiveCamera = new() {
        Position = new System.Windows.Media.Media3D.Point3D(0, 0, 5),
        LookDirection = new System.Windows.Media.Media3D.Vector3D(-0, -0, -5),
        UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0), NearPlaneDistance = 0.5,
        FarPlaneDistance = 150
    };

    public event EventHandler? CameraModelChanged;

    protected BaseViewModel() {
        Camera = DefaultPerspectiveCamera;

        // camera models
        CameraModelCollection = [
            Orthographic,
            Perspective,
        ];

        // on camera changed callback
        CameraModelChanged += (_, _) => {
            if (cameraModel == Orthographic) {
                if (!(Camera is OrthographicCamera))
                    Camera = DefaultOrthographicCamera;
            } else if (cameraModel == Perspective) {
                if (!(Camera is PerspectiveCamera))
                    Camera = DefaultPerspectiveCamera;
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
            if (EffectsManager is IDisposable effectManager)
                effectManager.Dispose();

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
