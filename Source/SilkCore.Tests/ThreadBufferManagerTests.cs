using HelixToolkit.SharpDX.Core.Core.Buffers;

namespace SilkCore.Tests;

public class ThreadBufferManagerTests {
    [Fact]
    [Trait("Category", "Unit")]
    public void GetBufferReturnsAtLeastRequestedSizeAboveTwoPow24() {
        // Above 2^24 the int -> float conversion inside the size computation rounds to
        // the nearest representable float; when it rounded down, GetBuffer allocated
        // fewer elements than requested.
        int[] requests = [16_777_217, 67_108_867, 67_108_869, 100_000_003];
        foreach (var request in requests) {
            var buffer = ThreadBufferManager<byte>.GetBuffer(request);
            Assert.True(buffer.Length >= request,
                $"GetBuffer({request}) returned a buffer {request - buffer.Length} elements short.");
        }
    }
}
