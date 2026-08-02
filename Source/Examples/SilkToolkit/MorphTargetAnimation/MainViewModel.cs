// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using DemoCore;
using HelixToolkit.SharpDX.Core.Animations;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.Wpf.SharpDX;
using HelixToolkit.Wpf.SharpDX.Controls;
using Media3D = System.Windows.Media.Media3D;

namespace MorphTargetAnimationDemo;

public class MainViewModel : BaseViewModel {
    public SceneNodeGroupModel3D ModelGroup { get; private set; }
    public string DebugLabel { get; set; }

    private HelixToolkitScene scn;
    private CompositionTargetEx compositeHelper = new CompositionTargetEx();
    private List<IAnimationUpdater> animationUpdaters;

    public double EndTime {
        set { SetValue(ref field, value); }
        get { return field; }
    } = 0;

    public double CurrTime {
        set {
            if (SetValue(ref field, value)) {
                foreach (IAnimationUpdater updater in animationUpdaters) {
                    updater.Update((float)value, 1);
                }
            }
        }
        get { return field; }
    } = 0;

    public bool IsPlaying {
        private set { SetValue(ref field, value); }
        get { return field; }
    } = false;

    public ICommand PlayCommand { get; }

    private long initTime = 0;

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();
        ModelGroup = new SceneNodeGroupModel3D();

        //Test importing
        Importer importer = new Importer();
        importer.Configuration.CreateSkeletonForBoneSkinningMesh = true;
        importer.Configuration.SkeletonSizeScale = 0.01f;
        importer.Configuration.GlobalScale = 0.1f;
        scn = importer.Load("Gunan_animated.fbx");

        //Add to model group for rendering
        ModelGroup.AddNode(scn.Root);

        //Setup each animation, this will actively play all (not always desired)
        animationUpdaters = [.. scn.Animations.CreateAnimationUpdaters().Values];
        EndTime = scn.Animations.Max(x => x.EndTime);
        PlayCommand = new RelayCommand((o) => {
            if (!IsPlaying) {
                initTime = 0;
                compositeHelper.Rendering += Render;
            } else {
                compositeHelper.Rendering -= Render;
            }

            IsPlaying = !IsPlaying;
        });
    }

    private void Render(object sender, System.Windows.Media.RenderingEventArgs e) {
        //Animation with perf testing
        long t = Stopwatch.GetTimestamp();
        if (initTime == 0) {
            initTime = t;
        }

        //Update animation. Ensures all animation times are in sync
        CurrTime = ((t - initTime) / (double)Stopwatch.Frequency) % EndTime;
        t = Stopwatch.GetTimestamp() - t;
        DebugLabel = t.ToString();
    }
}
