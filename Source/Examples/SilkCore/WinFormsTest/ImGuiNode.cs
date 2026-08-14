using ImGuiNET;
using Format = Silk.NET.DXGI.Format;
using Matrix = Silk.NET.Maths.Matrix4X4<float>;

namespace HelixToolkit.SharpDX.Core.Model;

using Core;
using Core.Components;
using Scene;
using Render;
using Shaders;
using Utilities;

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

    private ImGui2DBufferModel bufferModel;

    private IntPtr fontAtlasId = (IntPtr)1;

    private bool newFrame = false;

    private TimeSpan previousTime = TimeSpan.Zero;

    public event EventHandler UpdatingImGuiUi;

    public ImGuiNode() {
        AffectsGlobalVariable = true;
    }

    protected override RenderCore OnCreateRenderCore() => new ImGuiRenderCore();

    protected override void OnDetach() {
        RemoveAndDispose(ref bufferModel);
        (RenderCore as ImGuiRenderCore).TextureView = null;
        base.OnDetach();
    }

    protected override IRenderTechnique OnCreateRenderTechnique(IEffectsManager effectsManager) => effectsManager[ImGuiRenderTechnique];

    public override void Update(RenderContext context) {
        base.Update(context);
        if (newFrame) {
            newFrame = false;
            ImGui.Render();
        }

        var io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2((int)(context.ActualWidth * context.DpiScale),
                                                     (int)(context.ActualHeight * context.DpiScale));
        if (previousTime == TimeSpan.Zero) {
            previousTime = context.TimeStamp;
        }

        io.Framerate = (float)(context.TimeStamp - previousTime).TotalSeconds;
        previousTime = context.TimeStamp;
        ImGui.NewFrame();
        UpdatingImGuiUi?.Invoke(this, EventArgs.Empty);
    }

    protected override bool CanHitTest(HitTestContext context) => false;

    protected override bool OnHitTest(HitTestContext context, Matrix totalModelMatrix, ref List<HitTestResult> hits) => false;

    protected override void OnAttached() {
        previousTime = TimeSpan.Zero;
        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        ImGui.GetIO().Fonts.AddFontDefault();
        bufferModel = new ImGui2DBufferModel();
        (RenderCore as ImGuiRenderCore).Buffer = bufferModel;
        var io = ImGui.GetIO();
        unsafe {
            io.Fonts.GetTexDataAsRGBA32(out IntPtr textureData, out var width, out var height);
            var textureView = new ShaderResourceViewProxy(EffectsManager.Device);
            textureView.CreateView(textureData,
                                   width,
                                   height,
                                   Format.FormatR8G8B8A8Unorm);
            io.Fonts.SetTexID(fontAtlasId);
            io.Fonts.ClearTexData();
            (RenderCore as ImGuiRenderCore).TextureView = textureView;
        }

        ImGui.NewFrame();
        newFrame = true;
        base.OnAttached();
    }
}

public sealed class ImGuiRenderCore : RenderCore {
    public ImGui2DBufferModel Buffer { set; get; }

    public Matrix ProjectionMatrix { set; get; } = Matrix.Identity;

    public ShaderResourceViewProxy TextureView;

    private int texSlot;

    private int samplerSlot;

    private ShaderPass spritePass;

    private SamplerStateProxy sampler;

    private readonly ConstantBufferComponent globalTransformCb;

    public ImGuiRenderCore()
        : base(RenderType.ScreenSpaced) {
        globalTransformCb = AddComponent(new ConstantBufferComponent(
                                             new ConstantBufferDescription(
                                                 DefaultBufferNames.GlobalTransformCb,
                                                 GlobalTransformStruct.SizeInBytes)));
    }

    public override void Render(RenderContext context, DeviceContextProxy deviceContext) {
        if (Buffer == null || TextureView == null || spritePass.IsNull) {
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
        if (!Buffer.AttachBuffers(deviceContext, ref slot, EffectTechnique.EffectsManager)) {
            return;
        }

        var globalTrans = context.GlobalTransform;
        globalTrans.Projection = ProjectionMatrix;
        globalTransformCb.Upload(deviceContext, ref globalTrans);
        spritePass.BindShader(deviceContext);
        spritePass.BindStates(deviceContext, StateType.All);
        spritePass.PixelShader.BindTexture(deviceContext, texSlot, TextureView);
        spritePass.PixelShader.BindSampler(deviceContext, samplerSlot, sampler);
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
                    var pcmd = &(((ImDrawCmd*)cmdList.CmdBuffer.Data)[cmdI]);
                    if (pcmd->UserCallback != IntPtr.Zero) { } else {
                        deviceContext.SetScissorRectangle((int)pcmd->ClipRect.X,
                                                          (int)pcmd->ClipRect.Y,
                                                          (int)(pcmd->ClipRect.Z),
                                                          (int)(pcmd->ClipRect.W));

                        deviceContext.DrawIndexed((int)pcmd->ElemCount,
                                                  idxOffset,
                                                  vtxOffset);
                    }

                    idxOffset += (int)pcmd->ElemCount;
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

            Buffer.SpriteCount = data.TotalVtxCount;
            Buffer.IndexCount = data.TotalIdxCount;

            Buffer.VertexBufferInternal.EnsureBufferCapacity(deviceContext, data.TotalVtxCount, data.TotalVtxCount * 2);
            Buffer.IndexBufferInternal.EnsureBufferCapacity(deviceContext, data.TotalIdxCount, data.TotalIdxCount * 2);
            Buffer.VertexBufferInternal.MapBuffer(deviceContext,
                                                  (dataBox) => {
                                                      var ptr = dataBox.DataPointer;
                                                      for (int i = 0; i < data.CmdListsCount; i++) {
                                                          var cmdList = data.CmdLists[i];
                                                          int vCount = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
                                                          ptr = UnsafeHelper.Write(
                                                              ptr,
                                                              (IntPtr)cmdList.VtxBuffer.Data,
                                                              0,
                                                              vCount);
                                                      }
                                                  });
            Buffer.IndexBufferInternal.MapBuffer(deviceContext,
                                                 (dataBox) => {
                                                     var ptr = dataBox.DataPointer;
                                                     for (int i = 0; i < data.CmdListsCount; i++) {
                                                         var cmdList = data.CmdLists[i];
                                                         int iCount = cmdList.IdxBuffer.Size * sizeof(ushort);
                                                         ptr = UnsafeHelper.Write(
                                                             ptr,
                                                             (IntPtr)cmdList.IdxBuffer.Data,
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
        sampler = EffectTechnique.EffectsManager.StateManager.Register(DefaultSamplers.PointSamplerWrap);
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

    public IEnumerable<int> VertexStructSize {
        get { return VertexBuffer.Select(x => x != null ? x.StructureSize : 0); }
    }

    public IElementsBufferProxy IndexBuffer { get; }

    public Guid Guid { get; } = Guid.NewGuid();

    public int SpriteCount;
    public int IndexCount;

    internal DynamicBufferProxy VertexBufferInternal;

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
        VertexBuffer[0] = null;
        base.OnDispose(disposeManagedResources);
    }
}
