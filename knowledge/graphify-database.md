---
type: KnowledgeDatabase
title: Project Source Knowledge Graph
description: Generated Graphify database describing the repository's C# and MSBuild source tree.
resource: ../graphify-out/graph.json
tags: [knowledge, graphify, source, architecture]
generated: { by: process:graphify-source-builder, at: 2026-09-06T00:00:00+02:00 }
status: stable
verified: 2026-09-06
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

The 2026-09-06 snapshot covers 940 source files and contains 12,054 nodes, 19,154 edges, and 3,045
communities. The source paths are normalized to `Source/...`; no temporary staging corpus remains.

# Scope and Limitations

- Included: C# source and MSBuild project/configuration files below `Source/`.
- Extraction: deterministic AST and project-structure analysis; token cost was zero.
- Excluded from this graph: prose concepts under `/knowledge`, because semantic document extraction requires an LLM backend that is not configured in this repository.
- Rebuild after source changes with the same script; do not edit `graphify-out/` manually.
