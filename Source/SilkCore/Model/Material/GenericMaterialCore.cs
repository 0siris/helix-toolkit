/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System.Runtime.Serialization;
using HelixToolkit.SharpDX.Core.Interface;
using HelixToolkit.SharpDX.Core.Native;
using HelixToolkit.SharpDX.Core.ShaderManager;
using HelixToolkit.SharpDX.Core.Shaders;

namespace HelixToolkit.SharpDX.Core.Model.Material;
[DataContract]
public abstract class GenericMaterialCore : MaterialCore {
    protected readonly ConstantBufferDescription CbDescription = new(string.Empty, 0);

    public GenericMaterialCore(
        string materialShaderPassName,
        string shadowShaderPassName,
        string wireframePassName,
        ConstantBufferDescription constantBufferDesc
    ) {
        MaterialPassName = materialShaderPassName;
        ShadowPassName = shadowShaderPassName;
        WireframePassName = wireframePassName;
        CbDescription = constantBufferDesc;
    }

    public GenericMaterialCore(ConstantBufferDescription constantBufferDesc) {
        CbDescription = constantBufferDesc;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericMaterialCore" /> class.
    /// </summary>
    /// <param name="shaderPass">The shader pass. Currently only supports pixel shader parameter properties</param>
    /// <param name="modelMaterialConstantBufferName">Name of the model material constant buffer in pixel shader.</param>
    public GenericMaterialCore(ShaderPass shaderPass, string modelMaterialConstantBufferName) {
        CbDescription = new ConstantBufferDescription(modelMaterialConstantBufferName, 0);
    }

    [DataMember]
    public Dictionary<string, TextureModel> TextureDict { get; } = [];

    [DataMember]
    public Dictionary<string, SamplerStateDescription> SamplerDict { get; } = [];

    [DataMember]
    public Dictionary<string, float> FloatDict { get; } = [];

    [DataMember]
    public Dictionary<string, bool> BoolDict { get; } = [];

    [DataMember]
    public Dictionary<string, Vector2> Vector2Dict { get; } = [];

    [DataMember]
    public Dictionary<string, Vector3> Vector3Dict { get; } = [];

    [DataMember]
    public Dictionary<string, Vector4> Vector4Dict { get; } = [];

    [DataMember]
    public Dictionary<string, Matrix> MatrixDict { get; } = [];

    [DataMember]
    public string MaterialPassName { get; set; } = DefaultPassNames.Default;

    [DataMember]
    public string ShadowPassName { get; set; } = DefaultPassNames.ShadowPass;

    [DataMember]
    public string WireframePassName { get; set; } = DefaultPassNames.Wireframe;

    public string[] PropertieNames { get; } = [];

    public string[] TextureNames { get; } = [];

    public string[] SamplerNames { get; } = [];

    public void SetTexture(string name, Stream texture) {
        TextureModel? value = texture;
        if (value is not { } textureModel) {
            TextureDict.Remove(name);
        } else if (TextureDict.ContainsKey(name)) {
            TextureDict[name] = textureModel;
        } else {
            TextureDict.Add(name, textureModel);
        }
    }


    public void SetSampler(string name, SamplerStateDescription samplerDesc) {
        if (SamplerDict.ContainsKey(name))
            SamplerDict[name] = samplerDesc;
        else
            SamplerDict.Add(name, samplerDesc);
    }

    public TextureModel? GetTexture(string name) {
        if (TextureDict.TryGetValue(name, out var texture)) return texture;

        return null;
    }

    public SamplerStateDescription GetSampler(string name) {
        if (SamplerDict.TryGetValue(name, out var samplerDesc)) return samplerDesc;

        return new SamplerStateDescription();
    }


    public void SetProperty(string name, int value) {
        if (FloatDict.ContainsKey(name))
            FloatDict[name] = value;
        else
            FloatDict.Add(name, value);
    }

    public void SetProperty(string name, float value) {
        if (FloatDict.ContainsKey(name))
            FloatDict[name] = value;
        else
            FloatDict.Add(name, value);
    }

    public void SetProperty(string name, bool value) {
        if (FloatDict.ContainsKey(name))
            FloatDict[name] = value ? 1 : 0;
        else
            FloatDict.Add(name, value ? 1 : 0);
    }

    public void SetProperty(string name, Vector2 value) {
        if (Vector2Dict.ContainsKey(name))
            Vector2Dict[name] = value;
        else
            Vector2Dict.Add(name, value);
    }

    public void SetProperty(string name, Vector3 value) {
        if (Vector3Dict.ContainsKey(name))
            Vector3Dict[name] = value;
        else
            Vector3Dict.Add(name, value);
    }

    public void SetProperty(string name, Vector4 value) {
        if (Vector4Dict.ContainsKey(name))
            Vector4Dict[name] = value;
        else
            Vector4Dict.Add(name, value);
    }

    public void SetProperty(string name, Matrix value) {
        if (MatrixDict.ContainsKey(name))
            MatrixDict[name] = value;
        else
            MatrixDict.Add(name, value);
    }
}

[DataContract]
public sealed class GenericMeshMaterialCore : GenericMaterialCore {
    public GenericMeshMaterialCore()
        : base(new ConstantBufferDescription(string.Empty, 0)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericMeshMaterialCore" /> class.
    /// </summary>
    /// <param name="shaderPass">The shader pass. Currently only supports pixel shader parameter properties</param>
    /// <param name="modelMaterialConstantBufferName">Name of the model material constant buffer in pixel shader.</param>
    public GenericMeshMaterialCore(ShaderPass shaderPass, string modelMaterialConstantBufferName)
        : base(shaderPass, modelMaterialConstantBufferName) { }
}

[DataContract]
public sealed class GenericLineMaterialCore : GenericMaterialCore {
    public GenericLineMaterialCore()
        : base(new ConstantBufferDescription(string.Empty, 0)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericLineMaterialCore" /> class.
    /// </summary>
    /// <param name="shaderPass">The shader pass. Currently only supports pixel shader parameter properties</param>
    /// <param name="modelMaterialConstantBufferName">Name of the model material constant buffer in pixel shader.</param>
    public GenericLineMaterialCore(ShaderPass shaderPass, string modelMaterialConstantBufferName)
        : base(shaderPass, modelMaterialConstantBufferName) { }
}

[DataContract]
public sealed class GenericPointMaterialCore : GenericMaterialCore {
    public GenericPointMaterialCore()
        : base(new ConstantBufferDescription(string.Empty, 0)) { }

    /// <summary>
    ///     Initializes a new instance of the <see cref="GenericPointMaterialCore" /> class.
    /// </summary>
    /// <param name="shaderPass">The shader pass. Currently only supports pixel shader parameter properties</param>
    /// <param name="modelMaterialConstantBufferName">Name of the model material constant buffer in pixel shader.</param>
    public GenericPointMaterialCore(ShaderPass shaderPass, string modelMaterialConstantBufferName)
        : base(shaderPass, modelMaterialConstantBufferName) { }
}
