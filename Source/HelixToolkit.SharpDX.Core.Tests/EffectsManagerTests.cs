// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EffectsManagerTests.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using NUnit.Framework;
using System.Linq;

namespace HelixToolkit.SharpDX.Core.Tests
{
    [TestFixture]
    public class EffectsManagerTests
    {
        [Test]
        public void InitializationTest()
        {
            using var effectsManager = new DefaultEffectsManager();
            foreach (var techName in effectsManager.RenderTechniques)
            {
                var tech = effectsManager[techName];
                Assert.IsFalse(tech.IsNull);
                foreach (var passName in tech.ShaderPassNames)
                {
                    var p = tech[passName];
                    Assert.IsFalse(p.IsNULL);
                    var description = tech.Description.PassDescriptions.Single(x => x.Name == passName);
                    foreach (var shader in description.ShaderList)
                    {
                        Assert.That(p.GetShader(shader.ShaderType), Is.Not.Null, $"{techName}/{passName}/{shader.Name}");
                        Assert.That(p.GetShader(shader.ShaderType).IsNULL, Is.False, $"{techName}/{passName}/{shader.Name}");
                    }
                    if (description.InputLayoutDescription != null
                        && description.InputLayoutDescription != Shaders.InputLayoutDescription.EmptyInputLayout)
                    {
                        Assert.That(p.Layout, Is.Not.Null, $"{techName}/{passName}/InputLayout");
                    }
                }
            }
        }
    }
}
