/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

namespace HelixToolkit.SharpDX.Core;

/// <summary>
/// </summary>
public static class Disposer {
    /// <summary>
    ///     Dispose an object instance and set the reference to null
    /// </summary>
    /// <typeparam name="T">The type of object to dispose</typeparam>
    /// <param name="resource">A reference to the instance for disposal</param>
    /// <remarks>This method hides any thrown exceptions that might occur during disposal of the object (by design)</remarks>
    public static void RemoveAndDispose<T>(ref T? resource) where T : class, IDisposable {
        if (resource is null)
            return;

        try {
            resource.Dispose();
        } catch (Exception e){
            LoggerLib.Logger.Error(e, "Dispose failed");
        }

        resource = null;
    }

    public static void DisposeAll<T>(this IEnumerable<T> values) where T : IDisposable {
        foreach (var value in values)
            try {
                value.Dispose();
            } catch (Exception e) {
                LoggerLib.Logger.Error(e, "Dispose failed");
                //TODO should we throw?
            }
    }
    
    public static bool SetDispose<T>(ref T? backingField, T? newValue) where T : IDisposable {
        if (Equals(backingField, newValue))
            return false;
        
        backingField?.Dispose();

        backingField = newValue;
        return true;
    }
    
    public static bool SetDispose<T>(ref T? backingField, T? newValue, Action<T> onDispose) where T : IDisposable {
        if (Equals(backingField, newValue))
            return false;
        if (backingField is not null) {
            onDispose(backingField);
            backingField.Dispose();    
        }
        
        backingField = newValue;
        return true;
    }

}
