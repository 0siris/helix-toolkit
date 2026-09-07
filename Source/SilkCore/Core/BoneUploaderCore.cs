/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

public sealed class BoneUploaderCore : RenderCore {
    private Matrix[] boneMatrices = [];
    private bool matricesChanged = true;

    public BoneUploaderCore() : base(RenderType.None) { }

    [AllowNull]
    public Matrix[] BoneMatrices {
        get => boneMatrices;
        set {
            if (SetAffectsRender(ref boneMatrices, value ?? [])) {
                matricesChanged = true;
                BoneChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public StructuredBufferProxy? BoneSkinSb {
        get;
        set {
            if (field != value)
                field?.Dispose();
            field = value;
        }
    }
    public event EventHandler? BoneChanged;

    public void InvalidateBoneMatrices() => matricesChanged = true;

    protected override void OnDispose(bool disposeManagedResources) {
        if (disposeManagedResources)
            BoneChanged = null;

        base.OnDispose(disposeManagedResources);
    }
}
