using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Input;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.Wpf.SharpDX;
using Silk.NET.Direct3D12;
using Xunit;
using DiffuseMaterial = HelixToolkit.Wpf.SharpDX.DiffuseMaterial;
using Luid = Silk.NET.Core.Native.Luid;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Compares actual matched presentation backends without adding product profiling hooks.</summary>
/// <param name="output">Receives diagnostic timings, memory scope and strict transport counters.</param>
[Collection(WpfCollection.Name)]
public sealed class P5PresentationComparisonTests(ITestOutputHelper output) {
    /// <summary>Measures identical opaque mesh commands, caller CPU cost and request-to-publication latency.</summary>
    /// <returns>The serial hardware comparison using the production template and real timestamp queries.</returns>
    [Fact(Explicit = true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task MatchedBackendsExposeGpuDrawAndPublicationCosts() => StaThread.RunAsync(async () => {
            const int warmup = 32;
            const int samples = 64;
            long? adapterLuid = null;
            foreach (var hwnd in new[] { false, true }) {
                using var effects = new DefaultEffectsManager();
                await using var driver = new P4PresentationDriver(hwnd, effects);
                driver.Viewport.Width = 640;
                driver.Viewport.Height = 480;
                using var mesh = CreateMesh();
                driver.Viewport.Items.Add(mesh);
                driver.Root.UpdateLayout();
                await driver.WarmAsync();
                Assert.Equal(640f, driver.Context!.ActualWidth);
                Assert.Equal(480f, driver.Context.ActualHeight);
                var owner = (object?) driver.Wpf ?? driver.Native!;
                var device = P4PresentationDriver.ReadField<SilkD3D12Device>(owner, "device");
                var queue = P4PresentationDriver.ReadField<SilkD3D12CommandQueue>(
                    owner, hwnd
                        ? "commandQueue"
                        : "queue"
                );
                using var timestamps = new DrawTimestamps(device, queue);
                if (adapterLuid is { } expectedAdapter) Assert.Equal(expectedAdapter, timestamps.AdapterLuid);
                adapterLuid = timestamps.AdapterLuid;
                var core = Assert.IsType<TimestampedMeshCore>(mesh.SceneNode.RenderCore);
                core.Timestamps = timestamps;
                try {
                    for (var index = 0; index < warmup; index++) {
                        driver.Viewport.InvalidateRender();
                        _ = driver.Tick(wait: true);
                        await driver.FinishTransportAsync();
                        Assert.True(timestamps.ReadMilliseconds() >= 0);
                    }

                    using var process = Process.GetCurrentProcess();
                    process.Refresh();
                    var memoryBefore = process.PrivateMemorySize64;
                    var managedBefore = GC.GetTotalMemory(false);
                    var cpuBefore = process.TotalProcessorTime;
                    var before = driver.Counts;
                    var copies = driver.Wpf?.CopiedFrames ?? 0;
                    var publications = driver.Wpf?.PublishedFrames ?? 0;
                    var cpu = new double[samples];
                    var gpu = new double[samples];
                    var latency = new double[samples];
                    long allocated = 0;
                    var clockStarted = Stopwatch.GetTimestamp();
                    for (var index = 0; index < samples; index++) {
                        var requestStarted = Stopwatch.GetTimestamp();
                        driver.Viewport.InvalidateRender();
                        var allocationStarted = GC.GetAllocatedBytesForCurrentThread();
                        var recordStarted = Stopwatch.GetTimestamp();
                        var draws = driver.Tick(wait: true);
                        cpu[index] = Stopwatch.GetElapsedTime(recordStarted)
                            .TotalMilliseconds;
                        allocated += GC.GetAllocatedBytesForCurrentThread() - allocationStarted;
                        Assert.Equal(1, draws);
                        await driver.FinishTransportAsync();
                        latency[index] = Stopwatch.GetElapsedTime(requestStarted)
                            .TotalMilliseconds;
                        gpu[index] = timestamps.ReadMilliseconds();
                    }

                    var elapsed = Stopwatch.GetElapsedTime(clockStarted);
                    var processCpu = process.TotalProcessorTime - cpuBefore;
                    process.Refresh();
                    var memoryAfter = process.PrivateMemorySize64;
                    var managedAfter = GC.GetTotalMemory(false);
                    var copyDelta = (driver.Wpf?.CopiedFrames ?? 0) - copies;
                    var publicationDelta = (driver.Wpf?.PublishedFrames ?? 0) - publications;
                    driver.AssertAdvanced(before, samples);
                    if (!hwnd) {
                        Assert.Equal(copies + samples, driver.Wpf!.CopiedFrames);
                        Assert.Equal(publications + samples, driver.Wpf.PublishedFrames);
                    }

                    var captured = await driver.CaptureAsync();
                    Assert.Equal(640u, captured.Width);
                    Assert.Equal(480u, captured.Height);
                    P4PresentationDriver.AssertPixel(captured, 320, 240, 0, 255, 0, 255);
                    output.WriteLine(
                        $"P5 matched backend={(hwnd ? "HWND" : "WPF")}; resolution=640x480; warmup={warmup}; samples={samples}; " +
                        $"caller-wall-p50-ms={Percentile(cpu, .5):F6}; caller-wall-p95-ms={Percentile(cpu, .95):F6}; " +
                        $"GPU-mesh-p50-ms={Percentile(gpu, .5):F6}; GPU-mesh-p95-ms={Percentile(gpu, .95):F6}; " +
                        $"request-to-output-p50-ms={Percentile(latency, .5):F6}; request-to-output-p95-ms={Percentile(latency, .95):F6}; " +
                        $"process-CPU-ms={processCpu.TotalMilliseconds:F3}; wall-ms={elapsed.TotalMilliseconds:F3}; " +
                        $"record-bytes-per-frame={(double) allocated / samples:F2}; private-bytes-before={memoryBefore}; " +
                        $"private-bytes-after={memoryAfter}; managed-bytes-before={managedBefore}; managed-bytes-after={managedAfter}; " +
                        $"extra-cross-device-copies={copyDelta}; new-publications={publicationDelta}; " +
                        $"GPU-timestamp-Hz={timestamps.Frequency}; draw-query-pairs={timestamps.Pairs}."
                    );
                    output.WriteLine($"P5 adapter-LUID=0x{timestamps.AdapterLuid:X16}; requested-DXGI-index={effects.AdapterIndex}.");
                    output.WriteLine(
                        "GPU timestamps cover this mesh's native bindings/draw commands, excluding clear, FXAA, D3D11 copy, DWM and scanout. " +
                        "Caller wall time includes explicit fence/interop waits; process CPU time is the CPU-work metric. " +
                        "Request-to-output ends at accepted HWND Present or WPF publication, not physical scanout. " +
                        "Memory is process-wide diagnostic state, not isolated VRAM. Timing has no machine-specific pass threshold."
                    );
                } finally {
                    core.Timestamps = null;
                    await driver.FinishTransportAsync();
                }
            }
        }
    );

    /// <summary>Proves productive continuous requests and exact settled counters after their source is removed.</summary>
    /// <param name="hwnd">Whether to compare the historically continuous HWND backend.</param>
    /// <returns>The actual submission, completion and WPF copy/publication policy verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ContinuousSourceRemovalRestoresTheSelectedIdlePolicy(bool hwnd) => StaThread.RunAsync(async () => {
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            using var continuous = new ContinuousRender3D();
            driver.Viewport.Items.Add(mesh);
            await driver.WarmAsync();
            driver.Viewport.Items.Add(continuous);
            var before = driver.Counts;
            for (var index = 0; index < 32; index++) {
                Assert.True(driver.Tick(wait: true) > 0);
                await driver.FinishTransportAsync();
            }

            driver.AssertAdvanced(before, 32);
            Assert.True(driver.Viewport.RenderDemand.HasRenderDemand);
            driver.Viewport.Items.Remove(continuous);
            await driver.DrainAsync();
            before = driver.Counts;
            var copies = driver.Wpf?.CopiedFrames ?? 0;
            var publications = driver.Wpf?.PublishedFrames ?? 0;
            for (var index = 0; index < 64; index++) {
                _ = driver.Tick(wait: true);
                await driver.FinishTransportAsync();
            }

            driver.AssertAdvanced(
                before, hwnd
                    ? 64
                    : 0
            );
            if (!hwnd) {
                Assert.Equal(copies, driver.Wpf!.CopiedFrames);
                Assert.Equal(publications, driver.Wpf.PublishedFrames);
                Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            }

            output.WriteLine(
                $"P5 backend={(hwnd ? "HWND" : "WPF")}: continuous=32 accepted/completed frames; " +
                $"after-removal/64-pulses={driver.Submitted - before.Submitted}; " +
                $"new-WPF-copies={(driver.Wpf?.CopiedFrames ?? 0) - copies}; new-WPF-publications={(driver.Wpf?.PublishedFrames ?? 0) - publications}."
            );
        }
    );

    /// <summary>Times genuine WPF routed wheel handling through camera mutation and accepted presentation.</summary>
    /// <param name="hwnd">Whether to select the HWND output for the same managed input route.</param>
    /// <returns>The dispatcher-owned input-to-output measurement, excluding OS delivery and physical scanout.</returns>
    [Theory(Explicit = true)]
    [InlineData(false)]
    [InlineData(true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task RoutedWheelChangesCameraBeforeMeasuredOutput(bool hwnd) => StaThread.RunAsync(async () => {
        using var effects = new DefaultEffectsManager();
        await using var driver = new P4PresentationDriver(hwnd, effects);
        using var mesh = P4PresentationDriver.CreateMesh();
        driver.Viewport.Items.Add(mesh);
        driver.Viewport.IsInertiaEnabled = false;
        driver.Viewport.ZoomAroundMouseDownPoint = false;
        await driver.WarmAsync();
        var latency = new double[64];
        var before = driver.Counts;
        for (var index = 0; index < latency.Length; index++) {
            var wheel = new MouseWheelEventArgs(Mouse.PrimaryDevice, index + 1, index % 2 == 0 ? 120 : -120) {
                RoutedEvent = Mouse.MouseWheelEvent
            };
            var width = driver.Camera.Width;
            var start = Stopwatch.GetTimestamp();
            driver.Viewport.RaiseEvent(wheel);
            Assert.True(wheel.Handled);
            Assert.NotEqual(width, driver.Camera.Width);
            Assert.True(driver.Tick(wait: true) > 0);
            await driver.FinishTransportAsync();
            latency[index] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }
        driver.AssertAdvanced(before, latency.Length);
        P4PresentationDriver.AssertPixel(await driver.CaptureAsync(), 32, 24, 0, 255, 0, 255);
        output.WriteLine($"P5 routed-wheel backend={(hwnd ? "HWND" : "WPF")}; samples={latency.Length}; " +
            $"input-to-output-p50-ms={Percentile(latency, .5):F6}; input-to-output-p95-ms={Percentile(latency, .95):F6}. " +
            "Starts at managed routed-event dispatch; includes camera change and accepted output, not OS delivery or scanout.");
    });

    /// <summary>Reads a diagnostic percentile from retained sample storage outside the measured region.</summary>
    /// <param name="samples">The finite timing samples.</param>
    /// <param name="fraction">The requested percentile fraction.</param>
    /// <returns>The nearest-rank timing sample.</returns>
    private static double Percentile(double[] samples, double fraction) {
        Array.Sort(samples);
        return samples[Math.Clamp((int) Math.Ceiling(samples.Length * fraction) - 1, 0, samples.Length - 1)];
    }

    /// <summary>Creates the same ordinary filled mesh on both presentation backends.</summary>
    /// <returns>The application-owned mesh with a test-only timestamping core.</returns>
    private static TimestampedMeshModel CreateMesh() => new() {
        Material = new DiffuseMaterial { DiffuseColor = new Vector4(0, 1, 0, 1) },
        Geometry = new MeshGeometry3D {
            Positions = [new(-.5f, .5f, 0), new(.5f, .5f, 0), new(-.5f, -.5f, 0), new(.5f, -.5f, 0)],
            Normals = [Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ, Vector3.UnitZ],
            Indices = [0, 1, 2, 2, 1, 3]
        }
    };

    /// <summary>Supplies an otherwise ordinary native mesh with scoped test-only GPU timestamps.</summary>
    private sealed class TimestampedMeshModel : MeshGeometryModel3D {
        /// <inheritdoc />
        protected override SceneNode OnCreateSceneNode() => new TimestampedMeshNode();
    }

    /// <summary>Retains the normal mesh attachment, material and geometry resource lifecycle.</summary>
    private sealed class TimestampedMeshNode : MeshNode {
        /// <inheritdoc />
        protected override RenderCore OnCreateRenderCore() => new TimestampedMeshCore();
    }

    /// <summary>Brackets the original productive mesh commands without replacing their implementation.</summary>
    private sealed class TimestampedMeshCore : MeshRenderCore {
        /// <summary>Gets or sets the test-only timestamp owner for the current retained native device.</summary>
        internal DrawTimestamps? Timestamps { get; set; }

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
            Timestamps?.Begin(context);
            var result = base.TryRenderD3D12(context, resources, pass, bindings, in transforms, lights, environmentMap);
            Timestamps?.End(context);
            return result;
        }
    }

    /// <summary>Owns two real timestamp queries and a readback allocation on the productive renderer's device.</summary>
    private sealed class DrawTimestamps : IDisposable {
        /// <summary>Owns the native timestamp query heap until all recorded uses have completed.</summary>
        private nint heap;

        /// <summary>Owns two resolved 64-bit timestamps.</summary>
        private readonly SilkD3D12Resource readback;
        /// <summary>Borrows the queue until all timestamp resources have been drained and released.</summary>
        private readonly SilkD3D12CommandQueue queue;
        /// <summary>Owns an independent final drain fence for test resources even if an assertion fails.</summary>
        private readonly SilkD3D12Fence drainFence;

        /// <summary>Retains the native end-query entry point outside measured calls.</summary>
        private EndQuery? endQuery;

        /// <summary>Retains the native query resolution entry point outside measured calls.</summary>
        private ResolveQuery? resolve;

        /// <summary>Creates native timestamp resources and obtains the actual queue's timestamp frequency.</summary>
        /// <param name="device">The exact borrowed productive D3D12 device.</param>
        /// <param name="queue">The exact productive D3D12 queue.</param>
        internal DrawTimestamps(SilkD3D12Device device, SilkD3D12CommandQueue queue) {
            this.queue = queue;
            var desc = new QueryHeapDesc { Type = QueryHeapType.Timestamp, Count = 2 };
            var id = ID3D12QueryHeap.Guid;
            Marshal.ThrowExceptionForHR(Method<CreateQueryHeap>(device.NativePointer, 39)(device.NativePointer, in desc, in id, out heap));
            try {
                Marshal.ThrowExceptionForHR(Method<GetFrequency>(queue.NativePointer, 16)(queue.NativePointer, out var frequency));
                Assert.True(frequency > 0);
                Frequency = frequency;
                _ = Method<GetLuid>(device.NativePointer, 43)(device.NativePointer, out var luid);
                AdapterLuid = unchecked((long) ((ulong) (uint) luid.High << 32 | luid.Low));
                readback = device.CreateBuffer(16, HeapType.Readback);
                drainFence = device.CreateFence();
            } catch {
                readback?.Dispose();
                drainFence?.Dispose();
                Marshal.Release(heap);
                heap = 0;
                throw;
            }
        }

        /// <summary>Gets the actual productive queue's timestamp ticks per second.</summary>
        internal ulong Frequency { get; }
        /// <summary>Gets the exact productive device's adapter identity for matched-backend comparison.</summary>
        internal long AdapterLuid { get; }

        /// <summary>Gets the number of original mesh command ranges measured.</summary>
        internal int Pairs { get; private set; }

        /// <summary>Appends the first timestamp immediately before the normal mesh commands.</summary>
        /// <param name="context">The actual open productive command list.</param>
        internal void Begin(SilkD3D12CommandContext context) {
            endQuery ??= Method<EndQuery>(context.CommandListPointer, 53);
            resolve ??= Method<ResolveQuery>(context.CommandListPointer, 54);
            endQuery(context.CommandListPointer, heap, QueryType.Timestamp, 0);
        }

        /// <summary>Appends the second timestamp and resolves both values after the normal mesh commands.</summary>
        /// <param name="context">The same actual productive command list.</param>
        internal void End(SilkD3D12CommandContext context) {
            endQuery!(context.CommandListPointer, heap, QueryType.Timestamp, 1);
            resolve!(context.CommandListPointer, heap, QueryType.Timestamp, 0, 2, readback.NativePointer, 0);
            Pairs++;
        }

        /// <summary>Reads timestamp results only after the presentation's actual scene fence has completed.</summary>
        /// <returns>GPU elapsed time for the original mesh commands, in milliseconds.</returns>
        internal double ReadMilliseconds() {
            var values = readback.Read(16);
            var first = BinaryPrimitives.ReadUInt64LittleEndian(values);
            var last = BinaryPrimitives.ReadUInt64LittleEndian(values.AsSpan(8));
            Assert.True(last >= first);
            return (last - first) * 1000.0 / Frequency;
        }

        /// <summary>Releases only test-owned query resources after the caller has drained productive work.</summary>
        public void Dispose() {
            if (heap == 0) return;
            drainFence.Wait(queue.Signal(drainFence), P4PresentationDriver.Timeout);
            drainFence.Dispose();
            readback.Dispose();
            Marshal.Release(heap);
            heap = 0;
        }

        /// <summary>Resolves one stable COM ABI method outside measured frame recording.</summary>
        /// <typeparam name="T">The exact native delegate signature.</typeparam>
        /// <param name="instance">The native interface pointer.</param>
        /// <param name="slot">The stable vtable slot.</param>
        /// <returns>The native entry point.</returns>
        private static T Method<T>(nint instance, int slot) where T : Delegate =>
            Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * nint.Size));

        /// <summary>Creates the native timestamp query heap.</summary>
        /// <param name="device">The actual D3D12 device.</param>
        /// <param name="description">The timestamp heap description.</param>
        /// <param name="id">The query heap interface identity.</param>
        /// <param name="result">Receives the owned heap.</param>
        /// <returns>The HRESULT.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateQueryHeap(nint device, in QueryHeapDesc description, in Guid id, out nint result);

        /// <summary>Reads timestamp frequency from the native queue.</summary>
        /// <param name="queue">The productive queue.</param>
        /// <param name="frequency">Receives ticks per second.</param>
        /// <returns>The HRESULT.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetFrequency(nint queue, out ulong frequency);
        /// <summary>Reads the adapter identity through the exact Silk 2.23 native struct-return ABI.</summary>
        /// <param name="device">The productive device.</param>
        /// <param name="luid">Receives the adapter identity.</param>
        /// <returns>The native result-buffer address.</returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate nint GetLuid(nint device, out Luid luid);

        /// <summary>Writes one timestamp into the native query heap.</summary>
        /// <param name="commands">The actual command list.</param>
        /// <param name="queryHeap">The owned heap.</param>
        /// <param name="kind">The timestamp query type.</param>
        /// <param name="index">The query slot.</param>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void EndQuery(nint commands, nint queryHeap, QueryType kind, uint index);

        /// <summary>Resolves native query results into a real readback resource.</summary>
        /// <param name="commands">The actual command list.</param>
        /// <param name="queryHeap">The owned heap.</param>
        /// <param name="kind">The timestamp query type.</param>
        /// <param name="start">The first slot.</param>
        /// <param name="count">The number of slots.</param>
        /// <param name="destination">The readback allocation.</param>
        /// <param name="offset">The byte offset.</param>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void ResolveQuery(
            nint commands,
            nint queryHeap,
            QueryType kind,
            uint start,
            uint count,
            nint destination,
            ulong offset
        );
    }
}
