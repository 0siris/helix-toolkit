using System.Collections.Generic;
using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX.Model;

namespace HelixToolkit.Wpf.SharpDX;

/// <summary>
/// </summary>
public class InstancingBillboardModel3D : BillboardTextModel3D
{
    /// <summary>
    ///     Called when [create scene node].
    /// </summary>
    /// <returns></returns>
    protected override SceneNode OnCreateSceneNode()
    {
        return new InstancingBillboardNode {Material = material};
    }

    #region Dependency Properties

    /// <summary>
    ///     List of instance parameter.
    /// </summary>
    public static readonly DependencyProperty InstanceAdvArrayProperty =
        DependencyProperty.Register("InstanceParamArray", typeof(IList<BillboardInstanceParameter>),
            typeof(InstancingBillboardModel3D),
            new PropertyMetadata(null,
                (d, e) =>
                {
                    ((d as Element3DCore).SceneNode as InstancingBillboardNode).InstanceParamArray =
                        e.NewValue as IList<BillboardInstanceParameter>;
                }));

    /// <summary>
    ///     List of instance parameters.
    /// </summary>
    public IList<BillboardInstanceParameter> InstanceParamArray
    {
        get => (IList<BillboardInstanceParameter>) GetValue(InstanceAdvArrayProperty);
        set => SetValue(InstanceAdvArrayProperty, value);
    }

    #endregion
}