using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.SharpDX.Core.Utilities;
public interface IFrameStatistics : INotifyPropertyChanged {
    /// <summary>
    ///     Gets the average value.
    /// </summary>
    /// <value>
    ///     The average value.
    /// </value>
    double AverageValue { get; }

    /// <summary>
    ///     Gets the average frequency.
    /// </summary>
    /// <value>
    ///     The average frequency.
    /// </value>
    double AverageFrequency { get; }

    /// <summary>
    ///     Gets or sets the update frequency by number of samples
    /// </summary>
    /// <value>
    ///     The update frequency.
    /// </value>
    uint UpdateFrequency { get; set; }

    event EventHandler<FrameStatisticsArg> OnValueChanged;

    /// <summary>
    ///     Pushes the specified latency by milliseconds
    /// </summary>
    /// <param name="latency">The latency.</param>
    void Push(double latency);

    void Reset();
}

public sealed class FrameStatistics : ObservableObject, IFrameStatistics {
    private const int RingBufferSize = 120;
    private readonly SimpleRingBuffer<double> ringBuffer = new(RingBufferSize);

    private uint counter;
    private double movingAverage;
    private double total;
    public event EventHandler<FrameStatisticsArg>? OnValueChanged;

    /// <summary>
    ///     Average latency
    /// </summary>
    public double AverageValue {
        get;
        private set {
            if (Set(ref field, value)) {
                AverageFrequency = 1000 / value;
                OnValueChanged?.Invoke(this, new FrameStatisticsArg(value, AverageFrequency));
            }
        }
    }

    /// <summary>
    ///     Gets or sets the average frequency.
    /// </summary>
    /// <value>
    ///     The average frequency.
    /// </value>
    public double AverageFrequency {
        get;
        private set => Set(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the update frequency by number of samples, Default is 60
    /// </summary>
    /// <value>
    ///     The update frequency.
    /// </value>
    public uint UpdateFrequency { get; set; } = 30;

    /// <summary>
    ///     Pushes the specified latency by milliseconds.
    /// </summary>
    /// <param name="latency">The latency.</param>
    public void Push(double latency) {
        if (latency > 1000 || latency < 0) {
            Reset();
            return;
        }

        if (ringBuffer.IsFull()) {
            total -= ringBuffer.First;
            ringBuffer.RemoveFirst();
        }

        ringBuffer.Add(latency);
        total += latency;
        movingAverage = total / ringBuffer.Count; // moving average
        movingAverage = Math.Min(1000, Math.Max(0, movingAverage));
        counter = ++counter % UpdateFrequency;
        if (counter == 0) AverageValue = movingAverage;
    }

    public void Reset() {
        AverageValue = 0;
        movingAverage = 0;
        counter = 0;
        total = 0;
        ringBuffer.Clear();
    }
}

public interface IRenderStatistics {
    IFrameStatistics FpsStatistics { get; }

    IFrameStatistics LatencyStatistics { get; }

    int NumModel3D { get; }

    int NumCore3D { get; }

    int NumTriangles { get; }

    int NumDrawCalls { get; }

    float FrustumTestTime { get; }

    RenderDetail FrameDetail { get; set; }

    ICamera? Camera { get; set; }

    string GetDetailString();
    void Reset();
}

/// <summary>
/// </summary>
public sealed class RenderStatistics : IRenderStatistics {
    private const string LineBreak = "\n---------\n";

    /// <summary>
    ///     Gets the FPS statistics.
    /// </summary>
    /// <value>
    ///     The FPS statistics.
    /// </value>
    public IFrameStatistics FpsStatistics { get; } = new FrameStatistics();

    /// <summary>
    ///     Gets the render latency statistics.
    /// </summary>
    /// <value>
    ///     The latency statistics.
    /// </value>
    public IFrameStatistics LatencyStatistics { get; } = new FrameStatistics();

    /// <summary>
    ///     Gets the number of model3d per frame.
    /// </summary>
    /// <value>
    ///     The number model3d.
    /// </value>
    public int NumModel3D { get; internal set; }

    /// <summary>
    ///     Gets the number of render core3d per frame.
    /// </summary>
    /// <value>
    ///     The number core3 d.
    /// </value>
    public int NumCore3D { get; internal set; }

    /// <summary>
    ///     Gets the number triangles rendered in geometry model
    /// </summary>
    /// <value>
    ///     The number triangles.
    /// </value>
    public int NumTriangles { get; internal set; }

    /// <summary>
    ///     Gets the number draw calls per frame
    /// </summary>
    /// <value>
    ///     The number draw calls.
    /// </value>
    public int NumDrawCalls { get; internal set; }

    /// <summary>
    ///     Gets the frustum test time.
    /// </summary>
    /// <value>
    ///     The frustum test time.
    /// </value>
    public float FrustumTestTime { get; internal set; }

    /// <summary>
    ///     Gets or sets the camera.
    /// </summary>
    /// <value>
    ///     The camera.
    /// </value>
    public ICamera? Camera { get; set; }

    /// <summary>
    ///     Gets or sets the frame detail.
    /// </summary>
    /// <value>
    ///     The frame detail.
    /// </value>
    public RenderDetail FrameDetail { get; set; } = RenderDetail.None;

    public string GetDetailString() 
        => GetDetailString(FrameDetail);

    /// <summary>
    ///     Resets this instance.
    /// </summary>
    public void Reset() {
        FpsStatistics.Reset();
        LatencyStatistics.Reset();
        NumTriangles = NumCore3D = NumModel3D = NumDrawCalls = 0;
        FrustumTestTime = 0;
    }

    public string GetDetailString(RenderDetail detail) {
        if (detail == RenderDetail.None) 
            return string.Empty;
        var s = string.Empty;
        
        if (detail.HasFlag(RenderDetail.Fps)) 
            s += GetFps();
        
        if (detail.HasFlag(RenderDetail.Statistics)) 
            s += GetStatistics();
        
        if (detail.HasFlag(RenderDetail.TriangleInfo)) 
            s += GetTriangleCount();
        
        if (detail.HasFlag(RenderDetail.Camera)) 
            s += GetCamera();
        return s;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetFps() => $"FPS:{Math.Round(FpsStatistics.AverageFrequency, 2)}" + LineBreak;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetStatistics() => $"Render(ms): {Math.Round(LatencyStatistics.AverageValue, 4)}\n" +
                                      $"NumModel3D: {NumModel3D}\n" +
                                      $"NumCore3D: {NumCore3D}\n" +
                                      $"NumDrawCalls: {NumDrawCalls}"
                                      + LineBreak;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetTriangleCount() => $"NumTriangle: {NumTriangles}" + LineBreak;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetCamera()
        => Camera == null
               ? string.Empty
               : "Camera:\n" + Camera + LineBreak;

    /// <summary>
    ///     Returns a <see cref="System.String" /> that represents this instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="System.String" /> that represents this instance.
    /// </returns>
    public override string ToString() 
        => GetDetailString(RenderDetail.Fps | RenderDetail.Statistics);
}
