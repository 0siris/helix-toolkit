---
name: demo-automation
description: "Use this skill whenever a running SilkToolkit demo or example must be steered, inspected, or debugged: move its camera, screenshot its viewport, read its scene tree, pick objects, toggle visibility, check render info, techniques, or logs via REST and MCP. Use for any demo interaction or demo debugging task."
---

# Demo Automation Debugging

Use this skill whenever a SilkToolkit demo or example must be steered or debugged — never drive a demo blind, via the debugger, or by guessing from source alone when this API is available. Debug loop: inspect → intervene → prove with a screenshot. Use screenshots for visual analysis: capture via `GET /api/screenshot` or `viewport_screenshot`, view the image, and compare before/after states to confirm every hypothesis. No debugger, no restart, no window focus.

Every demo under `Source/Examples/SilkToolkit/` exposes a loopback REST API and an MCP endpoint (Streamable HTTP on `/mcp`) through `DemoCore.Automation`.

## Find the Demo

Read the base URL from `%TEMP%/helix-demo-automation/<pid>.url` (one file per demo; ignore dead PIDs, or use `DemoCore.Automation.DemoDiscovery.ListRunning()` from DemoCore). Fixed port via `HELIX_DEMO_PORT`, kill switch `HELIX_DEMO_AUTOMATION=0`. Loopback-only, no auth. `GET /api/status` names the demo.

## REST

| Call | Purpose |
|---|---|
| `GET /healthz`, `GET /api/status` | Liveness, demo name, viewport size, triangle count |
| `GET /api/camera`, `POST /api/camera` | Read state; patch (null keeps, `cameraType` switches perspective/orthographic) |
| `POST /api/camera/zoom-extents`, `POST /api/camera/reset` | Frame scene, restore default |
| `GET /api/scene/stats`, `GET /api/scene/tree?depth&limit` | Counts and bounds; node tree with Guid ids, visibility, bounds |
| `POST /api/scene/nodes/{guid}/visibility`, `.../focus` | Hide/show a node; zoom to its bounds |
| `POST /api/scene/clear`, `POST /api/model/load {"path"}` | Scene ops (scene-host demos only) |
| `POST /api/pick {"x","y","maxHits"}` | Viewport DIPs in, nearest-first hits out (`[]` = missed) |
| `GET /api/screenshot[?width&height]`, `GET /api/screenshot.json` | PNG bytes or base64 |
| `GET /api/render`, `POST /api/render/technique {"name"}` | Real FPS, triangles, active and known techniques |
| `GET /api/logs[?level&limit]`, `POST /api/logs/level {"level"}` | Ring buffer (500, from demo start), level: verbose/debug/info/warn/error/fatal |

Errors: 404 file/node, 409 `no-scene-host`, 422 validation (`unknown-technique`, `unknown-level`, `no-bound`), 503 surface/attach gap, 500 fallback. Bodies are `{error}`, never stack traces.

## MCP

`POST /mcp` with `Accept: application/json, text/event-stream`. Send `initialize` first (`protocolVersion` e.g. `2025-06-18`), then `tools/list` / `tools/call`. Failures arrive as `{error}` payloads.

Tools: `DemoStatus`, `camera_get|set`, `camera_zoom_extents|reset`, `scene_stats|clear|tree|set_visible|focus`, `model_load(path)`, `pick(x,y,maxHits?)`, `viewport_screenshot(width?,height?,maxBytes?)`, `render_info|set_technique(name)`, `logs_get(level?,limit?)`, `logs_set_level(level)`.

## Debug Recipes

- Empty/wrong scene: `status` → `scene/tree` → `screenshot`. What is missing vs. expected?
- Isolate an object: `pick` at its screen position → `scene_set_visible` off → screenshot diff proves identity → toggle back on.
- Circle misbehavior: `logs_get` (raise to `debug` if quiet) + `render_info` (FPS collapsed? wrong technique?).
- Camera suspicion: `camera_get`, move or `scene_focus` the node, compare screenshots.
- Technique suspicion: list `techniques` from `render_info`, set one, confirm the name reflects back.

## Limits

- `model/load` currently returns 503: `EffectsManager.geometryBufferManager` is unassigned repo-wide since the DX12 cutover (SilkCore backlog, not an automation bug).
- Setting the viewport technique writes the DP; visual effect in the Silk path is unverified — confirm via screenshot, not assumption.
- Picking empty space returns `[]`; the log buffer starts at demo attach; demos without a scene host answer 409 on scene/model routes.
