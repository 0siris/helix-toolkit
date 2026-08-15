using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

namespace HelixToolkit.SharpDX.Core.Interface;

/// <summary>
/// </summary>
public interface IModelContainer : IRenderHost {
    /// <summary>
    /// </summary>
    IEnumerable<SceneNode> Renderables { get; }

    /// <summary>
    /// </summary>
    IRenderHost? CurrentRenderHost { get; set; }

    /// <summary>
    /// </summary>
    /// <param name="viewport"></param>
    void AttachViewport3DX(IViewport3DX viewport);

    /// <summary>
    /// </summary>
    /// <param name="viewport"></param>
    void DettachViewport3DX(IViewport3DX viewport);

    void Attach(IRenderHost host);
    void Detach(IRenderHost host);
}
