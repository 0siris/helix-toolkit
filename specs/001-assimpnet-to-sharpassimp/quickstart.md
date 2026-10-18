# Quickstart: AssimpNet to SharpAssimp Migration

## Prerequisites

- .NET 10 SDK, PowerShell 7+, `External/Logging` submodule checked out.
- Branch `silk-toolkit`, clean tree.

## 1. Record the pre-migration baseline

```powershell
dotnet build Source/SilkAssimp/SilkAssimp.csproj --nologo -v minimal
```

Load each sample file through `Importer` (FileLoadDemo or a scratch runner)
and note mesh/vertex/material counts:

- `Models/obj/bunny/bunny.obj`
- `Models/glTF/robot nier/scene.gltf`
- `Models/stl/cube.stl`
- `Models/ply/tetra_ascii.ply`
- `Models/FBX/Solus The Knight/Solus_The_Knight.fbx`

## 2. Swap the binding and port touch points

Per `research.md` mapping; build after each file:

```powershell
dotnet build Source/SilkAssimp/SilkAssimp.csproj --nologo -v minimal
```

## 3. Run the gates

```powershell
# standard gate (must be green, includes new round-trip tests)
dotnet test Source/SilkToolkit.slnx --no-build --filter "Category!=Hardware&Category!=LargeModels"
# opt-in large models (manual)
dotnet test Source/SilkAssimp.Tests --filter "Category=LargeModels"
```

## 4. Expected outcomes

- No `AssimpNet` reference remains in the solution.
- Round-trip tests pass with baseline counts; any delta is commented inline.
- CI (`silk-tests.yml`, updated filter) is green.
