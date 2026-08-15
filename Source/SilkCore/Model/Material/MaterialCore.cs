/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material.Variables;

namespace HelixToolkit.SharpDX.Core.Model.Material;
/// <summary>
/// </summary>
public abstract class MaterialCore : ObservableObject, IMaterial {
    public string Name {
        get;
        set => Set(ref field, value);
    } = "Material";

    public Guid Guid { get; } = Guid.NewGuid();

    /// <summary>
    ///     Creates the material variables.
    /// </summary>
    /// <param name="manager">The manager.</param>
    /// <param name="technique">The technique.</param>
    /// <returns></returns>
    public abstract MaterialVariable CreateMaterialVariables(
        IEffectsManager manager,
        IRenderTechnique technique
    );
}
