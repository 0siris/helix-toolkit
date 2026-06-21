// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MeshGeometryModel3DTests.cs" company="Helix Toolkit">
//   Copyright (c) 2020 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

extern alias WpfAssembly;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NUnit.Framework;
using WpfRay = WpfAssembly::HelixToolkit.Wpf.SharpDX.Ray;

namespace HelixToolkit.Wpf.SharpDX.Tests.Elements3D
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    class CrossSectionMeshGeometryModel3DTests
    {
        private readonly Viewport3DX viewport = new Viewport3DX();

        private CrossSectionMeshGeometryModel3D GetGeometryModel3D()
        {
            var meshBuilder = new MeshBuilder();
            meshBuilder.AddBox(new Vector3(0f), 1, 1, 1);
            return new CrossSectionMeshGeometryModel3D()
            {
                Geometry = meshBuilder.ToMesh(),
            };
        }

        [Test]
        public void HitTestShouldReturnOnePointOnFrontOfCubeWithNoCuttingPlanes()
        {
            var ray = new WpfRay(new Vector3(2f, 0f, 0f), new Vector3(-1, 0, 0));
            var hits = new List<HitTestResult>();
            var geometryModel3D = GetGeometryModel3D();
            var hitContext = new HitTestContext(viewport.RenderContext, ref ray);
            geometryModel3D.HitTest(hitContext, ref hits);
            Assert.AreEqual(1,hits.Count);
            Assert.AreEqual(new Vector3(0.5f,0,0), hits[0].PointHit);
        }

        [TestCaseSource(nameof(GetPlanes))]
        public void HitTestShouldReturnOnePointOnBackOfCubeWithCuttingPlaneInXZero(Action<CrossSectionMeshGeometryModel3D> setupPlane)
        {
            var geometryModel3D = GetGeometryModel3D();
            setupPlane(geometryModel3D);
            var ray = new WpfRay(new Vector3(2f, 0f, 0f), new Vector3(-1, 0, 0));
            var hits = new List<HitTestResult>();
            var hitContext = new HitTestContext(viewport.RenderContext, ref ray);
            geometryModel3D.HitTest(hitContext, ref hits);
            Assert.AreEqual(1, hits.Count);
            Assert.AreEqual(new Vector3(-0.5f, 0, 0), hits[0].PointHit);
        }

        /// <summary>
        /// Get all planes in the CrossSectionMeshGeometryModel3D with reflection so that if more planes are added tests will fail
        /// </summary>
        public static IEnumerable<object[]> GetPlanes()
        {
            var properties = typeof(CrossSectionMeshGeometryModel3D)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var enables = properties
                .Where(p => p.Name.StartsWith("EnablePlane", StringComparison.InvariantCulture))
                .ToArray();
            var planes = properties
                .Where(p => p.Name.StartsWith("Plane", StringComparison.InvariantCulture))
                .ToArray();
            for (int i = 0; i < enables.Length; i++)
            {
                var planeProperty = planes[i];
                var enableProperty = enables[i];

                void Action(CrossSectionMeshGeometryModel3D model)
                {
                    var plane = Activator.CreateInstance(
                        planeProperty.PropertyType,
                        new object[] { new Vector3(0f), new Vector3(-1, 0, 0) });
                    planeProperty.SetValue(model, plane);
                    enableProperty.SetValue(model, true);
                }

                yield return new object[] { (Action<CrossSectionMeshGeometryModel3D>)Action };
            }

        }
    }
}
