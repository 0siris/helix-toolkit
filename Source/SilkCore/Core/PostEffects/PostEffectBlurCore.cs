/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core.PostEffects;

/// <summary>
/// </summary>
public class PostEffectBlurCore : DisposeObject {
    //TODO review enum and usage
    [Flags] 
    public enum BlurDepth {
        One = 1,
        Two = 3
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="PostEffectMeshOutlineBlurCore" /> class.
    /// </summary>
    public PostEffectBlurCore(
        ShaderPass blurVerticalPass,
        ShaderPass blurHorizontalPass,
        int textureSlot,
        int samplerSlot,
        SamplerStateDescription sampler,
        IEffectsManager manager
    ) {
        screenBlurPassVertical = blurVerticalPass;
        screenBlurPassHorizontal = blurHorizontalPass;
        this.textureSlot = textureSlot;
        this.samplerSlot = samplerSlot;
    }

    protected override void OnDispose(bool disposeManagedResources) {
        base.OnDispose(disposeManagedResources);
    }

#region Variables

    private const int NumPingPongBlurBuffer = 2;
    private readonly ShaderPass screenBlurPassVertical;
    private readonly ShaderPass screenBlurPassHorizontal;
    private readonly int textureSlot;
    private readonly int samplerSlot;
    private static readonly Color4 Transparent = new(0, 0, 0, 0);

#endregion
}
