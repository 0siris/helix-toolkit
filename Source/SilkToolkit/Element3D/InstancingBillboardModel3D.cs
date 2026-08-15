using System.Collections.Generic;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;

namespace HelixToolkit.Wpf.SharpDX.Element3D;

/// <summary>
/// </summary>
public class InstancingBillboardModel3D : BillboardTextModel3D {
    /// <summary>
    ///     Called when [create scene node].
    /// </summary>
    /// <returns></returns>
    protected override SceneNode OnCreateSceneNode() => new InstancingBillboardNode { Material = Material };

    #region Dependency Properties

    /// <summary>
    ///     List of instance parameter.
    /// </summary>
    public static readonly DependencyProperty InstanceAdvArrayProperty =
        DependencyProperty.Register("InstanceParamArray",
                                    typeof(IList<BillboardInstanceParameter>),
                                    typeof(InstancingBillboardModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             if (d is Element3DCore { SceneNode: InstancingBillboardNode node })
                                                                 node.InstanceParamArray =
                                                                     e.NewValue as IList<BillboardInstanceParameter>;
                                                         }));

    /// <summary>
    ///     List of instance parameters.
    /// </summary>
    public IList<BillboardInstanceParameter> InstanceParamArray {
        get => (IList<BillboardInstanceParameter>)GetValue(InstanceAdvArrayProperty);
        set => SetValue(InstanceAdvArrayProperty, value);
    }

    #endregion
}
