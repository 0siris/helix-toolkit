using System;
using System.CodeDom.Compiler;
using System.Windows;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;

namespace HelixToolkit.Wpf.SharpDX.Element3D;
#if !WINDOWS_UWP && !WINUI
public class DynamicCodeSurfaceModel3D : MeshGeometryModel3D {
    public static readonly DependencyProperty SourceCodeProperty =
        DependencyProperty.Register("SourceCode",
                                    typeof(string),
                                    typeof(DynamicCodeSurfaceModel3D),
                                    new PropertyMetadata(null,
                                                         (d, e) => {
                                                             if (d is DynamicCodeSurfaceModel3D model
                                                                 && model.SceneNode is DynamicCodeSurface3DNode node)
                                                                 node.Source = e.NewValue as string;
                                                         }));


    public static readonly DependencyProperty ParameterWProperty =
        DependencyProperty.Register("ParameterW",
                                    typeof(double),
                                    typeof(DynamicCodeSurfaceModel3D),
                                    new PropertyMetadata(1.0,
                                                         (d, e) => {
                                                             if (d is DynamicCodeSurfaceModel3D model
                                                                 && model.SceneNode is DynamicCodeSurface3DNode node)
                                                                 node.ParameterW = (float)(double)e.NewValue;
                                                         }));


    public static readonly DependencyProperty MeshSizeUProperty =
        DependencyProperty.Register("MeshSizeU",
                                    typeof(int),
                                    typeof(DynamicCodeSurfaceModel3D),
                                    new PropertyMetadata(120,
                                                         (d, e) => {
                                                             if (d is DynamicCodeSurfaceModel3D model
                                                                 && model.SceneNode is DynamicCodeSurface3DNode node)
                                                                 node.MeshSizeU = (int)e.NewValue;
                                                         }));


    public static readonly DependencyProperty MeshSizeVProperty =
        DependencyProperty.Register("MeshSizeV",
                                    typeof(int),
                                    typeof(DynamicCodeSurfaceModel3D),
                                    new PropertyMetadata(120,
                                                         (d, e) => {
                                                             if (d is DynamicCodeSurfaceModel3D model
                                                                 && model.SceneNode is DynamicCodeSurface3DNode node)
                                                                 node.MeshSizeV = (int)e.NewValue;
                                                         }));

    public static readonly DependencyProperty ErrorListProperty =
        DependencyProperty.Register("ErrorList",
                                    typeof(CompilerErrorCollection),
                                    typeof(DynamicCodeSurfaceModel3D),
                                    new PropertyMetadata(null));

    public string? SourceCode {
        get => GetValue(SourceCodeProperty) as string;
        set => SetValue(SourceCodeProperty, value);
    }


    public double ParameterW {
        get => (double)GetValue(ParameterWProperty);
        set => SetValue(ParameterWProperty, value);
    }


    public int MeshSizeU {
        get => (int)GetValue(MeshSizeUProperty);
        set => SetValue(MeshSizeUProperty, value);
    }

    public int MeshSizeV {
        get => (int)GetValue(MeshSizeVProperty);
        set => SetValue(MeshSizeVProperty, value);
    }


    public CompilerErrorCollection? ErrorList {
        get => GetValue(ErrorListProperty) as CompilerErrorCollection;
        set => SetValue(ErrorListProperty, value);
    }


    protected override void AssignDefaultValuesToSceneNode(SceneNode node) {
        if (SceneNode is DynamicCodeSurface3DNode n) {
            n.Source = SourceCode;
            n.ParameterW = (float)ParameterW;
            n.OnCompileError += N_OnCompileError;
        }
        base.AssignDefaultValuesToSceneNode(node);
    }

    private void N_OnCompileError(object? sender, EventArgs e) {
        if (sender is DynamicCodeSurface3DNode n)
            ErrorList = n.Errors;
    }

    protected override SceneNode OnCreateSceneNode() => new DynamicCodeSurface3DNode();
}
#endif
