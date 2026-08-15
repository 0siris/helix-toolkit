// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ParticleStormModel3D.cs" company="Helix Toolkit">
//   Copyright (c) 2017 Helix Toolkit contributors
// </copyright>
// <author>Lunci Hua</author>
// <summary>
//  Particle system.
//  References: https://github.com/spazzarama/Direct3D-Rendering-Cookbook
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Model.Scene;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.Wpf.SharpDX.Element3D.Abstract;
using HelixToolkit.Wpf.SharpDX.Extensions;
using static HelixToolkit.SharpDX.Core.Core.ParticleRenderCore;
using Media = System.Windows.Media;
using Media3D = System.Windows.Media.Media3D;
using Vector3D = System.Windows.Media.Media3D.Vector3D;

#pragma warning disable CS8601, CS8602, CS8604 // WPF dependency-property callbacks provide the owning model and scene node.

namespace HelixToolkit.Wpf.SharpDX.Element3D;

public class ParticleStormModel3D : Model.Elements3D.AbstractElements3D.Element3D {
    #region Dependency Properties

    public static DependencyProperty ParticleCountProperty = DependencyProperty.Register("ParticleCount",
        typeof(int),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(DefaultParticleCount,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).ParticleCount =
                    Math.Max(8, (int) e.NewValue);
            }));

    public int ParticleCount {
        get => (int) GetValue(ParticleCountProperty);
        set => SetValue(ParticleCountProperty, value);
    }

    public static DependencyProperty EmitterLocationProperty = DependencyProperty.Register("EmitterLocation",
        typeof(Media3D.Point3D),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(DefaultEmitterLocation.ToPoint3D(),
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).EmitterLocation =
                    ((Media3D.Point3D) e.NewValue).ToVector3();
            }));

    public Media3D.Point3D EmitterLocation {
        get => (Media3D.Point3D) GetValue(EmitterLocationProperty);
        set => SetValue(EmitterLocationProperty, value);
    }

    public static DependencyProperty ConsumerLocationProperty = DependencyProperty.Register("ConsumerLocation",
        typeof(Media3D.Point3D),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(DefaultConsumerLocation.ToPoint3D(),
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).ConsumerLocation =
                    ((Media3D.Point3D) e.NewValue).ToVector3();
            }));

    public Media3D.Point3D ConsumerLocation {
        get => (Media3D.Point3D) GetValue(ConsumerLocationProperty);
        set => SetValue(ConsumerLocationProperty, value);
    }

    public static DependencyProperty ParticleBoundsProperty = DependencyProperty.Register("ParticleBounds",
        typeof(Media3D.Rect3D),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(new Media3D.Rect3D(0, 0, 0, 100, 100, 100),
            (d, e) => {
                var bound = (Media3D.Rect3D) e.NewValue;
                ((d as Element3DCore).SceneNode as ParticleStormNode).DomainBoundMax = new Vector3(
                    (float) (bound.SizeX / 2 + bound.Location.X),
                    (float) (bound.SizeY / 2 + bound.Location.Y),
                    (float) (bound.SizeZ / 2 + bound.Location.Z));
                ((d as Element3DCore).SceneNode as ParticleStormNode).DomainBoundMin = new Vector3(
                    (float) (bound.Location.X - bound.SizeX / 2),
                    (float) (bound.Location.Y - bound.SizeY / 2),
                    (float) (bound.Location.Z - bound.SizeZ / 2));
            }));

    public Media3D.Rect3D ParticleBounds {
        get => (Media3D.Rect3D) GetValue(ParticleBoundsProperty);
        set => SetValue(ParticleBoundsProperty, value);
    }

    public static DependencyProperty EmitterRadiusProperty = DependencyProperty.Register("EmitterRadius",
        typeof(double),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(0.0,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).EmitterRadius =
                    (float) (double) e.NewValue;
            }));

    public double EmitterRadius {
        get => (double) GetValue(EmitterRadiusProperty);
        set => SetValue(EmitterRadiusProperty, value);
    }

    public static DependencyProperty ConsumerGravityProperty = DependencyProperty.Register("ConsumerGravity",
        typeof(double),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(0.0,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).ConsumerGravity =
                    (float) (double) e.NewValue;
            }));

    public double ConsumerGravity {
        get => (double) GetValue(ConsumerGravityProperty);
        set => SetValue(ConsumerGravityProperty, value);
    }

    public static DependencyProperty ConsumerRadiusProperty = DependencyProperty.Register("ConsumerRadius",
        typeof(double),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(0.0,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).ConsumerRadius =
                    (float) (double) e.NewValue;
            }));

    public double ConsumerRadius {
        get => (double) GetValue(ConsumerRadiusProperty);
        set => SetValue(ConsumerRadiusProperty, value);
    }

    public static DependencyProperty InitialEnergyProperty = DependencyProperty.Register("InitialEnergy",
        typeof(double),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(5.0,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).InitialEnergy =
                    Math.Max(1f, (float) (double) e.NewValue);
            }));

    public double InitialEnergy {
        get => (double) GetValue(InitialEnergyProperty);
        set => SetValue(InitialEnergyProperty, value);
    }

    public static DependencyProperty EnergyDissipationRateProperty = DependencyProperty.Register(
        "EnergyDissipationRate",
        typeof(double),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(1.0,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).EnergyDissipationRate =
                    Math.Max(1f, (float) (double) e.NewValue);
            }));

    public double EnergyDissipationRate {
        get => (double) GetValue(EnergyDissipationRateProperty);
        set => SetValue(EnergyDissipationRateProperty, value);
    }

    public static DependencyProperty RandomVectorGeneratorProperty = DependencyProperty.Register(
        "RandomVectorGenerator",
        typeof(IRandomVector),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(new UniformRandomVectorGenerator(),
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).RandomVectorGenerator =
                    (IRandomVector) e.NewValue;
            }));

    public IRandomVector RandomVectorGenerator {
        get => (IRandomVector) GetValue(RandomVectorGeneratorProperty);
        set => SetValue(RandomVectorGeneratorProperty, value);
    }

    public static DependencyProperty ParticleTextureProperty = DependencyProperty.Register("ParticleTexture",
        typeof(TextureModel),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(null,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).ParticleTexture =
                    (TextureModel) e.NewValue;
            }));

    public TextureModel? ParticleTexture {
        get => (TextureModel) GetValue(ParticleTextureProperty);
        set => SetValue(ParticleTextureProperty, value);
    }

    public static DependencyProperty NumTextureColumnProperty = DependencyProperty.Register("NumTextureColumn",
        typeof(int),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(1,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).NumTextureColumn =
                    (uint) Math.Max(1, (int) e.NewValue);
            }));

    public int NumTextureColumn {
        get => (int) GetValue(NumTextureColumnProperty);
        set => SetValue(NumTextureColumnProperty, value);
    }

    public static DependencyProperty NumTextureRowProperty = DependencyProperty.Register("NumTextureRow",
        typeof(int),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(1,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).NumTextureRow =
                    (uint) Math.Max(1, (int) e.NewValue);
            }));

    public int NumTextureRow {
        get => (int) GetValue(NumTextureRowProperty);
        set => SetValue(NumTextureRowProperty, value);
    }

    public static DependencyProperty ParticleSizeProperty = DependencyProperty.Register("ParticleSize",
        typeof(Size),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(new Size(1, 1),
            (d, e) => {
                var size = (Size) e.NewValue;
                ((d as Element3DCore).SceneNode as ParticleStormNode).ParticleSize =
                    new Vector2((float) size.Width, (float) size.Height);
            }));

    public Size ParticleSize {
        get => (Size) GetValue(ParticleSizeProperty);
        set => SetValue(ParticleSizeProperty, value);
    }


    public static DependencyProperty InitialVelocityProperty = DependencyProperty.Register("InitialVelocity",
        typeof(double),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(1.0,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).InitialVelocity =
                    (float) (double) e.NewValue;
            }));

    public double InitialVelocity {
        get => (double) GetValue(InitialVelocityProperty);
        set => SetValue(InitialVelocityProperty, value);
    }

    public static DependencyProperty AccelerationProperty = DependencyProperty.Register("Acceleration",
        typeof(Vector3D),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(DefaultAcceleration.ToVector3D(),
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).InitAcceleration =
                    ((Vector3D) e.NewValue).ToVector3();
            }));

    public Vector3D Acceleration {
        get => (Vector3D) GetValue(AccelerationProperty);
        set => SetValue(AccelerationProperty, value);
    }

    public static DependencyProperty CumulateAtBoundProperty = DependencyProperty.Register("CumulateAtBound",
        typeof(bool),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(false,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).CumulateAtBound =
                    (bool) e.NewValue;
            }));

    public bool CumulateAtBound {
        get => (bool) GetValue(CumulateAtBoundProperty);
        set => SetValue(CumulateAtBoundProperty, value);
    }

    public static DependencyProperty BlendColorProperty = DependencyProperty.Register("BlendColor",
        typeof(Media.Color),
        typeof(ParticleStormModel3D),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.White,
#else
        new PropertyMetadata(Media.Colors.White,
#endif
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).BlendColor =
                    ((Media.Color) e.NewValue).ToColor4();
            }));

    public Media.Color BlendColor {
        get => (Media.Color) GetValue(BlendColorProperty);
        set => SetValue(BlendColorProperty, value);
    }

    public static DependencyProperty AnimateSpriteByEnergyBoundProperty = DependencyProperty.Register(
        "AnimateSpriteByEnergy",
        typeof(bool),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(false,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).AnimateSpriteByEnergy =
                    (bool) e.NewValue;
            }));

    public bool AnimateSpriteByEnergy {
        get => (bool) GetValue(AnimateSpriteByEnergyBoundProperty);
        set => SetValue(AnimateSpriteByEnergyBoundProperty, value);
    }


    public double Turbulance {
        get => (double) GetValue(TurbulanceProperty);
        set => SetValue(TurbulanceProperty, value);
    }


    public static readonly DependencyProperty TurbulanceProperty =
        DependencyProperty.Register("Turbulance",
            typeof(double),
            typeof(ParticleStormModel3D),
            new PropertyMetadata(0.0,
                (d, e) => {
                    ((d as Element3DCore).SceneNode as ParticleStormNode)
                        .Turbulance = (float) (double) e.NewValue;
                }));


    public static DependencyProperty BlendProperty = DependencyProperty.Register("Blend",
        typeof(BlendOperation),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(BlendOperation.Add,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).Blend =
                    (BlendOperation) e.NewValue;
            }));

    public BlendOperation Blend {
        get => (BlendOperation) GetValue(BlendProperty);
        set => SetValue(BlendProperty, value);
    }

    public static DependencyProperty AlphaBlendProperty = DependencyProperty.Register("AlphaBlend",
        typeof(BlendOperation),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(BlendOperation.Add,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).AlphaBlend =
                    (BlendOperation) e.NewValue;
            }));

    public BlendOperation AlphaBlend {
        get => (BlendOperation) GetValue(AlphaBlendProperty);
        set => SetValue(AlphaBlendProperty, value);
    }

    public static DependencyProperty SourceBlendProperty = DependencyProperty.Register("SourceBlend",
        typeof(BlendOption),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(BlendOption.One,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).SourceBlend =
                    (BlendOption) e.NewValue;
            }));

    public BlendOption SourceBlend {
        get => (BlendOption) GetValue(SourceBlendProperty);
        set => SetValue(SourceBlendProperty, value);
    }

    public static DependencyProperty DestBlendProperty = DependencyProperty.Register("DestBlend",
        typeof(BlendOption),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(BlendOption.One,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).DestBlend =
                    (BlendOption) e.NewValue;
            }));

    public BlendOption DestBlend {
        get => (BlendOption) GetValue(DestBlendProperty);
        set => SetValue(DestBlendProperty, value);
    }

    public static DependencyProperty SourceAlphaBlendProperty = DependencyProperty.Register("SourceAlphaBlend",
        typeof(BlendOption),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(BlendOption.One,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).SourceAlphaBlend =
                    (BlendOption) e.NewValue;
            }));

    public BlendOption SourceAlphaBlend {
        get => (BlendOption) GetValue(SourceAlphaBlendProperty);
        set => SetValue(SourceAlphaBlendProperty, value);
    }

    public static DependencyProperty DestAlphaBlendProperty = DependencyProperty.Register("DestAlphaBlend",
        typeof(BlendOption),
        typeof(ParticleStormModel3D),
        new PropertyMetadata(BlendOption.Zero,
            (d, e) => {
                ((d as Element3DCore).SceneNode as ParticleStormNode).DestAlphaBlend =
                    (BlendOption) e.NewValue;
            }));

    public BlendOption DestAlphaBlend {
        get => (BlendOption) GetValue(DestAlphaBlendProperty);
        set => SetValue(DestAlphaBlendProperty, value);
    }

    /// <summary>
    ///     Gets or sets the blend factor for blending
    /// </summary>
    /// <value>
    ///     The blend factor.
    /// </value>
    public Media.Color BlendFactor {
        get => (Media.Color) GetValue(BlendFactorProperty);
        set => SetValue(BlendFactorProperty, value);
    }

    /// <summary>
    ///     The blend factor property
    /// </summary>
    public static readonly DependencyProperty BlendFactorProperty =
        DependencyProperty.Register("BlendFactor",
            typeof(Media.Color),
            typeof(ParticleStormModel3D),
#if WINUI
                new PropertyMetadata(Microsoft.UI.Colors.White, (d, e) =>
#else
            new PropertyMetadata(Media.Colors.White,
                (d, e) =>
#endif
                {
                    ((d as Element3DCore).SceneNode as ParticleStormNode)
                        .BlendFactor =
                        ((Media.Color) e.NewValue).ToColor4();
                }));


    /// <summary>
    ///     Gets or sets the sample mask used during blending
    /// </summary>
    /// <value>
    ///     The sample mask.
    /// </value>
    public int SampleMask {
        get => (int) GetValue(SampleMaskProperty);
        set => SetValue(SampleMaskProperty, value);
    }

    /// <summary>
    ///     The sample mask property
    /// </summary>
    public static readonly DependencyProperty SampleMaskProperty =
        DependencyProperty.Register("SampleMask",
            typeof(int),
            typeof(ParticleStormModel3D),
            new PropertyMetadata(-1,
                (d, e) => {
                    ((d as Element3DCore).SceneNode as ParticleStormNode)
                        .SampleMask = (int) e.NewValue;
                }));


    /// <summary>
    ///     List of instance matrix.
    /// </summary>
    public IList<Matrix> Instances {
        get => (IList<Matrix>) GetValue(InstancesProperty);
        set => SetValue(InstancesProperty, value);
    }

    /// <summary>
    ///     List of instance matrix.
    /// </summary>
    public static readonly DependencyProperty InstancesProperty =
        DependencyProperty.Register("Instances",
            typeof(IList<Matrix>),
            typeof(ParticleStormModel3D),
            new PropertyMetadata(null,
                (d, e) => {
                    ((d as Element3DCore).SceneNode as ParticleStormNode)
                        .Instances = e.NewValue as IList<Matrix>;
                }));

    /// <summary>
    ///     The enable view frustum check property
    /// </summary>
    public static readonly DependencyProperty EnableViewFrustumCheckProperty =
        DependencyProperty.Register("EnableViewFrustumCheck",
            typeof(bool),
            typeof(ParticleStormModel3D),
            new PropertyMetadata(true,
                (d, e) => {
                    ((d as Element3DCore).SceneNode as ParticleStormNode)
                        .EnableViewFrustumCheck = (bool) e.NewValue;
                }));

    #endregion


    protected override SceneNode OnCreateSceneNode() => new ParticleStormNode();

    protected override void AssignDefaultValuesToSceneNode(SceneNode node) {
        base.AssignDefaultValuesToSceneNode(node);
        if (node is ParticleStormNode c) {
            c.ParticleCount = ParticleCount;

            c.EmitterRadius = (float) EmitterRadius;
            c.ConsumerGravity = (float) ConsumerGravity;

            c.ConsumerRadius = (float) ConsumerRadius;
            c.InitialEnergy = (float) InitialEnergy;
            c.EnergyDissipationRate = (float) EnergyDissipationRate;
            c.RandomVectorGenerator = RandomVectorGenerator;
            c.ParticleTexture = ParticleTexture;
            c.NumTextureColumn = (uint) NumTextureColumn;
            c.NumTextureRow = (uint) NumTextureRow;
            c.ParticleSize = new Vector2((float) ParticleSize.Width, (float) ParticleSize.Height);
            c.InitialVelocity = (float) InitialVelocity;


            c.CumulateAtBound = CumulateAtBound;
            c.BlendColor = BlendColor.ToColor4();
            c.AnimateSpriteByEnergy = AnimateSpriteByEnergy;
            c.Turbulance = (float) Turbulance;
            c.Blend = Blend;
            c.AlphaBlend = AlphaBlend;
            c.SourceBlend = SourceBlend;
            c.DestBlend = DestBlend;
            c.SourceAlphaBlend = SourceAlphaBlend;
            c.DestAlphaBlend = DestAlphaBlend;
            c.SampleMask = SampleMask;
            c.BlendColor = BlendColor.ToColor4();
            c.EmitterLocation = EmitterLocation.ToVector3();
            c.ConsumerLocation = ConsumerLocation.ToVector3();
            c.InitAcceleration = Acceleration.ToVector3();
            c.DomainBoundMax = new Vector3((float) (ParticleBounds.SizeX / 2 + ParticleBounds.Location.X),
                (float) (ParticleBounds.SizeY / 2 + ParticleBounds.Location.Y),
                (float) (ParticleBounds.SizeZ / 2 + ParticleBounds.Location.Z));
            c.DomainBoundMin = new Vector3((float) (ParticleBounds.Location.X - ParticleBounds.SizeX / 2),
                (float) (ParticleBounds.Location.Y - ParticleBounds.SizeY / 2),
                (float) (ParticleBounds.Location.Z - ParticleBounds.SizeZ / 2));
        }
    }
}