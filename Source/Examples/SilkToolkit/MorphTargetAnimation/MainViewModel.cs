// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using DemoCore;
using HelixToolkit.SharpDX.Core.Assimp;
using HelixToolkit.SharpDX.Core.Extensions;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.Wpf.SharpDX.Controls;
using HelixToolkit.Wpf.SharpDX.Element3D;

namespace MorphTargetAnimationDemo;

public class MainViewModel : BaseViewModel {
    public SceneNodeGroupModel3D ModelGroup { get; private set; }
    public string DebugLabel { get; set; } = string.Empty;

    private CompositionTargetEx compositeHelper = new();
    private List<IAnimationUpdater> animationUpdaters;

    public double EndTime {
        set => SetValue(ref field, value);
        get;
    } = 0;

    public double CurrTime {
        set {
            if (SetValue(ref field, value)) {
                foreach (var updater in animationUpdaters) {
                    updater.Update((float) value, 1);
                }
            }
        }
        get;
    } = 0;

    public bool IsPlaying {
        private set => SetValue(ref field, value);
        get;
    } = false;

    public ICommand PlayCommand { get; }

    private long initTime;

    public MainViewModel() {
        EffectsManager = new DefaultEffectsManager();
        ModelGroup = new SceneNodeGroupModel3D();

        //Test importing
        var importer = new Importer();
        importer.Configuration.CreateSkeletonForBoneSkinningMesh = true;
        importer.Configuration.SkeletonSizeScale = 0.01f;
        importer.Configuration.GlobalScale = 0.1f;
        var scene = importer.Load("Gunan_animated.fbx")
                    ?? throw new InvalidOperationException("The animation scene could not be loaded.");

        //Add to model group for rendering
        ModelGroup.AddNode(scene.Root);

        //Setup each animation, this will actively play all (not always desired)
        animationUpdaters = [
            .. scene.Animations.CreateAnimationUpdaters()
                .Values
        ];
        EndTime = scene.Animations.Max(x => x.EndTime);
        PlayCommand = new RelayCommand((_) => {
            if (!IsPlaying) {
                initTime = 0;
                compositeHelper.Rendering += Render;
            } else {
                compositeHelper.Rendering -= Render;
            }

            IsPlaying = !IsPlaying;
        });
    }

    private void Render(object? sender, System.Windows.Media.RenderingEventArgs e) {
        //Animation with perf testing
        var t = Stopwatch.GetTimestamp();
        if (initTime == 0) {
            initTime = t;
        }

        //Update animation. Ensures all animation times are in sync
        CurrTime = ((t - initTime) / (double) Stopwatch.Frequency) % EndTime;
        t = Stopwatch.GetTimestamp() - t;
        DebugLabel = t.ToString();
    }
}