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
- `HelixToolkit.Wpf.sln` - WPF internal 3D engine (DirectX9)
- `HelixToolkit.Core.Wpf.sln` - Core WPF 3D models
- `HelixToolkit.SharpDX.sln` - SharpDX-based DirectX11 engine
- `HelixToolkit.SharpDX.Core.sln` - Core SharpDX components (netstandard/.NET Core)
- `HelixToolkit.WinUI.sln` - WinUI platform support
- `HelixToolkit.UWP.sln` - Universal Windows Platform

**Examples** directory contains ~60 demo projects.

## Key Conventions

- **Test framework**: NUnit 3.x with NUnit3TestAdapter
- **Build warnings**: `TreatWarningsAsErrors=true` in Release configuration
- **Code signing**: Assembly signed with `HelixToolkit.snk` in Release builds
- **Shared code**: Uses `.projitems` file imports for shared project files
- **Language version**: C# 14 (`LangVersion=latest`)
- **Target Frameworks**: `.NET 10` (WPF), `netstandard2.0` (Core), `net10.0-windows` (WinUI)
- **global.json**: SDK 10.0.0 with `rollForward: latestMajor`

## Testing Patterns

Tests require model files in `Models/` directory (STL, OBJ, OFF, LWO, 3DS formats).

## Important Notes

- **Right-handed coordinate system** by default; left-handed requires manual winding order correction
- **Row-major matrices** used throughout
- **Shader compilation**: Effect shaders in `ShaderEffects/` compiled via `compileEffects.cmd`
- **CI**: Uses AppVeyor (not GitHub Actions)
