---
type: KnowledgeDatabase
title: Project Source Knowledge Graph
description: Generated Graphify database describing the repository's C# and MSBuild source tree.
resource: ../graphify-out/graph.json
tags: [knowledge, graphify, source, architecture]
generated: { by: process:graphify-source-builder, at: 2026-10-20T11:59:18+02:00 }
status: stable
verified: 2026-10-20

generated: { by: process:graphify-source-builder, at: 2026-10-23T00:00:00+02:00 }
status: stable
verified: 2026-10-23
sources:
  - id: builder
    resource: ../tools/build_graphify_source.py
    title: Reproducible source graph builder
  - id: report
    resource: ../graphify-out/GRAPH_REPORT.md
    title: Current Graphify report
---

# Purpose

This generated knowledge database provides navigable relationships across the repository's C# and MSBuild source files. The durable source of truth remains the OKF bundle under `/knowledge` and the source tree under `/Source`.

# Initialization

From the repository root, run:

```powershell
python tools\build_graphify_source.py
```

The builder creates a temporary code-only corpus, runs Graphify AST extraction without an LLM API key, normalizes generated source paths to `Source/...`, runs deterministic community clustering, and removes the temporary corpus.

# Current Snapshot

The 2026-10-20 Phase 5 snapshot prepares 1,064 source files and contains 10,888 nodes, 19,850 edges and 3,150 communities.

The 2026-10-23 snapshot prepares994 source files and contains9,404 nodes,16,987 edges and2,668 communities.
Paths are normalized to `Source/...`; no temporary staging corpus remains. Extraction uses the local Graphify
AST API directly, in one process, avoiding CLI semantic-backend credential checks. Input/output token counts
are zero. The parser reports 17 files with syntax it can only partially extract; this graph is structural
assistance, not complete semantic or compiler verification. The .NET builds/tests remain authoritative.

The current 2026-10-25 namespace-fixture snapshot prepares1,075 source files and contains11,065 nodes,
20,079 edges and3,310 communities. Input/output model tokens remain zero. The same17 partial-parser
warnings remain; the verified temporary corpus was absent before and after generation. The two test
namespace edits do not establish a new production behavior or close the older three test failures.

# Scope and Limitations

- Included: C# source and MSBuild project/configuration files below `Source/`.
- HLSL bodies are outside AST extraction; shader compilation/resource tests remain a separate verification.
- Extraction: deterministic AST and project-structure analysis; token cost was zero.
- Excluded from this graph: prose concepts under `/knowledge`, because semantic document extraction requires an LLM backend that is not configured in this repository.
- Rebuild after source changes with the same script; do not edit `graphify-out/` manually.

# Installed 0.7.13 compatibility snapshot (2026-10-25)

The software-publication source check prepared 1,076 files (1,019 C# files). Installed Graphify 0.7.13
excludes every path component beginning with a dot, so the unchanged builder's hidden corpus initially
produced zero selected files. A second attempt reached AST extraction but rejected the removed
community_labels keyword on export. Both failed logs are retained; neither is a successful graph check.

The final run used the same repository builder with two process-local API adapters: select C# files from
the already filtered staging corpus, and omit the unsupported export keyword (report labels remain).
No repository script, installation or configuration was changed. The run produced 7,735 nodes, 12,389 edges
and 743 communities with zero model tokens. Source paths are normalized and the temporary corpus is gone.
These counts use a different installed extractor and are not coverage-parity claims against older snapshots.
This installed version has no MSBuild extractors. Compiler and actual runtime tests remain authoritative.
Evidence: TEMP/meshmixer-isolated-build-20261025/wpf-software-source-graph-r{1,2,3} logs and final results JSON.


# WPF teardown source snapshot (2026-10-25)

The existing builder prepares1,076 files and structurally extracts1,019 CSharp files after the additive
explicit/deferred WPF teardown change. Installed0.7.13 uses the same two documented process-local
collector/export adapters; the first missing-export-adapter error is retained separately. Final output:
7740 nodes,12399 edges,742 communities, zero model tokens. The builder, installation and
configuration remain unchanged, normalized paths contain no staging prefix and the staging corpus is removed.
No MSBuild extraction or semantic/compiler correctness is inferred from this graph. Evidence:
TEMP/meshmixer-isolated-build-20261025/inspector-close-detach-source-graph-r2-results.json and its log.


# Inspector region source snapshot (2026-10-25)

The unchanged existing builder was rerun after the scoped frame/shader/test changes: 1,076 prepared files,
1,019 CSharp files, 7,756 nodes, 12,431 edges and 722 communities, with zero model tokens. Installed 0.7.13
uses the same process-local collector/export compatibility adapters; imported cache aliases are disabled.
Normalized outputs contain no staging prefix, and the temporary corpus is removed. No installation,
configuration or builder mutation occurred. This graph does not establish CSharp/compiler or MSBuild coverage.
Evidence: TEMP/meshmixer-isolated-build-20261025/inspector-region-source-graph-r1-results.json and its log.


# WPF lock-pressure test source snapshot (2026-10-25)

The unchanged existing builder analyzes the one-file test-phase update: 1,076 prepared files, 1,019
CSharp files, 7,757 nodes, 12,437 edges and 752 communities, zero model tokens. The established
process-local collector/export adapters and disabled cache aliases remain; no installation, builder
or configuration change. Normalized paths have no staging prefix and the staging corpus is removed.
This source-only graph does not establish compiler, MSBuild or runtime correctness. Evidence:
TEMP/meshmixer-isolated-build-20261025/wpf-lock-phase-source-graph-results-r1.json and its log.
