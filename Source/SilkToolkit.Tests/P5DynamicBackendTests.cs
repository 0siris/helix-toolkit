using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX.Core;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Silk.NET.Direct3D12;
using Xunit;
using BlendOption = HelixToolkit.SharpDX.Core.Native.BlendOption;
using Vector3 = Silk.NET.Maths.Vector3D<float>;
using Vector4 = Silk.NET.Maths.Vector4D<float>;

namespace SilkToolkit.Tests;

/// <summary>Checks dynamic GPU work and transparent overlap through both shipped presentation backends.</summary>
[Collection(WpfCollection.Name)]
public sealed class P5DynamicBackendTests {
    /// <summary>Proves native particle insertion, simulation and drawing before source removal or hiding restores idle policy.</summary>
    /// <param name="hwnd">Whether to select the historically continuous HWND backend.</param>
    /// <param name="hide">Whether to hide the particle element instead of removing it from the scene.</param>
    /// <returns>The dispatcher-owned productive GPU and transport verification.</returns>
    [Theory(Explicit = true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task ParticleGpuProgressStopsRequestingFramesWhenSourceLeaves(bool hwnd, bool hide) =>
        StaThread.RunAsync(async () => {
            using var effects = new DefaultEffectsManager();
            await using var driver = new P4PresentationDriver(hwnd, effects);
            using var mesh = P4PresentationDriver.CreateMesh();
            using var particles = new ParticleStormModel3D {
                ParticleCount = 256,
                InitialEnergy = 100,
                EnergyDissipationRate = 1,
                InitialVelocity = 0,
                Acceleration = new Vector3D(),
                EmitterRadius = 0,
                EmitterLocation = new Point3D(0.7, 0, 0.2),
                ParticleSize = new Size(0.2, 0.2),
                BlendColor = Colors.Red,
                SourceBlend = BlendOption.One,
                DestBlend = BlendOption.Zero
            };
            var core = Assert.IsType<ParticleRenderCore>(particles.SceneNode.RenderCore);
            core.ExtraAcceleration = Vector3.Zero;
            driver.Viewport.Items.Add(mesh);
            await driver.WarmAsync();
            P4PresentationDriver.AssertPixel(await driver.CaptureAsync(), 54, 24, 255, 0, 0, 255);

            driver.Viewport.Items.Add(particles);
            var time = TimeSpan.FromSeconds(2);
            Assert.Equal(2, driver.Viewport.RenderD3D12Frame(time, waitForCompletion: true));
            await driver.FinishTransportAsync();
            var first = ReadParticleSnapshot(driver, core);
            Assert.Equal(8u, first.Count);
            Assert.InRange(first.MinimumEnergy, 99.99f, 100.01f);

            // The productive recorder uses real stopwatch time for simulation, independently of camera time.
            // Wait for a real GPU energy change instead of accepting a CPU demand flag as simulation evidence.
            var started = Stopwatch.GetTimestamp();
            var progressed = false;
            while (!progressed && Stopwatch.GetElapsedTime(started) < P4PresentationDriver.Timeout) {
                await Task.Delay(25);
                time += TimeSpan.FromMilliseconds(125);
                Assert.Equal(2, driver.Viewport.RenderD3D12Frame(time, waitForCompletion: true));
                await driver.FinishTransportAsync();
                var next = ReadParticleSnapshot(driver, core);
                Assert.InRange(next.Count, first.Count, 256u);
                progressed = next.MinimumEnergy < first.MinimumEnergy - 0.01f;
            }

            Assert.True(progressed, "The actual GPU particle energy did not decay across productive frames.");
            var before = driver.Counts;
            var copied = driver.Wpf?.CopiedFrames ?? 0;
            var published = driver.Wpf?.PublishedFrames ?? 0;
            for (var index = 0; index < 4; index++) {
                time += TimeSpan.FromMilliseconds(125);
                Assert.Equal(2, driver.Viewport.RenderD3D12Frame(time, waitForCompletion: true));
                await driver.FinishTransportAsync();
            }

            driver.AssertAdvanced(before, 4);
            if (!hwnd) {
                Assert.Equal(copied + 4, driver.Wpf!.CopiedFrames);
                Assert.Equal(published + 4, driver.Wpf.PublishedFrames);
            }

            var active = await driver.CaptureAsync();
            Assert.InRange((int) active.Pixels[(24 * 64 + 54) * 4 + 2], 200, 255);
            P4PresentationDriver.AssertPixel(active, 32, 24, 0, 255, 0, 255);
            if (hide) particles.Visibility = Visibility.Hidden;
            else driver.Viewport.Items.Remove(particles);
            await driver.DrainAsync();
            var inactive = await driver.CaptureAsync();
            P4PresentationDriver.AssertPixel(inactive, 54, 24, 255, 0, 0, 255);
            P4PresentationDriver.AssertPixel(inactive, 32, 24, 0, 255, 0, 255);
            await driver.DrainAsync();
            before = driver.Counts;
            copied = driver.Wpf?.CopiedFrames ?? 0;
            published = driver.Wpf?.PublishedFrames ?? 0;
            for (var index = 0; index < 16; index++) {
                time += TimeSpan.FromMilliseconds(125);
                Assert.Equal(hwnd ? 1 : 0, driver.Viewport.RenderD3D12Frame(time, waitForCompletion: true));
                await driver.FinishTransportAsync();
            }

            driver.AssertAdvanced(before, hwnd ? 16 : 0);
            if (!hwnd) {
                Assert.Equal(copied, driver.Wpf!.CopiedFrames);
                Assert.Equal(published, driver.Wpf.PublishedFrames);
                Assert.False(driver.Viewport.RenderDemand.HasRenderDemand);
            }
        });

    /// <summary>Checks the existing overlapping-layer coverage equation over opaque blue with actual backend captures.</summary>
    /// <param name="transparency">The ordinary, weighted or peeling transparency mode.</param>
    /// <param name="fxaa">Whether to run productive FXAA after transparency.</param>
    /// <returns>The matched native readback verification, excluding physical desktop composition.</returns>
    [Theory(Explicit = true)]
    [InlineData(OITRenderType.None, false)]
    [InlineData(OITRenderType.None, true)]
    [InlineData(OITRenderType.SinglePassWeighted, false)]
    [InlineData(OITRenderType.SinglePassWeighted, true)]
    [InlineData(OITRenderType.DepthPeeling, false)]
    [InlineData(OITRenderType.DepthPeeling, true)]
    [Trait("Category", "Hardware")]
    [Trait("Category", "DX12")]
    public Task TransparentOverlapMatchesCoverageAcrossBackends(OITRenderType transparency, bool fxaa) =>
        StaThread.RunAsync(async () => {
            var captures = new List<D3D12CapturedFrame>();
            foreach (var hwnd in new[] { false, true }) {
                using var effects = new DefaultEffectsManager();
                await using var driver = new P4PresentationDriver(hwnd, effects);
                using var back = P4PresentationDriver.CreateMesh();
                using var front = P4PresentationDriver.CreateMesh();
                back.IsTransparent = true;
                front.IsTransparent = true;
                front.Transform = new TranslateTransform3D(0, 0, 0.2);
                driver.Viewport.OitRenderMode = transparency;
                driver.Viewport.OitDepthPeelingIteration = 4;
                driver.Viewport.FxaaLevel = fxaa ? FXAALevel.High : FXAALevel.None;
                driver.Viewport.Items.Add(back);
                driver.Viewport.Items.Add(front);
                await driver.WarmAsync();
                var sample = 0;
                foreach (var alpha in new[] { 0f, 0.5f, 1f }) {
                    P4PresentationDriver.SetMeshColor(back, new Vector4(1, 0, 0, alpha));
                    P4PresentationDriver.SetMeshColor(front, new Vector4(1, 0, 0, alpha));
                    await driver.DrainAsync();
                    var before = driver.Counts;
                    var captured = await driver.CaptureAsync();
                    driver.AssertAdvanced(before, 1);
                    Assert.Equal(64u, captured.Width);
                    Assert.Equal(48u, captured.Height);
                    // NativePremultipliedOutputTests: coverage = 1 - (1 - alpha)^2.
                    // Opaque blue turns that coverage into red, leaving the complementary blue contribution.
                    var red = (int) MathF.Round((1 - (1 - alpha) * (1 - alpha)) * 255);
                    for (var y = 20; y < 28; y++)
                        for (var x = 28; x < 36; x++) {
                            P4PresentationDriver.AssertPixel(captured, x, y, 255 - red, 0, red, 255, 4);
                            if (hwnd) {
                                var offset = (y * 64 + x) * 4;
                                for (var channel = 0; channel < 4; channel++)
                                    Assert.InRange(Math.Abs(captured.Pixels[offset + channel] -
                                        captures[sample].Pixels[offset + channel]), 0, 4);
                            }
                        }

                    P4PresentationDriver.AssertPixel(captured, 2, 2, 255, 0, 0, 255);
                    if (!hwnd) captures.Add(captured);
                    sample++;
                }
            }
        });

    /// <summary>Reads fence-completed GPU counters, indirect arguments and particle energies from the productive owner.</summary>
    /// <param name="driver">The initialized presentation owner with no outstanding scene or transport work.</param>
    /// <param name="core">The actual particle core attached by that viewport.</param>
    /// <returns>The GPU particle count and minimum live energy, independent of append ordering.</returns>
    private static (uint Count, float MinimumEnergy) ReadParticleSnapshot(
        P4PresentationDriver driver, ParticleRenderCore core) {
        var owner = (object?) driver.Wpf ?? driver.Native!;
        var device = P4PresentationDriver.ReadField<SilkD3D12Device>(owner, "device");
        var queue = P4PresentationDriver.ReadField<SilkD3D12CommandQueue>(
            owner, driver.Wpf is null ? "commandQueue" : "queue");
        var renderer = driver.Native?.FrameRenderer ??
            P4PresentationDriver.ReadField<D3D12FrameRenderer>(owner, "renderer");
        var resources = Assert.IsType<SilkD3D12ParticleResources>(renderer.SceneRenderer.FindParticleResources(core));
        using var commands = device.CreateCommandContext();
        using var fence = device.CreateFence();
        using var counter = device.CreateBuffer(sizeof(uint), HeapType.Readback);
        using var arguments = device.CreateBuffer(ParticleCountIndirectArgs.SizeInBytes, HeapType.Readback);
        var stateBytes = checked((int) resources.Capacity * Particle.SizeInBytes);
        using var state = device.CreateBuffer((ulong) stateBytes, HeapType.Readback);
        var counterState = resources.RenderCounter.State;
        var argumentState = resources.Arguments.State;
        var simulationState = resources.RenderState.State;
        commands.Reset();
        commands.Transition(resources.RenderCounter, ResourceStates.CopySource);
        commands.CopyBuffer(counter, 0, resources.RenderCounter, 0, sizeof(uint));
        commands.Transition(resources.Arguments, ResourceStates.CopySource);
        commands.CopyBuffer(arguments, 0, resources.Arguments, 0, ParticleCountIndirectArgs.SizeInBytes);
        commands.Transition(resources.RenderState, ResourceStates.CopySource);
        commands.CopyBuffer(state, 0, resources.RenderState, 0, (ulong) stateBytes);
        commands.Transition(resources.RenderCounter, counterState);
        commands.Transition(resources.Arguments, argumentState);
        commands.Transition(resources.RenderState, simulationState);
        commands.Close();
        queue.Execute(commands);
        fence.Wait(queue.Signal(fence), P4PresentationDriver.Timeout);
        device.ThrowIfDeviceRemoved();
        var count = MemoryMarshal.Read<uint>(counter.Read(sizeof(uint)));
        var draw = MemoryMarshal.Read<ParticleCountIndirectArgs>(arguments.Read(ParticleCountIndirectArgs.SizeInBytes));
        Assert.InRange(count, 1u, resources.Capacity);
        Assert.Equal(count, draw.VertexCount);
        Assert.Equal(1u, draw.InstanceCount);
        Assert.Equal(0u, draw.StartVertexLocation);
        Assert.Equal(0u, draw.StartInstanceLocation);
        var bytes = state.Read(stateBytes);
        var minimumEnergy = float.PositiveInfinity;
        for (var index = 0; index < count; index++) {
            // The shipped Particle/Particle HLSL layout stores energy after position, initial energy and velocity.
            var energy = MemoryMarshal.Read<float>(bytes.AsSpan(index * Particle.SizeInBytes + 7 * sizeof(float)));
            Assert.True(float.IsFinite(energy) && energy > 0);
            minimumEnergy = Math.Min(minimumEnergy, energy);
        }

        return (count, minimumEnergy);
    }
}
