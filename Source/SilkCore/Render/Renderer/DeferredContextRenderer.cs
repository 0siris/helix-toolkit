/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;

namespace HelixToolkit.SharpDX.Core.Render;
/// <summary>
/// </summary>
public class DeferredContextRenderer : ImmediateContextRenderer {
    private readonly List<KeyValuePair<int, CommandList>> commandList = [];
    private readonly IRenderTaskScheduler scheduler;
    [System.Diagnostics.CodeAnalysis.AllowNull]
    private IDeviceContextPool deferredContextPool;

    /// <summary>
    ///     Initializes a new instance of the <see cref="DeferredContextRenderer" /> class.
    /// </summary>
    /// <param name="deviceResources">The deviceResources.</param>
    /// <param name="scheduler"></param>
    public DeferredContextRenderer(IDevice3DResources deviceResources, IRenderTaskScheduler scheduler) : base(
        deviceResources) {
        deferredContextPool = deviceResources.DeviceContextPool;
        this.scheduler = scheduler;
    }

    /// <summary>
    ///     Renders the scene.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="renderables">The renderables.</param>
    /// <param name="parameter">The parameter.</param>
    /// <param name="testFrustum"></param>
    /// <returns>Number of node has been rendered</returns>
    public override int RenderOpaque(
        RenderContext context,
        FastList<SceneNode> renderables,
        ref RenderParameter parameter,
        bool testFrustum
    ) {
        if (scheduler.ScheduleAndRun(renderables,
                                     deferredContextPool,
                                     context,
                                     parameter,
                                     testFrustum,
                                     commandList,
                                     out var counter))
            try {
                foreach (var command in commandList.OrderBy(x => x.Key))
                    ImmediateContext.ExecuteCommandList(command.Value, true);
                return counter;
            } finally {
                foreach (var command in commandList) command.Value.Dispose();
                commandList.Clear();
            }

        return base.RenderOpaque(context, renderables, ref parameter, testFrustum);
    }

    protected override void OnDispose(bool disposeManagedResources) {
        foreach (var command in commandList) command.Value.Dispose();
        commandList.Clear();
        deferredContextPool = null;
        base.OnDispose(disposeManagedResources);
    }
}
