using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;

namespace OffScreenRendering;

internal class MainWindowViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject {
    public ImageSource Image {
        get;
        set => SetProperty(ref field, value);
    }

    public ICommand RenderCommand { get; }

    private readonly Renderer renderer = new();

    public MainWindowViewModel() {
        RenderCommand = new RelayCommand(() => {
            renderer.Resize(1024, 768);
            Task.Run(() => { return renderer.Render(); }).ContinueWith((result) => { Image = result.Result; },
                                                                       TaskScheduler
                                                                           .FromCurrentSynchronizationContext());
        });
    }
}
