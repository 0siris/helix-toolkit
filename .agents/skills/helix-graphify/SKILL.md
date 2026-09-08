---
name: helix-graphify
description: Rebuild and verify the Helix Toolkit generated source graph locally after C#, MSBuild, or project-structure changes under Source. Use for graphify-out freshness, Graphify failures, and interrupted-run cleanup; never run Graphify through Rider or hand-edit generated output.
---

# Helix Graphify Workflow

Use the repository wrapper locally. It prepares a code-only corpus, extracts and clusters the graph, normalizes paths back to `Source/...`, and removes staging data.

## Run

1. Read `knowledge/index.md` and work from `F:/Repositories/helix-toolkit`.
2. Confirm that no Graphify run for this repository is already active. Never run two copies concurrently.
3. Run locally, not through Rider:

   ```powershell
   python tools/build_graphify_source.py
   ```

4. Wait for the command to finish. An interrupted or still-running process is not a successful gate.

Run this after relevant changes under `Source/` to C#, `.csproj`, `.props`, or `.targets`. Changes limited to agent skills or documentation do not require a graph rebuild.

## Handle Failures

- If `python` or `graphify` is missing, inspect the local PATH with `Get-Command python, graphify`. Do not install, update, or patch tooling without approval.
- After a failure, verify that no process remains and that `.graphify-code-corpus/` was removed before retrying.
- Treat package-version or stale-label notices as warnings unless the command exits unsuccessfully. Do not run install, update, or labeling commands unless requested.

## Verify

Require all of the following:

- The wrapper exits with code 0.
- `graphify-out/graph.json` exists and the output reports it was written.
- `.graphify-code-corpus/` no longer exists.
- `git diff --check` passes and repository status contains no unexpected files.

Report source-file, node, edge, and community counts when Graphify prints them. Keep warnings separate from failures. Never edit `graphify-out/` manually.
