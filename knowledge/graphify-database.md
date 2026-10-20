---
type: KnowledgeDatabase
title: Project Source Knowledge Graph
description: Generated Graphify database describing the repository's C# and MSBuild source tree.
resource: ../graphify-out/graph.json
tags: [knowledge, graphify, source, architecture]
generated: { by: process:graphify-source-builder, at: 2026-10-20T11:59:18+02:00 }
status: stable
verified: 2026-10-20
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
Paths are normalized to `Source/...`; no temporary staging corpus remains. Extraction uses the local Graphify
AST API directly, in one process, avoiding CLI semantic-backend credential checks. Input/output token counts
are zero. The parser reports 17 files with syntax it can only partially extract; this graph is structural
assistance, not complete semantic or compiler verification. The .NET builds/tests remain authoritative.

# Scope and Limitations

- Included: C# source and MSBuild project/configuration files below `Source/`.
- Extraction: deterministic AST and project-structure analysis; token cost was zero.
- Excluded from this graph: prose concepts under `/knowledge`, because semantic document extraction requires an LLM backend that is not configured in this repository.
- Rebuild after source changes with the same script; do not edit `graphify-out/` manually.
