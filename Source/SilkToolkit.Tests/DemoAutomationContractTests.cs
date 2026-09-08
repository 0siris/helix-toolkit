using System.Windows.Media.Media3D;
using DemoCore.Automation;
using Xunit;
using OrthographicCamera = HelixToolkit.Wpf.SharpDX.Camera.OrthographicCamera;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.Camera.PerspectiveCamera;

namespace SilkToolkit.Tests;

[Collection(WpfCollection.Name)]
public sealed class DemoAutomationContractTests {
    /// <summary>
    ///     Verifies that a perspective camera survives a capture and apply roundtrip.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task CameraStateRoundtripsPerspectiveCamera() =>
        StaThread.RunAsync(() => {
            var camera = new PerspectiveCamera {
                Position = new Point3D(1, 2, 3),
                LookDirection = new Vector3D(0, 0, -1),
                UpDirection = new Vector3D(0, 1, 0),
                FieldOfView = 60,
                NearPlaneDistance = 0.5,
                FarPlaneDistance = 200,
            };

            var state = CameraState.FromCamera(camera);
            Assert.Equal(CameraState.Perspective, state.CameraType);
            Assert.Equal([1d, 2d, 3d], state.Position);
            Assert.Equal(60, state.FieldOfView);
            Assert.Null(state.Width);

            var applied = state.ApplyTo(new PerspectiveCamera());
            var roundtripped = CameraState.FromCamera(applied);
            Assert.Equal(state.Position, roundtripped.Position);
            Assert.Equal(state.LookDirection, roundtripped.LookDirection);
            Assert.Equal(state.UpDirection, roundtripped.UpDirection);
            Assert.Equal(state.NearPlane, roundtripped.NearPlane);
            Assert.Equal(state.FarPlane, roundtripped.FarPlane);
            Assert.Equal(state.FieldOfView, roundtripped.FieldOfView);
            Assert.Equal(state.CameraType, roundtripped.CameraType);
        });

    /// <summary>
    ///     Verifies that a camera type change creates a new camera of the requested type.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task CameraStateSwitchesCameraType() =>
        StaThread.RunAsync(() => {
            var patch = new CameraState { CameraType = CameraState.Orthographic, Width = 25 };
            var applied = patch.ApplyTo(new PerspectiveCamera());

            var orthographic = Assert.IsType<OrthographicCamera>(applied);
            Assert.Equal(25, orthographic.Width);
        });

    /// <summary>
    ///     Verifies that malformed camera patches fail with argument errors for HTTP 422 mapping.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public Task CameraStateRejectsInvalidPatches() =>
        StaThread.RunAsync(() => {
            var camera = new PerspectiveCamera();
            Assert.Throws<ArgumentException>(() => new CameraState { Position = [1, 2] }.ApplyTo(camera));
            Assert.Throws<ArgumentException>(() => new CameraState { CameraType = "fisheye" }.ApplyTo(camera));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CameraState { FieldOfView = 0 }.ApplyTo(camera));
        });

    /// <summary>
    ///     Verifies that the demo port resolves from the environment and ignores invalid values.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void OptionsResolvePortFromEnvironment() {
        const string variable = "HELIX_DEMO_PORT";
        var previous = Environment.GetEnvironmentVariable(variable);
        try {
            Environment.SetEnvironmentVariable(variable, "61371");
            Assert.Equal(61371, DemoAutomationOptions.FromEnvironment().HttpPort);
            Environment.SetEnvironmentVariable(variable, "not-a-port");
            Assert.Equal(0, DemoAutomationOptions.FromEnvironment().HttpPort);
        } finally {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }

    /// <summary>
    ///     Verifies that the automation kill switch disables both servers.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void OptionsDisableFlagStopsBothServers() {
        const string variable = "HELIX_DEMO_AUTOMATION";
        var previous = Environment.GetEnvironmentVariable(variable);
        try {
            Environment.SetEnvironmentVariable(variable, "0");
            var options = DemoAutomationOptions.FromEnvironment();
            Assert.False(options.EnableHttp);
            Assert.False(options.EnableMcp);
        } finally {
            Environment.SetEnvironmentVariable(variable, previous);
        }
    }

    /// <summary>
    ///     Verifies that the missing scene host error carries the stable mapping token.
    /// </summary>
    [Fact]
    [Trait("Category", "Wpf")]
    public void MissingSceneHostCarriesStableToken() {
        Assert.Contains("no-scene-host", new NoSceneHostException().Message);
    }
}
