/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/

using System;
using System.ComponentModel;
using System.IO;
namespace HelixToolkit.SharpDX.Core
{
    using Model;
    /// <summary>
    /// 
    /// </summary>
    public interface IMaterial : INotifyPropertyChanged
    {
        string Name
        {
            set; get;
        }
        Guid Guid
        {
            get;
        }
        MaterialVariable CreateMaterialVariables(IEffectsManager manager, IRenderTechnique technique);
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IMaterialVariablePool : IDisposable
    {
        int Count
        {
            get;
        }
        MaterialVariable Register(IMaterial material, IRenderTechnique technique);
    }
}
