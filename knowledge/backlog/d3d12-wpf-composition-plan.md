---
type: BacklogPlan
title: D3D12-Ausgabe wieder in WPF komponieren
status: superseded
generated: false
sources:
  - ../../Source/SilkToolkit/Controls/D3D12PresentationSurface.cs
  - ../../Source/SilkToolkit/Controls/Viewport3DX.cs
  - ../../Source/SilkToolkit/Controls/Viewport3DX.Properties.cs
---

# Übergabeplan: WPF-Präsentationspfad für den vorhandenen D3D12-Renderer

## Aktueller Stand am 20.10.2026

Der Upstream-Stand `7b72e0cf5` ist lokal integriert. Maßgeblich sind nun der
[tatsächliche Implementierungsplan](d3d12-wpf-composition.md) und der
[Phase-5-Prüfbericht](../testing/wpf-composition-phase5.md). Dieser ältere Übergabeplan bleibt als
historische Anforderung erhalten. Insbesondere gilt jetzt der ursprüngliche Standard
`EnableSwapChainRendering=false` statt des unten vorgeschlagenen vorläufigen Standards `true`.

## Auftrag und Ausgang

Eigenständig im SilkToolkit umsetzen. Ausgang der Planung ist Commit
`72557068603fbe3017dc045782e24631970a4fb1`. Vor Beginn tatsächlichen Commit, Index, Arbeitsbaum und
Submodule prüfen und den verwendeten Ausgang festhalten; fremde Änderungen erhalten.

Ziel ist ein wählbarer WPF-Kompositionspfad, in dem gewöhnliche WPF-Controls ohne Popupfenster über der
3D-Ausgabe liegen. Der vorhandene native D3D12-Swapchain-Pfad bleibt funktionsfähig. Beide verwenden denselben
Renderer, Szenengraphen, Materialien, Shader und GPU-Kollisionspfad.

`6215e5c0b` entfernte am 26.08.2026 den früheren Präsentationspfad. Dessen Elternstand enthält `DPFCanvas`
und die Wahl durch `EnableSwapChainRendering`. Diesen Quellstand für WPF-Verträge untersuchen und geeignete
Implementierungen wiederverwenden. Der alte D3D11-Renderer und entfernte Anwendungspfade werden nicht restauriert.
Der ältere Plan `wpf-swapchain-canvas-plan.md` beschreibt einen anderen, HWND-basierten Umbau und ist hierfür
keine Umsetzungsvorlage.

## Verbindliche Grenzen

- Keine neue Renderingbibliothek und keine parallele Implementierung von Szenen-, Material- oder Shaderlogik.
- Keine Präprozessorschalter, SharpDX-Abhängigkeiten, Stubs oder ausgeschlossenen Renderingfunktionen.
- Öffentliche Namespaces, Assemblynamen und bestehende API-Verträge erhalten; notwendige APIs additiv ergänzen.
- Keine Änderungen an MeshMeister-, Original-Helix- oder klinischen Anwendungsabläufen in diesem Paket.
- Vorhandenen `ViewportWidgetOverlay` erhalten; Verbraucher des HWND-Pfads benötigen ihn weiterhin.
- Neue native Bindings zuerst im vorhandenen Silk-Bestand suchen; weitere Pakete nur bei belegtem Bedarf.
- Ressourcen-, Eingabe- und Frame-Verträge erhalten; keine unbegrenzten Wartezeiten im normalen UI-Takt.

## 1. Technischer Nachweis vor dem größeren Umbau

`D3DImage.SetBackBuffer` erwartet eine `IDirect3DSurface9`. Eine D3D12-Textur ist kein direkt gültiger
Backbuffer. Die Übergabe muss deshalb zuerst mit einer kleinen ausführbaren Probe nachgewiesen werden:

`D3D12-Farbtextur → GPU-Interop über D3D11/D3D9Ex → D3DImage → WPF`

Gemeinsame Ressourcen, Adapteridentität, Handle-Typen und unterstützte Texturformate prüfen. D3D11On12 ist
eine zu prüfende Möglichkeit für den D3D12-/D3D11-Übergang, kein vorab zugesicherter vollständiger D3D9-Pfad.
Falls nötig, eine zusätzliche GPU-Kopie zwischen kompatiblen Ressourcen verwenden. Kein CPU-Readback pro
Bild als regulärer Präsentationsweg; keine ungeprüfte Zusage vollständiger Kopierfreiheit.

Nachweis: wechselndes Farbtestbild, korrektes Alpha, wiederholte Frames, Resize und fehlerfreie Freigabe
auf der Zielhardware. D3D12-/D3D11-/D3D9-Zugriffe und WPF-Komposition dürfen nicht konkurrierend schreiben.
Einen konkreten Synchronisationsvertrag einschließlich WPF-Lock/Unlock und DirtyRect festlegen.

**Checkpoint:** unterstützte Adapter/Formate, Ressourcenbesitzer, Synchronisation und gemessene Kopien
dokumentieren. Erst mit funktionierender GPU-Übergabe die vollständige Anwendung integrieren. Scheitert der
Nachweis, konkrete technische Grenze melden; keinen CPU-Präsentationsweg stillschweigend einsetzen.

## 2. Gemeinsamen Renderdurchlauf von der Präsentation lösen

`D3D12PresentationSurface` besitzt derzeit Renderdurchlauf und Swapchain. Die konkret benötigte gemeinsame
Grenze anhand von Initialisierung, `BeginFrame`, `EndFrame`, `RenderViewportOnce` und Capture bestimmen.
Nur diese Teile herauslösen; keine allgemeine Backend-Infrastruktur auf Vorrat schaffen.

- Beide Pfade nutzen dieselbe Szenenaktualisierung, Passauswahl, Tiefenpuffer- und Effektausführung.
- HWND-Ausgabe behält ihre Swapchain; WPF-Ausgabe erhält eine größenabhängige Rendertextur.
- Beide nutzen bestehende Fence-/Frame- und Ressourcenbesitzer, soweit deren Vertrag passt.
- RenderHost, RenderContext, FlattenedScene, Capture und Abschlussereignisse bleiben echt und konsistent.
- Ein übersprungener Tick und ein lediglich eingereichter Frame sind kein fertig veröffentlichtes Bild.

## 3. WPF-Canvas und kompatible Auswahl herstellen

Geeignete Teile des früheren `DPFCanvas` wiederverwenden; neue native Übergabe getrennt von WPF-Layout halten.
`EnableSwapChainRendering` als DependencyProperty ohne Präprozessorzweige wiederherstellen:

| Wert | Ausgabe |
|---|---|
| `true` | bestehender D3D12-HWND-/Swapchain-Pfad |
| `false` | D3D12-Ausgabe über D3DImage im WPF-Visualbaum |

Der Standard bleibt zunächst `true`, damit bestehende Silk-Verbraucher nicht unbemerkt wechseln. Die
Abweichung vom historischen Standard dokumentieren. Wechsel nach Initialisierung entweder vollständig
lebensdauersicher implementieren oder einen belegten historischen Einschränkungsvertrag übernehmen;
keinen scheinbar funktionierenden Schalter anbieten.

WPF-Maus, Wheel, Capture, Fokus und Touch an die vorhandenen Kamera-/Picking-Verträge anschließen.
Keine doppelte Verarbeitung durch native und WPF-Eingaberouten. Größe, DPI, Hidden/Collapsed, Unloaded/Loaded,
Minimieren, Frontbuffer-Verlust, Monitor-/Adapterwechsel und Geräteverlust behandeln. Vor COM-/Handle-Freigabe
WPF vom Backbuffer lösen und ausstehende GPU-/Interop-Nutzung sicher abschließen.

Transparenz mit passendem Ausgabeformat und korrekter Alpha-Konvention prüfen. Ein transparenter Clear-Wert
allein belegt keine korrekte Darstellung von halbtransparenten Kanten oder Postprocessing.

## 4. Prüfung und Leistungsvergleich

Vorhandene Tests wiederverwenden; neue Tests nur für die neue Präsentations- und Integrationsgrenze:

- Pfadauswahl und ursprüngliche XAML-Property; Swapchain-Regressionsprüfungen.
- Vollständige Bildausgabe einschließlich Transparenz, Schatten, OIT, SSAO, FXAA, Screen-Space-Elementen,
  Postprocessing und GPU-Kollisionssegmenten; Capture und tatsächliche Frame-Abschlussereignisse.
- WPF-Control über Viewport, normales ZIndex/Clipping, Eingabe, Commands, VisualBrush und Blur.
- Wiederholtes Laden/Entladen, Resize, DPI-/Monitorwechsel, Frontbuffer-Verlust und Ressourcenfreigabe.
- Identische Szene/Kamera/Auflösung für beide Pfade: CPU-/GPU-Framezeiten, Eingabelatenz, Speicher und Kopien
  messen; Hardware/Adapter, Auflösung und Messverfahren angeben. Mehrkosten offen ausweisen.

Headless und seriell prüfen. Vorhandene WARP-Tests nutzen, aber deren Bestehen nicht als Beweis für
D3D9Ex-Sharing auf realer Hardware ausgeben. Nicht unterstützte Interop-Konfigurationen ausdrücklich ausweisen.
Sichtbare Tests separat kennzeichnen und nur nach ausdrücklicher Freigabe ausführen; ein allgemeiner
Implementierungsauftrag allein autorisiert keine sichtbaren Fensterproben.

## Arbeitsteilung nach dem ersten Checkpoint

Bei beauftragter Parallelbearbeitung höchstens drei schreibende Pakete mit getrennten Worktrees und einem
gemeinsamen Ausgangscommit; jede Datei hat genau einen Besitzer:

| Paket | Besitz und Ergebnis |
|---|---|
| A | Native GPU-Interop, Ressourcenübergabe und zugehörige Tests |
| B | Gemeinsamer Renderdurchlauf und Präsentationsintegration |
| C | DPFCanvas, Property-Auswahl, WPF-Eingabe und Kompositionstests |

Gemeinsame Signaturen vorab festlegen; B/C beginnen erst nach dem Interop-Nachweis. Integration,
Projektdateien, Dokumentation und serielle Builds/GPU-Tests bei einem Integrationsbesitzer halten.

## Übergabe an MeshMeister

Abzugeben sind:

1. Lokaler Toolkit-Commit mit vollständiger Implementierung; nichts pushen.
2. Exakte Pfadauswahl und Standardwert, unterstützte Hardware-/Adapterkonfigurationen und Grenzen.
3. Prüfergebnisse für beide Pfade, sichtbare/übersprungene Prüfungen und Leistungsvergleich.
4. Lifecycle-/Synchronisationsbeschreibung sowie konkrete Hinweise für Helix, Eingabe und Capture.
5. Aktualisierte Toolkit-Konzepte, Index und Wissenslog; Graphify nach Quelländerungen regenerieren;
   Dokumentationsprüfung und `git diff --check` durchführen.

**Freigabekriterium:** Ein normales WPF-Control liegt ohne Popup über der echten D3D12-Ausgabe, ist bedienbar,
und alle verwendeten Renderingfunktionen bleiben angeschlossen. Der bestehende HWND-Pfad besteht seine
Regressionen. Erst danach beginnt die Anpassung von Original Helix im Hauptrepository.

## Technische Referenzen

- [WPF und Direct3D9](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/wpf-and-direct3d9-interoperation)
- [Gemeinsame D3D9-/D3D11-Ressourcen](https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11device-opensharedresource)
- [D3D11On12](https://learn.microsoft.com/en-us/windows/win32/direct3d12/direct3d-11-on-12)
