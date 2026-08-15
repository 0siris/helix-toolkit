using System;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Element3D;

namespace CustomShaderDemo;

public class CustomNoiseMeshModel3D : MeshGeometryModel3D {
    protected override SceneNode OnCreateSceneNode() {
        var node = base.OnCreateSceneNode();
        if (node is null) {
            throw new InvalidOperationException("The base model did not create a scene node.");
        }

        node.OnSetRenderTechnique = (_) => {
            var effectsManager = node.EffectsManager
                                 ?? throw new InvalidOperationException("The scene node has no effects manager.");
            return effectsManager[CustomShaderNames.NoiseMesh];
        };
        return node;
    }
}