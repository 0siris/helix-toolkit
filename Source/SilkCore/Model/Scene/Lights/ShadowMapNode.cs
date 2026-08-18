/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using System.ComponentModel;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Lights;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Camera;
using HelixToolkit.SharpDX.Core.Model.Collection;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene.Lights;

/// <summary>
/// </summary>
public class ShadowMapNode : SceneNode {
    private readonly OrthographicCameraCore orthoCamera = new() {
        NearPlaneDistance = 1,
        FarPlaneDistance = 500
    };

    private readonly PerspectiveCameraCore persCamera = new() {
        NearPlaneDistance = 1,
        FarPlaneDistance = 500
    };

    private float distance = 200;

    private float orthoWidth = 100;
    private bool sceneChanged;

    private ShadowMapCore ShadowCore => (ShadowMapCore) RenderCore;

    /// <summary>
    ///     Gets or sets the resolution.
    /// </summary>
    /// <value>
    ///     The resolution.
    /// </value>
    public Size2 Resolution {
        get => new(ShadowCore.Width, ShadowCore.Height);
        set {
            ShadowCore.Width = value.Width;
            ShadowCore.Height = value.Height;
        }
    }

    /// <summary>
    /// </summary>
    public float Bias {
        get => ShadowCore.Bias;
        set => ShadowCore.Bias = value;
    }

    /// <summary>
    /// </summary>
    public float Intensity {
        get => ShadowCore.Intensity;
        set => ShadowCore.Intensity = value;
    }

    public float Distance {
        get => distance;
        set => SetAffectsRender(ref distance, value);
    }

    public float OrthoWidth {
        get => orthoWidth;
        set => SetAffectsRender(ref orthoWidth, value);
    }

    /// <summary>
    ///     Gets or sets the far field.
    /// </summary>
    /// <value>
    ///     The far field.
    /// </value>
    public float FarField {
        get;
        set {
            if (SetAffectsRender(ref field, value)) {
                orthoCamera.FarPlaneDistance = value;
                persCamera.FarPlaneDistance = value;
            }
        }
    } = 500;

    /// <summary>
    ///     Gets or sets the near field.
    /// </summary>
    /// <value>
    ///     The far field.
    /// </value>
    public float NearField {
        get;
        set {
            if (SetAffectsRender(ref field, value)) {
                orthoCamera.NearPlaneDistance = value;
                persCamera.NearPlaneDistance = value;
            }
        }
    } = 500;

    /// <summary>
    ///     Distance of the directional light from origin
    /// </summary>
    public ProjectionCameraCore? LightCamera {
        get;
        set {
            field?.PropertyChanged -= LightCamera_PropertyChanged;
            SetAffectsRender(ref field, value);
            field?.PropertyChanged += LightCamera_PropertyChanged;
        }
    }

    /// <summary>
    ///     Gets or sets a value indicating whether shadow map should automatically cover complete scene. Only effective with
    ///     directional light.
    ///     <para>Limitation: Currently unable to properly cover BoneSkinned model animation.</para>
    /// </summary>
    /// <value>
    ///     <c>true</c> if [automatic cover complete scene]; otherwise, <c>false</c>.
    /// </value>
    public bool AutoCoverCompleteScene { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the scene is dynamic. Only effective if
    ///     <see cref="AutoCoverCompleteScene" /> is true.
    ///     <para>Setting to true will force shadow map to update the shadow camera for each frame. May impact the performance.</para>
    /// </summary>
    /// <value>
    ///     <c>true</c> if scene is dynamic; otherwise, <c>false</c>.
    /// </value>
    public bool IsSceneDynamic { get; set; }

    /// <summary>
    ///     Gets or sets the shadow cast scene scale. Only effective if <see cref="AutoCoverCompleteScene" /> is true.
    ///     <para>
    ///         This is used if the mesh render shadow is much bigger than the meshes casting shadow.
    ///         The shadow cast camera has to cover the shadow rendering region, otherwise the shadow maybe cut off.
    ///         Increase the value to increase the shadow cast camera rendering region.
    ///     </para>
    /// </summary>
    /// <value>
    ///     Region scale for shadow cast.
    /// </value>
    public float CastSceneScale { get; set; } = 2f;

    /// <summary>
    ///     Called when [create render core].
    /// </summary>
    /// <returns></returns>
    protected override RenderCore OnCreateRenderCore() {
        var core = new ShadowMapCore();
        core.OnUpdateLightSource += Core_OnUpdateLightSource;
        return core;
    }

    /// <summary>
    ///     Assigns the default values to core.
    /// </summary>
    /// <param name="core">The core.</param>
    protected override void AssignDefaultValuesToCore(RenderCore core) {
        base.AssignDefaultValuesToCore(core);
        if (core is not ShadowMapCore c)
            throw new InvalidOperationException("Shadow map render core is required.");
        //c.FactorPCF = (float)FactorPCF;
        c.Intensity = Intensity;
        c.Bias = Bias;
        c.Width = Resolution.Width;
        c.Height = Resolution.Height;
    }

    private void LightCamera_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
        InvalidateRender();
    }

    /// <summary>
    ///     To override Attach routine, please override this.
    /// </summary>
    /// <param name="effectsManager"></param>
    /// <returns>
    ///     Return true if attached
    /// </returns>
    protected override bool OnAttach(IEffectsManager effectsManager) {
        base.OnAttach(effectsManager);
        Invalidated += Host_SceneGraphUpdated;
        sceneChanged = true;
        return true;
    }

    protected override void OnDetach() {
        Invalidated -= Host_SceneGraphUpdated;
        base.OnDetach();
    }

    private void Host_SceneGraphUpdated(object? sender, InvalidateTypes type) {
        if (type == InvalidateTypes.SceneGraph) sceneChanged = true;
    }

    /// <summary>
    ///     <para>Determine if this can be rendered.</para>
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    protected override bool CanRender(RenderContext context) {
        ShadowCore.NeedRender =
            base.CanRender(context) && context.RenderHost.IsShadowMapEnabled;
        return true;
    }

    private BoundingBox FindSceneBound(FastList<SceneNode> nodes) {
        var box = new BoundingBox();
        if (nodes.Count > 0)
            foreach (var node in nodes.Where(x => x is IThrowingShadow {IsThrowingShadow: true})) {
                if (node.BoundsWithTransform.Minimum == node.BoundsWithTransform.Maximum) continue;
                if (box.Minimum == box.Maximum)
                    box = node.BoundsWithTransform;
                else
                    box = BoundingBox.Merge(box, node.BoundsWithTransform);
            }

        return box;
    }

    private unsafe bool CreateCameraFromBound(ref BoundingBox box, ref Vector3 lookDir) {
        if (box.Maximum == box.Minimum) return false;
        var center = box.Center();

        var points = stackalloc Vector3[8];
        points[0] = box.Minimum;
        points[1] = box.Maximum;
        points[2] = new Vector3(box.Minimum.X, box.Minimum.Y, box.Maximum.Z);
        points[3] = new Vector3(box.Minimum.X, box.Maximum.Y, box.Maximum.Z);
        points[4] = new Vector3(box.Maximum.X, box.Maximum.Y, box.Minimum.Z);
        points[5] = new Vector3(box.Maximum.X, box.Minimum.Y, box.Minimum.Z);
        points[6] = new Vector3(box.Minimum.X, box.Maximum.Y, box.Minimum.Z);
        points[7] = new Vector3(box.Maximum.X, box.Minimum.Y, box.Maximum.Z);
        var plane = new Plane(center, lookDir);
        var farthestPoint = Vector3.Zero;
        var farestDist = 0f;

        for (var i = 0; i < 8; ++i) {
            SilkMath.Dot(ref plane.Normal, ref points[i], out var dot);
            dot += plane.D;
            if (dot > 0) continue;
            var t = dot - plane.D;
            var v = points[i] - t * plane.Normal;
            var vDist = v.Length;
            if (vDist > farestDist) {
                farthestPoint = points[i];
                farestDist = vDist;
            }
        }

        var dist = farestDist * CastSceneScale + 0.1f;
        var pos = center + -lookDir * dist;
        orthoCamera.Position = pos;
        orthoCamera.LookDirection = center - pos;
        orthoCamera.Width = dist * 2;
        orthoCamera.NearPlaneDistance = 0.1f;
        orthoCamera.FarPlaneDistance = dist * 2;
        orthoCamera.UpDirection = lookDir.FindAnyPerpendicular();
        return true;
    }

    private void SetOrthoCameraParameters(ref Vector3 lookDir) {
        orthoCamera.LookDirection = lookDir * distance;
        orthoCamera.Position = -lookDir * distance;
        orthoCamera.UpDirection = Vector3.UnitZ;
        orthoCamera.Width = orthoWidth;
    }

    private void Core_OnUpdateLightSource(object? sender, ShadowMapCore.UpdateLightSourceEventArgs e) {
        CameraCore? camera = LightCamera;
        if (LightCamera == null) {
            var lights = e.Context.RenderHost.PerFrameLights.Take(Constants.MaxLights);
            foreach (var light in lights) {
                if (light.LightType == LightType.Directional) {
                    if (light.RenderCore is not DirectionalLightCore dlight) continue;
                    var dir = SilkMath.TransformNormal(dlight.Direction, dlight.ModelMatrix)
                        .Normalized();
                    if (AutoCoverCompleteScene) {
                        if (sceneChanged || e.Context.UpdateSceneGraphRequested || IsSceneDynamic) {
                            sceneChanged = false;
                            var boundingBox = FindSceneBound(e.Context.RenderHost.PerFrameOpaqueNodes);
                            if (!CreateCameraFromBound(ref boundingBox, ref dir))
                                SetOrthoCameraParameters(ref dir);
                        }
                    } else {
                        SetOrthoCameraParameters(ref dir);
                    }

                    camera = orthoCamera;
                    break;
                }

                if (light.LightType == LightType.Spot) {
                    if (light.RenderCore is not SpotLightCore splight) continue;
                    persCamera.Position = splight.Position + splight.ModelMatrix.Row4.ToVector3();
                    var look = SilkMath.TransformNormal(splight.Direction, splight.ModelMatrix);
                    persCamera.LookDirection = look;
                    persCamera.FarPlaneDistance = splight.Range;
                    persCamera.FieldOfView = splight.OuterAngle;
                    persCamera.UpDirection = Vector3.UnitZ;
                    camera = persCamera;
                    break;
                }
            }
        }

        if (camera == null) {
            ShadowCore.FoundLightSource = false;
        } else {
            ShadowCore.FoundLightSource = true;
            ShadowCore.LightView = camera.CreateViewMatrix();
            ShadowCore.LightProjection = camera.CreateProjectionMatrix(ShadowCore.Width / ShadowCore.Height);
        }
    }

    protected override bool CanHitTest(HitTestContext? context) => false;

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;
}