---
name: helix-rider
description: Use Rider and ReSharper MCP correctly for the Helix Toolkit SilkToolkit solution. Use for solution builds, filtered tests, sequential example startup checks, debugger-based exception diagnosis, formatting, and diagnostics in F:/Repositories/helix-toolkit; use helix-graphify instead for Graphify.
---

# Helix Rider Workflow

Use Rider for IDE-owned build, run, test, and debug work. Keep one operation active at a time and continue from the current example instead of restarting an already completed sequence.

## Establish Context

1. Read `knowledge/index.md` and `.mcp.json`.
2. Use `F:/Repositories/helix-toolkit/Source` as `rootFolder`; do not hardcode the Rider port because `.mcp.json` is authoritative.
3. Probe with `rider_get_solution_projects` and require a successful result containing `SilkCore` or `SilkToolkit` before building, testing, or running.
4. Use `solutionName: "SilkToolkit"` and absolute source paths for ReSharper operations.

## Build and Test

1. Start one build with `rider_build_solution_start` and `rebuild: false`.
2. Poll that build's `sessionId` with `rider_build_solution_state` until terminal. Treat `buildIsSuccess: false` as the build failure; do not start a duplicate CLI build.
3. Run the standard tests through `rider_execute_terminal_command` from the solution root with `executeInShell: false`:

   ```text
   dotnet test SilkToolkit.slnx --no-build --filter "Category!=Hardware&Category!=DX12"
   ```

4. If Rider is unavailable before an operation starts, use the matching `dotnet` command from the repository root. Never rerun a test or build through the fallback merely because a started Rider operation failed.
5. Keep `Hardware` and `DX12` tests opt-in.

## Diagnose Examples Sequentially

1. Read `rider_get_run_configurations` and select the exact example configuration. Pass launch overrides only when `supportsDynamicLaunchOverrides` is true.
2. Start exactly one example. When diagnosing exceptions, start it with the Rider debugger.
3. Wait for startup. If execution pauses, inspect the exception, stack, and relevant frame values before editing.
4. Fix the shared root cause, add the smallest regression test, rebuild, rerun that test, and then rerun the same example.
5. Continue to the next example only after the current one starts stably. Stop only the current session or process and verify that no older example remains active.

A stable startup proves initialization, not interactive UI behavior.

## Finish Changed Code

1. Format only changed C# files with `resharper_format_file` in `format` mode; use `rider_reformat_file` only when ReSharper formatting is unavailable.
2. Run ReSharper diagnostics with `minSeverity: "warning"` and inspect targeted findings. Do not apply cleanup or global suggestions without prior inspection.
3. Run the Rider solution build and standard filtered tests.

Run Graphify locally with `$helix-graphify`; do not run it through Rider.
