using HelixToolkit.SharpDX.Core.Interface;

namespace WinFormsTest;

public struct ViewportOptions {
    public bool DirectionalLightFollowCamera;
    public bool WalkAround;
    public bool EnableSsao;
    public bool EnableFxaa;
    public bool EnableFrustum;
    public bool ShowRenderDetail;
    public System.Numerics.Vector3 BackgroundColor;
    public float DirectionLightIntensity;
    public float AmbientLightIntensity;
    public bool ShowWireframe;
    public bool ShowWireframeChanged;
    public bool PlayAnimation;
    public IAnimationUpdater? AnimationUpdater;
    public IViewport3DX Viewport;
    public bool ShowEnvironmentMap;
    public bool EnableDpiScale;
    public long InitTimeStamp;
}