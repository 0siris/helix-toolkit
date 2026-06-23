# SharpDX-Ablösung: Übergabe für Folgesessions

Stand: 23. Juni 2026  
Branch: `feature/wpf-sharpdx`  
Letzter Abschluss-Commit: `66f869ec0 chore(wpf)!: finish Silk.NET migration`

## Ziel

SharpDX vollständig aus dem Repository entfernen:

- keine SharpDX-Pakete oder Assembly-Referenzen,
- keine echten SharpDX-Typen oder Backend-Aufrufe,
- keine aktiven Projekte, Tests oder Beispiele, die SharpDX benötigen.

Öffentliche Namen wie `HelixToolkit.Wpf.SharpDX` dürfen vorerst als
Kompatibilitätsnamen bleiben. Sie sind keine technische Abhängigkeit.

## Erledigter Stand

Der definierte produktive WPF-Scope ist auf Silk.NET DirectX migriert:

- `HelixToolkit.SharpDX.Core`
- `HelixToolkit.Wpf.SharpDX`
- `HelixToolkit.SharpDX.Core.Assimp`
- `HelixToolkit.Wpf.SharpDX.Assimp`
- zugehörige Core- und WPF-Tests

Umgesetzt wurden unter anderem:

- D3D11-Gerät, Context, Ressourcen, Views, States und Shader als eigene
  Wrapper um Silk.NET-COM-Handles,
- Texture-, Buffer-, SRV/UAV/RTV/DSV- und RenderBuffer-Pfade,
- D3DImage-Interop über Silk.NET.Direct3D9,
- SwapChain-/HwndHost-Rendering,
- Direct2D-, DirectWrite- und WIC-Kompatibilität,
- Shader-Reflection über `D3DReflect`,
- DDS/WIC-Texture-Loading und Screen-Capture,
- Assimp Import/Export,
- Migration öffentlicher Math-Signaturen auf Silk.NET.Maths.

Validierter Abschlussstand:

- vier Zielbibliotheken bauen mit `0` Fehlern,
- Core-Tests: `15/15`,
- WPF-Tests: `44/44`,
- fünfsekündige Startup-Smokes erfolgreich:
  `SimpleDemo`, `FileLoadDemo`, `BillboardDemo`,
  `D2DScreenMenuExample`, `SwapChainRenderingDemo`.

Details und Phasenhistorie:
[`WPF-SharpDX-to-SilkNET-Migration.md`](WPF-SharpDX-to-SilkNET-Migration.md)

## Verwendete Strategie

1. Einen kleinen Compile-Pfad nach dem anderen migrieren.
2. Bestehende Projektstruktur und öffentliche Legacy-Namen beibehalten.
3. SharpDX-Typen durch Silk.NET.Maths oder schmale lokale
   Kompatibilitätstypen ersetzen.
4. Native COM-Ownership ausschließlich über die bestehenden Wrapper führen.
5. Nach jedem Teilstück gezielten Build, Tests und einen Beispiel-Smoke
   ausführen.
6. Erst nach erfolgreicher Migration die jeweilige SharpDX-Paketreferenz
   entfernen.

Keine neue parallele Abstraktionsschicht einführen. Die vorhandenen
Silk.NET-Wrapper in `Source/HelixToolkit.SharpDX.Shared/Native` erweitern.

## Noch offen

### 1. WPF-Beispiele vollständig migrieren

Viele Projekte unter `Source/Examples/WPF.SharpDX` referenzieren weiterhin
`SharpDX.Direct3D11`, `SharpDX.Direct2D1`, `SharpDX.D3DCompiler` oder
`SharpDX.Mathematics`.

Vorgehen pro Beispiel:

1. Math-Typen auf `Silk.NET.Maths` oder bestehende Helix-Typen umstellen.
2. Direkte D3D-Aufrufe auf vorhandene Engine-Wrapper umstellen.
3. SharpDX-Paket-/Assembly-Referenzen entfernen.
4. Projekt bauen und fünf Sekunden aus seinem Output-Verzeichnis starten.

Mit kleinen Beispielen beginnen. Diese Sonderfälle zuletzt bearbeiten:

- `CustomShaderDemo`
- `DeferredShadingDemo`
- `GenericMaterialDemo`
- `ScreenSpaceDemo`
- `DynamicTextureDemo`
- `ScreenDuplicationDemo`

### 2. `HelixToolkit.SharpDX.Core.Wpf` entfernen

Das Projekt war ein Übergangspaket für .NET Core 3.0 und dupliziert heute
den Zweck von `HelixToolkit.Wpf.SharpDX`.

- Projekt, Nuspec, Solution-, README-, CI- und Packaging-Einträge entfernen.
- `CoreWpfTest` und `DynamicPointsAndLines` auf
  `HelixToolkit.Wpf.SharpDX` umstellen.
- Kein Alias- oder Kompatibilitätspaket weiterpflegen.

### 3. UWP und WinUI aus dem Produktiv-Scope entfernen

Aktuell verbleiben echte SharpDX-Abhängigkeiten in:

- `HelixToolkit.UWP`
- `HelixToolkit.UWP.Assimp`
- `HelixToolkit.WinUI`
- UWP-/WinUI-Beispielen und Tests
- `HelixToolkit.UWP.Shared`

Diese Projekte bleiben vorerst als nicht unterstützter Legacy-Code im
Repository, werden aber aus `HelixToolkit.SharpDX.sln`, aktivem CI,
Packaging und Release-Dokumentation entfernt. Das verbindliche Endgate ist
die produktive WPF-Hauptmappe, nicht der vollständige Legacy-Bestand.

### 4. Shared-Altzweige bereinigen

Im produktiven WPF-Compile-Graph löschen:

- `NETFX_CORE`-Zweige mit `global::SharpDX.IO`,
- `DEFERRED`-Code in `DeferredRenderer.cs` und `RenderUtil.cs`,
- `DEBUGMEMORY`-ObjectTracker-Aufrufe,
- auskommentierte SharpDX-Typreferenzen.

`namespace SharpDX.Toolkit` ist derzeit eine lokal mitgeführte
DDS/WIC-Kompatibilitätsimplementierung, keine externe Assembly. Für eine
vollständige begriffliche Ablösung diesen Namespace am Ende intern umbenennen
und alle Verwendungen aktualisieren.

### 5. Metadaten und Infrastruktur

- verbliebene README-/Dokumentationsaussagen über SharpDX als Backend ändern,
- Package-Tags aller aktiven Pakete prüfen,
- Lösungen, AppVeyor, Packaging und Restore-Dateien bereinigen,
- historische Migrationsdokumente als historisch kennzeichnen oder entfernen,
- erst ganz am Ende öffentliche Projekt-/Namespace-Umbenennung separat
  entscheiden.

## Projekte starten und debuggen

### Vorhandene Projektmappen

Im Verzeichnis `Source` liegen sieben klassische `.sln`-Dateien. Aktuell
existiert keine `.slnx`-Projektmappe.

| Projektmappe | Zweck | Bedeutung für die Migration |
| --- | --- | --- |
| `HelixToolkit.SharpDX.sln` | Große Entwicklungsmappe für den DirectX-Renderer, WPF.SharpDX, Core, Assimp, Tests, ShaderBuilder und fast alle DirectX-Beispiele. | Hauptmappe für die SharpDX-Ablösung. Sie enthält aber noch nicht migrierte Beispiele, `Core.Wpf` und WinUI und ist daher kein grünes Gesamtgate. |
| `HelixToolkit.SharpDX.Core.sln` | Kleine Core-orientierte Mappe mit Rendering-Core, Core.Assimp und Shared-Projekten. | Bei Entfernung von `Core.Wpf` und Umstellung der Core-Demos ebenfalls bereinigen. |
| `HelixToolkit.WinUI.sln` | Legacy-Mappe für WinUI, ModelViewer und UWP-Shared-Code. | Nicht unterstützt und kein Produktiv-Gate. Bleibt vorerst außerhalb von CI und Packaging im Repository. |
| `HelixToolkit.Wpf.sln` | Klassischer WPF-3D-Renderer ohne DirectX-11-Engine, einschließlich WPF-Beispielen und Tests. | Von der Silk.NET-Migration weitgehend unabhängig; als Regressionstest für den normalen WPF-Bereich verwenden. |
| `HelixToolkit.Core.Wpf.sln` | Kleine Mappe für `HelixToolkit.Core.Wpf`, Shared-Code und `CoreWpfDemo`. | Unabhängiger WPF-Core-Pfad; nur auf Regressionen prüfen. |
| `HelixToolkit.Kinect.sln` | Klassischer WPF-Renderer plus Kinect-`DepthSensorDemo`. | Randbereich; kein Teil der DirectX-/SharpDX-Migration. |
| `HelixToolkit.AppVeyor.sln` | Historische CI-Sammelmappe mit 69 echten Projekten aus WPF, DirectX, Core, Beispielen und WinUI. | Breites Endgate, momentan ungeeignet: enthält viele noch nicht migrierte oder obsolete Projekte. Nach vollständiger Ablösung bereinigen und zuletzt bauen. |

Eine `HelixToolkit.UWP.sln` ist nicht vorhanden, obwohl ältere Dokumentation
sie noch nennt. UWP liegt nur noch als Projekte/Shared-Code vor und muss für
die vollständige Ablösung entweder entfernt oder in eine bewusst gepflegte
Projektmappe aufgenommen und migriert werden.

Empfohlene Verwendung:

1. Während der Migration gezielt einzelne `.csproj` bauen.
2. Für zusammenhängende WPF-DirectX-Arbeit `HelixToolkit.SharpDX.sln`
   in der IDE öffnen, aber nur betroffene Projekte bauen.
3. Nach Bereinigung der Beispiele die gesamte `HelixToolkit.SharpDX.sln`
   als Gate verwenden.
4. `HelixToolkit.AppVeyor.sln` erst als letztes Repository-Gesamtgate
   bereinigen und ausführen.

Projektmappe in Visual Studio öffnen:

```powershell
devenv Source\HelixToolkit.SharpDX.sln
```

In Rider über **Open** die gewünschte `.sln` auswählen.

### Voraussetzungen

- Visual Studio 2026 oder Rider mit .NET-Desktop-/WPF-Unterstützung,
- .NET SDK 10.0 gemäß `global.json`,
- Windows 10 SDK ab `10.0.18362.0`,
- für den D3D11-Debug-Layer optional das Windows-Feature
  **Graphics Tools**.

Alle Befehle aus dem Repository-Root
`F:\Repositories\helix-toolkit` ausführen.

### Bibliotheken bauen

Bibliotheksprojekte sind nicht direkt startbar. Sie werden gebaut und über
Tests oder ein WPF-Beispiel ausgeführt:

```powershell
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore -m:1
dotnet build Source\HelixToolkit.Wpf.SharpDX.Assimp\HelixToolkit.Wpf.SharpDX.Assimp.csproj --no-restore -m:1
```

Bei parallelen Builds können gemeinsame `obj`-Dateien gesperrt werden.
Deshalb die WPF-Projekte seriell mit `-m:1` bauen.

### WPF-Beispiel über CLI starten

Beispiel bauen:

```powershell
dotnet build Source\Examples\WPF.SharpDX\SimpleDemo\SimpleDemo.csproj -m:1
```

Danach aus dem Output-Verzeichnis starten:

```powershell
Push-Location Source\Examples\WPF.SharpDX\SimpleDemo\bin\Debug\net10.0-windows
.\SimpleDemo.exe
Pop-Location
```

Für andere Beispiele Projekt- und EXE-Namen entsprechend ersetzen. Das
Output-Verzeichnis als Arbeitsverzeichnis ist wichtig, weil Fonts, Modelle,
Texturen und Shader teilweise über relative Pfade geladen werden.

Ein kurzer automatischer Startup-Smoke:

```powershell
$dir = Resolve-Path Source\Examples\WPF.SharpDX\SimpleDemo\bin\Debug\net10.0-windows
$process = Start-Process "$dir\SimpleDemo.exe" -WorkingDirectory $dir -PassThru
Start-Sleep -Seconds 5
if ($process.HasExited) { throw "SimpleDemo exited with $($process.ExitCode)" }
Stop-Process -Id $process.Id
```

### Visual Studio debuggen

1. Das gewünschte WPF-Beispielprojekt öffnen oder in der Solution als
   Startprojekt festlegen.
2. Konfiguration `Debug` und `Any CPU` verwenden.
3. Mit `F5` starten oder mit `Ctrl+F5` ohne Debugger starten.
4. Falls relative Dateien nicht gefunden werden, in den
   Debug-Eigenschaften als Arbeitsverzeichnis
   `$(TargetDir)` beziehungsweise das konkrete
   `bin\Debug\net10.0-windows`-Verzeichnis setzen.
5. Unter **Exception Settings** mindestens
   **Common Language Runtime Exceptions: Thrown** aktivieren.
6. Für COM-/Interop-Fehler zusätzlich **Enable native code debugging**
   aktivieren.

Geeignete Breakpoints:

- `SilkD3D11DeviceFactory.CreateDefault`
- `EffectsManager.Initialize`
- `RenderHostBase.StartD3D`
- `RenderHostBase.OnEndingD3D`
- `Viewport3DX.HandleRenderException`
- `DPFCanvas.HandleExceptionOccured`
- `DPFSurfaceSwapChain.HandleExceptionOccured`

`SimpleDemo` nutzt den D3DImage-Pfad
(`EnableSwapChainRendering="False"`).  
`SwapChainRenderingDemo` nutzt den HwndHost-/SwapChain-Pfad
(`EnableSwapChainRendering="True"`).

### Rider debuggen

1. Das Beispielprojekt als .NET Project-Konfiguration auswählen.
2. Target Framework `net10.0-windows` und Konfiguration `Debug` setzen.
3. Als Working Directory das jeweilige
   `bin\Debug\net10.0-windows`-Verzeichnis setzen.
4. **Break on thrown** für CLR-Ausnahmen aktivieren.
5. Start mit **Debug**; für native COM-Schritte bei Bedarf Visual Studio
   verwenden, da dort Mixed-Mode-Debugging direkter unterstützt wird.

### Tests starten und debuggen

Komplette Testläufe:

```powershell
dotnet test Source\HelixToolkit.SharpDX.Core.Tests\HelixToolkit.SharpDX.Core.Tests.csproj --no-restore -m:1
dotnet test Source\HelixToolkit.Wpf.SharpDX.Tests\HelixToolkit.Wpf.SharpDX.Tests.csproj --no-restore -m:1
```

Einzelnen NUnit-Test filtern:

```powershell
dotnet test Source\HelixToolkit.Wpf.SharpDX.Tests\HelixToolkit.Wpf.SharpDX.Tests.csproj --filter "FullyQualifiedName~ObjReaderTests" -m:1
```

In Visual Studio über den Test Explorer oder in Rider über die Testmarkierung
neben dem Test mit **Debug Test** starten.

### Renderfehler untersuchen

`Viewport3DX` fängt Renderfehler ab und veröffentlicht sie über:

- `RenderException`,
- `RenderExceptionOccurred`,
- `IRenderHost.ExceptionOccurred`.

Beim Debuggen auf **Thrown** brechen, da die UI den Fehler sonst in
`MessageText` übernimmt und den RenderHost beendet.

Für Software-Rendering:

```csharp
EffectsManager = new DefaultEffectsManager(
    new EffectsManagerConfiguration { EnableSoftwareRendering = true });
```

Damit lässt sich prüfen, ob ein Fehler GPU-/Treiber-spezifisch ist.

Der D3D11-Debug-Layer ist in
`SilkD3D11DeviceFactory.CreateDefault(..., enableDebugLayer)` vorhanden,
aber im normalen Build deaktiviert. Der alte `DEBUGMEMORY`-Zweig enthält noch
SharpDX-ObjectTracker-Code und darf nicht einfach aktiviert werden. Zuerst
diese Altaufrufe entfernen; danach für einen lokalen Diagnose-Build
`enableDebugLayer: true` übergeben.

### Typische Startprobleme

- `FileNotFoundException` für Fonts, Modelle oder Texturen:
  Arbeitsverzeichnis auf `$(TargetDir)` setzen und
  `CopyToOutputDirectory` im Beispielprojekt prüfen.
- WPF-Markup-Datei in `obj` gesperrt:
  `dotnet build-server shutdown`, IDE-Build stoppen und seriell neu bauen.
- `0xE0434352`:
  verwaltete Ausnahme; stderr oder **Break on thrown** verwenden.
- `DXGI_ERROR_DEVICE_REMOVED` oder `DXGI_ERROR_DEVICE_RESET`:
  Breakpoint in `RenderHostBase` setzen und Device-Lost-Recovery prüfen.
- Schwarzes Fenster:
  zuerst `EffectsManager`, Shader-Loading, RenderTarget-Erzeugung und
  `RenderExceptionOccurred` prüfen; danach WARP-Modus testen.

## Verbindlicher Produktivplan

Baseline: Der vollständige Build von `HelixToolkit.SharpDX.sln` endet am
23. Juni 2026 mit `438` Fehlern. Fertig ist die Migration erst bei einem
grünen Debug- und Release-Build der bereinigten Hauptmappe.

### 1. Hauptmappe und Packaging bereinigen

- Aus `HelixToolkit.SharpDX.sln` entfernen:
  - `HelixToolkit.WinUI`
  - `HelixToolkit.UWP.Shared`
  - WinUI `ModelViewer`
  - `HelixToolkit.SharpDX.Core.Wpf`
- `HelixToolkit.SharpDX.Core.Wpf` samt Nuspec und Packaging entfernen.
- AppVeyor auf den WPF-/Silk.NET-Produktumfang reduzieren:
  keine WinUI-, UWP- oder Core.Wpf-Pakete.
- CI-Builds mit `dotnet` und dem SDK aus `global.json` ausführen.

### 2. Assembly-Ownership korrigieren

Der WPF-Build erzeugt aktuell tausende `CS0436`-Warnungen, weil Shared-Code
sowohl in Core als auch erneut in WPF kompiliert wird.

- Allgemeine Typen gehören `HelixToolkit`.
- Rendering-Core und lokaler Toolkit-Kompatibilitätscode gehören
  `HelixToolkit.SharpDX.Core`.
- WPF-Controls und WPF-spezifische Typen gehören
  `HelixToolkit.Wpf.SharpDX`.
- Fixed-Namespace-Dateien in Core-/WPF-spezifische Projitems trennen oder
  ausschließlich aus der besitzenden Assembly referenzieren.
- `CS0436` nicht unterdrücken, sondern doppelte Typdefinitionen beseitigen.

### 3. Alle DirectX-Demos migrieren

Alle Demos unter `Examples/WPF.SharpDX` sowie die vier Demos unter
`Examples/SharpDX.Core` bleiben erhalten.

Reihenfolge:

1. Reine Paket-/Math-Migration:
   SharpDX-Vektoren, Matrizen, Farben, Quaternionen und Geometrietypen durch
   `Silk.NET.Maths` oder bestehende Helix-Typen ersetzen.
2. Engine-nahe Demos:
   CullMode, States, Shaderbeschreibungen, Texturen und Buffer auf vorhandene
   Engine-Wrapper umstellen.
3. Direkte Rendering-Sonderfälle:
   `CustomShaderDemo`, `DeferredShadingDemo`, `GenericMaterialDemo`,
   `ScreenSpaceDemo`, `DynamicTextureDemo`, `ScreenDuplicationDemo`,
   `Viewport3DXCodeBehindTester` und `WinFormsTest`.

Zusätzliche Vorgaben:

- Alle alten .NET-Framework-Demos auf SDK-Format und `net10.0-windows`
  bringen.
- Bestehende Native-/DeviceContext-Wrapper erweitern; keine zweite
  Demo-spezifische Rendering-Abstraktion einführen.
- `DeferredShadingDemo`, `GenericMaterialDemo` und `ScreenSpaceDemo` nach
  erfolgreicher Migration wieder in `HelixToolkit.SharpDX.sln` aufnehmen.
- `CoreWpfTest` und `DynamicPointsAndLines` verwenden danach
  `HelixToolkit.Wpf.SharpDX`.
- `OffScreenRendering` bleibt Core-/Silk.NET-basiert.
- `WinFormsTest` behält WinForms und ImGui; nur `SharpDX.Windows`, Math- und
  D3D-Typen werden durch BCL-/Silk.NET-/Engine-Typen ersetzt.

### 4. Metadaten und Legacy-Grenzen aktualisieren

- README, Diagramme und Paketlisten auf WPF als unterstützte Plattform
  korrigieren.
- `HelixToolkit.SharpDX.Core.Wpf` als eingestellt und
  `HelixToolkit.Wpf.SharpDX` als Ersatz dokumentieren.
- WinUI/UWP ausdrücklich als nicht unterstützt und außerhalb von
  Hauptmappe, CI und Packaging markieren.
- Öffentliche WPF-Paketnamen, CLR-Namespaces und der XAML-Namespace bleiben
  vorerst aus Kompatibilitätsgründen unverändert.

## Prüfungen

### Suche

```powershell
rg -n 'PackageReference Include="SharpDX|Reference Include="SharpDX' <Projekte-der-Hauptmappe> -g '*.csproj' -g '*.props' -g '*.targets'
rg -n 'using SharpDX|global::SharpDX|SharpDX\.' <Projekte-der-Hauptmappe> -g '*.cs'
```

Treffer in öffentlichen Namen separat von echten Backend-Typen bewerten.
Das Endziel ist innerhalb der Hauptmappe: keine Paket-/Assembly-Treffer und
keine qualifizierten SharpDX-Backend-Typen.

### Builds und Tests

```powershell
dotnet build Source\HelixToolkit.SharpDX.Core\HelixToolkit.SharpDX.Core.csproj --no-restore -m:1
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore -m:1
dotnet build Source\HelixToolkit.SharpDX.Core.Assimp\HelixToolkit.SharpDX.Core.Assimp.csproj --no-restore -m:1
dotnet build Source\HelixToolkit.Wpf.SharpDX.Assimp\HelixToolkit.Wpf.SharpDX.Assimp.csproj --no-restore -m:1
dotnet test Source\HelixToolkit.SharpDX.Core.Tests\HelixToolkit.SharpDX.Core.Tests.csproj --no-restore -m:1
dotnet test Source\HelixToolkit.Wpf.SharpDX.Tests\HelixToolkit.Wpf.SharpDX.Tests.csproj --no-restore -m:1
dotnet build Source\HelixToolkit.SharpDX.sln --no-restore -m:1 -c Debug
dotnet build Source\HelixToolkit.SharpDX.sln --no-restore -m:1 -c Release
```

Beispiele einzeln bauen. Startup-Smokes mit dem jeweiligen
`bin\Debug\net10.0-windows`-Verzeichnis als Arbeitsverzeichnis ausführen.
Alle Demos müssen bauen; GUI-Demos müssen mindestens fünf Sekunden stabil
laufen. Offscreen-Ausgabe muss existieren und nicht leer sein. WinForms/ImGui
muss mindestens einen Frame rendern.

### Manuelle Release-Prüfungen

- D3DImage und SwapChain rendern,
- Resize, Minimize/Restore und DPI-Wechsel,
- FrontBuffer-Verlust und Wiederherstellung,
- Unload/Reload und Window-Close,
- Device-removed/reset,
- längerer Speicher-/Resource-Lifetime-Smoke mit Debug-Layer.

### Hygiene

```powershell
git diff --check
git status --short
```

## Nächster Schritt

Zuerst Hauptmappe, CI und Packaging von WinUI/UWP/Core.Wpf entkoppeln und
`CoreWpfTest` sowie `DynamicPointsAndLines` auf das produktive WPF-Paket
umstellen. Danach die Demos in kleinen, fachlich zusammenhängenden Paketen
migrieren und jeweils Build plus Startup-Smoke dokumentieren.
