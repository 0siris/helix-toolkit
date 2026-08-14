/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
public class Sprite2DNode : SceneNode {
    private Sprite2DBufferModel bufferModel;

    private int indexCount;

    private int[] indices;

    private int spriteCount;

    private SpriteStruct[] sprites;
    private TextureModel texture;

    public TextureModel Texture {
        get => texture;
        set {
            if (SetAffectsRender(ref texture, value) && IsAttached)
                (RenderCore as Sprite2DRenderCore).UpdateTexture(value, EffectsManager.MaterialTextureManager);
        }
    }

    public Matrix ProjectionMatrix {
        get => (RenderCore as Sprite2DRenderCore).ProjectionMatrix;
        set => (RenderCore as Sprite2DRenderCore).ProjectionMatrix = value;
    }

    public SpriteStruct[] Sprites {
        get => sprites;
        set {
            if (Set(ref sprites, value) && IsAttached) bufferModel.Sprites = value;
        }
    }

    public int SpriteCount {
        get => spriteCount;
        set {
            if (SetAffectsRender(ref spriteCount, value) && IsAttached) bufferModel.SpriteCount = value;
        }
    }

    public int[] Indices {
        get => indices;
        set {
            if (SetAffectsRender(ref indices, value) && IsAttached) bufferModel.Indices = value;
        }
    }

    public int IndexCount {
        get => indexCount;
        set {
            if (SetAffectsRender(ref indexCount, value) && IsAttached) bufferModel.IndexCount = value;
        }
    }

    protected override RenderCore OnCreateRenderCore() => new Sprite2DRenderCore();

    protected override void OnAttached() {
        bufferModel = new Sprite2DBufferModel {
            Sprites = Sprites,
            SpriteCount = SpriteCount
        };
        if (texture != null)
            (RenderCore as Sprite2DRenderCore).UpdateTexture(texture,
                                                             EffectTechnique.EffectsManager
                                                                 .MaterialTextureManager);
        base.OnAttached();
    }

    protected override void OnDetach() {
        RemoveAndDispose(ref bufferModel);
        base.OnDetach();
    }

    protected override bool CanRender(RenderContext context) => base.CanRender(context) && sprites != null && indices != null
                                                                && spriteCount != 0 && indexCount != 0;

    protected override bool CanHitTest(HitTestContext? context) => false;

    protected override bool OnHitTest(
        HitTestContext context,
        Matrix totalModelMatrix,
        ref List<HitTestResult> hits
    )
        => false;
}
