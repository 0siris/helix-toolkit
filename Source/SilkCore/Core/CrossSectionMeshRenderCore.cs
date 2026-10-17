/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Lights;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Core;

public class CrossSectionMeshRenderCore : MeshRenderCore, ICrossSectionRenderParams {
    #region Properties

    /// <summary>
    ///     Gets or sets the cutting operation.
    /// </summary>
    /// <value>
    ///     The cutting operation.
    /// </value>
    public CuttingOperation CuttingOperation {
        get;
        set { SetAffectsRender(ref field, value); }
    } = CuttingOperation.Intersect;

    /// <summary>
    ///     Defines the sectionColor
    /// </summary>
    public Color4 SectionColor {
        get;
        set { SetAffectsRender(ref field, value); }
    } = Color.Green;

    public Bool4 PlaneEnabled {
        get;
        set { SetAffectsRender(ref field, value); }
    }

    public Bool4 Plane5To8Enabled {
        get;
        set { SetAffectsRender(ref field, value); }
    }

    /// <summary>
    ///     Defines the plane 1(Normal + d)
    /// </summary>
    public Vector4 Plane1Params {
        get;
        set { SetAffectsRender(ref field, value); }
    }

    /// <summary>
    ///     Defines the plane 2(Normal + d)
    /// </summary>
    public Vector4 Plane2Params {
        get;
        set { SetAffectsRender(ref field, value); }
    }

    /// <summary>
    ///     Defines the plane 3(Normal + d)
    /// </summary>
    public Vector4 Plane3Params {
        get;
        set { SetAffectsRender(ref field, value); }
    }

    /// <summary>
    ///     Defines the plane 4(Normal + d)
    /// </summary>
    public Vector4 Plane4Params {
        get;
        set { SetAffectsRender(ref field, value); }
    }

    /// <summary>
    ///     Defines the plane 5(Normal + d)
    /// </summary>
    public Vector4 Plane5Params {
        get;
        set { SetAffectsRender(ref field, value); }
    }

    /// <summary>
    ///     Defines the plane 6(Normal + d)
    /// </summary>
    public Vector4 Plane6Params {
        get;
        set { SetAffectsRender(ref field, value); }
    }

    /// <summary>
    ///     Defines the plane 7(Normal + d)
    /// </summary>
    public Vector4 Plane7Params {
        get;
        set { SetAffectsRender(ref field, value); }
    }

    /// <summary>
    ///     Defines the plane 8(Normal + d)
    /// </summary>
    public Vector4 Plane8Params {
        get;
        set { SetAffectsRender(ref field, value); }
    }

    #endregion

    /// <inheritdoc />
    internal override bool TryRenderD3D12(
        SilkD3D12CommandContext context,
        SilkD3D12ResourceManager resources,
        ShaderPass pass,
        SilkD3D12MeshBindings bindings,
        in GlobalTransformStruct transforms,
        LightsBufferModel? lights = null,
        TextureModel? environmentMap = null
    ) {
        bindings.UpdateClipping(this);
        return base.TryRenderD3D12(context, resources, pass, bindings, in transforms, lights, environmentMap);
    }
}
