using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Model.Camera;
using Vector3 = Silk.NET.Maths.Vector3D<float>;

namespace HelixToolkit.SharpDX.Core.Native;

/// <summary>Checks that framing preserves the actual view direction and contains every projected corner.</summary>
public sealed class CameraFramingTests {
    [Fact]
    public void FrameSeparatedPatchesFromFront() {
        var camera = new PerspectiveCameraCore {
            Position = new Vector3(0, 0, 100), LookDirection = new Vector3(0, 0, -100),
            UpDirection = Vector3.UnitY, NearPlaneDistance = .01f, FarPlaneDistance = 1000000
        };
        Verify(camera, 809f / 773f, new BoundingBox(new Vector3(0, 0, -.2f), new Vector3(6, 2, .8f)));
    }

    [Fact]
    public void FrameRotatedOffsetObjectInPortraitViewport() {
        var camera = new PerspectiveCameraCore {
            Position = new Vector3(20, 30, 40), LookDirection = new Vector3(2, -1, -3),
            UpDirection = Vector3.UnitY, NearPlaneDistance = .1f, FarPlaneDistance = 1000
        };
        Verify(camera, .5f, new BoundingBox(new Vector3(-12, 7, 2), new Vector3(-8, 10, 9)));
    }

    private static void Verify(PerspectiveCameraCore camera, float aspect, BoundingBox bounds) {
        var forward = SilkMath.Normalize(camera.LookDirection);
        camera.ZoomExtents(aspect, bounds, out var position, out var look, out var up);
        Assert.True(SilkMath.Dot(forward, SilkMath.Normalize(look)) > .99999f,
            "Framing must not turn the camera around or put its target behind it.");
        camera.Position = position;
        camera.LookDirection = look;
        camera.UpDirection = up;
        var viewProjection = camera.CreateViewMatrix() * camera.CreateProjectionMatrix(aspect);
        foreach (var corner in bounds.GetCorners()) {
            var projected = SilkMath.TransformCoordinate(corner, viewProjection);
            Assert.InRange(projected.X, -1.00001f, 1.00001f);
            Assert.InRange(projected.Y, -1.00001f, 1.00001f);
            Assert.InRange(projected.Z, 0f, 1.00001f);
        }
    }
}
