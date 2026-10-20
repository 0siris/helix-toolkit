using HelixToolkit.Wpf.SharpDX.Controls;
using Xunit;

namespace SilkToolkit.Tests;

/// <summary>Verifies demand budgets, reservation races, independent presentation work, and warmed allocations.</summary>
public sealed class D3D12RenderDemandTests {
    /// <summary>Verifies construction requests six total frames and scene-graph preparation only on the first.</summary>
    [Fact]
    public void InitialDemandContainsSixTotalFrames() {
        var demand = new D3D12RenderDemand();

        Assert.Equal(6, demand.MinimumUpdateCount);
        Assert.True(demand.TryBeginFrame(out var invalidation));
        Assert.Equal(D3D12RenderInvalidation.SceneGraph | D3D12RenderInvalidation.Render, invalidation);
        demand.CompleteFrame(submitted: true);
        Assert.True(demand.TryBeginFrame(out invalidation));
        Assert.Equal(D3D12RenderInvalidation.Render, invalidation);
        demand.CompleteFrame(submitted: true);

        Assert.Equal(4, SubmitRemainingFrames(demand));
        Assert.False(demand.HasRenderDemand);
        Assert.False(demand.TryBeginFrame(out invalidation));
        Assert.Equal(D3D12RenderInvalidation.None, invalidation);
    }

    /// <summary>Verifies initial and isolated requests count submitted frames with historical zero/negative clamping.</summary>
    /// <param name="minimum">The configured frame budget.</param>
    /// <param name="stored">The expected nonnegative setting.</param>
    /// <param name="frames">The total frames for each isolated request.</param>
    [Theory]
    [InlineData(6, 6, 6)]
    [InlineData(1, 1, 1)]
    [InlineData(0, 0, 1)]
    [InlineData(-3, 0, 1)]
    [InlineData(int.MinValue, 0, 1)]
    public void IsolatedRequestUsesTotalSubmittedFrameBudget(int minimum, int stored, int frames) {
        var demand = new D3D12RenderDemand(minimum);

        Assert.Equal(stored, demand.MinimumUpdateCount);
        Assert.Equal(frames, SubmitRemainingFrames(demand));
        demand.Request(D3D12RenderInvalidation.Render);
        Assert.Equal(frames, SubmitRemainingFrames(demand));
        demand.Request(D3D12RenderInvalidation.None);
        Assert.False(demand.HasRenderDemand);
    }

    /// <summary>Verifies changing the setting preserves current demand and only affects subsequent requests.</summary>
    [Fact]
    public void RuntimeSettingChangePreservesExistingBudgetAndDoesNotWakeIdleScene() {
        var demand = new D3D12RenderDemand();
        Assert.True(demand.TryBeginFrame(out _));
        demand.MinimumUpdateCount = 1;
        demand.CompleteFrame(submitted: true);
        Assert.Equal(5, SubmitRemainingFrames(demand));

        demand.MinimumUpdateCount = 3;
        Assert.False(demand.HasRenderDemand);
        demand.Request(D3D12RenderInvalidation.Render);
        Assert.Equal(3, SubmitRemainingFrames(demand));

        demand.MinimumUpdateCount = -8;
        Assert.Equal(0, demand.MinimumUpdateCount);
        Assert.False(demand.HasRenderDemand);
        demand.Request(D3D12RenderInvalidation.Render);
        Assert.Equal(1, SubmitRemainingFrames(demand));
    }

    /// <summary>Verifies a setting change alone cannot stop cancellation from refunding its original reservation.</summary>
    [Fact]
    public void RuntimeSettingChangeDoesNotReplaceCanceledReservationBudget() {
        var demand = new D3D12RenderDemand();
        Assert.True(demand.TryBeginFrame(out _));
        demand.MinimumUpdateCount = 0;
        demand.CompleteFrame(submitted: false);

        Assert.Equal(6, SubmitRemainingFrames(demand));
        demand.Request(D3D12RenderInvalidation.Render);
        Assert.Equal(1, SubmitRemainingFrames(demand));
    }

    /// <summary>Verifies a new request uses the latest setting without an extra cancellation refund.</summary>
    [Fact]
    public void RuntimeSettingChangeAppliesToNewRequestDuringCanceledFrame() {
        var demand = new D3D12RenderDemand();
        Assert.True(demand.TryBeginFrame(out _));
        demand.MinimumUpdateCount = 1;
        demand.Request(D3D12RenderInvalidation.PerFrameRenderables);
        demand.CompleteFrame(submitted: false);

        Assert.True(demand.TryBeginFrame(out var invalidation));
        Assert.Equal(
            D3D12RenderInvalidation.Render | D3D12RenderInvalidation.SceneGraph |
            D3D12RenderInvalidation.PerFrameRenderables, invalidation
        );
        demand.CompleteFrame(submitted: true);
        Assert.False(demand.HasRenderDemand);
    }

    /// <summary>Verifies requests coalesce as bit flags rather than allowing lower priority work to erase higher bits.</summary>
    /// <param name="first">The first invalidation bit.</param>
    /// <param name="second">The second invalidation bit.</param>
    [Theory]
    [InlineData(4, 1)]
    [InlineData(1, 4)]
    [InlineData(2, 1)]
    [InlineData(4, 2)]
    [InlineData(2, 4)]
    [InlineData(6, 1)]
    public void CoalescedRequestsRetainEveryPreparationBit(int first, int second) {
        var demand = new D3D12RenderDemand();
        _ = SubmitRemainingFrames(demand);
        demand.Request((D3D12RenderInvalidation) first);
        demand.Request((D3D12RenderInvalidation) second);

        Assert.True(demand.TryBeginFrame(out var invalidation));
        Assert.Equal((D3D12RenderInvalidation) (first | second) | D3D12RenderInvalidation.Render, invalidation);
        demand.CompleteFrame(submitted: true);
        Assert.Equal(5, SubmitRemainingFrames(demand));
    }

    /// <summary>Verifies repeated invalidations before preparation restart a budget instead of adding budgets.</summary>
    [Fact]
    public void RepeatedRequestsRestartRatherThanAccumulateFrames() {
        var demand = new D3D12RenderDemand();
        Assert.True(demand.TryBeginFrame(out _));
        demand.CompleteFrame(submitted: true);
        demand.Request(D3D12RenderInvalidation.Render);
        demand.Request(D3D12RenderInvalidation.Render);
        demand.Request(D3D12RenderInvalidation.None);

        Assert.Equal(6, SubmitRemainingFrames(demand));
    }

    /// <summary>Verifies a request during updates or accepted submission survives completion of the older reservation.</summary>
    /// <param name="submitted">Whether the older frame actually reaches the GPU.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RequestDuringActiveFrameRetainsFullFollowingBudget(bool submitted) {
        var demand = new D3D12RenderDemand();
        _ = SubmitRemainingFrames(demand);
        demand.Request(D3D12RenderInvalidation.PerFrameRenderables);
        Assert.True(demand.TryBeginFrame(out var reserved));
        Assert.Equal(D3D12RenderInvalidation.Render | D3D12RenderInvalidation.PerFrameRenderables, reserved);

        demand.Request(D3D12RenderInvalidation.SceneGraph);
        demand.Request(D3D12RenderInvalidation.Render);
        demand.CompleteFrame(submitted);

        Assert.True(demand.TryBeginFrame(out var following));
        var expected = D3D12RenderInvalidation.Render | D3D12RenderInvalidation.SceneGraph;
        if (!submitted) expected |= D3D12RenderInvalidation.PerFrameRenderables;
        Assert.Equal(expected, following);
        demand.CompleteFrame(submitted: true);
        Assert.Equal(5, SubmitRemainingFrames(demand));
    }

    /// <summary>Verifies requests made during pending GPU work and later completion notifications both remain productive.</summary>
    [Fact]
    public void PendingGpuAndCompletionEventRequestsEachRestartFollowingBudget() {
        var demand = new D3D12RenderDemand(1);
        Assert.True(demand.TryBeginFrame(out _));
        demand.CompleteFrame(submitted: true);
        Assert.False(demand.HasRenderDemand);

        // The consumer does not begin another frame until its independent GPU fence is complete.
        demand.Request(D3D12RenderInvalidation.PerFrameRenderables);
        demand.Request(D3D12RenderInvalidation.SceneGraph);
        Assert.True(demand.TryBeginFrame(out var afterGpu));
        Assert.Equal(
            D3D12RenderInvalidation.Render | D3D12RenderInvalidation.PerFrameRenderables |
            D3D12RenderInvalidation.SceneGraph, afterGpu
        );
        demand.CompleteFrame(submitted: true);

        // A completion listener can invalidate after the reservation has already been committed.
        demand.Request(D3D12RenderInvalidation.Render);
        Assert.Equal(1, SubmitRemainingFrames(demand));
    }

    /// <summary>Verifies repeated skipped submissions restore preparation flags and consume no actual frame budget.</summary>
    [Fact]
    public void CanceledReservationsRestoreFlagsAndBudget() {
        var demand = new D3D12RenderDemand();
        for (var attempt = 0; attempt < 12; attempt++) {
            Assert.True(demand.TryBeginFrame(out var invalidation));
            Assert.Equal(D3D12RenderInvalidation.Render | D3D12RenderInvalidation.SceneGraph, invalidation);
            demand.CompleteFrame(submitted: false);
        }

        Assert.Equal(6, SubmitRemainingFrames(demand));
    }

    /// <summary>Verifies an empty request cannot supersede a canceled reservation or erase its flags.</summary>
    [Fact]
    public void EmptyRequestDuringReservationDoesNotChangeBudget() {
        var demand = new D3D12RenderDemand();
        Assert.True(demand.TryBeginFrame(out _));
        demand.Request(D3D12RenderInvalidation.None);
        demand.CompleteFrame(submitted: false);

        Assert.Equal(6, SubmitRemainingFrames(demand));
    }

    /// <summary>Verifies busy attempts and unmatched completion calls cannot consume or manufacture demand.</summary>
    [Fact]
    public void BusyAndUnmatchedCompletionCallsDoNotChangeBudget() {
        var demand = new D3D12RenderDemand(1);
        Assert.True(demand.TryBeginFrame(out _));
        Assert.False(demand.HasRenderDemand);
        for (var attempt = 0; attempt < 12; attempt++) {
            Assert.False(demand.TryBeginFrame(out var invalidation));
            Assert.Equal(D3D12RenderInvalidation.None, invalidation);
        }

        demand.CompleteFrame(submitted: true);
        demand.CompleteFrame(submitted: true);
        demand.CompleteFrame(submitted: false);

        Assert.False(demand.HasRenderDemand);
        Assert.False(demand.TryBeginFrame(out _));
    }

    /// <summary>Verifies independent publication/recovery claims, repeated retries, and requests during active attempts.</summary>
    [Fact]
    public void PresentationRequestsSurviveCleanSceneAndRequireExplicitRetry() {
        var demand = new D3D12RenderDemand();
        _ = SubmitRemainingFrames(demand);
        Assert.False(demand.HasPublicationDemand);
        Assert.False(demand.HasRecoveryDemand);
        Assert.False(demand.TryTakePublicationRequest());
        Assert.False(demand.TryTakeRecoveryRequest());
        demand.RequestPublication();
        demand.RequestRecovery();

        for (var retry = 0; retry < 12; retry++) {
            Assert.True(demand.HasPublicationDemand);
            Assert.True(demand.HasRecoveryDemand);
            Assert.False(demand.HasRenderDemand);
            Assert.False(demand.TryBeginFrame(out _));
            Assert.True(demand.TryTakePublicationRequest());
            Assert.True(demand.TryTakeRecoveryRequest());
            Assert.False(demand.TryTakePublicationRequest());
            Assert.False(demand.TryTakeRecoveryRequest());
            // Claim before doing external work, then retain a concurrent request or explicitly schedule a retry.
            demand.RequestPublication();
            demand.RequestRecovery();
            demand.CompleteFrame(submitted: false);
        }

        Assert.True(demand.TryTakePublicationRequest());
        Assert.True(demand.TryTakeRecoveryRequest());
        Assert.False(demand.HasPublicationDemand);
        Assert.False(demand.HasRecoveryDemand);
        Assert.False(demand.HasRenderDemand);
        demand.Request(D3D12RenderInvalidation.Render);
        Assert.Equal(6, SubmitRemainingFrames(demand));
    }

    /// <summary>Verifies publication and recovery work cannot consume or extend an outstanding scene budget.</summary>
    [Fact]
    public void PresentationRequestsDoNotChangeActiveRenderBudget() {
        var demand = new D3D12RenderDemand();
        Assert.True(demand.TryBeginFrame(out _));
        demand.RequestPublication();
        demand.RequestRecovery();
        Assert.True(demand.TryTakePublicationRequest());
        Assert.True(demand.TryTakeRecoveryRequest());
        demand.RequestPublication();
        demand.RequestRecovery();
        demand.CompleteFrame(submitted: true);

        Assert.Equal(5, SubmitRemainingFrames(demand));
        Assert.True(demand.HasPublicationDemand);
        Assert.True(demand.HasRecoveryDemand);
        Assert.True(demand.TryTakePublicationRequest());
        Assert.True(demand.TryTakeRecoveryRequest());
    }

    /// <summary>Verifies concurrent producers preserve all flags and coalesce into one bounded following budget.</summary>
    /// <param name="submitted">Whether the previously reserved frame succeeds or is canceled.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConcurrentRequestsDuringReservationRetainAllBitsWithoutBudgetInflation(bool submitted) {
        var demand = new D3D12RenderDemand();
        Assert.True(demand.TryBeginFrame(out _));
        Parallel.For(
            0, 4, worker => {
                for (var request = 0; request < 1_000; request++) {
                    demand.Request((D3D12RenderInvalidation) (1 << (worker % 3)));
                    demand.RequestPublication();
                    demand.RequestRecovery();
                }
            }
        );
        demand.CompleteFrame(submitted);

        Assert.True(demand.TryBeginFrame(out var invalidation));
        Assert.Equal(
            D3D12RenderInvalidation.Render | D3D12RenderInvalidation.PerFrameRenderables |
            D3D12RenderInvalidation.SceneGraph, invalidation
        );
        demand.CompleteFrame(submitted: true);
        Assert.Equal(5, SubmitRemainingFrames(demand));
        Assert.True(demand.TryTakePublicationRequest());
        Assert.False(demand.TryTakePublicationRequest());
        Assert.True(demand.TryTakeRecoveryRequest());
        Assert.False(demand.TryTakeRecoveryRequest());
    }

    /// <summary>Verifies contending frame consumers can acquire only one reservation before it is completed.</summary>
    [Fact]
    public void ConcurrentBeginAttemptsAcquireOnlyOneReservation() {
        var demand = new D3D12RenderDemand();
        var acquired = 0;
        Parallel.For(
            0, 32, _ => {
                if (demand.TryBeginFrame(out var invalidation)) {
                    Assert.Equal(D3D12RenderInvalidation.Render | D3D12RenderInvalidation.SceneGraph, invalidation);
                    Interlocked.Increment(ref acquired);
                } else {
                    Assert.Equal(D3D12RenderInvalidation.None, invalidation);
                }
            }
        );

        Assert.Equal(1, acquired);
        demand.CompleteFrame(submitted: true);
        Assert.Equal(5, SubmitRemainingFrames(demand));
    }

    /// <summary>Verifies warmed idle ticks and active reservation/request/publication cycles allocate zero managed bytes.</summary>
    /// <param name="active">Whether to exercise full transitions instead of only idle polling.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WarmedCoordinatorOperationsAllocateZeroBytes(bool active) {
        var demand = new D3D12RenderDemand(1);
        _ = SubmitRemainingFrames(demand);
        _ = ExerciseOperations(demand, active, 4_096);
        _ = GC.GetAllocatedBytesForCurrentThread();

        var before = GC.GetAllocatedBytesForCurrentThread();
        var observations = ExerciseOperations(demand, active, 8_192);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        Assert.Equal(0L, allocated);
        Assert.Equal(
            active
                ? 8_192 * 6
                : 8_192, observations
        );
        Assert.False(demand.HasRenderDemand);
    }

    /// <summary>Submits all bounded test demand while failing promptly if a broken coordinator never settles.</summary>
    /// <param name="demand">The coordinator under test.</param>
    /// <returns>The number of actual submitted frames.</returns>
    private static int SubmitRemainingFrames(D3D12RenderDemand demand) {
        var submitted = 0;
        while (demand.TryBeginFrame(out _)) {
            Assert.True(submitted < 100, "Render demand did not settle within the test budget");
            demand.CompleteFrame(submitted: true);
            submitted++;
        }

        return submitted;
    }

    /// <summary>Exercises warmed coordinator paths without assertions, delegates, tasks, or other measured allocations.</summary>
    /// <param name="demand">The idle coordinator whose single-frame budget is reused.</param>
    /// <param name="active">Whether to exercise active transitions as well as idle polling.</param>
    /// <param name="iterations">The number of complete operation cycles.</param>
    /// <returns>The number of observed expected successful state transitions.</returns>
    private static int ExerciseOperations(D3D12RenderDemand demand, bool active, int iterations) {
        var observations = 0;
        for (var iteration = 0; iteration < iterations; iteration++) {
            if (active) {
                demand.MinimumUpdateCount = iteration % 2;
                demand.Request(D3D12RenderInvalidation.SceneGraph | D3D12RenderInvalidation.PerFrameRenderables);
                if (demand.TryBeginFrame(out _)) observations++;
                demand.Request(D3D12RenderInvalidation.Render);
                demand.CompleteFrame(submitted: false);
                if (demand.TryBeginFrame(out _)) observations++;
                demand.CompleteFrame(submitted: true);
                demand.Request(D3D12RenderInvalidation.Render);
                if (demand.TryBeginFrame(out _)) observations++;
                demand.CompleteFrame(submitted: false);
                if (demand.TryBeginFrame(out _)) observations++;
                demand.CompleteFrame(submitted: true);
                demand.RequestPublication();
                demand.RequestRecovery();
                if (demand.TryTakePublicationRequest() && demand.TryTakeRecoveryRequest()) observations++;
            }

            if (!demand.HasRenderDemand && !demand.HasPublicationDemand && !demand.HasRecoveryDemand &&
                !demand.TryBeginFrame(out _) && !demand.TryTakePublicationRequest() &&
                !demand.TryTakeRecoveryRequest() && demand.MinimumUpdateCount >= 0) observations++;
            demand.Request(D3D12RenderInvalidation.None);
            demand.CompleteFrame(submitted: false);
        }

        return observations;
    }
}