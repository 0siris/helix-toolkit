// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace TemplateDemo;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Media3D;
using DemoCore;
using HelixToolkit.Wpf.SharpDX;

public class MainViewModel : BaseViewModel {
    public ObservableCollection<SelectionViewModel> ViewModels { get; } =
        [];

    [field: AllowNull, MaybeNull]
    public SelectionViewModel SelectedViewModel {
        set { SetValue(ref field, value); }
        get { return field; }
    } = null;

    private PhongMaterialCollection materials = [];

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();

        CreateViewModels();
    }

    private void CreateViewModels() {
        var vm = new SelectionViewModel(nameof(Sphere));
        for (int i = 0; i < 10; ++i) {
            vm.Items.Add(new Sphere() { Transform = new TranslateTransform3D(0, i, 0), Material = materials[i] });
        }

        ViewModels.Add(vm);

        vm = new SelectionViewModel(nameof(Cube));
        for (int i = 0; i < 10; ++i) {
            vm.Items.Add(new Cube() { Transform = new TranslateTransform3D(i, i, 0), Material = materials[i] });
        }

        ViewModels.Add(vm);
    }
}

public class SelectionViewModel {
    public string Name { private set; get; }
    public ObservableCollection<Shape> Items { get; } = [];

    public SelectionViewModel(string name) {
        Name = name;
    }
}
