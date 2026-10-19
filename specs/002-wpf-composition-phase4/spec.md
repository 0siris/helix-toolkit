# WPF composition — Phase 4

User authorization: execute Phase 4 of
[`wpf-komposition-phasen-parallele-tasks-und-reviews.md`](../../wpf-komposition-phasen-parallele-tasks-und-reviews.md),
using subagents for parallel tasks. Phase 3 ends at `619feb80d`.

## Requirements

- Restore public `EnableSwapChainRendering=false` by default; explicit `true` selects the existing HWND path.
  Select a backend before initialization and reject changes after initialization. Preserve public names and aliases.
- Connect the real Viewport template to the passive WPF canvas and productive presentation owner.
  Respect `EnableDpiScale`, separating physical render resolution from DIP input coordinates.
- Wake an idle viewport for every image-relevant property/scene/material/camera/manager change, including original
  property aliases. Diagnostic/status updates must not create a perpetual render loop.
- Integrate EffectsManager invalidation, resource disposal and reinitialization with dispatcher/GPU-safe lifetime.
  Preserve externally supplied managers. Shared scenes notify all consumers and retain nodes until the last owner leaves.
- Make `EnableD2DRendering` control Scene2D and capture while preserving ordinary WPF controls and the independent view cube.
- Preserve host/context/scene identities, explicit capture representation, exactly-once GPU-completion events,
  retained demand, bounded frame budgets and productive continuous sources.
- Preserve input priority (2D, view cube, scene), consumed input, mouse/touch promotion, manipulation cleanup,
  derived viewport events and gesture adorners. Test load/reload, template replacement and frontbuffer recovery.
- Keep warm heap pressure low. Normal transport uses GPU-only copying, nonblocking polls and retained callbacks/storage.
- Existing native tests/examples explicitly opt into HWND when that is their subject. Visible WPF acceptance includes
  real controls, clipping, ZIndex, VisualBrush, Blur and Adorner behavior without a popup.

## Acceptance and limits

The authoritative Phase 4 matrix is the existing phase plan, including P4.1–P4.7 and the findings through F34. Complete that matrix,
independent reviews and the serial build/test/heap gates, document material limitations, then stop before Phase 5.
The Phase 3 standard baseline is 729/737 with eight known failures; do not hide these through altered test selection.
Physical GPU fault injection, additional adapters and full final performance/parity audits remain Phase 5 scope.
