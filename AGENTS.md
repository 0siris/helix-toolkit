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
- Phase 2: integrate `External/Logging` and `External/Assertions`, add ProjectReferences, implement the compatibility adapter, and pass the library/API gate.
- Phase 3: enable the approved `.editorconfig` and analyzer rules.
- Phase 4: migrate logging, guards, tests, and source formatting while preserving public renderer behavior.
- Phase 5: run solution, smoke, documentation, and name-hygiene verification.
- Do not add references to blocked external repositories in target code or documentation. Use the runtime-only verification variable defined by the migration plan for the final name-hygiene scan.

## Build

```powershell
dotnet build Source\SilkToolkit.slnx
```

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
