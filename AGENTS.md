# AGENTS.md

## Start Here

- Read `knowledge/index.md` before answering repository questions or making non-trivial changes.
- Treat `knowledge/` as the repository's OKF knowledge bundle.
- The local OKF specification copy is `knowledge/references/okf-spec.md`.

## Current Repository

- Main solution: `Source/SilkToolkit.slnx`
- Build tool: `dotnet` CLI
- Shell: PowerShell 7+
- SDK: see `global.json`
- Shared build settings: `Source/Directory.Build.props`

## Build

```powershell
dotnet build Source\SilkToolkit.slnx
```

## OKF Rules

- Add durable repository knowledge under `knowledge/`.
- Every non-reserved `knowledge/**/*.md` file is an OKF Concept and must start with YAML frontmatter containing a non-empty `type`.
- `index.md` and `log.md` are reserved OKF files.
- Update `knowledge/log.md` when adding or meaningfully changing OKF concepts.
- Prefer bundle-relative links such as `/projects/silkcore.md`.

## Working Rules

- Do not overwrite or revert user changes unless explicitly asked.
- Keep edits scoped to the request.
- Prefer existing repo patterns and local knowledge over new abstractions or tooling.
