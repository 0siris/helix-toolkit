using HelixToolkit.SharpDX.Core.Core.Abstract;
using HelixToolkit.SharpDX.Core.DefaultShaders;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Scene.Abstract;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.Render.DeviceContextProxy;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities.Buffers;
using ImGuiNET;
using Format = Silk.NET.DXGI.Format;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;

namespace WinFormsTest;

public class ImGuiNode : SceneNode {
    #region Custom Render Technique

    public const string ImGuiRenderTechnique = "ImGuiRender";

    public static InputElement[] VsInputImGui2D { get; } = [
        new("POSITION", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
        new("COLOR", 0, Format.FormatR8G8B8A8Unorm, InputElement.AppendAligned, 0),
    ];

    public static readonly TechniqueDescription RenderTechnique;

    static ImGuiNode() {
        RenderTechnique = new TechniqueDescription(ImGuiRenderTechnique) {
            InputLayoutDescription = new InputLayoutDescription(DefaultVsShaderByteCodes.VsSprite2D,
                VsInputImGui2D),
            PassDescriptions = [
                new ShaderPassDescription(DefaultPassNames.Default) {
                    ShaderList = [
                        DefaultVsShaderDescriptions.VsSprite2D,
                        DefaultPsShaderDescriptions.PsSprite2D,
                    ],
                    Topology = PrimitiveTopology.TriangleList,
                    BlendStateDescription = DefaultBlendStateDescriptions.BsAlphaBlend,
                    DepthStencilStateDescription = DefaultDepthStencilDescriptions.DssNoDepthNoStencil,
                    RasterStateDescription = DefaultRasterDescriptions.RsSpriteCw,
                }
            ]
        };
    }

    #endregion

    private ImGui2DBufferModel? bufferModel;

    private IntPtr fontAtlasId = 1;

    private bool newFrame;

    private TimeSpan previousTime = TimeSpan.Zero;

    public event EventHandler? UpdatingImGuiUi;

    public ImGuiNode() {
        AffectsGlobalVariable = true;
    }

    protected override RenderCore OnCreateRenderCore() => new ImGuiRenderCore();

    protected override void OnDetach() {
        RemoveAndDispose(ref bufferModel);
        if (RenderCore is ImGuiRenderCore renderCore)
            renderCore.TextureView = null;
        base.OnDetach();
    }

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager)
        => effectsManager[ImGuiRenderTechnique];

    public override void Update(RenderContext context) {
        base.Update(context);
        if (newFrame) {
            newFrame = false;
            ImGui.Render();
        }

        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2((int) (context.ActualWidth * context.DpiScale),
            (int) (context.ActualHeight * context.DpiScale));
        if (previousTime == TimeSpan.Zero) {
            previousTime = context.TimeStamp;
        }

        io.Framerate = (float) (context.TimeStamp - previousTime).TotalSeconds;
        previousTime = context.TimeStamp;
        ImGui.NewFrame();
        UpdatingImGuiUi?.Invoke(this, EventArgs.Empty);
    }

    protected override bool CanHitTest(HitTestContext? context) => false;

    protected override bool OnHitTest(HitTestContext context, Matrix totalModelMatrix, ref List<HitTestResult> hits)
        => false;

    protected override void OnAttached() {
        previousTime = TimeSpan.Zero;
        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        ImGui.GetIO()
            .Fonts.AddFontDefault();
        bufferModel = new ImGui2DBufferModel();
        if (RenderCore is not ImGuiRenderCore renderCore)
            throw new InvalidOperationException("The ImGui render core is required.");
        renderCore.Buffer = bufferModel;
        var io = ImGui.GetIO();
        unsafe {
            io.Fonts.GetTexDataAsRGBA32(out IntPtr textureData, out var width, out var height);
            if (EffectsManager is not {Device: { } device})
                throw new InvalidOperationException("An effects manager device is required.");
            var textureView = new ShaderResourceViewProxy(device);
            textureView.CreateView(textureData,
                width,
                height,
                Format.FormatR8G8B8A8Unorm);
            io.Fonts.SetTexID(fontAtlasId);
            io.Fonts.ClearTexData();
            renderCore.TextureView = textureView;
        }

        ImGui.NewFrame();
        newFrame = true;
        base.OnAttached();
    }
}

public sealed class ImGuiRenderCore : RenderCore {
    public ImGui2DBufferModel? Buffer { set; get; }

    public Matrix ProjectionMatrix { set; get; } = Matrix.Identity;

    public ShaderResourceViewProxy? TextureView;

    private int texSlot;

    private int samplerSlot;

    private ShaderPass? spritePass;

    private SamplerStateProxy? sampler;

    private readonly ConstantBufferComponent globalTransformCb;

    public ImGuiRenderCore()
        : base(RenderType.ScreenSpaced) {
        globalTransformCb = AddComponent(new ConstantBufferComponent(
            new ConstantBufferDescription(
                DefaultBufferNames.GlobalTransformCb,
                GlobalTransformStruct.SizeInBytes)));
    }

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        if (Buffer is not { } buffer || TextureView is not { } textureView
                                     || spritePass is not { } pass || sampler is not { } actualSampler || pass.IsNull) {
            return;
        }

        var io = ImGui.GetIO();
        ImGui.Render();
        if (!UpdateBuffer(deviceContext)) {
            return;
        }

        ProjectionMatrix = CreateOrthographicOffCenter(0f,
            io.DisplaySize.X / context.DpiScale,
            io.DisplaySize.Y / context.DpiScale,
            0.0f,
            -1.0f,
            1.0f);

        int slot = 0;
        if (EffectTechnique?.EffectsManager is not { } effectsManager
            || !buffer.AttachBuffers(deviceContext, ref slot, effectsManager)) {
            return;
        }

        var globalTrans = context.GlobalTransform;
        globalTrans.Projection = ProjectionMatrix;
        globalTransformCb.Upload(deviceContext, ref globalTrans);
        pass.BindShader(deviceContext);
        pass.BindStates(deviceContext, StateType.All);
        pass.PixelShader.BindTexture(deviceContext, texSlot, textureView);
        pass.PixelShader.BindSampler(deviceContext, samplerSlot, actualSampler);
        deviceContext.SetViewport(0, 0, io.DisplaySize.X, io.DisplaySize.Y);

        #region Render

        unsafe {
            var drawData = ImGui.GetDrawData();
            drawData.ScaleClipRects(new System.Numerics.Vector2(context.DpiScale, context.DpiScale));
            int idxOffset = 0;
            int vtxOffset = 0;
            for (int n = 0; n < drawData.CmdListsCount; n++) {
                var cmdList = drawData.CmdLists[n];
                for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++) {
                    var pcmd = &(((ImDrawCmd*) cmdList.CmdBuffer.Data)[cmdI]);
                    if (pcmd->UserCallback != IntPtr.Zero) { } else {
                        deviceContext.SetScissorRectangle((int) pcmd->ClipRect.X,
                            (int) pcmd->ClipRect.Y,
                            (int) (pcmd->ClipRect.Z),
                            (int) (pcmd->ClipRect.W));

                        deviceContext.DrawIndexed((int) pcmd->ElemCount,
                            idxOffset,
                            vtxOffset);
                    }

                    idxOffset += (int) pcmd->ElemCount;
                }

                vtxOffset += cmdList.VtxBuffer.Size;
            }

            #endregion
        }

        RaiseInvalidateRender();
    }

    private static Matrix CreateOrthographicOffCenter(
        float left,
        float right,
        float bottom,
        float top,
        float zNearPlane,
        float zFarPlane
    ) {
        var result = Matrix.Identity;
        result.M11 = 2.0f / (right - left);
        result.M22 = 2.0f / (top - bottom);
        result.M33 = 1.0f / (zNearPlane - zFarPlane);
        result.M41 = (left + right) / (left - right);
        result.M42 = (top + bottom) / (bottom - top);
        result.M43 = zNearPlane / (zNearPlane - zFarPlane);
        return result;
    }

    private bool UpdateBuffer(DeviceContextProxy deviceContext) {
        unsafe {
            var data = ImGui.GetDrawData();
            if (data.CmdListsCount == 0) {
                return false;
            }

            if (Buffer is not { } buffer)
                return false;

            buffer.SpriteCount = data.TotalVtxCount;
            buffer.IndexCount = data.TotalIdxCount;

            buffer.VertexBufferInternal.EnsureBufferCapacity(deviceContext, data.TotalVtxCount, data.TotalVtxCount * 2);
            buffer.IndexBufferInternal.EnsureBufferCapacity(deviceContext, data.TotalIdxCount, data.TotalIdxCount * 2);
            buffer.VertexBufferInternal.MapBuffer(deviceContext,
                (dataBox) => {
                    var ptr = dataBox.DataPointer;
                    for (int i = 0; i < data.CmdListsCount; i++) {
                        var cmdList = data.CmdLists[i];
                        int vCount = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
                        ptr = UnsafeHelper.Write(
                            ptr,
                            cmdList.VtxBuffer.Data,
                            0,
                            vCount);
                    }
                });
            buffer.IndexBufferInternal.MapBuffer(deviceContext,
                (dataBox) => {
                    var ptr = dataBox.DataPointer;
                    for (int i = 0; i < data.CmdListsCount; i++) {
                        var cmdList = data.CmdLists[i];
                        int iCount = cmdList.IdxBuffer.Size * sizeof(ushort);
                        ptr = UnsafeHelper.Write(
                            ptr,
                            cmdList.IdxBuffer.Data,
                            0,
                            iCount);
                    }
                });
        }

        return true;
    }

    protected override bool OnAttach(IRenderTechnique technique) {
        spritePass = technique[DefaultPassNames.Default];
        texSlot = spritePass.PixelShader.ShaderResourceViewMapping.TryGetBindSlot(DefaultBufferNames.SpriteTb);
        samplerSlot = spritePass.PixelShader.SamplerMapping.TryGetBindSlot(DefaultSamplerStateNames.SpriteSampler);
        if (EffectTechnique?.EffectsManager is not { } effectsManager)
            throw new InvalidOperationException("An effects manager is required.");
        sampler = effectsManager.StateManager.Register(DefaultSamplers.PointSamplerWrap);
        return true;
    }

    protected override void OnDetach() {
        TextureView = null;
        RemoveAndDispose(ref sampler);
    }
}

public sealed class ImGui2DBufferModel : DisposeObject, IGuid, IAttachableBufferModel {
    public PrimitiveTopology Topology { get; set; } = PrimitiveTopology.TriangleList;

    public IElementsBufferProxy[] VertexBuffer { get; } = new DynamicBufferProxy[1];

    public IEnumerable<int> VertexStructSize => VertexBuffer.Select(x => x.StructureSize);

    public IElementsBufferProxy IndexBuffer { get; }

    public Guid Guid { get; } = Guid.NewGuid();

    public int SpriteCount;
    public int IndexCount;

    [System.Diagnostics.CodeAnalysis.AllowNull]
    internal DynamicBufferProxy VertexBufferInternal;

    [System.Diagnostics.CodeAnalysis.AllowNull]
    internal DynamicBufferProxy IndexBufferInternal;

    public unsafe ImGui2DBufferModel() {
        VertexBufferInternal = new DynamicBufferProxy(sizeof(ImDrawVert), BindFlags.VertexBuffer);
        VertexBuffer[0] = VertexBufferInternal;
        IndexBuffer = IndexBufferInternal = new DynamicBufferProxy(sizeof(ushort), BindFlags.IndexBuffer);
    }

    public bool AttachBuffers(
        DeviceContextProxy context,
        ref int vertexBufferStartSlot,
        IDeviceResources deviceResources
    ) {
        if (SpriteCount == 0 || IndexCount == 0) {
            return false;
        }

        context.SetVertexBuffers(0,
            new VertexBufferBinding(VertexBufferInternal.Buffer,
                VertexBufferInternal.StructureSize,
                VertexBufferInternal.Offset));
        context.SetIndexBuffer(IndexBufferInternal.Buffer, Format.FormatR16Uint, IndexBufferInternal.Offset);
        return true;
    }

    public bool UpdateBuffers(DeviceContextProxy context, IDeviceResources deviceResources) => true;

    protected override void OnDispose(bool disposeManagedResources) {
        RemoveAndDispose(ref VertexBufferInternal);
        RemoveAndDispose(ref IndexBufferInternal);
        base.OnDispose(disposeManagedResources);
    }
}