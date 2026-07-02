using HelixToolkit.SharpDX.Core.Model;

namespace HelixToolkit.SharpDX.Core;

public sealed class MaterialVariablePool : IDisposable, IMaterialVariablePool
{
    private readonly DoubleKeyDictionary<Guid, Guid, MaterialVariable> dictionary = new();
    private readonly IEffectsManager effectsManager;
    private ushort IDMAX;

    public MaterialVariablePool(IEffectsManager manager)
    {
        effectsManager = manager;
    }

    public int Count { get; private set; }

    public MaterialVariable Register(IMaterial material, IRenderTechnique technique)
    {
        if (material == null || technique.IsNull) return EmptyMaterialVariable.EmptyVariable;
        var guid = material.Guid;
        var techGuid = technique.GUID;
        lock (dictionary)
        {
            if (dictionary.TryGetValue(guid, techGuid, out var value))
            {
                value.IncRef();
                return value;
            }

            var v = material.CreateMaterialVariables(effectsManager, technique);
            v.Initialize();
            v.Disposed += (s, e) =>
            {
                lock (dictionary)
                {
                    dictionary.Remove(guid, techGuid);
                    --Count;
                }
            };
            dictionary.Add(guid, techGuid, v);
            ++Count;
            if (IDMAX - (ushort) Count > 1000)
            {
                IDMAX = 0;
                foreach (var m in dictionary) m.Value.ID = ++IDMAX;
            }
            else
            {
                v.ID = ++IDMAX;
            }

            return v;
        }
    }

    #region IDisposable Support

    private bool disposedValue; // To detect redundant calls

    private void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
                lock (dictionary)
                {
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
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        // TODO: uncomment the following line if the finalizer is overridden above.
        // GC.SuppressFinalize(this);
    }

    #endregion
}