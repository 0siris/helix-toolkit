

using System.Collections.Generic;

using System.Windows;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
namespace HelixToolkit.Wpf.SharpDX

{
    using Model;

    /// <summary>
    /// 
    /// </summary>
    public class BoneSkinMeshGeometryModel3D : MeshGeometryModel3D
    {
        public static DependencyProperty BoneMatricesProperty = DependencyProperty.Register("BoneMatrices", typeof(Matrix[]), typeof(BoneSkinMeshGeometryModel3D),
            new PropertyMetadata(BoneMatricesStruct.DefaultBones,
                (d, e) =>
                {
                    ((d as Element3DCore).SceneNode as BoneSkinMeshNode).BoneMatrices = (Matrix[])e.NewValue;
                }));

        public Matrix[] BoneMatrices
        {
            set
            {
                SetValue(BoneMatricesProperty, value);
            }
            get
            {
                return (Matrix[])GetValue(BoneMatricesProperty);
            }
        }

        protected override SceneNode OnCreateSceneNode()
        {
            return new BoneSkinMeshNode();
        }
    }
}
