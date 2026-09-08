using DemoCore;
using HelixToolkit.SharpDX.Core.ShaderManager;

namespace ScreenDuplicationDemo;

/// <summary>
/// Provides the screen-duplication demo state.
/// </summary>
public class MainViewModel : BaseViewModel {
    /// <summary>
    /// Initializes the rendering resources used by the viewport and its overlays.
    /// </summary>
    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();
    }
}
