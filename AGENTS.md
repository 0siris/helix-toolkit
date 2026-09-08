# AGENTS.md

## Start Here

- Read `knowledge/index.md` before answering repository questions or making non-trivial changes.
- Treat `knowledge/` as the repository's OKF v0.2 knowledge bundle.
- The local canonical OKF specification copy is `knowledge/references/okf-spec.md`.
- Treat `graphify-out/` as generated knowledge-database output; do not hand-edit it.

## Current Repository

- Main solution: `Source/SilkToolkit.slnx`
- Build tool: `dotnet` CLI
- Shell: PowerShell 7+
- SDK: see `global.json`
- Shared build settings: `Source/Directory.Build.props`
## C#/.NET Coding Conventions

- C# uses four-space indentation; keep lines near 120 characters where practical; MSBuild/XML uses two spaces.
- Nullable reference types are enabled. Trust nullable analysis and remove redundant null checks for non-nullable values.
- Use file-scoped namespaces for single-namespace files. Keep block-scoped namespaces only for multiple namespaces or documented technical constraints.
- Omit braces only for clear single-statement control-flow bodies; keep them for nesting, multiple statements, or complex logic.
- Prefer clear collection expressions, simple extension methods, immutable semantic value types, structured logging templates, and async-all-the-way APIs.
- Every non-generated C# declaration under `Source/` receives complete XML documentation, including private/internal declarations; generated designer/resource output is exempt.
- Source comments and technical documentation are English. User-facing agent communication and plans may be German.
- Use `LoggerLib` for logging and `Assertions` for BCL argument/state guards after Phase 2 integration. Preserve existing public `HelixToolkit.Logger.LogManager` signatures through the adapter.
- Existing library APIs are additive-only: no removed/renamed members, changed parameters, or behavior-breaking changes.

## Phased Adoption Workflow

- Phase 1: establish this agent contract and its OKF policy; do not change source or build configuration.
- Phase 2: integrate `External/Logging` and the `ValidSphere` NuGet package, add references, implement the compatibility adapter, and pass the library/API gate.
- Phase 3: enable the approved `.editorconfig` and analyzer rules.
- Phase 4: migrate logging, guards, tests, and source formatting while preserving public renderer behavior.
- Phase 5: run solution, smoke, documentation, and name-hygiene verification.
- Do not add references to blocked external repositories in target code or documentation. Use the runtime-only verification variable defined by the migration plan for the final name-hygiene scan.

## Build

```powershell
dotnet build Source\SilkToolkit.slnx
```

## Rider/ReSharper MCP Workflow

- Project MCP configuration: `.mcp.json` defines `rider` at `http://127.0.0.1:64482/stream` and `resharper` at `http://127.0.0.1:23741/`.
- Rider context is always `rootFolder: "F:/Repositories/helix-toolkit/Source"`; Rider file and project paths are relative to that solution root.
- ReSharper context is always `solutionName: "SilkToolkit"`; ReSharper source file paths are absolute. Multiple Rider solutions may be open, so never rely on implicit solution selection.
- Before a build or test, probe Rider with `rider_get_solution_projects` and require a successful response containing `SilkCore` or `SilkToolkit`.
- If the probe succeeds, build with `rider_build_solution_start` (`rebuild: false` for normal builds), then poll `rider_build_solution_state` with its `sessionId` until a terminal state. A started build with `buildIsSuccess: false` is a build failure, not a reason to rerun through `dotnet`.
- Use `dotnet build Source\SilkToolkit.slnx` from the repository root only when the Rider probe or build start fails because the MCP is unavailable or lacks the solution context.
- For the standard tests, use Rider MCP `rider_execute_terminal_command` in the solution root with `executeInShell: false` and `dotnet test SilkToolkit.slnx --no-build --filter "Category!=Hardware"`. The tested wrapper fails when `executeInShell: true`; this route preserves the deterministic repository filter, and the discovered test run configurations do not support dynamic launch overrides.
- If Rider is unavailable before tests start, use `dotnet test Source\SilkToolkit.slnx --no-build --filter "Category!=Hardware"` from the repository root. Do not rerun a started test process through the fallback after a test failure.
- Use `rider_get_run_configurations` and `rider_execute_run_configuration` for targeted projects or run points. Do not combine `configurationName` with `filePath`/`line`, and only pass launch overrides when `supportsDynamicLaunchOverrides` is `true`.
- After code edits, run `resharper_get_diagnostics` with `solutionName: "SilkToolkit"`; use `minSeverity: "warning"` for the normal gate and `"error"` for a blocker-only check.
- Before applying a positional fix, list options with `resharper_list_quick_fixes`; apply only the selected `fixId` through `resharper_apply_quick_fix`. Use `resharper_fix_usings` for unambiguous imports and explicit `resolutions` for ambiguous types.
- Format changed C# files with `resharper_format_file`, `mode: "format"`, absolute paths, and `solutionName: "SilkToolkit"`. Use `mode: "cleanup"` and `resharper_apply_suggestions` only after an explicit dry run or inspection selection; they can make semantic style changes.
- If ReSharper formatting is unavailable, use `rider_reformat_file` with solution-relative paths. Use `rider_lint_files` for batches and `rider_get_file_problems` for a single-file error check.
- For symbol work, use Rider/ReSharper navigation and semantic APIs before text search: `rider_search_symbol`, `resharper_go_to_definition`, `resharper_find_usages`, `resharper_find_implementations`, `resharper_get_call_hierarchy`, `resharper_flow`, and semantic rename APIs.
- `Hardware` and `DX12` tests remain opt-in. Debugger APIs, cleanup, and global suggestions are opt-in and must not be part of the standard quality gate.

## OKF Rules

- Add durable repository knowledge under `knowledge/`.
- Every non-reserved `knowledge/**/*.md` file is an OKF Concept and must start with a UTF-8 YAML frontmatter block containing a non-empty `type`.
- `index.md` and `log.md` are reserved OKF files; the bundle-root `index.md` may declare `okf_version: "0.2"`.
- New or meaningfully changed concepts SHOULD use OKF v0.2 provenance and lifecycle fields such as `generated`, `sources`, `verified`, `status`, and `stale_after` where applicable.
- Update `knowledge/log.md` when adding or meaningfully changing OKF concepts.
- Prefer bundle-relative links such as `/projects/silkcore.md`.

## Knowledge Database

- The durable source of truth is the versioned OKF bundle under `knowledge/`.
- The generated project graph analyzes the C# and MSBuild source files under `Source/`.
- Initialize or rebuild the project graph with `python tools/build_graphify_source.py`.
- Run the same script after source changes; it prepares a temporary code-only corpus, runs Graphify, normalizes paths back to `Source/...`, and removes the staging corpus.
- Generated artifacts belong under `graphify-out/` and must not be edited manually.

## Working Rules

- Do not overwrite or revert user changes unless explicitly asked.
- Keep edits scoped to the request.
- Prefer existing repo patterns and local knowledge over new abstractions or tooling.
