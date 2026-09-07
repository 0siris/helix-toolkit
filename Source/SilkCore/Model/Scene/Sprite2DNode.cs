/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene;
public class Sprite2DNode : SceneNode {
    private Sprite2DBufferModel? bufferModel;

    private int indexCount;

    private int[]? indices;

    private int spriteCount;

    private SpriteStruct[]? sprites;
    private TextureModel? texture;

    private Sprite2DBufferModel BufferModel => bufferModel
        ?? throw new InvalidOperationException("Sprite buffer model is not initialized.");

    private Sprite2DRenderCore SpriteCore => RenderCore as Sprite2DRenderCore
        ?? throw new InvalidOperationException("Sprite render core is not initialized.");

    public TextureModel? Texture {
        get => texture;
        set {
            if (SetAffectsRender(ref texture, value)) SpriteCore.Texture = value;
        }
    }

    public Matrix ProjectionMatrix {
        get => SpriteCore.ProjectionMatrix;
        set => SpriteCore.ProjectionMatrix = value;
    }

    public SpriteStruct[]? Sprites {
        get => sprites;
        set {
            if (Set(ref sprites, value) && IsAttached) BufferModel.Sprites = value;
        }
    }

    public int SpriteCount {
        get => spriteCount;
        set {
            if (SetAffectsRender(ref spriteCount, value) && IsAttached) BufferModel.SpriteCount = value;
        }
    }

    public int[]? Indices {
        get => indices;
        set {
            if (SetAffectsRender(ref indices, value) && IsAttached) BufferModel.Indices = value;
        }
    }

    public int IndexCount {
        get => indexCount;
        set {
            if (SetAffectsRender(ref indexCount, value) && IsAttached) BufferModel.IndexCount = value;
        }
    }

    protected override RenderCore OnCreateRenderCore() => new Sprite2DRenderCore();

    protected override void OnAttached() {
        bufferModel = new Sprite2DBufferModel {
            Sprites = Sprites,
            SpriteCount = SpriteCount
        };
        SpriteCore.Texture = texture;
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
