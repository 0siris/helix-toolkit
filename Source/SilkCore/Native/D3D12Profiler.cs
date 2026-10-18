using System.Runtime.InteropServices;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>
///     Labels external PIX/Nsight captures without adding timers, GPU waits or a public diagnostics interface.
/// </summary>
internal static class D3D12Profiler {
    /// Gets whether profiling was explicitly enabled before this process first used the renderer.
    internal static readonly bool Enabled = Environment.GetEnvironmentVariable("SILK_D3D12_PROFILE") == "1";

    /// <summary>
    ///     Names a native D3D12 object for external captures, only when profiling is enabled.
    /// </summary>
    /// <param name="pointer">The live D3D12 object.</param>
    /// <param name="name">The resource role without application data.</param>
    internal static unsafe void NameObject(nint pointer, string name) {
        if (!Enabled) return;
        fixed (char* text = name)
            _ = ((Silk.NET.Direct3D12.ID3D12Object*) pointer)->SetName(text);
    }

    /// <summary>
    ///     Begins a CPU/GPU region using Microsoft's stable command-list instrumentation ABI.
    /// </summary>
    /// <param name="context">The open command list receiving the region.</param>
    /// <param name="name">A constant region label without user or patient data.</param>
    /// <returns>A region closed before the command list is submitted.</returns>
    internal static Region Begin(SilkD3D12CommandContext context, string name) {
        if (!Enabled) return default;
        var pointer = context.CommandListPointer;
        BeginEvent(pointer, 0xff4f9ed9, name);
        return new Region(pointer);
    }

    /// <summary>
    ///     Publishes real occupancy counters only during an explicitly enabled capture session.
    /// </summary>
    /// <param name="scene">The scene and peeling table owner.</param>
    /// <param name="effects">The transient effect table owner.</param>
    /// <param name="resources">The ordinary shared resource heap.</param>
    /// <param name="samplers">The ordinary shared sampler heap.</param>
    internal static void ReportDescriptors(D3D12EffectBindingPool scene, D3D12EffectBindingPool effects,
        SilkD3D12DescriptorHeap resources, SilkD3D12DescriptorHeap samplers) {
        if (!Enabled) return;
        ReportCounter("Silk.Scene.HeapPages", scene.PageCount);
        ReportCounter("Silk.Scene.ResourceDescriptors", scene.ResourceDescriptorCount);
        ReportCounter("Silk.Scene.SamplerDescriptors", scene.SamplerDescriptorCount);
        ReportCounter("Silk.Effects.HeapPages", effects.PageCount);
        ReportCounter("Silk.Effects.ResourceDescriptors", effects.ResourceDescriptorCount);
        ReportCounter("Silk.Effects.SamplerDescriptors", effects.SamplerDescriptorCount);
        ReportCounter("Silk.Shared.ResourceDescriptors", resources.Count);
        ReportCounter("Silk.Shared.SamplerDescriptors", samplers.Count);
    }

    /// <summary>
    ///     Calls the stable ANSI command-list begin ABI, which also emits an implicit CPU region.
    /// </summary>
    /// <param name="commandList">The native open command list.</param>
    /// <param name="color">The opaque ARGB region color.</param>
    /// <param name="name">The literal region label; percent formatting is not used.</param>
    [DllImport("WinPixEventRuntime.dll", EntryPoint = "PIXBeginEventOnCommandList", ExactSpelling = true,
        CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    private static extern void BeginEvent(nint commandList, ulong color, [MarshalAs(UnmanagedType.LPStr)] string name);

    /// <summary>
    ///     Closes the native command-list region and its implicit CPU region.
    /// </summary>
    /// <param name="commandList">The same command list on the same recording thread.</param>
    [DllImport("WinPixEventRuntime.dll", EntryPoint = "PIXEndEventOnCommandList", ExactSpelling = true)]
    private static extern void EndEvent(nint commandList);

    /// <summary>
    ///     Reports one named value to PIX timing captures and System Monitor.
    /// </summary>
    /// <param name="name">The UTF-16 counter name.</param>
    /// <param name="value">The actual current occupancy.</param>
    [DllImport("WinPixEventRuntime.dll", EntryPoint = "PIXReportCounter", ExactSpelling = true, CharSet = CharSet.Unicode)]
    private static extern void ReportCounter([MarshalAs(UnmanagedType.LPWStr)] string name, float value);

    /// <summary>
    ///     Balances an optional native region without a managed allocation.
    /// </summary>
    internal readonly ref struct Region {
        /// The originating command list, or zero when profiling is disabled.
        private readonly nint _commandList;

        /// <summary>
        ///     Retains the open command list until the lexical region ends.
        /// </summary>
        /// <param name="commandList">The command list on the current recording thread.</param>
        internal Region(nint commandList) => _commandList = commandList;

        /// Closes the region before its owning command list can be submitted or disposed.
        public void Dispose() {
            if (_commandList != 0) EndEvent(_commandList);
        }
    }
}
