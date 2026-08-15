using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Model.Material.Variables;

namespace HelixToolkit.SharpDX.Core.ShaderManager;

public sealed class MaterialVariablePool : IDisposable, IMaterialVariablePool {
    private readonly Dictionary<(Guid, Guid), MaterialVariable> dictionary = [];
    private readonly IEffectsManager effectsManager;
    private ushort idmax;

    public MaterialVariablePool(IEffectsManager manager) {
        effectsManager = manager;
    }

    public int Count { get; private set; }

    public MaterialVariable Register(IMaterial material, IRenderTechnique technique) {
        if (technique.IsNull) return EmptyMaterialVariable.EmptyVariable;
        var guid = material.Guid;
        var techGuid = technique.Guid;
        lock (dictionary) {
            if (dictionary.TryGetValue((guid, techGuid), out var value)) {
                value.IncRef();
                return value;
            }

            var v = material.CreateMaterialVariables(effectsManager, technique);
            v.Initialize();
            v.Disposed += (_, _) => {
                lock (dictionary) {
                    dictionary.Remove((guid, techGuid));
                    --Count;
                }
            };
            dictionary.Add((guid, techGuid), v);
            ++Count;
            if (idmax - (ushort)Count > 1000) {
                idmax = 0;
                foreach (var m in dictionary) m.Value.Id = ++idmax;
            } else {
                v.Id = ++idmax;
            }

            return v;
        }
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    private void Dispose(bool disposing) {
        if (!disposedValue) {
            if (disposing)
                lock (dictionary) {
                    foreach (var v in dictionary.Values.ToArray()) v.ForceDispose();
                    dictionary.Clear();
                }
            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            disposedValue = true;
        }
    }

    // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
    // ~MaterialVariablePool() {
    //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
    //   Dispose(false);
    // }

    // This code added to correctly implement the disposable pattern.
    public void Dispose() {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }

    #endregion
}
