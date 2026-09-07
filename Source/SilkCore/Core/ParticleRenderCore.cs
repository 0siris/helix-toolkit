/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
//#if DEBUG
//#define OUTPUTDEBUGGING
//#endif

using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.Core.Buffers;
using HelixToolkit.SharpDX.Core.Core.Components;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model;
using HelixToolkit.SharpDX.Core.Model.Material;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;

namespace HelixToolkit.SharpDX.Core.Core;

/// <summary>
/// </summary>
public class ParticleRenderCore : RenderCore {
    public static readonly int DefaultParticleCount = 512;
    public static readonly float DefaultInitialVelocity = 1f;
    public static readonly Vector3 DefaultAcceleration = new(0, 0.1f, 0);
    public static readonly Vector2 DefaultParticleSize = new(1, 1);
    public static readonly Vector3 DefaultEmitterLocation = Vector3.Zero;
    public static readonly Vector3 DefaultConsumerLocation = new(0, 10, 0);
    public static readonly float DefaultConsumerGravity = 0;
    public static readonly float DefaultConsumerRadius = 0;
    public static readonly Vector3 DefaultBoundMaximum = new(5, 5, 5);
    public static readonly Vector3 DefaultBoundMinimum = new(-5, -5, -5);
    public static readonly float DefaultInitialEnergy = 5;
    public static readonly float DefaultEnergyDissipationRate = 1f;

    #region variables

    /// <summary>
    ///     Texture tile columns
    /// </summary>
    public uint NumTextureColumn {
        get => frameVariables.NumTexCol;
        set => frameVariables.NumTexCol = value;
    }

    /// <summary>
    ///     Texture tile rows
    /// </summary>
    public uint NumTextureRow {
        get => frameVariables.NumTexRow;
        set => frameVariables.NumTexRow = value;
    }

    /// <summary>
    ///     Change Sprite based on particle energy, sequence from (1,1) to (NumTextureRow, NumTextureColumn) evenly divided by
    ///     tile counts
    /// </summary>
    public bool AnimateSpriteByEnergy {
        get => frameVariables.AnimateByEnergyLevel == 1;
        set => frameVariables.AnimateByEnergyLevel = value
            ? 1
            : 0;
    }

    public float Turbulance {
        get => frameVariables.Turbulance;
        set => frameVariables.Turbulance = value;
    }

    /// <summary>
    ///     Minimum time elapse to insert new particles
    /// </summary>
    public float InsertElapseThrottle { get; private set; }

    private double prevTimeMillis;

    /// <summary>
    ///     Random generator, used to generate particle for different direction, etc
    /// </summary>
    public IRandomVector VectorGenerator { get; set; } = new UniformRandomVectorGenerator();

    private bool isRestart = true;

    private bool isInitialParticleChanged = true;

    private int particleCount = DefaultParticleCount;

    /// <summary>
    ///     Maximum Particle count
    /// </summary>
    public int ParticleCount {
        get => particleCount;
        set {
            if (particleCount == value) return;
            particleCount = value;
            if (IsAttached)
                OnInitialParticleChanged(value);
        }
    }

    private TextureModel? particleTexture;

    /// <summary>
    ///     Particle Texture
    /// </summary>
    public TextureModel? ParticleTexture {
        get => particleTexture;
        set => Set(ref particleTexture, value);
    }

    /// <summary>
    ///     Particle texture sampler description.
    /// </summary>
    public SamplerStateDescription SamplerDescription {
        get;
        set => Set(ref field, value);
    } = DefaultSamplers.LinearSamplerWrapAni1;

    /// <summary>
    ///     Gets a value indicating whether this instance has texture.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance has texture; otherwise, <c>false</c>.
    /// </value>
    public bool HasTexture => particleTexture != null;

    /// <summary>
    ///     Particle Size
    /// </summary>
    public Vector2 ParticleSize {
        get => frameVariables.ParticleSize;
        set => frameVariables.ParticleSize = value;
    }

    /// <summary>
    /// </summary>
    public Vector3 EmitterLocation {
        get => insertVariables.EmitterLocation;
        set => insertVariables.EmitterLocation = value;
    }

    /// <summary>
    /// </summary>
    public bool CumulateAtBound {
        get => frameVariables.CumulateAtBound == 1;
        set => frameVariables.CumulateAtBound = value
            ? 1u
            : 0;
    }

    /// <summary>
    /// </summary>
    public Vector3 ExtraAcceleration {
        get => frameVariables.ExtraAcceleration;
        set => frameVariables.ExtraAcceleration = value;
    }

    /// <summary>
    ///     Gets or sets the domain bound maximum.
    /// </summary>
    /// <value>
    ///     The domain bound maximum.
    /// </value>
    public Vector3 DomainBoundMax {
        get => frameVariables.DomainBoundsMax;
        set => frameVariables.DomainBoundsMax = value;
    }

    /// <summary>
    ///     Gets or sets the domain bound minimum.
    /// </summary>
    /// <value>
    ///     The domain bound minimum.
    /// </value>
    public Vector3 DomainBoundMin {
        get => frameVariables.DomainBoundsMin;
        set {
            if (frameVariables.DomainBoundsMin != value) {
                frameVariables.DomainBoundsMin = value;
                RaiseInvalidateRender();
            }
        }
    }

    /// <summary>
    ///     Gets or sets the consumer gravity.
    /// </summary>
    /// <value>
    ///     The consumer gravity.
    /// </value>
    public float ConsumerGravity {
        get => frameVariables.ConsumerGravity;
        set => frameVariables.ConsumerGravity = value;
    }

    /// <summary>
    ///     Gets or sets the consumer location.
    /// </summary>
    /// <value>
    ///     The consumer location.
    /// </value>
    public Vector3 ConsumerLocation {
        get => frameVariables.ConsumerLocation;
        set => frameVariables.ConsumerLocation = value;
    }

    /// <summary>
    ///     Gets or sets the consumer radius.
    /// </summary>
    /// <value>
    ///     The consumer radius.
    /// </value>
    public float ConsumerRadius {
        get => frameVariables.ConsumerRadius;
        set => frameVariables.ConsumerRadius = value;
    }

    /// <summary>
    ///     Gets or sets the energy dissipation rate.
    /// </summary>
    /// <value>
    ///     The energy dissipation rate.
    /// </value>
    public float EnergyDissipationRate {
        get => insertVariables.EnergyDissipationRate;
        set => insertVariables.EnergyDissipationRate = value;
    }

    /// <summary>
    ///     Gets or sets the initial acceleration.
    /// </summary>
    /// <value>
    ///     The initial acceleration.
    /// </value>
    public Vector3 InitialAcceleration {
        get => insertVariables.InitialAcceleration;
        set => insertVariables.InitialAcceleration = value;
    }

    /// <summary>
    ///     Gets or sets the initial energy.
    /// </summary>
    /// <value>
    ///     The initial energy.
    /// </value>
    public float InitialEnergy {
        get => insertVariables.InitialEnergy;
        set => insertVariables.InitialEnergy = value;
    }

    /// <summary>
    ///     Gets or sets the initial velocity.
    /// </summary>
    /// <value>
    ///     The initial velocity.
    /// </value>
    public float InitialVelocity {
        get => insertVariables.InitialVelocity;
        set => insertVariables.InitialVelocity = value;
    }

    /// <summary>
    ///     Gets or sets the color of the particle blend.
    /// </summary>
    /// <value>
    ///     The color of the particle blend.
    /// </value>
    public Color4 ParticleBlendColor {
        get => insertVariables.ParticleBlendColor;
        set => insertVariables.ParticleBlendColor = value;
    }

    /// <summary>
    ///     Gets or sets the emitter radius.
    /// </summary>
    /// <value>
    ///     The emitter radius.
    /// </value>
    public float EmitterRadius {
        get => insertVariables.EmitterRadius;
        set => insertVariables.EmitterRadius = value;
    }

    /// <summary>
    ///     Particle per frame parameters
    /// </summary>
    private ParticlePerFrame frameVariables = new() {
        ExtraAcceleration = DefaultAcceleration,
        CumulateAtBound = 0,
        DomainBoundsMax = DefaultBoundMaximum,
        DomainBoundsMin = DefaultBoundMinimum,
        ConsumerGravity = DefaultConsumerGravity,
        ConsumerLocation = DefaultConsumerLocation,
        ConsumerRadius = DefaultConsumerRadius
    };

    /// <summary>
    ///     Particle insert parameters
    /// </summary>
    private ParticleInsertParameters insertVariables = new() {
        EmitterLocation = DefaultEmitterLocation,
        EmitterRadius = DefaultConsumerRadius,
        EnergyDissipationRate = DefaultEnergyDissipationRate,
        InitialAcceleration = DefaultAcceleration,
        InitialEnergy = DefaultInitialEnergy,
        InitialVelocity = DefaultInitialVelocity,
        ParticleBlendColor = Color.White.ToColor4()
    };

    #region ShaderVariables

    private ShaderPass updatePass = ShaderPass.NullPass;
    private ShaderPass insertPass = ShaderPass.NullPass;
    private ShaderPass renderPass = ShaderPass.NullPass;
    private double totalElapsed;
    private ParticleModelStruct modelStruct;

    #endregion

    #region Buffers

    /// <summary>
    ///     Gets or sets the instance buffer.
    /// </summary>
    /// <value>
    ///     The instance buffer.
    /// </value>
    public IElementsBufferModel InstanceBuffer {
        get;
        set { Set(ref field, value); }
    } = MatrixInstanceBufferModel.Empty;

    private BufferDescription bufferDesc = new() {
        BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource,
        OptionFlags = ResourceOptionFlags.BufferStructured,
        StructureByteStride = Particle.SizeInBytes,
        CpuAccessFlags = CpuAccessFlags.None,
        Usage = ResourceUsage.Default
    };

    private UnorderedAccessViewDescription uavBufferViewDesc = new() {
        Dimension = UnorderedAccessViewDimension.Buffer,
        Format = Format.FormatUnknown,
        Buffer = new UnorderedAccessViewDescription.BufferResource {
            FirstElement = 0,
            Flags = UnorderedAccessViewBufferFlags.Append
        }
    };

    private ShaderResourceViewDescription srvBufferViewDesc = new() {
        Dimension = ShaderResourceViewDimension.Buffer
    };

    private ParticleCountIndirectArgs drawArgument;

    #endregion

    private BlendStateDescription blendDesc = new() {
        IndependentBlendEnable = false,
        AlphaToCoverageEnable = false
    };

    /// <summary>
    ///     Particle blend state description
    /// </summary>
    public BlendStateDescription BlendDescription {
        get => blendDesc;
        set => Set(ref blendDesc, value);
    }

    private Color4 blendFactor = Color.White;

    /// <summary>
    ///     Gets or sets the blend factor used for blending.
    /// </summary>
    /// <value>
    ///     The blend factor.
    /// </value>
    public Color4 BlendFactor {
        get => blendFactor;
        set => SetAffectsRender(ref blendFactor, value);
    }

    private int sampleMask = -1;

    /// <summary>
    ///     Gets or sets the sample mask used for blending.
    /// </summary>
    /// <value>
    ///     The sample mask.
    /// </value>
    public int SampleMask {
        get => sampleMask;
        set => SetAffectsRender(ref sampleMask, value);
    }

    #region Shader Variable Names

    /// <summary>
    ///     Set current sim state variable name inside compute shader for binding
    /// </summary>
    public string CurrentSimStateUavBufferName { get; set; } = DefaultBufferNames.CurrentSimulationStateUb;

    /// <summary>
    ///     Set new sim state variable name inside compute shader for binding
    /// </summary>
    public string NewSimStateUavBufferName { get; set; } = DefaultBufferNames.NewSimulationStateUb;

    /// <summary>
    ///     Set sim state name inside vertex shader for binding
    /// </summary>
    public string SimStateBufferName { get; set; } = DefaultBufferNames.SimulationStateTb;

    /// <summary>
    ///     Set texture variable name inside shader for binding
    /// </summary>
    public string ShaderTextureBufferName { get; set; } = DefaultBufferNames.ParticleMapTb;

    /// <summary>
    ///     Set texture sampler variable name inside shader for binding
    /// </summary>
    public string ShaderTextureSamplerName { get; set; } = DefaultSamplerStateNames.ParticleTextureSampler;

    #endregion

    private int currentStateSlot;
    private int newStateSlot;
    private int renderStateSlot;
    private int textureSlot;
    private int samplerSlot;

    #endregion

    public ParticleRenderCore() : base(RenderType.Particle) {
    }

    /// <inheritdoc />
    protected override bool OnAttachD3D12() {
        if (ParticleCount <= 0) return false;
        OnInitialParticleChanged(ParticleCount);
        prevD3D12Time = double.NaN;
        return true;
    }

    /// <inheritdoc />
    protected override void OnDetachD3D12() {
        isInitialParticleChanged = true;
        isRestart = true;
        prevD3D12Time = double.NaN;
    }

    /// <summary>
    ///     Creates the existing particle constant-buffer payloads for one Direct3D 12 frame.
    /// </summary>
    /// <param name="timeStamp">The current frame timestamp in seconds.</param>
    /// <param name="frame">The b7 simulation and geometry-shader payload.</param>
    /// <param name="insert">The b8 particle-insertion payload.</param>
    /// <param name="model">The b4 particle-model payload.</param>
    /// <returns>Whether the insertion compute pass must run this frame.</returns>
    internal bool PrepareD3D12(
        double timeStamp,
        out ParticlePerFrame frame,
        out ParticleInsertParameters insert,
        out ParticleModelStruct model
    ) {
        if (!double.IsFinite(timeStamp)) throw new ArgumentOutOfRangeException(nameof(timeStamp));
        var elapsed = double.IsNaN(prevD3D12Time) ? 0 : Math.Max(0, timeStamp - prevD3D12Time);
        prevD3D12Time = timeStamp;
        totalElapsed += elapsed;
        frameVariables.TimeFactors = (float)elapsed;
        frameVariables.RandomVector = VectorGenerator.RandomVector3;
        frameVariables.MaxParticles = checked((uint)ParticleCount);
        modelStruct.World = ModelMatrix;
        modelStruct.HasInstances = InstanceBuffer.HasElements ? 1 : 0;
        modelStruct.HasTexture = HasTexture ? 1 : 0;
        var shouldInsert = isRestart || totalElapsed > InsertElapseThrottle;
        if (shouldInsert) totalElapsed = 0;
        isRestart = false;
        frame = frameVariables;
        insert = insertVariables;
        model = modelStruct;
        RaiseInvalidateRender();
        return shouldInsert;
    }

    /// <summary>
    ///     Gets whether the current Direct3D 12 allocation must be recreated for a changed particle limit.
    /// </summary>
    /// <param name="capacity">The existing native capacity.</param>
    /// <returns>Whether the allocation is too small or belongs to a restarted simulation.</returns>
    internal bool RequiresD3D12Recreate(uint capacity) =>
        capacity < checked((uint)ParticleCount) || isInitialParticleChanged;

    /// <summary>
    ///     Marks the native particle allocation synchronized with the current maximum count.
    /// </summary>
    internal void CompleteD3D12Recreate() {
        isInitialParticleChanged = false;
        isRestart = true;
        UpdateInsertThrottle();
        UpdateCanRenderFlag();
    }

    /// <summary>
    ///     The previous Direct3D 12 frame timestamp in seconds.
    /// </summary>
    private double prevD3D12Time = double.NaN;

    private void OnUpdatePerModelStruct(RenderContext context) {
        modelStruct.World = ModelMatrix;
        modelStruct.HasInstances = InstanceBuffer.HasElements
            ? 1
            : 0;
        modelStruct.HasTexture = HasTexture
            ? 1
            : 0;
        frameVariables.RandomVector = VectorGenerator.RandomVector3;
    }

    /// <summary>
    ///     Updates the insert throttle.
    /// </summary>
    public void UpdateInsertThrottle() {
        InsertElapseThrottle = 8.0f * insertVariables.InitialEnergy / insertVariables.EnergyDissipationRate /
                               Math.Max(0, particleCount + 8);
    }

    private void UpdateTime(RenderContext context, ref double totalElapsed) {
        var timeElapsed = Math.Max(0, (context.TimeStamp.TotalMilliseconds - prevTimeMillis) / 1000);
        prevTimeMillis = context.TimeStamp.TotalMilliseconds;
        totalElapsed += timeElapsed;
        //Update perframe variables
        frameVariables.TimeFactors = (float)timeElapsed;
    }


    private void OnInitialParticleChanged(int count) {
        isInitialParticleChanged = true;
        if (count <= 0)
            return;

        UpdateInsertThrottle();
        isInitialParticleChanged = false;
        isRestart = true;
        UpdateCanRenderFlag();
    }

    protected override bool OnUpdateCanRenderFlag() => base.OnUpdateCanRenderFlag() && !isInitialParticleChanged;
}
