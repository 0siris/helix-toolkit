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
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

public class DrawScreenQuadCore : RenderCore {
    private readonly ConstantBufferComponent modelCb;

    public ScreenQuadModelStruct ModelStruct;

    private ShaderPass pass = ShaderPass.NullPass;
    private string passName = DefaultPassNames.Default;

    private SamplerStateProxy? Sampler {
        get;
        set {
            if (field != value)
                field?.Dispose();
            field = value;
        }
    }

    private SamplerStateDescription samplerDescription = DefaultSamplers.LinearSamplerClampAni1;
    private int samplerSlot;

    private TextureModel? texture;

    private ShaderResourceViewProxy? TextureProxy {
        get;
        set {
            if (field != value)
                field?.Dispose();
            field = value;
        }
    }
    private int textureSlot;

    public DrawScreenQuadCore() : base(RenderType.Opaque) {
        modelCb = AddComponent(new ConstantBufferComponent(
                                   new ConstantBufferDescription(
                                       DefaultBufferNames.ScreenQuadCb,
                                       ScreenQuadModelStruct.SizeInBytes)));
        ModelStruct = new ScreenQuadModelStruct {
            TopLeft = new Vector4(-1, 1, 1, 1),
            TopRight = new Vector4(1, 1, 1, 1),
            BottomLeft = new Vector4(-1, -1, 1, 1),
            BottomRight = new Vector4(1, -1, 1, 1),
            TexTopLeft = new Vector2(0, 1),
            TexTopRight = new Vector2(1, 1),
            TexBottomLeft = new Vector2(0, 0),
            TexBottomRight = new Vector2(1, 0)
        };
    }

    public string PassName {
        get => passName;
        set => SetAffectsRender(ref passName, value);
    }

    /// <summary>
    ///     Gets or sets the texture.
    /// </summary>
    /// <value>
    ///     The texture.
    /// </value>
    public TextureModel? Texture {
        get => texture;
        set => SetAffectsRender(ref texture, value);
    }

    /// <summary>
    ///     Gets or sets the sampler description.
    /// </summary>
    /// <value>
    ///     The sampler description.
    /// </value>
    public SamplerStateDescription SamplerDescription {
        get => samplerDescription;
        set => SetAffectsRender(ref samplerDescription, value);
    }

}
