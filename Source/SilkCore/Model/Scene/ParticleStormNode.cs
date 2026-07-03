/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/


using HelixToolkit.SharpDX.Core.Core;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core {
    namespace Model.Scene {
        /// <summary>
        /// </summary>
        public class ParticleStormNode : SceneNode, IInstancing, IBoundable {
            private volatile bool blendChanged = true;

            private bool enableViewFrustumCheck = true;

            public ParticleStormNode() {
                HasBound = true;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [enable view frustum check].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [enable view frustum check]; otherwise, <c>false</c>.
            /// </value>
            public bool EnableViewFrustumCheck {
                get => enableViewFrustumCheck && HasBound;
                set => enableViewFrustumCheck = value;
            }

            private ParticleRenderCore particleCore => RenderCore as ParticleRenderCore;

            /// <summary>
            ///     Gets the instance buffer.
            /// </summary>
            /// <value>
            ///     The instance buffer.
            /// </value>
            public IElementsBufferModel<Matrix> InstanceBuffer { get; } = new MatrixInstanceBufferModel();

            private void OnBlendStateChanged() {
                blendChanged = true;
            }

            /// <summary>
            ///     Called when [create render core].
            /// </summary>
            /// <returns></returns>
            protected override RenderCore OnCreateRenderCore() {
                return new ParticleRenderCore();
            }

            protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) {
                return effectsManager[DefaultRenderTechniqueNames.ParticleStorm];
            }

            protected override bool OnAttach(IEffectsManager effectsManager) {
                base.OnAttach(effectsManager);
                InstanceBuffer.Initialize();
                InstanceBuffer.Elements = Instances;
                particleCore.InstanceBuffer = InstanceBuffer;
                return true;
            }

            /// <summary>
            ///     Updates the specified context.
            /// </summary>
            /// <param name="context">The context.</param>
            public override void Update(RenderContext context) {
                base.Update(context);
                if (blendChanged) {
                    var desc = new BlendStateDescription();
                    desc.RenderTarget[0] = new RenderTargetBlendDescription {
                        IsBlendEnabled = true,
                        BlendOperation = Blend,
                        AlphaBlendOperation = AlphaBlend,
                        SourceBlend = SourceBlend,
                        DestinationBlend = DestBlend,
                        SourceAlphaBlend = SourceAlphaBlend,
                        DestinationAlphaBlend = DestAlphaBlend,
                        RenderTargetWriteMask = ColorWriteMaskFlags.All
                    };
                    particleCore.BlendDescription = desc;
                    blendChanged = false;
                }

                if (boundChanged) {
                    UpdateBounds();
                    boundChanged = false;
                }
            }

            private void UpdateBounds(bool transformOnly = false) {
                if (!transformOnly) {
                    originalBound = new BoundingBox(DomainBoundMin, DomainBoundMax);
                    originalBoundsSphere = BoundingSphereExtensions.FromBox(originalBound);
                    BoundingBox newBound;
                    BoundingSphere newBoundSphere;
                    if (HasInstances) {
                        newBound = OriginalBounds.Transform(Instances[0]);
                        newBoundSphere = OriginalBoundsSphere.TransformBoundingSphere(Instances[0]);
                        foreach (var instance in Instances) {
                            var b = OriginalBounds.Transform(instance);
                            BoundingBox.Merge(ref newBound, ref b, out newBound);
                            var bs = OriginalBoundsSphere.TransformBoundingSphere(instance);
                            BoundingSphereExtensions.Merge(ref newBoundSphere, ref bs, out newBoundSphere);
                        }
                    } else {
                        newBound = OriginalBounds;
                        newBoundSphere = OriginalBoundsSphere;
                    }

                    var old = bounds;
                    if (Set(ref bounds, newBound))
                        RaiseOnBoundChanged(new BoundChangeArgs<BoundingBox>(ref bounds, ref old));
                    var oldS = boundsSphere;
                    if (Set(ref boundsSphere, newBoundSphere))
                        RaiseOnBoundSphereChanged(new BoundChangeArgs<BoundingSphere>(ref boundsSphere, ref oldS));
                }

                var oldT = boundsWithTransform;
                if (Set(ref boundsWithTransform, bounds.Transform(ModelMatrix)))
                    RaiseOnTransformBoundChanged(new BoundChangeArgs<BoundingBox>(ref boundsWithTransform, ref oldT));
                var oldTS = boundsSphereWithTransform;
                if (Set(ref boundsSphereWithTransform, boundsSphere.TransformBoundingSphere(ModelMatrix)))
                    RaiseOnTransformBoundSphereChanged(
                        new BoundChangeArgs<BoundingSphere>(ref boundsSphereWithTransform, ref oldTS));
            }

            /// <summary>
            ///     Called when [detach].
            /// </summary>
            protected override void OnDetach() {
                InstanceBuffer.Dispose();
                base.OnDetach();
            }

            /// <summary>
            ///     Tests the view frustum.
            /// </summary>
            /// <param name="viewFrustum">The view frustum.</param>
            /// <returns></returns>
            public override bool TestViewFrustum(ref BoundingFrustum viewFrustum) {
                if (!EnableViewFrustumCheck) return true;
                return BoundingFrustumExtensions.Intersects(ref viewFrustum, ref boundsWithTransform);
            }

            public sealed override bool HitTest(HitTestContext context, ref List<HitTestResult> hits) {
                return false;
            }

            protected sealed override bool OnHitTest(
                HitTestContext context,
                Matrix totalModelMatrix,
                ref List<HitTestResult> hits
            ) {
                return false;
            }

        #region Properties

            /// <summary>
            ///     Gets or sets the particle count.
            /// </summary>
            /// <value>
            ///     The particle count.
            /// </value>
            public int ParticleCount {
                get => particleCore.ParticleCount;
                set => particleCore.ParticleCount = value;
            }

            /// <summary>
            ///     Gets or sets the emitter location.
            /// </summary>
            /// <value>
            ///     The emitter location.
            /// </value>
            public Vector3 EmitterLocation {
                get => particleCore.EmitterLocation;
                set => particleCore.EmitterLocation = value;
            }

            /// <summary>
            ///     Gets or sets the emitter radius.
            /// </summary>
            /// <value>
            ///     The emitter radius.
            /// </value>
            public float EmitterRadius {
                get => particleCore.EmitterRadius;
                set => particleCore.EmitterRadius = value;
            }

            /// <summary>
            ///     Gets or sets the consumer location.
            /// </summary>
            /// <value>
            ///     The consumer location.
            /// </value>
            public Vector3 ConsumerLocation {
                get => particleCore.ConsumerLocation;
                set => particleCore.ConsumerLocation = value;
            }

            /// <summary>
            ///     Gets or sets the consumer radius.
            /// </summary>
            /// <value>
            ///     The consumer radius.
            /// </value>
            public float ConsumerRadius {
                get => particleCore.ConsumerRadius;
                set => particleCore.ConsumerRadius = value;
            }

            /// <summary>
            ///     Gets or sets the consumer gravity.
            /// </summary>
            /// <value>
            ///     The consumer gravity.
            /// </value>
            public float ConsumerGravity {
                get => particleCore.ConsumerGravity;
                set => particleCore.ConsumerGravity = value;
            }

            /// <summary>
            ///     Gets or sets the initial energy.
            /// </summary>
            /// <value>
            ///     The initial energy.
            /// </value>
            public float InitialEnergy {
                get => particleCore.InitialEnergy;
                set {
                    particleCore.InitialEnergy = value;
                    particleCore.UpdateInsertThrottle();
                }
            }

            /// <summary>
            ///     Gets or sets the energy dissipation rate.
            /// </summary>
            /// <value>
            ///     The energy dissipation rate.
            /// </value>
            public float EnergyDissipationRate {
                get => particleCore.EnergyDissipationRate;
                set => particleCore.EnergyDissipationRate = value;
            }

            /// <summary>
            ///     Gets or sets the random vector generator.
            /// </summary>
            /// <value>
            ///     The random vector generator.
            /// </value>
            public IRandomVector RandomVectorGenerator {
                get => particleCore.VectorGenerator;
                set => particleCore.VectorGenerator = value;
            }

            /// <summary>
            ///     Gets or sets the particle texture.
            /// </summary>
            /// <value>
            ///     The particle texture.
            /// </value>
            public TextureModel ParticleTexture {
                get => particleCore.ParticleTexture;
                set => particleCore.ParticleTexture = value;
            }

            /// <summary>
            ///     Gets or sets the number texture column.
            /// </summary>
            /// <value>
            ///     The number texture column.
            /// </value>
            public uint NumTextureColumn {
                get => particleCore.NumTextureColumn;
                set => particleCore.NumTextureColumn = value;
            }

            /// <summary>
            ///     Gets or sets the number texture row.
            /// </summary>
            /// <value>
            ///     The number texture row.
            /// </value>
            public uint NumTextureRow {
                get => particleCore.NumTextureRow;
                set => particleCore.NumTextureRow = value;
            }

            /// <summary>
            ///     Gets or sets the size of the particle.
            /// </summary>
            /// <value>
            ///     The size of the particle.
            /// </value>
            public Vector2 ParticleSize {
                get => particleCore.ParticleSize;
                set => particleCore.ParticleSize = value;
            }

            /// <summary>
            ///     Gets or sets the initial velocity.
            /// </summary>
            /// <value>
            ///     The initial velocity.
            /// </value>
            public float InitialVelocity {
                get => particleCore.InitialVelocity;
                set => particleCore.InitialVelocity = value;
            }

            /// <summary>
            ///     Gets or sets the initialize acceleration.
            /// </summary>
            /// <value>
            ///     The initialize acceleration.
            /// </value>
            public Vector3 InitAcceleration {
                get => particleCore.InitialAcceleration;
                set => particleCore.InitialAcceleration = value;
            }

            private Vector3 domainBoundMax = ParticleRenderCore.DefaultBoundMaximum;

            /// <summary>
            ///     Gets or sets the domain bound maximum.
            /// </summary>
            /// <value>
            ///     The domain bound maximum.
            /// </value>
            public Vector3 DomainBoundMax {
                get => domainBoundMax;
                set {
                    if (Set(ref domainBoundMax, value)) {
                        particleCore.DomainBoundMax = value;
                        boundChanged = true;
                    }
                }
            }

            private Vector3 domainBoundMin = ParticleRenderCore.DefaultBoundMinimum;

            /// <summary>
            ///     Gets or sets the domain bound minimum.
            /// </summary>
            /// <value>
            ///     The domain bound minimum.
            /// </value>
            public Vector3 DomainBoundMin {
                get => domainBoundMin;
                set {
                    if (Set(ref domainBoundMin, value)) {
                        particleCore.DomainBoundMin = value;
                        boundChanged = true;
                    }
                }
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [cumulate at bound].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [cumulate at bound]; otherwise, <c>false</c>.
            /// </value>
            public bool CumulateAtBound {
                get => particleCore.CumulateAtBound;
                set => particleCore.CumulateAtBound = value;
            }

            /// <summary>
            ///     Gets or sets the color of the blend.
            /// </summary>
            /// <value>
            ///     The color of the blend.
            /// </value>
            public Color4 BlendColor {
                get => particleCore.ParticleBlendColor;
                set => particleCore.ParticleBlendColor = value;
            }

            /// <summary>
            ///     Gets or sets a value indicating whether [animate sprite by energy].
            /// </summary>
            /// <value>
            ///     <c>true</c> if [animate sprite by energy]; otherwise, <c>false</c>.
            /// </value>
            public bool AnimateSpriteByEnergy {
                get => particleCore.AnimateSpriteByEnergy;
                set => particleCore.AnimateSpriteByEnergy = value;
            }

            /// <summary>
            ///     Gets or sets the turbulance.
            /// </summary>
            /// <value>
            ///     The turbulance.
            /// </value>
            public float Turbulance {
                get => particleCore.Turbulance;
                set => particleCore.Turbulance = value;
            }

            private BlendOperation blend = BlendOperation.Add;

            /// <summary>
            ///     Gets or sets the blend.
            /// </summary>
            /// <value>
            ///     The blend.
            /// </value>
            public BlendOperation Blend {
                get => blend;
                set {
                    if (Set(ref blend, value)) OnBlendStateChanged();
                }
            }

            private BlendOperation alphaBlend = BlendOperation.Add;

            /// <summary>
            ///     Gets or sets the alpha blend.
            /// </summary>
            /// <value>
            ///     The alpha blend.
            /// </value>
            public BlendOperation AlphaBlend {
                get => alphaBlend;
                set {
                    if (Set(ref alphaBlend, value)) OnBlendStateChanged();
                }
            }

            private BlendOption sourceBlend = BlendOption.One;

            /// <summary>
            ///     Gets or sets the source blend.
            /// </summary>
            /// <value>
            ///     The source blend.
            /// </value>
            public BlendOption SourceBlend {
                get => sourceBlend;
                set {
                    if (Set(ref sourceBlend, value)) OnBlendStateChanged();
                }
            }

            private BlendOption destBlend = BlendOption.One;

            /// <summary>
            ///     Gets or sets the dest blend.
            /// </summary>
            /// <value>
            ///     The dest blend.
            /// </value>
            public BlendOption DestBlend {
                get => destBlend;
                set {
                    if (Set(ref destBlend, value)) OnBlendStateChanged();
                }
            }

            private BlendOption sourceAlphaBlend = BlendOption.One;

            /// <summary>
            ///     Gets or sets the source alpha blend.
            /// </summary>
            /// <value>
            ///     The source alpha blend.
            /// </value>
            public BlendOption SourceAlphaBlend {
                get => sourceAlphaBlend;
                set {
                    if (Set(ref sourceAlphaBlend, value)) OnBlendStateChanged();
                }
            }

            private BlendOption destAlphaBlend = BlendOption.Zero;

            /// <summary>
            ///     Gets or sets the dest alpha blend.
            /// </summary>
            /// <value>
            ///     The dest alpha blend.
            /// </value>
            public BlendOption DestAlphaBlend {
                get => destAlphaBlend;
                set {
                    if (Set(ref destAlphaBlend, value)) OnBlendStateChanged();
                }
            }

            /// <summary>
            ///     Gets or sets the blend factor for blending
            /// </summary>
            /// <value>
            ///     The blend factor.
            /// </value>
            public Color4 BlendFactor {
                get => particleCore.BlendFactor;
                set => particleCore.BlendFactor = value;
            }

            /// <summary>
            ///     Gets or sets the sample mask for blending
            /// </summary>
            /// <value>
            ///     The sample mask.
            /// </value>
            public int SampleMask {
                get => particleCore.SampleMask;
                set => particleCore.SampleMask = value;
            }

            private IList<Matrix> instances;

            /// <summary>
            ///     Gets or sets the instances.
            /// </summary>
            /// <value>
            ///     The instances.
            /// </value>
            public IList<Matrix> Instances {
                get => instances;
                set {
                    if (Set(ref instances, value)) {
                        InstanceBuffer.Elements = value;
                        boundChanged = true;
                    }
                }
            }

            /// <summary>
            ///     Gets a value indicating whether this instance has instances.
            /// </summary>
            /// <value>
            ///     <c>true</c> if this instance has instances; otherwise, <c>false</c>.
            /// </value>
            public bool HasInstances => InstanceBuffer.HasElements;

        #endregion

        #region IBoundable

            private BoundingBox originalBound = MaxBound;
            public override BoundingBox OriginalBounds => originalBound;

            private BoundingSphere originalBoundsSphere = MaxBoundSphere;
            public override BoundingSphere OriginalBoundsSphere => originalBoundsSphere;

            private BoundingBox bounds = MaxBound;
            public override BoundingBox Bounds => bounds;

            private BoundingBox boundsWithTransform = MaxBound;
            public override BoundingBox BoundsWithTransform => boundsWithTransform;

            private BoundingSphere boundsSphere;
            public override BoundingSphere BoundsSphere => boundsSphere;

            private BoundingSphere boundsSphereWithTransform;
            public override BoundingSphere BoundsSphereWithTransform => boundsSphereWithTransform;

            protected volatile bool boundChanged = true;

        #endregion
        }
    }
}
