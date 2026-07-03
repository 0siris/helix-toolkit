using System;
using System.CodeDom.Compiler;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;

namespace HelixToolkit.Wpf.SharpDX;
#if !WINDOWS_UWP && !WINUI
public class DynamicCodeSurfaceModel3D : MeshGeometryModel3D {
    public static readonly DependencyProperty SourceCodeProperty =
        DependencyProperty.Register("SourceCode",
                                    typeof(string),
                                    typeof(DynamicCodeSurfaceModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             ((d as DynamicCodeSurfaceModel3D).SceneNode as
                                                              DynamicCodeSurface3DNode).Source =
                                                                 e.NewValue as string;
                                                         }));


    public static readonly DependencyProperty ParameterWProperty =
        DependencyProperty.Register("ParameterW",
                                    typeof(double),
                                    typeof(DynamicCodeSurfaceModel3D),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             ((d as DynamicCodeSurfaceModel3D).SceneNode as
                                                              DynamicCodeSurface3DNode).ParameterW =
                                                                 (float) (double) e.NewValue;
                                                         }));


    public static readonly DependencyProperty MeshSizeUProperty =
        DependencyProperty.Register("MeshSizeU",
                                    typeof(int),
                                    typeof(DynamicCodeSurfaceModel3D),
                                    new PropertyMetadata(120,
                                                         (d, e) => {
                                                             ((d as DynamicCodeSurfaceModel3D).SceneNode as
                                                              DynamicCodeSurface3DNode).MeshSizeU =
                                                                 (int) e.NewValue;
                                                         }));


    public static readonly DependencyProperty MeshSizeVProperty =
        DependencyProperty.Register("MeshSizeV",
                                    typeof(int),
                                    typeof(DynamicCodeSurfaceModel3D),
                                    new PropertyMetadata(120,
                                                         (d, e) => {
                                                             ((d as DynamicCodeSurfaceModel3D).SceneNode as
                                                              DynamicCodeSurface3DNode).MeshSizeV =
                                                                 (int) e.NewValue;
                                                         }));

    public static readonly DependencyProperty ErrorListProperty =
        DependencyProperty.Register("ErrorList",
                                    typeof(CompilerErrorCollection),
                                    typeof(DynamicCodeSurfaceModel3D),
                                    new PropertyMetadata(null));

    public string SourceCode {
        get => (string) GetValue(SourceCodeProperty);
        set => SetValue(SourceCodeProperty, value);
    }


    public double ParameterW {
        get => (double) GetValue(ParameterWProperty);
        set => SetValue(ParameterWProperty, value);
    }


    public int MeshSizeU {
        get => (int) GetValue(MeshSizeUProperty);
        set => SetValue(MeshSizeUProperty, value);
    }

    public int MeshSizeV {
        get => (int) GetValue(MeshSizeVProperty);
        set => SetValue(MeshSizeVProperty, value);
    }


    public CompilerErrorCollection ErrorList {
        get => (CompilerErrorCollection) GetValue(ErrorListProperty);
        set => SetValue(ErrorListProperty, value);
    }


    protected override void AssignDefaultValuesToSceneNode(SceneNode node) {
        var n = SceneNode as DynamicCodeSurface3DNode;
        n.Source = SourceCode;
        n.ParameterW = (float) ParameterW;
        n.OnCompileError += N_OnCompileError;
        base.AssignDefaultValuesToSceneNode(node);
    }

    private void N_OnCompileError(object sender, EventArgs e) {
        if (sender is DynamicCodeSurface3DNode n)
            ErrorList = n.Errors;
    }

    protected override SceneNode OnCreateSceneNode() {
        return new DynamicCodeSurface3DNode();
    }
}
#endif
