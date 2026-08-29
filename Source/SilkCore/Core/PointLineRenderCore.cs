/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using System.Diagnostics.CodeAnalysis;
using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Geometry;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Material.Variables;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public class PointLineRenderCore : GeometryRenderCore, IMaterialRenderParams {
    private MaterialVariable materialVariables = EmptyMaterialVariable.EmptyVariable;

    protected PointLineModelStruct ModelStruct;

    /// <summary>
    ///     Used to wrap all material resources
    /// </summary>
    [AllowNull]
    public MaterialVariable MaterialVariables {
        get => materialVariables;
        set {
            value ??= EmptyMaterialVariable.EmptyVariable;
            SetAffectsCanRenderFlag(ref materialVariables, value);
        }
    }

    protected virtual void OnUpdatePerModelStruct() {
        ModelStruct.World = ModelMatrix;
        ModelStruct.HasInstances = InstanceBuffer.HasElements
            ? 1
            : 0;
    }

    protected override bool OnUpdateCanRenderFlag()
        => base.OnUpdateCanRenderFlag() &&
           (IsAttached || materialVariables != EmptyMaterialVariable.EmptyVariable);

    /// <summary>
    ///     Records this existing point or line core through Direct3D 12.
    /// </summary>
    /// <param name="context">The open command context.</param>
    /// <param name="resources">The render-host resource manager.</param>
    /// <param name="pass">The selected point or line pass.</param>
    /// <param name="bindings">The in-flight point/line/billboard bindings.</param>
    /// <param name="transforms">The global camera and viewport transforms.</param>
    /// <returns>Whether a draw was recorded.</returns>
    internal bool TryRenderD3D12(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        ShaderPass pass,
        SilkD3D12PointLineBindings bindings,
        in GlobalTransformStruct transforms
    ) {
        if (D3D12Material is null) return false;
        OnUpdatePerModelStruct();
        var billboardTexture = GeometryBuffer is DefaultBillboardBufferModel {
            Geometry: BillboardBase billboard
        }
            ? billboard.Texture
            : null;
        bindings.Update(context,
            resources,
            in transforms,
            in ModelStruct,
            D3D12Material,
            billboardTexture);
        return base.TryRenderD3D12(context,
            resources,
            pass,
            bindings.ResourceTableStart,
            bindings.SamplerTableStart);
    }

    /// <summary>
    ///     Gets or sets the existing material core used by the Direct3D 12 path.
    /// </summary>
    internal MaterialCore? D3D12Material { get; set; }
}
