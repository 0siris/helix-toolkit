/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core {
    namespace Shaders {
        /// <summary>
        /// </summary>
        public static class DefaultVSShaderByteCodes {
            /// <summary>
            /// </summary>
            public static string VSMeshDefault { get; } = "vsMeshDefault";

            /// <summary>
            ///     Gets the vs mesh batched.
            /// </summary>
            /// <value>
            ///     The vs mesh batched.
            /// </value>
            public static string VSMeshBatched { get; } = "vsMeshBatched";

            /// <summary>
            /// </summary>
            public static string VSMeshTessellation { get; } = "vsMeshTessellation";

            /// <summary>
            /// </summary>
            public static string VSMeshShadow { get; } = "vsMeshShadow";

            /// <summary>
            ///     Gets the vs mesh depth.
            /// </summary>
            /// <value>
            ///     The vs mesh depth.
            /// </value>
            public static string VSMeshDepth { get; } = "vsMeshDepth";

            /// <summary>
            /// </summary>
            public static string VSMeshBatchedShadow { get; } = "vsMeshBatchedShadow";

            /// <summary>
            ///     Gets the vs mesh batched ssao.
            /// </summary>
            /// <value>
            ///     The vs mesh batched ssao.
            /// </value>
            public static string VSMeshBatchedSSAO { get; } = "vsMeshBatchedSSAO";

            /// <summary>
            /// </summary>
            public static string VSMeshInstancing { get; } = "vsMeshInstancing";

            public static string VSMeshSSAO { get; } = "vsMeshSSAO";

            /// <summary>
            /// </summary>
            public static string VSMeshInstancingTessellation { get; } = "vsMeshInstancingTessellation";

            public static string VSMeshBoneSkinningBasic { get; } = "vsBoneSkinningBasic";

            /// <summary>
            /// </summary>
            public static string VSPoint { get; } = "vsPoint";

            /// <summary>
            /// </summary>
            public static string VSPointShadow { get; } = "vsPointShadow";

            /// <summary>
            /// </summary>
            public static string VSBillboard { get; } = "vsBillboard";

            /// <summary>
            /// </summary>
            public static string VSBillboardInstancing { get; } = "vsBillboardInstancing";

            /// <summary>
            /// </summary>
            public static string VSMeshClipPlane { get; } = "vsMeshClipPlane";

            /// <summary>
            /// </summary>
            public static string VSMeshClipPlaneQuad { get; } = "vsMeshClipPlaneQuad";

            /// <summary>
            /// </summary>
            public static string VSParticle { get; } = "vsParticle";

            /// <summary>
            /// </summary>
            public static string VSSkybox { get; } = "vsSkybox";

            /// <summary>
            /// </summary>
            public static string VSMeshWireframe { get; } = "vsMeshWireframe";

            /// <summary>
            ///     Gets the vs mesh batched wireframe.
            /// </summary>
            /// <value>
            ///     The vs mesh batched wireframe.
            /// </value>
            public static string VSMeshBatchedWireframe { get; } = "vsMeshBatchedWireframe";

            /// <summary>
            /// </summary>
            public static string VSMeshBoneSkinningWireframe { get; } = "vsBoneSkinningWireframe";

            /// <summary>
            /// </summary>
            public static string VSMeshOutlineP1 { get; } = "vsMeshOutlinePass1";


            /// <summary>
            /// </summary>
            public static string VSMeshOutlineScreenQuad { get; } = "vsMeshOutlineScreenQuad";

            /// <summary>
            ///     Gets the vs plane grid.
            /// </summary>
            /// <value>
            ///     The vs plane grid.
            /// </value>
            public static string VSPlaneGrid { get; } = "vsPlaneGrid";

            public static string VSScreenQuad { get; } = "vsScreenQuad";

            public static string VSSprite2D { get; } = "vsSprite";

            public static string VSVolume3D { get; } = "vsVolume";

            public static string VSSSAO { get; } = "vsSSAO";
#if !WINDOWS_UWP
            /// <summary>
            /// </summary>
            public static string VSScreenDup { get; } = "vsScreenDup";

            /// <summary>
            /// </summary>
            public static string VSScreenDupCursor { get; } = "vsScreenDupCursor";
#endif
        }


        /// <summary>
        /// </summary>
        public static class DefaultInputLayout {
            /// <summary>
            /// </summary>
            public static readonly InputElement[] VSInput = [
                new InputElement("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("NORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("TANGENT", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("BINORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 1),
                new InputElement("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 2),
                //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
                new InputElement("TEXCOORD",
                                 1,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 3,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 2,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 3,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 3,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 3,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 4,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 3,
                                 InputClassification.PerInstanceData,
                                 1)
            ];

            public static InputElement[] VSMeshBatchedInput = [
                new InputElement("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("NORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("TANGENT", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("BINORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR", 1, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0)
            ];

            /// <summary>
            /// </summary>
            public static readonly InputElement[] VSInputInstancing = [
                new InputElement("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("NORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("TANGENT", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("BINORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 1),
                new InputElement("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 2),
                //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
                new InputElement("TEXCOORD",
                                 1,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 3,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 2,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 3,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 3,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 3,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 4,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 3,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("COLOR",
                                 1,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 4,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("COLOR",
                                 2,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 4,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 5,
                                 Format.FormatR32G32Float,
                                 InputElement.AppendAligned,
                                 4,
                                 InputClassification.PerInstanceData,
                                 1)
            ];

            /// <summary>
            ///     Gets the vs input bone skinned basic.
            /// </summary>
            /// <value>
            ///     The vs input bone skinned basic.
            /// </value>
            public static readonly InputElement[] VSInputBoneSkinnedBasic = [
                new InputElement("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("NORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("TANGENT", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("BINORMAL", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0),
                new InputElement("BONEIDS", 0, Format.FormatR32G32B32A32Sint, InputElement.AppendAligned, 1),
                new InputElement("BONEWEIGHTS", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 1)
            ];

            /// <summary>
            /// </summary>
            public static readonly InputElement[] VSInputPoint = [
                new InputElement("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
                new InputElement("TEXCOORD",
                                 0,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 1,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 2,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 3,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1)
            ];

            /// <summary>
            /// </summary>
            public static readonly InputElement[] VSInputBillboard = [
                new InputElement("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR", 1, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 1, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 2, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 3, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 4, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 5, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
                new InputElement("TEXCOORD",
                                 6,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 7,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 8,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 9,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1)
            ];

            /// <summary>
            ///     Gets the vs input billboard instancing.
            /// </summary>
            /// <value>
            ///     The vs input billboard instancing.
            /// </value>
            public static readonly InputElement[] VSInputBillboardInstancing = [
                new InputElement("POSITION", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR", 1, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 1, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 2, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 3, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 4, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 5, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                //INSTANCING: die 4 texcoords sind die matrix, die mit jedem buffer reinwandern
                new InputElement("TEXCOORD",
                                 6,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 7,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 8,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 9,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 1,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("COLOR",
                                 2,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 2,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 10,
                                 Format.FormatR32G32Float,
                                 InputElement.AppendAligned,
                                 2,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 11,
                                 Format.FormatR32G32Float,
                                 InputElement.AppendAligned,
                                 2,
                                 InputClassification.PerInstanceData,
                                 1)
            ];

            /// <summary>
            ///     Gets the vs input particle.
            /// </summary>
            /// <value>
            ///     The vs input particle.
            /// </value>
            public static readonly InputElement[] VSInputParticle = [
                new InputElement("TEXCOORD",
                                 1,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 0,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 2,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 0,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 3,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 0,
                                 InputClassification.PerInstanceData,
                                 1),
                new InputElement("TEXCOORD",
                                 4,
                                 Format.FormatR32G32B32A32Float,
                                 InputElement.AppendAligned,
                                 0,
                                 InputClassification.PerInstanceData,
                                 1)
            ];

            /// <summary>
            ///     Gets the vs input skybox.
            /// </summary>
            /// <value>
            ///     The vs input skybox.
            /// </value>
            public static readonly InputElement[] VSInputSkybox = [
                new InputElement("SV_POSITION", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0)
            ];

            /// <summary>
            ///     Gets the vs input sprite 2d.
            /// </summary>
            /// <value>
            ///     The vs input sprite 2d.
            /// </value>
            public static readonly InputElement[] VSInputSprite2D = [
                new InputElement("POSITION", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("TEXCOORD", 0, Format.FormatR32G32Float, InputElement.AppendAligned, 0),
                new InputElement("COLOR", 0, Format.FormatR32G32B32A32Float, InputElement.AppendAligned, 0)
            ];

            /// <summary>
            ///     Gets the vs input volume3d.
            /// </summary>
            /// <value>
            ///     The vs input volume3d.
            /// </value>
            public static readonly InputElement[] VSInputVolume3D = [
                new InputElement("SV_POSITION", 0, Format.FormatR32G32B32Float, InputElement.AppendAligned, 0)
            ];
        }

        /// <summary>
        /// </summary>
        public static class DefaultVSShaderDescriptions {
            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSMeshDefault = new(nameof(VSMeshDefault),
                                                                         ShaderStage.Vertex,
                                                                         new ShaderReflector(),
                                                                         DefaultVSShaderByteCodes.VSMeshDefault);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSMeshBatched = new(nameof(VSMeshBatched),
                                                                         ShaderStage.Vertex,
                                                                         new ShaderReflector(),
                                                                         DefaultVSShaderByteCodes.VSMeshBatched);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSMeshTessellation = new(nameof(VSMeshTessellation),
                                                                              ShaderStage.Vertex,
                                                                              new ShaderReflector(),
                                                                              DefaultVSShaderByteCodes
                                                                                  .VSMeshTessellation);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSMeshShadow = new(nameof(VSMeshShadow),
                                                                        ShaderStage.Vertex,
                                                                        new ShaderReflector(),
                                                                        DefaultVSShaderByteCodes.VSMeshShadow);

            /// <summary>
            ///     The vs mesh ssao
            /// </summary>
            public static readonly ShaderDescription VSMeshSSAO = new(nameof(VSMeshSSAO),
                                                                      ShaderStage.Vertex,
                                                                      new ShaderReflector(),
                                                                      DefaultVSShaderByteCodes.VSMeshSSAO);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSMeshBatchedShadow = new(nameof(VSMeshBatchedShadow),
                                                                               ShaderStage.Vertex,
                                                                               new ShaderReflector(),
                                                                               DefaultVSShaderByteCodes
                                                                                   .VSMeshBatchedShadow);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSMeshBatchedSSAO = new(nameof(VSMeshBatchedSSAO),
                                                                             ShaderStage.Vertex,
                                                                             new ShaderReflector(),
                                                                             DefaultVSShaderByteCodes
                                                                                 .VSMeshBatchedSSAO);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSMeshInstancing = new(nameof(VSMeshInstancing),
                                                                            ShaderStage.Vertex,
                                                                            new ShaderReflector(),
                                                                            DefaultVSShaderByteCodes.VSMeshInstancing);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSMeshInstancingTessellation = new(
                nameof(VSMeshInstancingTessellation),
                ShaderStage.Vertex,
                new ShaderReflector(),
                DefaultVSShaderByteCodes.VSMeshInstancingTessellation);

            /// <summary>
            ///     The vs mesh bone skinned basic
            /// </summary>
            public static readonly ShaderDescription VSMeshBoneSkinnedBasic = new(nameof(VSMeshBoneSkinnedBasic),
                ShaderStage.Vertex,
                new ShaderReflector(),
                DefaultVSShaderByteCodes.VSMeshBoneSkinningBasic);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSPoint = new(nameof(VSPoint),
                                                                   ShaderStage.Vertex,
                                                                   new ShaderReflector(),
                                                                   DefaultVSShaderByteCodes.VSPoint);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSPointShadow = new(nameof(VSPointShadow),
                                                                         ShaderStage.Vertex,
                                                                         new ShaderReflector(),
                                                                         DefaultVSShaderByteCodes.VSPointShadow);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSBillboardText = new(nameof(VSBillboardText),
                                                                           ShaderStage.Vertex,
                                                                           new ShaderReflector(),
                                                                           DefaultVSShaderByteCodes.VSBillboard);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSBillboardInstancing = new(nameof(VSBillboardInstancing),
                ShaderStage.Vertex,
                new ShaderReflector(),
                DefaultVSShaderByteCodes.VSBillboardInstancing);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSMeshClipPlane = new(nameof(VSMeshClipPlane),
                                                                           ShaderStage.Vertex,
                                                                           new ShaderReflector(),
                                                                           DefaultVSShaderByteCodes.VSMeshClipPlane);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSFullScreenQuad = new(nameof(VSFullScreenQuad),
                                                                            ShaderStage.Vertex,
                                                                            new ShaderReflector(),
                                                                            DefaultVSShaderByteCodes
                                                                                .VSMeshClipPlaneQuad);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSParticle = new(nameof(VSParticle),
                                                                      ShaderStage.Vertex,
                                                                      new ShaderReflector(),
                                                                      DefaultVSShaderByteCodes.VSParticle);

            /// <summary>
            /// </summary>
            public static readonly ShaderDescription VSSkybox = new(nameof(VSSkybox),
                                                                    ShaderStage.Vertex,
                                                                    new ShaderReflector(),
                                                                    DefaultVSShaderByteCodes.VSSkybox);

            /// <summary>
            ///     The vs mesh wireframe
            /// </summary>
            public static readonly ShaderDescription VSMeshWireframe = new(nameof(VSMeshWireframe),
                                                                           ShaderStage.Vertex,
                                                                           new ShaderReflector(),
                                                                           DefaultVSShaderByteCodes.VSMeshWireframe);

            /// <summary>
            ///     The vs mesh depth
            /// </summary>
            public static readonly ShaderDescription VSMeshDepth = new(nameof(VSMeshDepth),
                                                                       ShaderStage.Vertex,
                                                                       new ShaderReflector(),
                                                                       DefaultVSShaderByteCodes.VSMeshDepth);

            /// <summary>
            ///     The vs mesh batched wireframe
            /// </summary>
            public static readonly ShaderDescription VSMeshBatchedWireframe = new(nameof(VSMeshBatchedWireframe),
                ShaderStage.Vertex,
                new ShaderReflector(),
                DefaultVSShaderByteCodes.VSMeshBatchedWireframe);

            /// <summary>
            ///     The vs bone skinning wireframe
            /// </summary>
            public static readonly ShaderDescription VSBoneSkinningWireframe = new(nameof(VSBoneSkinningWireframe),
                ShaderStage.Vertex,
                new ShaderReflector(),
                DefaultVSShaderByteCodes.VSMeshBoneSkinningWireframe);

            /// <summary>
            ///     The vs mesh outline pass1
            /// </summary>
            public static readonly ShaderDescription VSMeshOutlinePass1 = new(nameof(VSMeshOutlinePass1),
                                                                              ShaderStage.Vertex,
                                                                              new ShaderReflector(),
                                                                              DefaultVSShaderByteCodes.VSMeshOutlineP1);

            /// <summary>
            ///     The vs mesh outline pass1
            /// </summary>
            public static readonly ShaderDescription VSMeshOutlineScreenQuad = new(nameof(VSMeshOutlineScreenQuad),
                ShaderStage.Vertex,
                new ShaderReflector(),
                DefaultVSShaderByteCodes.VSMeshOutlineScreenQuad);

            /// <summary>
            ///     The vs plane grid
            /// </summary>
            public static readonly ShaderDescription VSPlaneGrid = new(nameof(VSPlaneGrid),
                                                                       ShaderStage.Vertex,
                                                                       new ShaderReflector(),
                                                                       DefaultVSShaderByteCodes.VSPlaneGrid);

            /// <summary>
            ///     The vs screen quad
            /// </summary>
            public static readonly ShaderDescription VSScreenQuad = new(nameof(VSScreenQuad),
                                                                        ShaderStage.Vertex,
                                                                        new ShaderReflector(),
                                                                        DefaultVSShaderByteCodes.VSScreenQuad);

            /// <summary>
            ///     The vs sprite
            /// </summary>
            public static readonly ShaderDescription VSSprite2D = new(nameof(VSSprite2D),
                                                                      ShaderStage.Vertex,
                                                                      new ShaderReflector(),
                                                                      DefaultVSShaderByteCodes.VSSprite2D);

            /// <summary>
            ///     The vs volume3d
            /// </summary>
            public static readonly ShaderDescription VSVolume3D = new(nameof(VSVolume3D),
                                                                      ShaderStage.Vertex,
                                                                      new ShaderReflector(),
                                                                      DefaultVSShaderByteCodes.VSVolume3D);

            /// <summary>
            ///     The vsssao
            /// </summary>
            public static readonly ShaderDescription VSSSAO = new(nameof(VSSSAO),
                                                                  ShaderStage.Vertex,
                                                                  new ShaderReflector(),
                                                                  DefaultVSShaderByteCodes.VSSSAO);

#if !WINDOWS_UWP
            /// <summary>
            ///     The vs screen dup
            /// </summary>
            public static readonly ShaderDescription VSScreenDup = new(nameof(VSScreenDup),
                                                                       ShaderStage.Vertex,
                                                                       new ShaderReflector(),
                                                                       DefaultVSShaderByteCodes.VSScreenDup);

            /// <summary>
            ///     The vs screen dup mouse cursor
            /// </summary>
            public static readonly ShaderDescription VSScreenDupCursor = new(nameof(VSScreenDupCursor),
                                                                             ShaderStage.Vertex,
                                                                             new ShaderReflector(),
                                                                             DefaultVSShaderByteCodes
                                                                                 .VSScreenDupCursor);

#endif
        }
    }
}
