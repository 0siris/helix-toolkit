/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using HelixToolkit.Logger;
using HelixToolkit.SharpDX.Core.Render;
using HelixToolkit.SharpDX.Core.Shaders;
using HelixToolkit.SharpDX.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace HelixToolkit.SharpDX.Core {
    namespace Model {
        /// <summary>
        /// </summary>
        public abstract class MaterialVariable : DisposeObject {
            private static LoggerLib.ILog Logger => LoggerLib.Logger.Current;

            public static readonly ConstantBufferDescription DefaultMeshConstantBufferDesc
                = new(DefaultBufferNames.ModelCB,
                      PhongPBRMaterialStruct.SizeInBytes);

            public static readonly ConstantBufferDescription DefaultPointLineConstantBufferDesc
                = new(DefaultBufferNames.PointLineModelCB,
                      PointLineMaterialStruct.SizeInBytes);

            public static readonly ConstantBufferDescription DefaultVolumeConstantBufferDesc
                = new(DefaultBufferNames.VolumeModelCB,
                      VolumeParamsStruct.SizeInBytes);

            public static readonly ConstantBufferDescription DefaultNonMaterialBufferDesc
                = new(DefaultBufferNames.SimpleMeshCB,
                      SimpleMeshStruct.SizeInBytes);

            private readonly MaterialCore material;

            private readonly ConstantBufferDescription materialCBDescription;
            private readonly ConstantBufferDescription nonMaterialCBDescription = DefaultNonMaterialBufferDesc;

            private readonly int storageId = -1;
            private readonly object updateLock = new();
            private bool initialized;

            /// <summary>
            ///     Gets the material cb.
            /// </summary>
            /// <value>
            ///     The material cb.
            /// </value>
            protected ConstantBufferProxy materialCB;

            /// <summary>
            ///     Gets the non material cb. Used for non material related rendering such as Shadow map
            /// </summary>
            /// <value>
            ///     The non material cb.
            /// </value>
            protected ConstantBufferProxy nonMaterialCB;

            private ArrayStorage storage;

            /// <summary>
            ///     Initializes a new instance of the <see cref="MaterialVariable" /> class.
            /// </summary>
            /// <param name="manager">The manager.</param>
            /// <param name="technique">The technique.</param>
            /// <param name="meshMaterialConstantBufferDesc">The Constant Buffer description</param>
            /// <param name="materialCore"></param>
            public MaterialVariable(
                IEffectsManager manager,
                IRenderTechnique technique,
                ConstantBufferDescription meshMaterialConstantBufferDesc,
                MaterialCore materialCore
            ) {
                Technique = technique;
                EffectsManager = manager;
                if (materialCore != null) {
                    material = materialCore;
                    material.PropertyChanged += MaterialCore_PropertyChanged;
                }

                materialCBDescription = meshMaterialConstantBufferDesc;
                if (manager != null) {
                    storage = manager.StructArrayPool.Register(materialCBDescription.StructSize);
                    storageId = storage.GetId();
                }
            }

            /// <summary>
            ///     Gets or sets the identifier. Used for material sorting
            /// </summary>
            /// <value>
            ///     The identifier.
            /// </value>
            public ushort ID { get; set; } = 0;

            protected IRenderTechnique Technique { get; }

            protected IEffectsManager EffectsManager { get; }

            protected bool NeedUpdate { get; private set; } = true;

            public event EventHandler UpdateNeeded;

            internal void Initialize() {
                if (EffectsManager == null) return;
                materialCB = EffectsManager.ConstantBufferPool.Register(materialCBDescription);
                nonMaterialCB = EffectsManager.ConstantBufferPool.Register(nonMaterialCBDescription);
                OnInitialPropertyBindings();
                foreach (var v in propertyBindings.Values) v.Invoke();
                initialized = true;
            }

            protected virtual void OnInitialPropertyBindings() { }

            /// <summary>
            ///     Binds the material textures, samplers, etc,.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="deviceContext">The device context.</param>
            /// <param name="shaderPass">The shader pass.</param>
            /// <returns></returns>
            public abstract bool BindMaterialResources(
                RenderContext context,
                DeviceContextProxy deviceContext,
                ShaderPass shaderPass
            );

            protected virtual void UpdateInternalVariables(DeviceContextProxy deviceContext) { }

            /// <summary>
            ///     Gets the pass.
            /// </summary>
            /// <param name="renderType">Type of the render.</param>
            /// <param name="context">The context.</param>
            /// <returns></returns>
            public abstract ShaderPass GetPass(RenderType renderType, RenderContext context);

            /// <summary>
            ///     Gets the shadow pass.
            /// </summary>
            /// <param name="renderType"></param>
            /// <param name="context">The context.</param>
            /// <returns></returns>
            public abstract ShaderPass GetShadowPass(RenderType renderType, RenderContext context);

            /// <summary>
            ///     Gets the wireframe pass.
            /// </summary>
            /// <param name="renderType"></param>
            /// <param name="context">The context.</param>
            /// <returns></returns>
            public abstract ShaderPass GetWireframePass(RenderType renderType, RenderContext context);

            /// <summary>
            ///     Gets the depth pass.
            /// </summary>
            /// <param name="renderType">Type of the render.</param>
            /// <param name="context">The context.</param>
            /// <returns></returns>
            public abstract ShaderPass GetDepthPass(RenderType renderType, RenderContext context);

            /// <summary>
            ///     Gets the name of the pass by.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <returns></returns>
            public ShaderPass GetPassByName(string name) {
                return Technique[name];
            }

            /// <summary>
            ///     Updates the material structure. And upload data to constant buffer
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="context">The context.</param>
            /// <param name="model">The model.</param>
            public bool UpdateMaterialStruct<T>(DeviceContextProxy context, ref T model) where T : unmanaged {
                if (!initialized) return false;
                if (NeedUpdate)
                    lock (updateLock) {
                        if (NeedUpdate) {
                            UpdateInternalVariables(context);
                            NeedUpdate = false;
                        }
                    }

                var structSize = UnsafeHelper.SizeOf<T>();
                var box = materialCB.Map(context);
                UnsafeHelper.Write(box.DataPointer, ref model);
                var succ = storage.Read(storageId,
                                        structSize,
                                        box.DataPointer + structSize,
                                        storage.StructSize - structSize);
                materialCB.Unmap(context);
                return succ;
            }

            /// <summary>
            ///     Updates the non material structure.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="context">The context.</param>
            /// <param name="model">The model.</param>
            /// <returns></returns>
            public bool UpdateNonMaterialStruct<T>(DeviceContextProxy context, ref T model) where T : unmanaged {
                if (!initialized) return false;
                if (UnsafeHelper.SizeOf<T>() != nonMaterialCB.StructureSize) {
                    Debug.Assert(false);
                    return false;
                }

                var box = nonMaterialCB.Map(context);
                UnsafeHelper.Write(box.DataPointer, ref model);
                nonMaterialCB.Unmap(context);
                return true;
            }

            /// <summary>
            ///     Draws the specified device context.
            /// </summary>
            /// <param name="deviceContext">The device context.</param>
            /// <param name="bufferModel">Geometry buffer model.</param>
            /// <param name="instanceCount">The instance count.</param>
            public abstract void Draw(
                DeviceContextProxy deviceContext,
                IAttachableBufferModel bufferModel,
                int instanceCount
            );

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected void InvalidateRenderer() {
                Technique?.EffectsManager?.RaiseInvalidateRender();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            protected void NotifyUpdateNeeded() {
                NeedUpdate = true;
                UpdateNeeded?.Invoke(this, EventArgs.Empty);
                InvalidateRenderer();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void DrawIndexed(DeviceContextProxy context, int indexCount, int instanceCount) {
                if (instanceCount <= 0)
                    context.DrawIndexed(indexCount, 0, 0);
                else
                    context.DrawIndexedInstanced(indexCount, instanceCount, 0, 0, 0);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void DrawPoints(DeviceContextProxy context, int vertexCount, int instanceCount) {
                if (instanceCount <= 0)
                    context.Draw(vertexCount, 0);
                else
                    context.DrawInstanced(vertexCount, instanceCount, 0, 0);
            }

            /// <summary>
            ///     Writes the value to internal buffer array
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name">The name.</param>
            /// <param name="value">The value.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteValue<T>(string name, ref T value) where T : unmanaged {
                if (materialCB != null && materialCB.TryGetVariableByName(name, out var variable)) {
                    if (UnsafeHelper.SizeOf<T>() > variable.Size) {
                        var structSize = UnsafeHelper.SizeOf<T>();
                        throw new ArgumentException(
                            $"Input struct size {structSize} is larger than shader variable {variable.Name} size {variable.Size}");
                    }

                    if (!storage.Write(storageId, variable.StartOffset, ref value))
                        throw new ArgumentException($"Failed to write value on {name}");
                } else {
#if DEBUG
                    throw new ArgumentException(
                        $"Variable not found in constant buffer {materialCB.Name}. Variable = {name}");
#else
                    Logger.Warn("Variable not found in constant buffer {Value0}. Variable = {Value1}", materialCB.Name, name);
#endif
                }
            }

            /// <summary>
            ///     Writes the value to internal buffer array
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name">The name.</param>
            /// <param name="value">The value.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void WriteValue<T>(string name, T value) where T : unmanaged {
                WriteValue(name, ref value);
            }

            /// <summary>
            /// </summary>
            /// <param name="disposeManagedResources"></param>
            protected override void OnDispose(bool disposeManagedResources) {
                RemoveAndDispose(ref materialCB);
                RemoveAndDispose(ref nonMaterialCB);
                storage.ReleaseId(storageId);
                RemoveAndDispose(ref storage);
                if (disposeManagedResources) {
                    UpdateNeeded = null;
                    material?.PropertyChanged -= MaterialCore_PropertyChanged;
                    propertyBindings.Clear();
                }

                base.OnDispose(disposeManagedResources);
            }

            #region Material Property Bindings

            private readonly Dictionary<string, Action> propertyBindings = [];

            protected void AddPropertyBinding(string propertyName, Action action) {
                propertyBindings.Add(propertyName, action);
            }

            protected void TriggerPropertyAction(string propertyName) {
                if (propertyBindings.TryGetValue(propertyName, out var act)) act.Invoke();
            }

            private void MaterialCore_PropertyChanged(object sender, PropertyChangedEventArgs e) {
                TriggerPropertyAction(e.PropertyName);
                InvalidateRenderer();
            }

            #endregion
        }
    }
}
