using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

namespace HelixToolkit.SharpDX.Core.Interface;

/// <summary>
/// </summary>
public interface IModelContainer {
    /// <summary>
    /// </summary>
    IEnumerable<SceneNode> Renderables { get; }

}
