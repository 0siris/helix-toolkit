using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Model;
/*
The MIT License(MIT)
Copyright(c) 2020 Helix Toolkit contributors
*/
using System.ComponentModel;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Interface;

namespace HelixToolkit.SharpDX.Core.Model.Scene;

/// <summary>Shares material state and render invalidation across mesh, line and point scene nodes.</summary>
public abstract class MaterialGeometryNode : GeometryNode {
    /// <summary>The material assigned to the render core.</summary>
    private MaterialCore? material;

    /// <summary>The material whose property notifications are currently observed.</summary>
    private MaterialCore? observedMaterial;

    /// <summary>Suppresses material subscriptions throughout nested detachment and disposal callbacks.</summary>
    private int materialObservationSuppressionDepth;

    /// <summary>The retained callback used throughout attachment and material replacement.</summary>
    private readonly PropertyChangedEventHandler materialPropertyChanged;

    /// <summary>Creates a material node with one reusable property notification callback.</summary>
    protected MaterialGeometryNode() {
        materialPropertyChanged = OnMaterialPropertyChanged;
    }

    /// <summary>
    ///     Specifiy if model material is transparent.
    ///     During rendering, transparent objects are rendered after opaque objects. Transparent objects' order in scene graph
    ///     are preserved.
    /// </summary>
    public bool IsTransparent {
        get;
        set {
            if (Set(ref field, value))
                if (RenderType == RenderType.Opaque || RenderType == RenderType.Transparent)
                    RenderType = value
                        ? RenderType.Transparent
                        : RenderType.Opaque;
        }
    }

    /// <summary>Gets or sets the material shared with the render core.</summary>
    public MaterialCore? Material {
        get => material;
        set {
            if (!Set(ref material, value))
                return;

            if (RenderCore is MeshRenderCore meshRenderCore) meshRenderCore.D3D12Material = material;
            if (RenderCore is PointLineRenderCore pointLineRenderCore)
                pointLineRenderCore.D3D12Material = material;

            ObserveMaterial(
                !IsDisposed && (IsAttached || RenderCore.IsAttached)
                    ? material
                    : null
            );
            InvalidateRender();
        }
    }

    /// <summary>Updates material observation without changing subscriptions on repeated native attachment.</summary>
    /// <param name="value">The active material, or null to stop observing.</param>
    private void ObserveMaterial(MaterialCore? value) {
        if (materialObservationSuppressionDepth != 0) value = null;
        if (ReferenceEquals(observedMaterial, value)) return;
        if (observedMaterial is not null) observedMaterial.PropertyChanged -= materialPropertyChanged;
        observedMaterial = value;
        if (observedMaterial is not null) observedMaterial.PropertyChanged += materialPropertyChanged;
    }

    /// <summary>Wakes this node and its ancestor consumers after any material parameter changes.</summary>
    /// <param name="sender">The observed material.</param>
    /// <param name="args">The changed material property.</param>
    private void OnMaterialPropertyChanged(object? sender, PropertyChangedEventArgs args) => InvalidateRender();

    /// <inheritdoc />
    protected override bool OnAttach(IEffectsManager effectsManager) {
        var attached = base.OnAttach(effectsManager);
        if (attached && !IsDisposed) ObserveMaterial(material);
        return attached;
    }

    /// <inheritdoc />
    internal override bool Attach() {
        var attached = base.Attach();
        if (RenderCore.IsAttached && !IsDisposed) ObserveMaterial(material);
        return attached;
    }

    /// <inheritdoc />
    protected override void OnDetachD3D12() {
        materialObservationSuppressionDepth++;
        try {
            ObserveMaterial(null);
            base.OnDetachD3D12();
        } finally {
            ObserveMaterial(null);
            materialObservationSuppressionDepth--;
        }
    }

    /// <inheritdoc />
    protected override void OnDetach() {
        materialObservationSuppressionDepth++;
        try {
            ObserveMaterial(null);
            base.OnDetach();
        } finally {
            ObserveMaterial(null);
            materialObservationSuppressionDepth--;
        }
    }

    /// <inheritdoc />
    protected override void OnDispose(bool disposeManagedResources) {
        materialObservationSuppressionDepth++;
        try {
            ObserveMaterial(null);
            base.OnDispose(disposeManagedResources);
        } finally {
            ObserveMaterial(null);
            materialObservationSuppressionDepth--;
        }
    }

    /// <inheritdoc />
    protected override OrderKey OnUpdateRenderOrderKey() => OrderKey.Create(RenderOrder, 0);

    /// <inheritdoc />
    protected override bool CanRender(RenderContext context) =>
        base.CanRender(context) && material is not null;
}