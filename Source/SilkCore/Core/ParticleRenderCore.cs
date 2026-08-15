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
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
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
        set {
            if (Set(ref particleTexture, value) && IsAttached)
                OnTextureChanged();
        }
    }

    /// <summary>
    ///     Particle texture sampler description.
    /// </summary>
    public SamplerStateDescription SamplerDescription {
        get;
        set {
            if (Set(ref field, value) && IsAttached) {
                if (EffectTechnique is not { } technique) return;
                var newSampler = technique.EffectsManager.StateManager.Register(value);
                RemoveAndDispose(ref textureSampler);
                textureSampler = newSampler;
            }
        }
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

    private readonly ConstantBufferComponent perFrameCb;
    private readonly ConstantBufferComponent insertCb;
    private readonly ConstantBufferComponent modelCb;

    private ShaderResourceViewProxy? textureView;
    private SamplerStateProxy? textureSampler;
    private BlendStateProxy? blendState;
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

    //Buffer indirectArgsBuffer;
    private readonly ConstantBufferProxy particleCountGsiaBuffer
        = new("particleCount",
            ParticleCountIndirectArgs.SizeInBytes,
            BindFlags.None,
            CpuAccessFlags.None,
            ResourceOptionFlags.DrawIndirectArguments);

    private readonly ConstantBufferProxy particleCountStaging
        = new("particleStaging",
            4 * sizeof(int),
            BindFlags.None,
            CpuAccessFlags.Read,
            ResourceOptionFlags.None,
            ResourceUsage.Staging);

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

    /// <summary>
    ///     Gets or sets the buffer proxies.
    /// </summary>
    /// <value>
    ///     The buffer proxies.
    /// </value>
    protected UavBufferViewProxy?[] BufferProxies { get; } = new UavBufferViewProxy?[2];

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
        set {
            if (Set(ref blendDesc, value) && IsAttached)
                OnBlendStateChanged();
        }
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

    /// <summary>
    ///     Gets or sets the vertex layout.
    /// </summary>
    /// <value>
    ///     The vertex layout.
    /// </value>
    public InputLayoutProxy? VertexLayout { get; private set; }

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
        modelCb = AddComponent(new ConstantBufferComponent(new ConstantBufferDescription(
            DefaultBufferNames.ParticleModelCb,
            ParticleModelStruct.SizeInBytes)));
        perFrameCb = AddComponent(new ConstantBufferComponent(DefaultBufferNames.ParticleFrameCb,
            ParticlePerFrame.SizeInBytes));
        insertCb = AddComponent(new ConstantBufferComponent(DefaultBufferNames.ParticleCreateParameters,
            ParticleInsertParameters.SizeInBytes));
        NeedUpdate = true;
    }

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
    ///     Called when [attach].
    /// </summary>
    /// <param name="technique">The technique.</param>
    /// <returns></returns>
    protected override bool OnAttach(IRenderTechnique technique) {
        VertexLayout = technique.Layout;
        updatePass = technique[DefaultParticlePassNames.Update];
        insertPass = technique[DefaultParticlePassNames.Insert];
        renderPass = technique[DefaultParticlePassNames.Default];

        if (updatePass.GetShader(ShaderStage.Compute) is not { } updateShader
            || renderPass.GetShader(ShaderStage.Vertex) is not { } renderShader)
            return false;

        #region Get binding slots

        currentStateSlot = updateShader.UnorderedAccessViewMapping
            .TryGetBindSlot(CurrentSimStateUavBufferName);
        newStateSlot = updateShader.UnorderedAccessViewMapping
            .TryGetBindSlot(NewSimStateUavBufferName);

        renderStateSlot = renderShader.ShaderResourceViewMapping
            .TryGetBindSlot(SimStateBufferName);
        textureSlot = renderPass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(ShaderTextureBufferName);
        samplerSlot = renderPass.PixelShader.SamplerMapping.TryGetBindSlot(ShaderTextureSamplerName);

        #endregion

        if (isInitialParticleChanged) OnInitialParticleChanged(ParticleCount);
        textureSampler = technique.EffectsManager.StateManager.Register(SamplerDescription);
        OnTextureChanged();
        OnBlendStateChanged();
        return true;
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
        frameVariables.TimeFactors = (float) timeElapsed;
    }


    private void OnInitialParticleChanged(int count) {
        isInitialParticleChanged = true;
        if (count <= 0)
            return;

        if (bufferDesc.SizeInBytes <
            count * Particle.SizeInBytes) // Create new buffer, otherwise reuse existing buffers
        {
            DisposeBuffers();
            InitializeBuffers(count);
        }

        UpdateInsertThrottle();
        isInitialParticleChanged = false;
        isRestart = true;
        UpdateCanRenderFlag();
    }

    private void DisposeBuffers() {
        bufferDesc.SizeInBytes = 0;
        particleCountGsiaBuffer.DisposeAndClear();

        particleCountStaging.DisposeAndClear();

        for (var i = 0; i < BufferProxies.Length; ++i)
            RemoveAndDispose(ref BufferProxies[i]);
    }

    /// <summary>
    ///     Called when [detach].
    /// </summary>
    protected override void OnDetach() {
        DisposeBuffers();
        isInitialParticleChanged = true;
        RemoveAndDispose(ref textureSampler);
        RemoveAndDispose(ref blendState);
        RemoveAndDispose(ref textureView);
    }

    private void InitializeBuffers(int count) {
        if (Device is not { } device) return;

        bufferDesc.SizeInBytes = particleCount * Particle.SizeInBytes;
        uavBufferViewDesc.Buffer.ElementCount = particleCount;

        for (var i = 0; i < BufferProxies.Length; ++i)
            BufferProxies[i] = new UavBufferViewProxy(device,
                ref bufferDesc,
                ref uavBufferViewDesc,
                ref srvBufferViewDesc);

        particleCountStaging.CreateBuffer(device);
        particleCountGsiaBuffer.CreateBuffer(device);
    }

    private void OnTextureChanged() {
        if (EffectTechnique is not { } technique) return;
        var newView = technique.EffectsManager.MaterialTextureManager.Register(ParticleTexture);
        RemoveAndDispose(ref textureView);
        textureView = newView;
    }

    private void OnBlendStateChanged() {
        if (EffectTechnique is not { } technique) return;
        var newState = technique.EffectsManager.StateManager.Register(blendDesc);
        RemoveAndDispose(ref blendState);
        blendState = newState;
    }


    protected override bool OnUpdateCanRenderFlag() => base.OnUpdateCanRenderFlag() && !isInitialParticleChanged;

    /// <summary>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="deviceContext"></param>
    protected override void OnUpdate(RenderContext context, DeviceContextProxy deviceContext) {
        if (BufferProxies[0] is not { } currentBuffer
            || BufferProxies[1] is not { } nextBuffer
            || InstanceBuffer.Buffer is not { } instanceBuffer
            || particleCountGsiaBuffer.Buffer is not { } indirectBuffer)
            return;

        UpdateTime(context, ref totalElapsed);
        //Set correct instance count from instance buffer
        drawArgument.InstanceCount =
            !InstanceBuffer.HasElements
                ? 1
                : (uint) instanceBuffer.ElementCount;
        //Upload the draw argument
        particleCountGsiaBuffer.UploadDataToBuffer(deviceContext, ref drawArgument);

        updatePass.BindShader(deviceContext);
        updatePass.ComputeShader.BindUav(deviceContext, currentStateSlot, currentBuffer);
        updatePass.ComputeShader.BindUav(deviceContext, newStateSlot, nextBuffer);
        if (isRestart) {
            frameVariables.NumParticles = 0;
            perFrameCb.Upload(deviceContext, ref frameVariables);
            // Call ComputeShader to add initial particles
            deviceContext.Dispatch(1, 1, 1);
            isRestart = false;
        } else {
            #region Get consume buffer count

            // Get consume buffer count.
            //Due to some intel integrated graphic card having issue copy structure count directly into constant buffer.
            //Has to use staging buffer to read and pass into constant buffer              
            frameVariables.NumParticles = (uint) ReadCount(string.Empty, deviceContext, currentBuffer.Uav);
            perFrameCb.Upload(deviceContext, ref frameVariables);

            #endregion

            deviceContext.Dispatch(Math.Max(1, (int) Math.Ceiling((double) frameVariables.NumParticles / 512)),
                1,
                1);
            // Get append buffer count
            nextBuffer.CopyCount(deviceContext, indirectBuffer, 0);
        }

#if OUTPUTDEBUGGING
                ReadCount("UAV 0", deviceContext, BufferProxies[0].UAV);
#endif


        if (totalElapsed > InsertElapseThrottle) {
            insertCb.Upload(deviceContext, ref insertVariables);
            // Add more particles 
            insertPass.BindShader(deviceContext);
            insertPass.ComputeShader.BindUav(deviceContext, newStateSlot, nextBuffer);
            deviceContext.Dispatch(1, 1, 1);
            totalElapsed = 0;
#if OUTPUTDEBUGGING
                    ReadCount("UAV 1", deviceContext, BufferProxies[1].UAV);
#endif
        }

        // Swap UAV buffers for next frame
        (BufferProxies[0], BufferProxies[1]) = (nextBuffer, currentBuffer);
    }

    /// <summary>
    ///     Called when [render].
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="deviceContext">The device context.</param>
    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        if (textureSampler is not { } sampler || blendState is not { } state
            || BufferProxies[0] is not { } currentBuffer
            || particleCountGsiaBuffer.Buffer is not { } indirectBuffer
            || VertexLayout is not { } vertexLayout)
            return;
        OnUpdatePerModelStruct(context);
        perFrameCb.Upload(deviceContext, ref frameVariables);
        modelCb.Upload(deviceContext, ref modelStruct);
        // Clear binding
        updatePass.ComputeShader.BindUav(deviceContext, currentStateSlot, null);
        updatePass.ComputeShader.BindUav(deviceContext, newStateSlot, null);

        // Render existing particles
        renderPass.BindShader(deviceContext);
        renderPass.BindStates(deviceContext, StateType.RasterState | StateType.DepthStencilState);

        renderPass.VertexShader.BindTexture(deviceContext, renderStateSlot, currentBuffer);
        renderPass.PixelShader.BindTexture(deviceContext, textureSlot, textureView);
        renderPass.PixelShader.BindSampler(deviceContext, samplerSlot, sampler);
        deviceContext.InputLayout = vertexLayout;
        var firstSlot = 0;
        InstanceBuffer.AttachBuffer(deviceContext, ref firstSlot);
        deviceContext.SetBlendState(state, blendFactor, sampleMask);
        deviceContext.DrawInstancedIndirect(indirectBuffer, 0);
        RaiseInvalidateRender(); //Since particle is running all the time. Invalidate once finished rendering
    }


    private int ReadCount(string src, DeviceContextProxy context, UnorderedAccessView uav) {
        if (particleCountStaging.Buffer is not { } stagingBuffer) return 0;
        context.CopyStructureCount(stagingBuffer, 0, uav);
        var db = context.MapSubresource(stagingBuffer, MapMode.Read, MapFlags.None);
        var currentParticleCount = UnsafeHelper.Read<int>(db.DataPointer);
#if OUTPUTDEBUGGING
                if (Logger.IsEnabled(LogLevel.Debug))
                {
                    Logger.Debug("{Value0}: {Value1}", src, currentParticleCount);
                }
#endif
        context.UnmapSubresource(stagingBuffer, 0);
        return currentParticleCount;
    }
}
