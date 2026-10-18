using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.SharpDX.Core;

/// Provides the live native frame owner for original scene consumers.
public interface IRenderHost {
    /// <summary>
    ///     Gets the actual viewport owning this host; assigning another viewport is unsupported.
    /// </summary>
    /// <exception cref="NotSupportedException">The supplied viewport is not this host's actual owner.</exception>
    IViewport3DX Viewport { get; set; }
    /// Gets the complete nondisposed hierarchy prepared by the most recent actual frame, in preorder with root depth zero.
    FastList<KeyValuePair<int, SceneNode>> PerFrameFlattenedScene { get; }
    /// Gets the actual native update context, or null before the first frame.
    RenderContext? RenderContext { get; }
    /// <summary>
    ///     Gets the physical pixel scale; before surface creation sets the viewport's existing DPI setting.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite or nonpositive.</exception>
    /// <exception cref="NotSupportedException">An initialized native owner's physical DPI would be overridden.</exception>
    float DpiScale { get; set; }
    /// Gets or sets the current viewport effects manager.
    IEffectsManager? EffectsManager { get; set; }
    /// Occurs once for each completed native frame.
    event EventHandler? Rendered;
    /// <summary>
    ///     Updates and renders through the actual native frame consumer and waits for completion.
    /// </summary>
    /// <returns>Whether a native frame was submitted or completed, including an empty scene.</returns>
    bool UpdateAndRender();
    /// <summary>
    ///     Queues a native resize using device-independent dimensions and the physical DPI scale.
    /// </summary>
    /// <param name="width">The device-independent width.</param>
    /// <param name="height">The device-independent height.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is negative.</exception>
    void Resize(int width, int height);
    /// Starts the existing composition rendering subscription.
    void StartRendering();
    /// Stops the existing composition rendering subscription.
    void StopRendering();
    /// Requests a real WPF/native repaint.
    void InvalidateRender();
    /// Requests preparation of the live hierarchy on the next native frame.
    void InvalidateSceneGraph();
    /// Requests preparation of per-frame renderables on the next native frame.
    void InvalidatePerFrameRenderables();
}
