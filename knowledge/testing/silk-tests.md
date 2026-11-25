---
type: TestStrategy
title: Silk test strategy
description: xUnit v3 test layout, categories, coverage, and local commands for the Silk projects.
tags: [testing, xunit, coverage, silkcore, silktoolkit, silkassimp]
timestamp: 2026-07-20T00:00:00+02:00
generated: { by: "agent:gpt-6.1-sol", at: 2026-10-20T07:15:06Z }
status: stable
sources:
  - resource: ../../AGENTS.md
  - resource: ../../Source/SilkCore.Tests/SilkCore.Tests.csproj
  - resource: ../../Source/SilkToolkit.Tests/SilkToolkit.Tests.csproj
  - resource: ../../Source/SilkAssimp.Tests/SilkAssimp.Tests.csproj
  - resource: wpf-composition-phase4.md
---

# Silk Test Strategy

The solution contains one xUnit v3 project per production assembly:

* `Source/SilkCore.Tests`
* `Source/SilkToolkit.Tests`
* `Source/SilkAssimp.Tests`

Tests use only xUnit assertions. The standard command excludes both `Hardware` and `DX12` categories;
those checks remain opt-in, including WARP cases also marked `DX12`. Other `Unit`, `Warp` and `Wpf` cases
remain eligible under the standard filter.

This 2026-10-20 Phase 5 documentation correction is source/static only; no commands below were run for it.
The dated [Phase 4 results](/testing/wpf-composition-phase4.md) retain their original scope and counts;
parent Phase 5 build, test and runtime gates are pending.

## Local Commands

### Rider MCP primary route

After a successful Rider build, run the deterministic standard suite through `rider_execute_terminal_command` with `rootFolder: "F:/Repositories/helix-toolkit/Source"`, `executeInShell: false`, and:

```powershell
dotnet test SilkToolkit.slnx --no-build --filter "Category!=Hardware&Category!=DX12"
```

The discovered `SilkCore.Tests`, `SilkToolkit.Tests`, and `SilkAssimp.Tests` Rider configurations do not support dynamic launch overrides, so the terminal route preserves the repository's category filter. Use `rider_get_run_configurations` plus `rider_execute_run_configuration` only for targeted projects or run points.

### CLI fallback

If Rider is unavailable before the test process starts, run the same standard suite from the repository root:

```powershell
dotnet build Source\SilkToolkit.slnx
dotnet test Source\SilkToolkit.slnx --no-build --filter "Category!=Hardware&Category!=DX12"
```

Do not rerun a started Rider test process through the fallback after a test failure. Both `Hardware` and `DX12`
require a separate opt-in run. Core WARP evidence does not imply support for the WPF bridge, which rejects WARP.

Run explicit smokes locally through the VSTest RunSettings option:

```powershell
dotnet test Source\SilkCore.Tests\SilkCore.Tests.csproj --filter "Category=DX12" -- xUnit.Explicit=only
dotnet test Source\SilkToolkit.Tests\SilkToolkit.Tests.csproj --filter "Category=Hardware" -- xUnit.Explicit=only
```

Collect coverage for one production assembly:

```powershell
dotnet test Source\SilkCore.Tests\SilkCore.Tests.csproj -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura '-p:Include=[SilkCore]*'
```

## Coverage

The GitHub workflow writes separate Cobertura reports for `SilkCore`, `SilkToolkit`, and `SilkAssimp`. It publishes each line rate in the step summary and emits a warning below 70%. The 70% value is a direction, not a build or merge threshold.

Tests must not depend on the repository's absolute path or the full `Models` directory. Temporary files belong in the operating system's temporary directory and must be removed by the creating test.

## Related Concepts

* [Current Solution](/architecture/current-solution.md)
* [DX12 Migration Plan](/backlog/dx12-migration-plan.md)

# Citations

[1] [SilkCore tests](../../Source/SilkCore.Tests/SilkCore.Tests.csproj)
[2] [SilkToolkit tests](../../Source/SilkToolkit.Tests/SilkToolkit.Tests.csproj)
[3] [SilkAssimp tests](../../Source/SilkAssimp.Tests/SilkAssimp.Tests.csproj)
[4] [Silk test workflow](../../.github/workflows/silk-tests.yml)

## Camera and selection-line fixture namespace recovery, 2026-10-25

Two existing fixtures still imported namespaces removed by the original-namespace restoration.
Only imports and one fully qualified camera type were corrected; exact inverse checks recover both
previous byte sequences, including BOM, CRLF and EOF. Assertions, formulas and expected pixels did not change.
The complete original SilkCore.Tests Debug project now builds with zero errors and 169 warnings, using
BuildProjectReferences=false. The selected actual run passes CameraFramingTests 2/2 and
SelectionPreviewLinePipelineTests 18/18, including WARP readback, with no skips. Independent SOURCE/DATA
review reports findings[]. The three failures in the historical scoped 604-case run remain open and were
not rerun or erased by this result. WPF software publication and full product acceptance are separate gates.

Parent evidence: TEMP/meshmixer-isolated-build-20261025/build-silk-core-fixtures-current-r1.log and
silk-camera-line-fixtures-current-r1.trx. All eight protected parent Debug product/test pins remained equal.


## Renderer test contracts, 2026-10-25

Three retained D3D12RuntimePrimitiveTests failures were test-contract errors. SSAO kernels
use the original timestamp-seeded random sequence; independent arrays have no equality
or inequality requirement. Both arrays now retain length/layout checks and verify finite
hemisphere samples, index-dependent length and angular limits. FXAA constants are unchanged.
Texture removal and instance growth retire resources; assertions now distinguish cache
removal from explicit fence-safe cleanup. Instance sources are initialized as in real attachment.
No renderer product source, seed, scheduling, disposal policy or public API changed.

Actual unchanged Debug Before: three failures. After NoRef host builds, the same three
and25 empty-mesh/particle neighbors pass in Debug and Release with no skips. The full
original Core test project passes624/624 in Debug using
Category!=Hardware&Category!=DX12&Category!=VisibleUI. All601 historical Passed tuples
are preserved, the three corrected cases pass, and20 recovered camera/line cases are included.
Release broad reports620 Passed/4 Failed; the four intersection allocation assertions also
fail in isolation, expecting18432 bytes but observing0 after256 recorded frames. Their
subsequent readback assertions are unreached. No Before Release measurement of those four
exists, so this is a newly observed separate result, not a verified historical failure.

Parent evidence: TEMP/meshmixer-isolated-build-20261025/renderer-contract-results-r1.json
and renderer-contract-source-pdb-r1.json bind actual TRXs, current DLL/PDB/source and held
product/host copies. Before host PDB was overwritten by the next build and is not retained.
Independent SOURCE/DATA review confirms findings[] for these three corrections and retained data limits. Rider/ReSharper tools were unavailable;
scoped dotnet NoRef fallback was used. Graphify completed with17 partial syntax-warning files;
no complete AST, WPF software-policy, full Release or product acceptance is claimed.


## Measured intersection allocation baseline, 2026-10-25

The four Release failures above came from a fixed positive72-byte frame expectation,
not a proved allocation requirement. The fixture now measures a complete empty-candidate
frame immediately before the unchanged intersection frame on the same attached renderer.
Input publication and native generation are retained; both paths receive128 warmups and
256 measured frames with real submissions and fences. Exact baseline/full equality remains,
with no tolerance budget or configuration literal. The assertion follows the last WarmDraw
argument and red-pixel readbacks, after the baseline's blue clear. All later frames remain.

Actual original-project NoRef hosts build D/R with0 errors/94 warnings. Focus4/4 and broad
624/624 pass in both configurations, no skips; all names and prior Passed outcomes remain.
Debug baseline/full each18432B, Release each0B;1000 reuse checks0B and positive control64B.
All four earlier Release failures now reach their warm readbacks and remaining frame cases.
The pixel proof concerns the last warm frame, not individual readback of all256 frames.

Independent SOURCE/DATA review confirms no findings and exact source inverse/PDB/TRX binding.
Product DLLs remain unchanged. Shared allocation regressions are outside the differential
alone; unchanged frame/ordinary-mesh neighbors remain part of the passing624-case selection.
No concrete Debug allocator or JIT cause was proved. Graphify completes with17 partial-parser
warnings; no general performance, hardware, WPF, Original-pixel or product acceptance follows.
Parent evidence: TEMP/meshmixer-isolated-build-20261025/intersection-baseline-results-r1.json.


## Rebuild integration into silk-toolkit (2026-11-02)

The published `meshmixer-rebuild` revision `344317ff12c1bbfda1ed9c9b020069f81bbce076`
contains the previous `silk-toolkit` target `9e5b08091ab41d61ce6aacaf3f93861e5879ffcd`.
Integration is a fast-forward preserving both histories, including the MeshMixer renderer/WPF work and
published collision-resource, billboard-lifetime and TopMost fixes. No product or test source is edited
for this integration. The consumer's pinned submodule revision is not changed by moving this branch.

The Debug solution build succeeds. The standard `Category!=Hardware` run reports 984 passed and five
failed cases: two `AssimpMaterialImportTests.LoadNativeGltfFactorsTest` cases, both
`QueuedNativeMouseTests.RightDragMovesCameraBeforeOutsideRelease` cases, and
`NonblockingFramePresentationTests.DelayedFenceKeepsDispatcherLiveAndRendersLatestCameraAfterCompletion`.
Each failure is independently reproduced on the prior target revision, with the same message and first
assertion stack frame. The suite is therefore explicitly not reported as fully green. These five existing
failures remain open; integration does not weaken assertions or expand into an unrelated repair campaign.

Evidence: `C:/Windows/Temp/silk-toolkit-merge-results` retains the combined candidate TRX files and the two
focused `silk-toolkit-target-baseline-*.trx` files. Rider MCP was unavailable (connection refused on
127.0.0.1:64482); the repository's standard CLI fallback was used. Hardware/DX12 opt-in categories were
not newly enabled for this merge.
