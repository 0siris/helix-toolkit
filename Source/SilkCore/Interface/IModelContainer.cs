using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.SharpDX.Core;

/// <summary>
/// </summary>
public interface IModelContainer {
    /// <summary>
    /// </summary>
    IEnumerable<SceneNode> Renderables { get; }

}
