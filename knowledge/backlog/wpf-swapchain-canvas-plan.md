---
type: BacklogPlan
title: WPF Swap-Chain Canvas Plan
description: Second pure-WPF IRenderCanvas implementation (DPFCanvasSwapChain + HwndSwapChainHost) as a switchable alternative to the WinForms-based DPFSurfaceSwapChain; WinForms remains active and is removed in a follow-up step.
tags: [backlog, wpf, swapchain, helix, winforms-removal, hwndhost]
timestamp: 2026-07-09T19:00:00+02:00
---

# WPF-SwapChain-Canvas — Plan

## Context
Es gibt zwei bestehende `IRenderCanvas`-Implementierungen in `SilkToolkit`: `DPFCanvas` (rein WPF, `D3DImage`-basiert) und `DPFSurfaceSwapChain` (WinForms-basiert, nutzt `WindowsFormsHost` + `UserControl` + `WndProc` als HWND-Fabrik für D3D-Swap-Chain). Der Plan aus `local://winforms-extraction-plan.md` (Extraktion der WinForms-Typen in ein Schwesterprojekt) wird verworfen. Stattdessen: eine **zweite, rein WPF-basierte Implementierung** von DPFCanvas, die die D3D-Swap-Chain-Performance bietet aber auf `WindowsFormsHost`/`UserControl`/`WndProc` verzichtet. Ein Schalter auf `Viewport3DX` wählt zwischen den drei Pfaden (`DPFCanvas` | neue WPF-Swap-Chain | bestehende WinForms-Swap-Chain). WinForms bleibt vorerst aktiv und wird in einem späteren Schritt abgeschaltet.

Endzustand dieses Plans: `EnableSwapChainRendering="True"` plus neue Property `UseWpfSwapChain="True"` aktiviert die neue `DPFCanvasSwapChain` (rein WPF). Die bestehende `DPFSurfaceSwapChain` (WinForms) bleibt unverändert kompilierbar. `DPFCanvas` (rein WPF, D3DImage) bleibt die Default-Implementierung.

**Scope:** alle Edits und neuen Dateien leben in Projekten der `Source/SilkToolkit.slnx`; **kein** neues Projekt wird angelegt. Die neuen Dateien kommen unter `Source/SilkToolkit/Controls/`. Keine Datei außerhalb der slnx wird berührt; keine Datei außerhalb der slnx wird hinzugefügt.

## Approach

Schritte in der Reihenfolge, dass die Solution nach jedem Schritt baubar bleibt. Schritte 1–3 sind die neuen Klassen (rein WPF), Schritt 4 ist der Schalter, Schritt 5 ist die Verifikation, dass der WinForms-Pfad weiterhin funktioniert.

### 1. Neue Klasse `HwndSwapChainHost : HwndHost` anlegen
Pfad: `Source/SilkToolkit/Controls/HwndSwapChainHost.cs`. **Rein WPF** — keine `using System.Windows.Forms;`-Direktive.

Klasse `HwndSwapChainHost` erbt von `System.Windows.Interop.HwndHost`. Sie ist die WPF-seitige Entsprechung zu `WinformHostExtend` (ohne `WindowsFormsHost`-Abhängigkeit).

Konkret:

- **Konstruktor:** parameterlos, analog zu `WinformHostExtend` (`Controls/WinformHostExtend.cs:30-32`).
- **`BuildWindowCore(IntPtr hwndParent)` überschreiben:** erzeugt per P/Invoke `CreateWindowEx` ein einfaches Win32-Kind-Fenster (`STATIC`-Klasse, Stile `WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN | WS_CLIPSIBLINGS`, Position/Größe `(0, 0, 1, 1)` — wird vom WPF-Layout angepasst). Speichert das HWND in einem privaten Feld. Gibt `new HandleRef(this, hwnd)` zurück. P/Invoke-Signaturen (`CreateWindowEx`, `DestroyWindow`, `SetWindowPos`) als `internal static class W32` mit `DllImport("user32")` in derselben Datei — orientiert an der bestehenden `W32`-Klasse in `Controls/MouseHandlers/VirtualTouchDevice.cs:101-156`.
- **`DestroyWindowCore(HandleRef hwnd)` überschreiben:** `DestroyWindow(hwnd.Handle)`.
- **HwndSource hooken:** direkt nach dem Erzeugen des HWND `HwndSource.FromHwnd(hwnd).AddHook(WndHook)`. Die Hook-Methode hat Signatur `private IntPtr WndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)`.
- **WM_TOUCH behandeln:** analog zu `VirtualTouchDevice.WndProc` (`Controls/MouseHandlers/VirtualTouchDevice.cs:37-79`), aber mit `IntPtr`-Parametern statt `Message`-Struct. Die `TOUCHINPUT`-Struct und `GetTouchInputInfo`/`CloseTouchInputHandle` werden 1:1 aus `VirtualTouchDevice.W32` wiederverwendet (die Structs sind nicht WinForms-spezifisch). Touch-Aktivierung: in einer überschriebenen `OnWindowCreated(HwndCreatedEventArgs e)` (HwndHost-Lifecycle) `RegisterTouchWindow(hwnd, 0)` via P/Invoke aufrufen.
- **WM_MOUSE*-Behandlung:** `WM_MOUSEMOVE` (0x0200), `WM_LBUTTONDOWN` (0x0201), `WM_RBUTTONDOWN` (0x0204), `WM_MBUTTONDOWN` (0x0207), `WM_XBUTTONDOWN` (0x020B), `WM_MOUSEWHEEL` (0x020A) abfangen und in WPF-`MouseEventArgs`/`MouseWheelEventArgs`/`MouseButtonEventArgs` umwandeln, dann via `RaiseEvent` als RoutedEvent in den WPF-Tree schicken. Die Konstanten für die `WPARAM`-Maskierung der Maus-Tasten (z. B. `MK_LBUTTON = 0x0001`) als private const ints in der Klasse.
- **RoutedEvents deklarieren (gleiche Signaturen wie in `WinformHostExtend`):**
  ```csharp
  public delegate void FormMouseMoveEventHandler(object sender, FormMouseMoveEventArgs e);
  public delegate void FormMouseWheelEventHandler(object sender, FormMouseWheelEventArgs e);
  public static readonly RoutedEvent FormMouseMoveEvent = EventManager.RegisterRoutedEvent(
      "FormMouseMove", RoutingStrategy.Bubble, typeof(FormMouseMoveEventHandler), typeof(HwndSwapChainHost));
  public static readonly RoutedEvent FormMouseWheelEvent = EventManager.RegisterRoutedEvent(
      "FormMouseWheel", RoutingStrategy.Bubble, typeof(FormMouseWheelEventHandler), typeof(HwndSwapChainHost));
  ```
  Dazu `FormMouseMoveEventArgs` und `FormMouseWheelEventArgs` als verschachtelte Klassen in `HwndSwapChainHost`, **byteweise identisch** zu `Controls/WinformHostExtend.cs:149-228` (gleiche Felder `Delta`, `Location`, `X`, `Y`, gleiche Konstruktor-Signaturen, gleicher impliziter Operator zu `MouseWheelEventArgs` in `FormMouseWheelEventArgs`). Begründung: identische Signaturen halten den Vertrag in `Viewport3DX.FormMouseMove`/`FormMouseWheel` kompatibel.
- **`public event FormMouseMoveEventHandler FormMouseMove { add => AddHandler(...); remove => RemoveHandler(...); }`** — exakt wie `Controls/WinformHostExtend.cs:45-53`.
- **`public double DpiScale { get; set; }`** mit `DpiScaleChanged` Event — exakt wie `Controls/WinformHostExtend.cs:36-55`. DPI-Quelle: `HwndSource.CompositionTarget.TransformFromDevice.M11` (Kehrwert = DPI/96) neu berechnen in einer `UpdateDpiScale()`-Methode, die in `OnWindowCreated` und bei `WM_DPICHANGED` (0x02E0) aufgerufen wird.
- **Keine** `using System.Windows.Forms;`-Direktive in dieser Datei. Nur: `System`, `System.Runtime.InteropServices`, `System.Windows`, `System.Windows.Interop`, `System.Windows.Input`.

### 2. Neue Klasse `DPFCanvasSwapChain : Grid, IRenderCanvas, IDisposable` anlegen
Pfad: `Source/SilkToolkit/Controls/DPFCanvasSwapChain.cs`. **Rein WPF** — keine `using System.Windows.Forms;`-Direktive.

Strukturell identisch zu `DPFSurfaceSwapChain` (`Controls/DPFSurfaceSwapChain.cs`), aber:
- `private readonly HwndSwapChainHost swapChainHost = new();` ersetzt `private readonly WinformHostExtend winformHost = new();`
- `private RenderControl surfaceD3D;` wird ersetzt durch `private HwndSource swapChainHwnd;` oder einfacher: das HWND wird bei Bedarf via `swapChainHost.Handle` abgefragt
- `SetupVisual(bool attachedToWindow)` (analog `DPFSurfaceSwapChain.cs:98-109`): `Children.Add(image); Children.Add(swapChainHost); swapChainHost.DpiScaleChanged += DPFSurfaceSwapChain_DpiScaleChanged;` — **kein** `surfaceD3D = new RenderControl(this);` und **kein** `swapChainHost.Child = surfaceD3D;`
- D3D-Swap-Chain-Bindung in `SetupRenderHost` und im Konstruktor: statt `surfaceD3D.Handle` wird `swapChainHost.Handle` übergeben. Der `Handle` ist erst verfügbar **nachdem** WPF `BuildWindowCore` aufgerufen hat. Daher: Der `SwapChainRenderHost` wird erst im `Loaded`-Handler erzeugt (nicht im Konstruktor), oder die `DPFSurfaceSwapChain` muss das HWND-Ready abwarten (z. B. via `swapChainHost.Handle`-Property, die intern das HWND nach `BuildWindowCore` cached).
- `OnRenderSizeChanged` (Zeile 206-223): wie bisher, `RenderHost.Resize((int)ActualWidth, (int)ActualHeight)`.
- `D3DImageExt` (Zeile 268-309): **1:1** übernehmen, identisch.
- `Dispose(bool disposing)` (Zeile 315-329): `swapChainHost?.Dispose();` statt `winformHost?.Dispose();`.
- `public static T FindVisualAncestor<T>(...)` (Zeile 252-263): 1:1 übernehmen.

**DPI-Scale-Synchronisation:** in `DPFCanvasSwapChain` wird `DpiScale` zu `swapChainHost.DpiScale` durchgereicht, wie in `DPFSurfaceSwapChain.cs:89-92`. Wenn der `HwndSwapChainHost` ein `DpiScaleChanged` auslöst, ruft der Handler `RenderHost.DpiScale` an.

**Wichtig — Public-API-Grenze:** Da `FormMouseMoveEventArgs` und `FormMouseWheelEventArgs` jetzt in `HwndSwapChainHost` definiert sind (statt in `WinformHostExtend`), ist `Viewport3DX.FormMouseMove` (Controls/Viewport3DX.Properties.cs:2958-2968) breaking — der Event-Handler-Typ wechselt die deklarierende Klasse. **Lösung:** Die `RoutedEvent`-Registrierung muss vom `OwnerType` unabhängig für beide Klassen erfolgen. Da `RoutedEvent` global eindeutig ist (Name + OwnerType), werden für `Viewport3DX.FormMouseMove` zwei RoutedEvent-Instanzen benötigt: eine in `WinformHostExtend` (existiert), eine in `HwndSwapChainHost` (neu). `Viewport3DX` muss **beide** Events abonnieren. **Konkrete Umsetzung:**
- `Viewport3DX.cs:1154-1157` wird so erweitert, dass je nach gewähltem Pfad nur das eine Event abonniert wird:
  ```csharp
  if (EnableSwapChainRendering) {
      if (UseWpfSwapChain) {
          HwndSwapChainHost.FormMouseMove += Viewport3DX_FormMouseMove;
          HwndSwapChainHost.FormMouseWheel += Viewport3DX_FormMouseWheel;
      } else {
          WinformHostExtend.FormMouseMove += Viewport3DX_FormMouseMove;
          WinformHostExtend.FormMouseWheel += Viewport3DX_FormMouseWheel;
      }
  }
  ```
- Die Handler `Viewport3DX_FormMouseMove` und `Viewport3DX_FormMouseWheel` (Controls/Viewport3DX.cs:893, 953) bekommen eine zweite Überladung mit dem `HwndSwapChainHost`-Args-Typ. Oder eleganter: Beide EventArgs-Typen werden auf eine gemeinsame Schnittstelle oder einen gemeinsamen Basistyp umgestellt — **einfachste Variante:** zwei private Methoden, eine pro Args-Typ, mit identischem Body.
- `Viewport3DX.cs:1177-1179` (Unsubscribe) wird symmetrisch erweitert.

### 3. Switch-Property auf `Viewport3DX` ergänzen
- Neue `DependencyProperty` in `Controls/Viewport3DX.Properties.cs` (oder direkt in `Viewport3DX.cs`, je nach Konvention der Datei):
  ```csharp
  public static readonly DependencyProperty UseWpfSwapChainProperty = DependencyProperty.Register(
      "UseWpfSwapChain", typeof(bool), typeof(Viewport3DX), new PropertyMetadata(false));
  public bool UseWpfSwapChain {
      get => (bool) GetValue(UseWpfSwapChainProperty);
      set => SetValue(UseWpfSwapChainProperty, value);
  }
  ```
- Default ist `false` (WinForms-Pfad bleibt aktiv für `EnableSwapChainRendering="True"`).
- PropertyTrigger / Re-Erstellung des `hostPresenter.Content` analog zu `EnableSwapChainRenderingPropertyChangedCallback`. Siehe `Controls/Viewport3DX.cs:627-631` für die aktuelle Logik.

### 4. Auswahllogik in `Viewport3DX` aktualisieren
Pfad: `Source/SilkToolkit/Controls/Viewport3DX.cs:627-631`. Aktuell:
```csharp
if (EnableSwapChainRendering)
    hostPresenter.Content = new DPFSurfaceSwapChain(EnableDeferredRendering, BelongsToParentWindow) {DpiScale = DpiScale};
else
    hostPresenter.Content = new DPFCanvas(EnableDeferredRendering, BelongsToParentWindow) {DpiScale = DpiScale};
```
Neu:
```csharp
if (EnableSwapChainRendering) {
    if (UseWpfSwapChain)
        hostPresenter.Content = new DPFCanvasSwapChain(EnableDeferredRendering, BelongsToParentWindow) {DpiScale = DpiScale};
    else
        hostPresenter.Content = new DPFSurfaceSwapChain(EnableDeferredRendering, BelongsToParentWindow) {DpiScale = DpiScale};
} else {
    hostPresenter.Content = new DPFCanvas(EnableDeferredRendering, BelongsToParentWindow) {DpiScale = DpiScale};
}
```

`ScreenDuplicationViewport3DX.cs:221` (`new DPFSurfaceSwapChain(surface => new ScreenCloneRenderHost(surface))`) bleibt unverändert — der Screen-Duplication-Pfad benötigt zwingend eine HWND-gebundene Swap-Chain; der Wechsel auf die neue WPF-Klasse erfolgt hier in einem späteren Schritt, nicht in diesem Plan. Inline-Hinweis: Der Screen-Duplication-Pfad benötigt zwingend `EnableSwapChainRendering` und ist auf den WinForms-Pfad angewiesen, bis `DPFCanvasSwapChain` Screen-Duplication unterstützt.

### 5. WinForms-Pfad bleibt unverändert
Keine Edits an `Controls/RenderControl.cs`, `Controls/WinformHostExtend.cs`, `Controls/DPFSurfaceSwapChain.cs`, `Controls/MouseHandlers/VirtualTouchDevice.cs`, `Source/SilkToolkit/SilkToolkit.csproj`. Das `<UseWindowsForms>true</UseWindowsForms>`-Flag in `SilkToolkit.csproj` bleibt; `using System.Windows.Forms;`-Direktiven in den vier Dateien bleiben; die `DPFSurfaceSwapChain`-Klasse bleibt die Default-Implementierung für `EnableSwapChainRendering="True"`.

### 6. Plan in Backlog ablegen
Ziel: dieser Plan wird im OKF-Wissen-Bundle als Backlog-Konzept persistiert, sodass zukünftige Sessions den Plan finden und referenzieren können (siehe `AGENTS.md` Working Rules und `knowledge/index.md:23-25`).

**Neue Datei:** `knowledge/backlog/wpf-swapchain-canvas-plan.md`.

**YAML-Frontmatter** (genau wie `knowledge/backlog/dx12-migration-plan.md:1-7`):
```yaml
---
type: BacklogPlan
title: WPF Swap-Chain Canvas Plan
description: Second pure-WPF IRenderCanvas implementation (DPFCanvasSwapChain + HwndSwapChainHost) as a switchable alternative to the WinForms-based DPFSurfaceSwapChain; WinForms remains active and is removed in a follow-up step.
tags: [backlog, wpf, swapchain, helix, winforms-removal, hwndhost]
timestamp: 2026-07-09T19:00:00+02:00
---
```

**Body:** der gesamte Inhalt dieser Plan-Datei ab `# WPF-SwapChain-Canvas — Plan` (alles unterhalb der Frontmatter in `local://wpf-swapchain-canvas-plan.md`) wird verbatim als Body der Backlog-Datei übernommen. Der bestehende DX12-Plan (`knowledge/backlog/dx12-migration-plan.md`) ist die Format-Vorlage.

**Index-Update:** `knowledge/index.md:23-25` (Sektion `## Backlog`) um einen Bullet ergänzen, direkt unter dem bestehenden DX12-Plan-Bullet:
```markdown
* [WPF Swap-Chain Canvas Plan](/backlog/wpf-swapchain-canvas-plan.md) - Rein WPF basierte IRenderCanvas-Implementierung als schaltbare Alternative zur WinForms-basierten Variante.
```

**Log-Update:** `knowledge/log.md` um einen neuen Eintrag ergänzen (Format der existierenden Einträge vorher frisch lesen, da das Datei-Layout variieren kann). Wenn das OKF-Log-Format etabliert ist, genügt:
```markdown
* 2026-07-09: Added `knowledge/backlog/wpf-swapchain-canvas-plan.md` — second pure-WPF IRenderCanvas implementation (DPFCanvasSwapChain) as a switchable alternative to the WinForms-based DPFSurfaceSwapChain.
```
Falls die `log.md` ein anderes Format verlangt (z. B. strukturierte Sections), an die existierende Konvention anpassen.

Begründung: `AGENTS.md` schreibt vor, dass jede nicht-reservierte `knowledge/**/*.md` mit YAML-Frontmatter (`type` nicht leer) starten muss. `index.md` und `log.md` sind die einzigen reservierten Dateien. Backlog-Items sind `type: BacklogPlan`-Konzepte (siehe `dx12-migration-plan.md:2` als Vorlage).

## Critical files & anchors
1. `Source/SilkToolkit/Controls/HwndSwapChainHost.cs` — neue Datei. ~250 Zeilen, `HwndHost`-Subklasse mit RoutedEvents, DPI-Tracking, Touch/Mouse-Hooking. Rein WPF.
2. `Source/SilkToolkit/Controls/DPFCanvasSwapChain.cs` — neue Datei. ~300 Zeilen, `Grid`-basierter `IRenderCanvas` analog `DPFSurfaceSwapChain` aber mit `HwndSwapChainHost` statt `WinformHostExtend`. Rein WPF.
3. `Source/SilkToolkit/Controls/Viewport3DX.cs` — Zeile 627-631 (Auswahllogik), Zeile 1154-1157 (Event-Subscribe), Zeile 1177-1179 (Event-Unsubscribe). Hier muss der Pfad-abhängige Code rein.
4. `Source/SilkToolkit/Controls/Viewport3DX.Properties.cs` — neue DependencyProperty `UseWpfSwapChainProperty` und CLR-Wrapper.
5. `Source/SilkToolkit/Controls/Viewport3DX.cs:893,953` — die zwei Event-Handler bekommen jeweils eine Überladung für `HwndSwapChainHost.FormMouseMoveEventArgs`/`FormMouseWheelEventArgs`.

## Verification
Aus dem Repo-Root:

0. **Backlog-Datei vorhanden:** `Test-Path F:\Repositories\helix-toolkit\knowledge\backlog\wpf-swapchain-canvas-plan.md` muss `True` liefern. `Get-Content F:\Repositories\helix-toolkit\knowledge\backlog\wpf-swapchain-canvas-plan.md -TotalCount 10` muss mit dem YAML-Frontmatter aus Schritt 6 beginnen (Zeile 1: `---`, Zeile 2: `type: BacklogPlan`). `Select-String -Path F:\Repositories\helix-toolkit\knowledge\index.md -Pattern "WPF Swap-Chain Canvas Plan"` muss den neuen Bullet aus Schritt 6 zeigen.

1. `dotnet build Source/SilkToolkit.slnx -c Debug` muss erfolgreich sein. Falls `DPFCanvasSwapChain` oder `HwndSwapChainHost` einen Compile-Fehler werfen, ist die wahrscheinlichste Ursache ein vergessenes `using System.Windows.Interop;` oder ein P/Invoke-Signaturfehler.
2. Grep-Verifikation:
   - `grep -n "using System.Windows.Forms" Source/SilkToolkit/Controls/HwndSwapChainHost.cs Source/SilkToolkit/Controls/DPFCanvasSwapChain.cs` muss null Treffer liefern. (Treffer in `RenderControl.cs`, `WinformHostExtend.cs`, `DPFSurfaceSwapChain.cs`, `VirtualTouchDevice.cs` sind erwartet und korrekt — diese Pfade bleiben aktiv.)
   - `grep -n "UseWindowsForms" Source/SilkToolkit/SilkToolkit.csproj` muss exakt einen Treffer liefern (das Flag bleibt).
3. `dotnet build Source/SilkToolkit.slnx -c Release` muss ebenfalls erfolgreich sein.
4. **Smoke-Test Pfad 1 (bestehender WinForms-Pfad, Default):** `Source/Examples/SilkToolkit/SwapChainRenderingDemo` starten mit `EnableSwapChainRendering="True" UseWpfSwapChain="False"` (Default). Verifizieren, dass das Viewport rendert und Maus/Touch funktioniert. Beweist, dass die bestehende Route (`DPFSurfaceSwapChain` → `WinformHostExtend` → `RenderControl`) weiterhin arbeitet.
5. **Smoke-Test Pfad 2 (neuer reiner WPF-Pfad):** dasselbe Demo starten mit `EnableSwapChainRendering="True" UseWpfSwapChain="True"`. Verifizieren, dass das Viewport rendert, Maus funktioniert, Touch funktioniert (sofern Touch-Hardware vorhanden). Beweist, dass die neue `DPFCanvasSwapChain` → `HwndSwapChainHost` Route funktioniert.
6. **Smoke-Test Pfad 3 (DPFCanvas, Default ohne Swap-Chain):** `Source/Examples/SilkToolkit/SimpleDemo` starten mit `EnableSwapChainRendering="False"`. Verifizieren, dass es rendert. Beweist, dass der bestehende `DPFCanvas`-Pfad unberührt ist.

## Assumptions & contingencies
- **Klassenname `DPFCanvasSwapChain`.** Falls das Team einen anderen Namen bevorzugt (z. B. `DPFSwapChainCanvas`, `DPFCanvas2`, `WpfSwapChainHost`), an genau dieser Stelle ersetzen — keine weiteren Auswirkungen, da die Klasse nirgendwo referenziert wird außer in `Viewport3DX.cs:627-631`.
- **Property-Name `UseWpfSwapChain`.** Alternative: `EnablePureWpfSwapChain`, `PreferWpfSwapChain`, oder ein Enum `RenderImplementation { D3DImage, WpfSwapChain, WinFormsSwapChain }` (würde `EnableSwapChainRendering` deprecated machen — breaking change für XAML). Bool-Variante ist die minimal-invasive Wahl.
- **HwndHost mit `STATIC`-Klasse.** Annahme: `STATIC` als HWND-Klasse ist ausreichend, weil D3D-Swap-Chain nur ein HWND mit gültigem DC braucht. Fallback: bei Renderproblemen eine eigene Fensterklasse via `RegisterClassEx` registrieren (analog zur `W32`-Klasse in `VirtualTouchDevice`). Code dafür: 30 zusätzliche Zeilen in `HwndSwapChainHost.W32`.
- **Touch-Code wird in `HwndSwapChainHost` dupliziert.** `VirtualTouchDevice` wird **nicht** refaktoriert; sein Code wird 1:1 mit `IntPtr`-Parametern in `HwndSwapChainHost.WndHook` kopiert. Die `TOUCHINPUT`-Struct und P/Invokes sind bereits assembly-neutral (`Controls/MouseHandlers/VirtualTouchDevice.cs:101-156`).
- **`FormMouseMoveEventArgs`/`FormMouseWheelEventArgs` werden in `HwndSwapChainHost` dupliziert.** Byteweise identisch zu `WinformHostExtend.cs:149-228`. Falls das Team Single-Source-of-Truth bevorzugt: extrahieren in eine separate `FormMouseEventArgs.cs`-Datei (rein WPF) und beide Klassen davon ableiten lassen — das wäre ein Refactor, der **nicht** in diesem Schritt nötig ist.
- **DPI-Erkennung über `HwndSource.CompositionTarget.TransformFromDevice`.** Annahme: `Per-Monitor v2` DPI-Awareness ist in `app.manifest` aktiviert (Standard für WPF-Apps seit .NET 4.6.2). Falls die Demos Hybrid-DPI haben, kann es zu Rundungsfehlern kommen — durch `Math.Round(scale, 2)` im `DpiScale`-Setter abfedern.
- **ScreenDuplication-Pfad nicht migriert.** `ScreenDuplicationViewport3DX.cs:221` benutzt weiter `DPFSurfaceSwapChain` (WinForms). Migration auf den neuen Pfad ist eine separate Aufgabe und wird in einem Folge-Plan behandelt, sobald der neue Pfad stabil läuft.
- **GetPressedMouseButtons bleibt wie im alten Plan** — wird in diesem Plan **nicht** angefasst. Die `MouseButtons`-Alias-Entfernung in `Controls/Viewport3DX.cs:32` und die `int`-Rückgabe-Änderung von `GetPressedMouseButtons()` (Controls/Viewport3DX.cs:840-848) ist ein separates Vorhaben und gehört nicht hierhin (kein WinForms-Entfernen in diesem Schritt).
