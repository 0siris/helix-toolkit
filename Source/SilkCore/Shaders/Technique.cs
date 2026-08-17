/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Utilities;

namespace HelixToolkit.SharpDX.Core.Shaders;
public sealed class Technique : DisposeObject, IRenderTechnique {
    private readonly Dictionary<string, Lazy<ShaderPass>> passDict = [];
    private readonly List<Lazy<ShaderPass>> passList = [];
    private InputLayoutProxy? layout;

    /// <summary>
    /// </summary>
    /// <param name="description"></param>
    /// <param name="manager"></param>
    public Technique(TechniqueDescription description, IEffectsManager? manager) {
        Description = description;
        Name = description.Name ?? string.Empty;
        effectsManager = manager;
        if (description is {InputLayoutDescription: not null, PassDescriptions: not null}
            && manager is { } actualManager)
            foreach (var desc in description.PassDescriptions) {
                if (desc.Name is not { } passName) continue;
                desc.InputLayoutDescription ??= description.InputLayoutDescription;
                var pass = new Lazy<ShaderPass>(() => new ShaderPass(desc, actualManager), true);
                passDict.Add(passName, pass);
                passList.Add(pass);
            }
    }

    public static IRenderTechnique NullTechnique { get; } =
        new Technique(new TechniqueDescription { IsNull = true }, null);

    /// <summary>
    ///     Gets the unique identifier.
    /// </summary>
    /// <value>
    ///     The unique identifier.
    /// </value>
    public Guid Guid { get; } = Guid.NewGuid();

    /// <summary>
    ///     Gets or sets the description.
    /// </summary>
    /// <value>
    ///     The description.
    /// </value>
    public TechniqueDescription Description { get; }

    /// <summary>
    ///     Gets a value indicating whether this Technique is null.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this Technique is null; otherwise, <c>false</c>.
    /// </value>
    public bool IsNull => Description.IsNull;

    /// <summary>
    ///     <see cref="IRenderTechnique.Layout" />
    /// </summary>
    public InputLayoutProxy Layout => layout
        ?? throw new InvalidOperationException("The technique has no input layout.");

    /// <summary>
    ///     <see cref="IRenderTechnique.Device" />
    /// </summary>
    public NativeD3DDevice? Device => effectsManager?.NativeDeviceResources.Device;

    /// <summary>
    ///     <see cref="IRenderTechnique.Name" />
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     <see cref="IRenderTechnique.ShaderPassNames" />
    /// </summary>
    public IEnumerable<string> ShaderPassNames => passDict.Keys;

    /// <summary>
    ///     <see cref="IRenderTechnique.ConstantBufferPool" />
    /// </summary>
    public IConstantBufferPool ConstantBufferPool => EffectsManager.ConstantBufferPool;

    /// <summary>
    ///     <see cref="IRenderTechnique.EffectsManager" />
    /// </summary>
    private IEffectsManager? effectsManager;

    public IEffectsManager EffectsManager => effectsManager
        ?? throw new InvalidOperationException("The technique is not attached to an effects manager.");

    /// <summary>
    ///     <see cref="IRenderTechnique.GetPass(string)" />
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public ShaderPass GetPass(string name) => !string.IsNullOrEmpty(name) && passDict.TryGetValue(name, out var value)
        ? value.Value
        : ShaderPass.NullPass;

    /// <summary>
    ///     <see cref="IRenderTechnique.GetPass(int)" />
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public ShaderPass GetPass(int index) => index >= 0 && passList.Count > index ? passList[index].Value : ShaderPass.NullPass;

    /// <summary>
    ///     Adds the pass.
    /// </summary>
    /// <param name="description">The description.</param>
    /// <returns></returns>
    public bool AddPass(ShaderPassDescription description) {
        if (description.Name is not { } name || passDict.ContainsKey(name)) return false;
        var pass = new Lazy<ShaderPass>(() => new ShaderPass(description, EffectsManager), true);
        passDict.Add(name, pass);
        passList.Add(pass);
        return true;
    }

    /// <summary>
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool RemovePass(string name) {
        if (passDict.TryGetValue(name, out var pass)) {
            passDict.Remove(name);
            passList.Remove(pass);
            if (pass.IsValueCreated) {
                var p = pass.Value;
                RemoveAndDispose(ref p);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     <see cref="IRenderTechnique.GetPass(int)" />
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public ShaderPass this[int index] => GetPass(index);

    /// <summary>
    ///     <see cref="IRenderTechnique.GetPass(string)" />
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public ShaderPass this[string name] => GetPass(name);

    /// <summary>
    /// </summary>
    /// <param name="disposeManagedResources"></param>
    protected override void OnDispose(bool disposeManagedResources) {
        passDict.Clear();
        foreach (var p in passList)
            if (p.IsValueCreated)
                p.Value.Dispose();

        passList.Clear();
        RemoveAndDispose(ref layout);
        effectsManager = null;
        base.OnDispose(disposeManagedResources);
    }
}
