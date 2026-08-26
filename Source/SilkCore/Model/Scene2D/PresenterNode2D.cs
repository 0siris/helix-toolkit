/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene2D.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Model.Scene2D;
public class PresenterNode2D : SceneNode2D {
    private SceneNode2D? content;

    public PresenterNode2D() {
        ItemsInternal = [];
        Items = new ReadOnlyObservableFastList<SceneNode2D>(ItemsInternal);
    }

    public SceneNode2D? Content {
        get => content;
        set {
            if (content != value) {
                if (content != null) {
                    content.Detach();
                    content.Parent = null;
                    ItemsInternal.Clear();
                }

                content = value;
                if (content is { } actualContent) {
                    actualContent.Parent = this;
                    if (IsAttached) actualContent.Attach(DpiScale);
                    ItemsInternal.Add(actualContent);
                }

                InvalidateMeasure();
            }
        }
    }

    protected override bool OnAttach() {
        if (base.OnAttach()) {
            content?.Attach(DpiScale);
            return true;
        }

        return false;
    }

    protected override void OnDetach() {
        content?.Detach();
        base.OnDetach();
    }

    //protected override void OnRender(RenderContext2D context)
    //{
    //    base.OnRender(context);
    //    if (content != null)
    //    {
    //        content.Render(context);
    //    }
    //}

protected override bool OnHitTest(ref Vector2 mousePoint, out HitTest2DResult? hitResult) {
        if (content != null) return content.HitTest(mousePoint, out hitResult);

        hitResult = null;
        return false;
    }

    protected override Size2F MeasureOverride(Size2F availableSize) {
        if (content != null) {
            content.Measure(availableSize);
            return new Size2F(content.DesiredSize.X, content.DesiredSize.Y);
        }

        return new Size2F();
    }

    protected override RectangleF ArrangeOverride(RectangleF finalSize) {
        if (content != null) {
            content.Arrange(finalSize);
            return new RectangleF(0, 0, content.DesiredSize.X, content.DesiredSize.Y);
        }

        return finalSize;
    }
}
