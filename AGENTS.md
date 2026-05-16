# AGENTS.md

## Build-Anforderungen

- **Visual Studio 2026**
- **Windows 10 SDK** (Min. Version 10.0.18362.0)
- **LangVersion:** 10.0 (C# 14 Features)
- **Shell:** PowerShell 7+
- **Solution-Format:** Modernes `.slnx` XML-basiertes Format (ab .NET SDK 9.0.200)
- **Build-Tool:** `dotnet` CLI (nicht msbuild)
- **.NET SDK:** 10.0.0+ (`global.json` configured)

## Quick Start

```bash
# Build all solutions
msbuild Source\HelixToolkit.Wpf.sln /t:Build /p:Configuration=Debug
msbuild Source\HelixToolkit.SharpDX.sln /t:Build /p:Configuration=Debug
msbuild Source\HelixToolkit.Core.Wpf.sln /t:Build /p:Configuration=Debug

# Run tests
dotnet test Source\HelixToolkit.Wpf.Tests\HelixToolkit.Wpf.Tests.csproj
dotnet test Source\HelixToolkit.Wpf.SharpDX.Tests\HelixToolkit.Wpf.SharpDX.Tests.csproj
dotnet test Source\HelixToolkit.Tests\HelixToolkit.Tests.csproj
```

## Repository Structure

**7 separate solutions** targeting different platforms:

| Solution | Platform | Engine | Key Defines |
|----------|----------|--------|-------------|
| `HelixToolkit.Wpf.sln` | WPF (.NET 4.6.2 / netcoreapp3.1) | DirectX9 | (none) |
| `HelixToolkit.Core.Wpf.sln` | WPF (netcoreapp3.1) | Core WPF | (shared from Wpf) |
| `HelixToolkit.SharpDX.sln` | WPF/UWP/WinUI | DirectX11 | SHARPDX, DX11, MSAA, DX11_1 |
| `HelixToolkit.SharpDX.Core.sln` | netstandard2.0 | DirectX11 Core | NETFX_CORE, CORE, SHARPDX |
| `HelixToolkit.WinUI.sln` | WinUI 3 (net6.0-windows) | DirectX11.1 | WINUI, DX11_1 |
| `HelixToolkit.UWP.sln` | UWP (10.0.18362.0) | DirectX11 | WINDOWS_UWP, NETFX_CORE |
| `HelixToolkit.Kinect.sln` | WPF | Kinect DepthSensor | (uses Wpf engine) |
| `HelixToolkit.AppVeyor.sln` | CI (all platforms) | All engines | - |

**Key Engine Projects:**
- `HelixToolkit.Wpf.csproj` - WPF DirectX9 renderer (netstandard2.0 wrapper)
- `HelixToolkit.Wpf.Input.csproj` - WPF input handling
- `HelixToolkit.SharpDX.Core.csproj` - SharpDX DirectX11 core engine (netstandard2.0)
- `HelixToolkit.SharpDX.Core.Wpf.csproj` - SharpDX WPF interop layer
- `HelixToolkit.Wpf.SharpDX.csproj` - WPF + SharpDX bridge
- `HelixToolkit.WinUI.csproj` - WinUI 3 renderer
- `HelixToolkit.UWP.csproj` - UWP renderer
- `HelixToolkit.SharpDX.Core.Assimp.csproj` - Assimp 3D model loader integration
- `HelixToolkit.Wpf.SharpDX.Assimp.csproj` - WPF + Assimp integration
- `HelixToolkit.csproj` - Core shared library (netstandard2.0, no defines)

**Shared Projects (.projitems):**
- `HelixToolkit.Shared.shproj` - Shared core code (used by Wpf, SharpDX.Core, UWP, WinUI)
- `HelixToolkit.Wpf.Shared.shproj` - WPF-specific shared code
- `HelixToolkit.SharpDX.Shared.shproj` - SharpDX-specific shared code
- `HelixToolkit.SharpDX.SharedModel.shproj` - Shared 3D model types
- `HelixToolkit.UWP.Shared.shproj` - UWP-specific shared code
- `HelixToolkit.SharpDX.Assimp.Shared.shproj` - Assimp shared code

**Examples** directory contains ~60 demo projects across WPF, WPF.SharpDX, SharpDX.Core, WinUI, UWP, and Kinect categories.

## Define Constants (non-default, excluding TRACE/DEBUG)

| Constant | Platform/SDK |
|----------|-------------|
| `NETFX_CORE` | .NET Native / UWP runtime |
| `CORE` | SharpDX Core library |
| `SHARPDX` | SharpDX DirectX binding |
| `DX11` | DirectX 11 |
| `DX11_1` | DirectX 11.1 |
| `MSAA` | Multi-sample Anti-Aliasing |
| `WINDOWS_UWP` | Universal Windows Platform |
| `WINUI` | Windows UI Library |
| `COREWPF` | Core WPF binding |
| `ASSIMP` | Assimp model loader (separate package) |

## Key Conventions

- **Test framework**: NUnit 3.x with NUnit3TestAdapter
- **Build warnings**: `TreatWarningsAsErrors=true` in Release configuration
- **Code signing**: Assembly signed with `HelixToolkit.snk` in Release builds
- **Shared code**: Uses `.projitems` file imports for shared project files
- **Language version**: C# 14 (`LangVersion=latest`)
- **Target Frameworks**: `.NET 10` (WPF), `netstandard2.0` (Core), `net10.0-windows` (WinUI)
- **global.json**: SDK 10.0.0 with `rollForward: latestMajor`

## Repository Visualization

Repository structure diagrams are maintained in `docs/`:
- `docs/repository-structure.puml` - PlantUML source (7 solutions, project dependencies, defines)
- `docs/repository-structure.svg` - Generated SVG diagram

To regenerate:
```bash
java -jar "C:\Users\weigandt-dev\Downloads\plantuml-mit-1.2026.2.jar" -tsvg docs\repository-structure.puml -o docs\
```

## Testing Patterns

Tests require model files in `Models/` directory (STL, OBJ, OFF, LWO, 3DS formats).

## Important Notes

- **Right-handed coordinate system** by default; left-handed requires manual winding order correction
- **Row-major matrices** used throughout
- **Shader compilation**: Effect shaders in `ShaderEffects/` compiled via `compileEffects.cmd`
- **CI**: Uses AppVeyor (not GitHub Actions)
