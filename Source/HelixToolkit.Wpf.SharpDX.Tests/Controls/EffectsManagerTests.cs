using HelixToolkit.Wpf.SharpDX;
using NUnit.Framework;
namespace HelixToolkit.Wpf.SharpDX.Tests.Controls
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
                }
            }
        }
    }
}
