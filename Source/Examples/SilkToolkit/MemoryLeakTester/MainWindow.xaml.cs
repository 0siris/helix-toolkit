using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace MemoryLeakTester;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    private Window? testWin;
    private DispatcherTimer? timer;
    private readonly SystemStateParams systemparams = new();
    private readonly IList<Tuple<string, Type>> projectWinPairs = [];

    public MainWindow() {
        InitializeComponent();
        //ProjectWinPairs.Add(new Tuple<string, Type>("CustomShaderDemo", typeof(CustomShaderDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("BoneSkinDemo", typeof(BoneSkinDemo.MainWindow)));
        // ProjectWinPairs.Add(new Tuple<string, Type>("DeferredShadingDemo", typeof(DeferredShadingDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("DynamicTextureDemo", typeof(DynamicTextureDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("InstancingDemo", typeof(InstancingDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("LightingDemo", typeof(LightingDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("OctreeDemo", typeof(OctreeDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("ParticleSystemDemo", typeof(ParticleSystemDemo.MainWindow)));
        // ProjectWinPairs.Add(new Tuple<string, Type>("ScreenSpaceDemo", typeof(ScreenSpaceDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("ShadowMapDemo", typeof(ShadowMapDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("TessellationDemo", typeof(TessellationDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("SimpleDemo", typeof(SimpleDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("MaterialDemo", typeof(MaterialDemo.MainWindow)));
        projectWinPairs.Add(new Tuple<string, Type>("EnvironmentMapDemo", typeof(EnvironmentMapDemo.MainWindow)));
        ProjectCombo.ItemsSource = projectWinPairs;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e) {
        if (timer == null) {
            timer = new DispatcherTimer {
                Interval = TimeSpan.FromSeconds(0.5)
            };
            timer.Tick += Timer_Tick;
            systemparams.Count = 0;
            timer.Start();
            StartButton.Visibility = Visibility.Collapsed;
            StopButton.Visibility = Visibility.Visible;
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e) {
        timer?.Stop();
        timer = null;
        StartButton.Visibility = Visibility.Visible;
        StopButton.Visibility = Visibility.Collapsed;
    }

    private void Timer_Tick(object? sender, EventArgs e) {
        if (testWin is null) {
            CreateWindow();
        } else if (testWin is { } window) {
            window.Close();
            testWin = null;
            GC.Collect(0, GCCollectionMode.Forced);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
            GC.Collect();
            Debug.WriteLine("SharpDX object tracking is not available after the Silk.NET migration.");
            var log = systemparams.Update();
            Paragraph.Inlines.Add(log);
            LogTextbox.ScrollToEnd();
        }
    }

    private void CreateWindow() {
        if (ProjectCombo.SelectedIndex == -1) {
            testWin = new TestWindow();
        } else {
            var pair = projectWinPairs[ProjectCombo.SelectedIndex];
            testWin = Activator.CreateInstance(pair.Item2) as Window;
        }

        testWin?.Show();
    }

    internal sealed class SystemStateParams {
        public long Count;
        public long WorkingSet { private set; get; }
        public long PrivateMemory { private set; get; }
        public long ManagedMemory { private set; get; }
        public long HandleCount { private set; get; }
        public long ThreadCount { private set; get; }
        public bool NeedsUpdate { private set; get; }
        public readonly double ChangePercent = 1; //10%

        public Run Update() {
            if (Count == 0) {
                InitialBaseline();
            }

            ++Count;
            var proc = Process.GetCurrentProcess();

            var workingSet = proc.WorkingSet64;
            var privateMemory = proc.PrivateMemorySize64;
            var managedMemory = GC.GetTotalMemory(false);
            var handleCount = proc.HandleCount;
            var threadCount = proc.Threads.Count;
            var run = new Run(
                $"{Count}  Total: {privateMemory / 1000000} MB; Physical: {workingSet / 1000000} MB; Managed: {managedMemory / 1000000} MB; Handle: {handleCount}; Threads: {threadCount};\n");
            if (Changed(PrivateMemory, privateMemory)
                || Changed(HandleCount, handleCount) || Changed(ThreadCount, threadCount)) {
                run.Foreground = new SolidColorBrush(Colors.Red);
            }

            return run;
        }

        private void InitialBaseline() {
            var proc = Process.GetCurrentProcess();

            WorkingSet = proc.WorkingSet64;
            PrivateMemory = proc.PrivateMemorySize64;
            ManagedMemory = GC.GetTotalMemory(false);
            HandleCount = proc.HandleCount;
            ThreadCount = proc.Threads.Count;
        }

        private bool Changed(long orgValue, long newValue) => Math.Abs(orgValue - newValue) > orgValue * ChangePercent;
    }
}