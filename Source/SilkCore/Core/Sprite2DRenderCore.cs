/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class Sprite2DRenderCore : RenderCore {
    private readonly ConstantBufferComponent globalTransformCb;

    private SamplerStateProxy? sampler;

    private int samplerSlot;

    private ShaderPass spritePass = ShaderPass.NullPass;

    private int texSlot;

    private ShaderResourceViewProxy? textureView;

    public Sprite2DRenderCore()
        : base(RenderType.ScreenSpaced) {
        globalTransformCb = AddComponent(new ConstantBufferComponent(
                                             new ConstantBufferDescription(DefaultBufferNames.GlobalTransformCb,
                                                                           GlobalTransformStruct.SizeInBytes)));
    }

    public IAttachableBufferModel? Buffer { get; set; }

    public Matrix ProjectionMatrix { get; set; } = Matrix.Identity;

    public void UpdateTexture(TextureModel texture, ITextureResourceManager manager) {
        var tex = manager.Register(texture, true);
        RemoveAndDispose(ref textureView);
        textureView = tex;
    }

}
