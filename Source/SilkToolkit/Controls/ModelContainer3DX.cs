using System.Collections.Generic;
using System.Linq;
using System.Windows;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.Wpf.SharpDX.Model.Elements3D.AbstractElements3D;

namespace HelixToolkit.Wpf.SharpDX.Controls;

/// <summary>
/// Contains a scene-node collection shared by multiple Direct3D 12 viewports.
/// </summary>
public class ModelContainer3DX : HelixItemsControl, IModelContainer {
    /// <summary>
    /// Identifies the effects-manager dependency property retained for existing XAML bindings.
    /// </summary>
    public static readonly DependencyProperty EffectsManagerProperty = DependencyProperty.Register(
        nameof(EffectsManager),
        typeof(IEffectsManager),
        typeof(ModelContainer3DX),
        new PropertyMetadata(null));

    /// <summary>
    /// Initializes a shared model container.
    /// </summary>
    public ModelContainer3DX() {
        IsHitTestVisible = false;
        Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Gets or sets the effects manager used by existing bindings.
    /// </summary>
    public IEffectsManager? EffectsManager {
        get => (IEffectsManager?) GetValue(EffectsManagerProperty);
        set => SetValue(EffectsManagerProperty, value);
    }

    /// <inheritdoc />
    public IEnumerable<SceneNode> Renderables => Items
        .OfType<Model.Elements3D.AbstractElements3D.Element3D>()
        .Select(element => element.SceneNode);
}
