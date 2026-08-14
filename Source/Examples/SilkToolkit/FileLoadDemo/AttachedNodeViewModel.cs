namespace FileLoadDemo;

/// <summary>
/// Provide your own view model to manipulate the scene nodes
/// </summary>
/// <seealso cref="DemoCore.ObservableObject" />
public class AttachedNodeViewModel : DemoCore.ObservableObject {
    public bool Selected {
        set {
            if (SetValue(ref field, value)) {
                if (node is MeshNode m) {
                    m.PostEffects = value ? $"highlight[color:#FFFF00]" : "";
                    foreach (var n in node.TraverseUp()) {
                        if (n.Tag is AttachedNodeViewModel vm) {
                            vm.Expanded = true;
                        }
                    }
                }
            }
        }
        get;
    } = false;

    public bool Expanded {
        set => SetValue(ref field, value);
        get;
    } = false;

    public bool IsAnimationNode => node.IsAnimationNode;

    public string Name => node.Name;

    private SceneNode node;

    public AttachedNodeViewModel(SceneNode node) {
        this.node = node;
        node.Tag = this;
    }
}
