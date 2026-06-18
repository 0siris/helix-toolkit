#if SILKNET
global using BoundingSphere = Silk.NET.Maths.Sphere<float>;
global using Color4 = Silk.NET.Maths.Vector4D<float>;
global using Color3 = Silk.NET.Maths.Vector3D<float>;
global using Format = Silk.NET.DXGI.Format;
global using Int3 = Silk.NET.Maths.Vector3D<int>;
global using Int4 = Silk.NET.Maths.Vector4D<int>;
global using Matrix = Silk.NET.Maths.Matrix4X4<float>;
global using Point = Silk.NET.Maths.Vector2D<int>;
global using PointerSize = System.IntPtr;
global using Quaternion = Silk.NET.Maths.Quaternion<float>;
global using Vector2 = Silk.NET.Maths.Vector2D<float>;
global using Vector3 = Silk.NET.Maths.Vector3D<float>;
global using Vector4 = Silk.NET.Maths.Vector4D<float>;
#if !NETFX_CORE
global using BoundingBox = HelixToolkit.Wpf.SharpDX.BoundingBox;
global using BoundingFrustum = HelixToolkit.Wpf.SharpDX.BoundingFrustum;
global using Plane = HelixToolkit.Wpf.SharpDX.Plane;
global using Ray = HelixToolkit.Wpf.SharpDX.Ray;
global using NativeD3DDevice = HelixToolkit.Wpf.SharpDX.Native.SilkD3DDevice;
global using NativeD3DResource = HelixToolkit.Wpf.SharpDX.Resource;
global using NativeD3DTexture1D = HelixToolkit.Wpf.SharpDX.Texture1D;
global using NativeD3DTexture2D = HelixToolkit.Wpf.SharpDX.Texture2D;
global using NativeD3DTexture3D = HelixToolkit.Wpf.SharpDX.Texture3D;
global using NativeTexture1DDescription = HelixToolkit.Wpf.SharpDX.Texture1DDescription;
global using NativeTexture2DDescription = HelixToolkit.Wpf.SharpDX.Texture2DDescription;
global using NativeTexture3DDescription = HelixToolkit.Wpf.SharpDX.Texture3DDescription;
global using BindFlags = HelixToolkit.Wpf.SharpDX.BindFlags;
global using CpuAccessFlags = HelixToolkit.Wpf.SharpDX.CpuAccessFlags;
global using DataBox = HelixToolkit.Wpf.SharpDX.DataBox;
global using ResourceOptionFlags = HelixToolkit.Wpf.SharpDX.ResourceOptionFlags;
global using ResourceUsage = HelixToolkit.Wpf.SharpDX.ResourceUsage;
global using SampleDescription = HelixToolkit.Wpf.SharpDX.SampleDescription;
#else
#if CORE
global using BoundingBox = HelixToolkit.SharpDX.Core.BoundingBox;
global using BoundingFrustum = HelixToolkit.SharpDX.Core.BoundingFrustum;
global using Plane = HelixToolkit.SharpDX.Core.Plane;
global using Ray = HelixToolkit.SharpDX.Core.Ray;
global using NativeD3DDevice = HelixToolkit.SharpDX.Core.Native.SilkD3DDevice;
global using NativeD3DResource = HelixToolkit.SharpDX.Core.Resource;
global using NativeD3DTexture1D = HelixToolkit.SharpDX.Core.Texture1D;
global using NativeD3DTexture2D = HelixToolkit.SharpDX.Core.Texture2D;
global using NativeD3DTexture3D = HelixToolkit.SharpDX.Core.Texture3D;
global using NativeTexture1DDescription = HelixToolkit.SharpDX.Core.Texture1DDescription;
global using NativeTexture2DDescription = HelixToolkit.SharpDX.Core.Texture2DDescription;
global using NativeTexture3DDescription = HelixToolkit.SharpDX.Core.Texture3DDescription;
global using BindFlags = HelixToolkit.SharpDX.Core.BindFlags;
global using CpuAccessFlags = HelixToolkit.SharpDX.Core.CpuAccessFlags;
global using DataBox = HelixToolkit.SharpDX.Core.DataBox;
global using ResourceOptionFlags = HelixToolkit.SharpDX.Core.ResourceOptionFlags;
global using ResourceUsage = HelixToolkit.SharpDX.Core.ResourceUsage;
global using SampleDescription = HelixToolkit.SharpDX.Core.SampleDescription;
#else
global using BoundingBox = HelixToolkit.UWP.BoundingBox;
global using BoundingFrustum = HelixToolkit.UWP.BoundingFrustum;
global using Plane = HelixToolkit.UWP.Plane;
global using Ray = HelixToolkit.UWP.Ray;
global using NativeD3DDevice = HelixToolkit.UWP.Native.SilkD3DDevice;
global using NativeD3DResource = HelixToolkit.UWP.Resource;
global using NativeD3DTexture1D = HelixToolkit.UWP.Texture1D;
global using NativeD3DTexture2D = HelixToolkit.UWP.Texture2D;
global using NativeD3DTexture3D = HelixToolkit.UWP.Texture3D;
global using NativeTexture1DDescription = HelixToolkit.UWP.Texture1DDescription;
global using NativeTexture2DDescription = HelixToolkit.UWP.Texture2DDescription;
global using NativeTexture3DDescription = HelixToolkit.UWP.Texture3DDescription;
global using BindFlags = HelixToolkit.UWP.BindFlags;
global using CpuAccessFlags = HelixToolkit.UWP.CpuAccessFlags;
global using DataBox = HelixToolkit.UWP.DataBox;
global using ResourceOptionFlags = HelixToolkit.UWP.ResourceOptionFlags;
global using ResourceUsage = HelixToolkit.UWP.ResourceUsage;
global using SampleDescription = HelixToolkit.UWP.SampleDescription;
#endif
#endif
#endif
