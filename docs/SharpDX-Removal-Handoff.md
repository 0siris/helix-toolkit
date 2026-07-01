# SilkToolkit: Übergabe für Folgesessions

Stand: 1. Juli 2026
Branch: `feature/wpf-sharpdx`
Letzter bekannter Commit: `b108e3473 feat(core): introduce CameraController and assembly descriptions`

Aktueller Working Tree enthält noch nicht committete SilkToolkit-Arbeit:

- `Source/SilkToolkit.slnx`
- `Source/SilkCore/SilkCore.csproj`
- `Source/HelixToolkit.Native.ShaderBuilder/HelixToolkit.Native.ShaderBuilder.csproj`
- untracked Build-Artefakt:
  `Source/Examples/WPF.SharpDX/DeferredShadingDemo/FodyWeavers.xsd`

## Ziel

Aktueller Produktivfokus ist die eigenständige Silk.NET-Mappe
`Source/SilkToolkit.slnx`:

- `SilkCore`, `SilkToolkit` und `SilkAssimp` bauen ohne Shared-`projitems`-
  Imports aus den alten HelixToolkit-Projekten,
- alle Beispiele aus `Source/HelixToolkit.SharpDX.sln` sind in
  `Source/SilkToolkit.slnx` eingehängt,
- Beispielprojekte referenzieren nur `Source/SilkCore`,
  `Source/SilkToolkit` oder `Source/SilkAssimp`; NuGet-Abhängigkeiten bleiben
  erlaubt,
- Shader werden über `HelixToolkit.Native.ShaderBuilder` erzeugt und als
  Resources aus `SilkCore` geladen.

Öffentliche Namen wie `HelixToolkit.Wpf.SharpDX` dürfen vorerst als
Kompatibilitätsnamen bleiben. Sie sind keine technische Abhängigkeit.
WinUI und UWP bleiben nur als nicht unterstützter Legacy-Code außerhalb von
Hauptmappe, CI und Packaging im Repository. Eine spätere repo-weite
Namensbereinigung ist optional und nicht Teil des aktuellen Produktivgates.

## Erledigter Stand

Der aktuelle SilkToolkit-Stand ist:

- `SilkCore`, `SilkToolkit` und `SilkAssimp` sind konsolidierte lokale
  Projekte.
- `SilkToolkit` importiert keine Dateien mehr über
  `HelixToolkit.Wpf.SharpDX.Shared.projitems`.
- Core-Duplikate sind in `SilkCore` gebündelt; WPF-spezifische Dateien liegen
  lokal in `SilkToolkit`.
- `SilkAssimp` referenziert `SilkCore`.
- `Source/SilkToolkit.slnx` enthält `SilkCore`, `SilkToolkit`, `SilkAssimp`,
  `HelixToolkit.Native.ShaderBuilder` und alle DirectX-Beispiele.
- `HelixToolkit.Native.ShaderBuilder` kopiert erzeugte `.cso`-Dateien auch nach
  `Source/SilkCore/Resources`.
- `SilkCore` embedded `Resources/*.cso` mit LogicalNames im Format
  `HelixToolkit.SharpDX.Core.Resources.*.cso`.
- Validiert: `SilkCore`, `SilkToolkit` und `SilkToolkit.slnx` bauen mit
  `0` Fehlern; `SilkCore` enthält `103` Shader-Resources.

Historischer Kontext:

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

Zusätzlich erledigt am 23. Juni 2026:

- `HelixToolkit.SharpDX.sln` von `HelixToolkit.WinUI`,
  `HelixToolkit.UWP.Shared`, WinUI `ModelViewer` und
  `HelixToolkit.SharpDX.Core.Wpf` entkoppelt.
- Verwaiste Shared-Project-Importe dieser entfernten Projekte aus der
  Hauptmappe entfernt.
- `CoreWpfTest` und `DynamicPointsAndLines` hängen nicht mehr am alten
  `HelixToolkit.SharpDX.Core.Wpf`, sondern direkt an
  `HelixToolkit.Wpf.SharpDX`.
- AppVeyor restauriert und baut die bereinigte `HelixToolkit.SharpDX.sln`
  mit `dotnet`; WinUI- und `HelixToolkit.SharpDX.Core.Wpf`-Pakete werden
  nicht mehr gepackt.
- `Source/HelixToolkit.SharpDX.Core.Wpf.nuspec` wurde entfernt.
- `CoreWpfTest` und `DynamicPointsAndLines` bauen nach der Umstellung wieder
  einzeln mit `0` Fehlern.
- `HelixToolkit.Wpf.SharpDX` referenziert `HelixToolkit.SharpDX.Core` nicht
  mehr als normale Projektabhängigkeit. Die WPF-Assembly kompiliert ihre
  Legacy-API weiter selbst und deklariert die dafür benötigten Silk.NET-
  Abhängigkeiten direkt.
- Die aktiven NuSpecs `HelixToolkit.Wpf.Sharpdx.nuspec`,
  `HelixToolkit.SharpDX.Core.nuspec` und
  `HelixToolkit.SharpDX.Assimp.nuspec` enthalten keine SharpDX-
  Paketabhängigkeiten mehr.

Zusätzlich erledigt am 25. Juni 2026:

- Erstes WPF.SharpDX-Demo-Paket aus Step 3 migriert:
  `RenderTechniqueImportExport`, `ImageViewDemo` und `LineShadingDemo`.
- In diesen Demos echte `SharpDX.Vector*`-/`SharpDX.Color*`-Verwendungen
  durch `Silk.NET.Maths` beziehungsweise die lokale WPF.SharpDX-
  Kompatibilitätsfarbe ersetzt.
- Direkte `SharpDX.Direct3D11`- und `SharpDX.Mathematics`-Paketreferenzen aus
  diesen drei Demo-Projekten entfernt.
- Alte SharpDX-BindingRedirects aus `ImageViewDemo` und `LineShadingDemo`
  entfernt.
- Die drei Demos bauen einzeln mit `0` Fehlern.
- `CustomShaderDemo` wurde auf SDK-Projektformat und `net10.0-windows`
  umgestellt. Die Demo enthält keine echten SharpDX-Paketrefs, Usings oder
  BindingRedirects mehr und baut einzeln mit `0` Fehlern.
- `CustomViewCubeDemo` wurde auf `Silk.NET.Maths`-Aliase umgestellt. Die Demo
  enthält keine echten SharpDX-Paketrefs oder Usings mehr und baut einzeln
  mit `0` Fehlern.
- `SSAODemo` wurde auf `Silk.NET.Maths`-Aliase umgestellt. Direkte
  `SharpDX.Direct3D11`- und `SharpDX.Mathematics`-Paketreferenzen wurden
  entfernt; die Demo baut einzeln mit `0` Fehlern.
- `MeshSimplification` hatte nur noch ungenutzte SharpDX-Imports und direkte
  SharpDX-Paketreferenzen. Diese wurden entfernt; die Demo baut einzeln mit
  `0` Fehlern.
- `EnvironmentMapDemo` wurde auf `Silk.NET.Maths`-Aliase und lokale
  Translation-Matrix-Erzeugung umgestellt. Alte SharpDX-Paketreferenzen und
  BindingRedirects wurden entfernt; die Demo baut einzeln mit `0` Fehlern.
- `ExampleBrowser` wurde für die aktuell fehlernden Workitems
  `CursorPosition`, `Workitem10043`, `Workitem10045`, `Workitem10048` und
  `Workitem1349` von echten SharpDX-Math-/Color-Typen befreit. Die direkten
  SharpDX-Paketreferenzen und die alte `app.config` wurden entfernt; das
  Projekt baut einzeln mit `0` Fehlern.
- `InstancingDemo` wurde von SharpDX-Math, SharpDX-`Random`-Extensions,
  direkten SharpDX-Paketreferenzen, SharpDX-XAML-Assembly-Namespaces und
  alten BindingRedirects befreit. Die Demo baut einzeln mit `0` Fehlern.
- `LightingDemo` wurde auf `Silk.NET.Maths`-Vektoren/Farben und eine lokale
  `Random.NextDouble(min,max)`-Extension umgestellt. Direkte
  SharpDX-Paketreferenzen, SharpDX-XAML-Assembly-Namespaces und alte
  BindingRedirects wurden entfernt; die Demo baut einzeln mit `0` Fehlern.
- `ManipulatorDemo` und `MouseDragDemo` wurden auf `Silk.NET.Maths`-Vektoren
  und Matrizen umgestellt. Direkte SharpDX-Paketreferenzen, alte
  BindingRedirects und ungenutzte SharpDX-XAML-Assembly-Namespaces wurden
  entfernt; beide Demos bauen einzeln mit `0` Fehlern.
- `PolygonTriangulationDemo`, `ShadowMapDemo` und `TemplateDemo` wurden von
  echten SharpDX-Math-/Color-Typen, direkten SharpDX-Paketreferenzen und alten
  BindingRedirects befreit; alle drei Demos bauen einzeln mit `0` Fehlern.
- `TessellationDemo`, `DynamicTextureDemo`, `OctreeDemo` und `BoneSkinDemo`
  wurden von echten SharpDX-Math-/Color-Typen, direkten SharpDX-Paketreferenzen
  und alten BindingRedirects befreit. `OctreeDemo` ersetzt die entfernten
  SharpDX-`Random`-Extensions lokal. Alle vier Demos bauen einzeln mit
  `0` Fehlern.
- `CrossSectionDemo`, `ParticleSystemDemo`, `XRayDemo` und
  `GroupElementTester` wurden von echten SharpDX-Math-/Color-/State-Typen,
  direkten SharpDX-Paketreferenzen und alten BindingRedirects befreit.
  `CrossSectionDemo` ersetzt SharpDX-Matrix- und Trackball-Helfer lokal.
  Alle vier Demos bauen einzeln mit `0` Fehlern.
- `OrderIndependantTransparentRendering`, `Viewport3DXCodeBehindTester`,
  `PostEffectsDemo` und `DynamicCodeSurfaceDemo` wurden von echten
  SharpDX-Math-/Color-/State-Typen, direkten SharpDX-Paketreferenzen und alten
  BindingRedirects befreit. Alle vier Demos bauen einzeln mit `0` Fehlern.
- `MaterialDemo`, `BatchedMeshDemo`, `VolumeRendering` und der kleine
  verbleibende `MemoryLeakTester`-Rest wurden von echten SharpDX-Math-/
  Color-/State-Typen und direkten SharpDX-Paketreferenzen befreit. Alle vier
  Projekte bauen einzeln mit `0` Fehlern.
- `OffScreenRendering` wurde von echten SharpDX-Math-/Color-Typen und den
  SharpDX-`Random`-Extensions befreit. Das Core-Beispiel baut einzeln mit
  `0` Fehlern.

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
- `ScreenSpaceDemo`
- `DynamicTextureDemo`
- `ScreenDuplicationDemo`

### 2. `HelixToolkit.SharpDX.Core.Wpf` fertig entfernen

Das Projekt war ein Übergangspaket für .NET Core 3.0 und dupliziert heute
den Zweck von `HelixToolkit.Wpf.SharpDX`.

- In der Hauptmappe, AppVeyor-Packaging und den beiden aktiven Core-WPF-Demos
  ist es bereits entkoppelt.
- Das Projektverzeichnis selbst und historische Solution-Einträge in
  `HelixToolkit.SharpDX.Core.sln` und `HelixToolkit.AppVeyor.sln` bleiben
  noch als Legacy-Reste liegen.
- README-/Release-Dokumentation noch bereinigen.
- Kein Alias- oder Kompatibilitätspaket weiterpflegen.

### 3. UWP und WinUI aus dem Produktiv-Scope entfernen

Aktuell verbleiben echte SharpDX-Abhängigkeiten in:

- `HelixToolkit.UWP`
- `HelixToolkit.UWP.Assimp`
- `HelixToolkit.WinUI`
- UWP-/WinUI-Beispielen und Tests
- `HelixToolkit.UWP.Shared`

Diese Projekte bleiben vorerst als nicht unterstützter Legacy-Code im
Repository. Sie sind bereits aus `HelixToolkit.SharpDX.sln`, aktivem
AppVeyor-Build und Packaging entfernt. Release-Dokumentation ist noch zu
bereinigen. Das verbindliche Endgate ist die produktive WPF-Hauptmappe,
nicht der vollständige Legacy-Bestand.

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

Im Verzeichnis `Source` liegen die historischen `.sln`-Dateien und die neue
SilkToolkit-Mappe `SilkToolkit.slnx`.

| Projektmappe | Zweck | Bedeutung für die Migration |
| --- | --- | --- |
| `SilkToolkit.slnx` | Aktuelle Silk.NET-Mappe mit `SilkCore`, `SilkToolkit`, `SilkAssimp`, ShaderBuilder und allen DirectX-Beispielen. | Aktuelles Hauptgate. |
| `HelixToolkit.SharpDX.sln` | Historische Entwicklungsmappe für den DirectX-Renderer, WPF.SharpDX, Core, Assimp, Tests, ShaderBuilder und DirectX-Beispiele. | Historischer Vergleichsstand, nicht mehr der aktuelle SilkToolkit-Fokus. |
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

1. Für SilkToolkit-Arbeit `Source/SilkToolkit.slnx` verwenden.
2. Bei Fehlern gezielt `SilkCore`, `SilkToolkit`, `SilkAssimp` oder das
   betroffene Beispiel bauen.
3. `HelixToolkit.SharpDX.sln` nur noch als historischen Vergleichsstand
   verwenden.
4. `HelixToolkit.AppVeyor.sln` erst als spätes Repository-Gesamtgate
   bereinigen und ausführen.

Projektmappe in Visual Studio öffnen:

```powershell
devenv Source\SilkToolkit.slnx
```

In Rider über **Open** die gewünschte `.slnx` oder `.sln` auswählen.

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
dotnet build Source\SilkCore\SilkCore.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
dotnet build Source\SilkToolkit\SilkToolkit.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
dotnet build Source\SilkAssimp\SilkAssimp.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
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

## Historischer Produktivplan

Dieser Abschnitt dokumentiert den abgeschlossenen SharpDX-Migrationsverlauf.
Aktuelles Gate ist `Source/SilkToolkit.slnx`.

Baseline: Der vollständige Build von `HelixToolkit.SharpDX.sln` endete am
23. Juni 2026 vor Step 1 mit `438` Fehlern. Nach Step 1 endete der Build mit
`295` Fehlern und `4041` Warnungen. Nach Step 2 endete der Build mit `291`
Fehlern und `399` Warnungen. Nach dem ersten Step-3-Demo-Paket endete der
Build mit `283` Fehlern und `425` Warnungen. Nach der `CustomShaderDemo`-
TFM-Migration endete der Build mit `281` Fehlern und `441` Warnungen. Nach
`CustomViewCubeDemo` endete der Build mit `258` Fehlern und `454` Warnungen.
Nach `SSAODemo` endete der Build mit `254` Fehlern und `456` Warnungen. Nach
`MeshSimplification` endete der Build mit `253` Fehlern und `468` Warnungen.
Nach `EnvironmentMapDemo` endete der Build mit `245` Fehlern und
`473` Warnungen. Nach `ExampleBrowser` endete der Build mit `227` Fehlern und
`488` Warnungen. Nach `InstancingDemo` und `LightingDemo` endete der Build mit
`204` Fehlern und `502` Warnungen. Nach `ManipulatorDemo` und `MouseDragDemo`
endete der Build mit `155` Fehlern und `517` Warnungen. Nach
`PolygonTriangulationDemo`, `ShadowMapDemo` und `TemplateDemo` endet der Build
mit `143` Fehlern und `524` Warnungen. Nach `TessellationDemo`,
`DynamicTextureDemo`, `OctreeDemo` und `BoneSkinDemo` endet der Build mit
`134` Fehlern und `590` Warnungen. Nach `CrossSectionDemo`,
`ParticleSystemDemo`, `XRayDemo` und `GroupElementTester` endet der Build mit
`108` Fehlern und `661` Warnungen. Nach
`OrderIndependantTransparentRendering`, `Viewport3DXCodeBehindTester`,
`PostEffectsDemo` und `DynamicCodeSurfaceDemo` endet der Build mit
`48` Fehlern und `691` Warnungen. Nach `MaterialDemo`, `BatchedMeshDemo`,
`VolumeRendering` und `MemoryLeakTester` endet der Build mit `17` Fehlern und
`732` Warnungen. Nach `OffScreenRendering` endet der Build mit `7` Fehlern
und `734` Warnungen. Nach `WinFormsTest` baut `HelixToolkit.SharpDX.sln` mit
`0` Fehlern und `786` Warnungen. Fertig ist die Migration erst
bei einem grünen Debug- und Release-Build der bereinigten Hauptmappe.

### 1. Hauptmappe und Packaging bereinigen

Status: erledigt am 23. Juni 2026.

- Aus `HelixToolkit.SharpDX.sln` entfernt:
  `HelixToolkit.WinUI`, `HelixToolkit.UWP.Shared`, WinUI `ModelViewer` und
  `HelixToolkit.SharpDX.Core.Wpf`.
- `HelixToolkit.SharpDX.Core.Wpf`-Nuspec gelöscht und Packaging entfernt.
- AppVeyor auf `dotnet restore/build Source\HelixToolkit.SharpDX.sln`
  umgestellt.
- WinUI- und `HelixToolkit.SharpDX.Core.Wpf`-Pakete werden nicht mehr gebaut.
- `HelixToolkit.Core.Wpf.nuspec` bleibt unverändert, weil das ein anderes,
  nicht-DirectX-11-spezifisches WPF-Paket ist.
- Verifikation:
  `CoreWpfTest` und `DynamicPointsAndLines` bauen einzeln mit `0` Fehlern;
  `dotnet build Source\HelixToolkit.SharpDX.sln --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly`
  bleibt mit `295` Fehlern rot. Die verbleibenden Fehler liegen in noch nicht
  migrierten WPF-/Core-Demos, vor allem alte .NET-Framework-Demo,
  SharpDX-Math-/Color-Typen, `WinFormsTest` und `OffScreenRendering`.

### 2. Assembly-Ownership korrigieren

Status: erledigt für die produktive WPF-Bibliothekskante am 24. Juni 2026.

Der WPF-Build erzeugte tausende `CS0436`-Warnungen, weil Shared-Code sowohl
in Core als auch erneut in WPF kompiliert und Core zugleich als normale
Referenz importiert wurde.

- Allgemeine Typen gehören `HelixToolkit`.
- Rendering-Core und lokaler Toolkit-Kompatibilitätscode gehören
  `HelixToolkit.SharpDX.Core`.
- WPF-Controls und WPF-spezifische Typen gehören
  `HelixToolkit.Wpf.SharpDX`.
- Fixed-Namespace-Dateien in Core-/WPF-spezifische Projitems trennen oder
  ausschließlich aus der besitzenden Assembly referenzieren.
- `CS0436` nicht unterdrücken, sondern doppelte Typdefinitionen beseitigen.
- Umsetzung:
  `HelixToolkit.Wpf.SharpDX` referenziert `HelixToolkit.SharpDX.Core` nicht
  mehr direkt. Die WPF-Assembly bleibt Besitzerin ihrer Legacy-API und hat
  die benötigten Silk.NET-/Cyotek-Pakete direkt. Ein gezielter WPF-Build-
  Check auf `CS0436` liefert keine Treffer.
- Verifikation:
  `HelixToolkit.SharpDX.Core`, `HelixToolkit.Wpf.SharpDX`,
  `HelixToolkit.Wpf.SharpDX.Assimp`, `CoreWpfTest` und
  `DynamicPointsAndLines` bauen einzeln mit `0` Fehlern. Core-Tests
  `15/15`, WPF-Tests `44/44`.

### 3. Alle DirectX-Demos migrieren

Status: erledigt für die Hauptmappe am 26. Juni 2026. Erstes kleines WPF.SharpDX-Demo-Paket,
`CustomShaderDemo`-TFM-Migration und `CustomViewCubeDemo` erledigt am
25. Juni 2026. `SSAODemo`, `MeshSimplification` und `EnvironmentMapDemo`
ebenfalls erledigt. `ExampleBrowser` ist ebenfalls aus der direkten
SharpDX-Paketkante entfernt. `InstancingDemo` und `LightingDemo` sind
ebenfalls migriert und bauen einzeln grün. `ManipulatorDemo` und
`MouseDragDemo` sind ebenfalls migriert und bauen einzeln grün.
`PolygonTriangulationDemo`, `ShadowMapDemo` und `TemplateDemo` sind ebenfalls
migriert und bauen einzeln grün.

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
- `WinFormsTest` behält WinForms und ImGui; `SharpDX.Windows`, Math- und
  D3D-Typen wurden durch BCL-/Silk.NET-/Engine-Typen ersetzt.
- Verifikation bisher:
  `RenderTechniqueImportExport`, `ImageViewDemo`, `LineShadingDemo`,
  `CustomShaderDemo`, `CustomViewCubeDemo`, `SSAODemo` und
  `MeshSimplification` sowie `EnvironmentMapDemo` bauen einzeln mit
  `0` Fehlern. `ExampleBrowser`, `InstancingDemo`, `LightingDemo`,
  `ManipulatorDemo`, `MouseDragDemo`, `PolygonTriangulationDemo`,
  `ShadowMapDemo`, `TemplateDemo`, `TessellationDemo`,
  `DynamicTextureDemo`, `OctreeDemo`, `BoneSkinDemo`, `CrossSectionDemo`,
  `ParticleSystemDemo`, `XRayDemo`, `GroupElementTester`,
  `OrderIndependantTransparentRendering`, `Viewport3DXCodeBehindTester`,
  `PostEffectsDemo`, `DynamicCodeSurfaceDemo`, `MaterialDemo`,
  `BatchedMeshDemo`, `VolumeRendering` und `MemoryLeakTester` bauen ebenfalls
  einzeln mit `0` Fehlern. `OffScreenRendering` und `WinFormsTest` bauen
  ebenfalls einzeln mit `0` Fehlern.
  `dotnet build Source\HelixToolkit.SharpDX.sln --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly`
  baut mit `0` Fehlern und `786` Warnungen.

### 4. Metadaten und Legacy-Grenzen aktualisieren

Status: erledigt am 28. Juni 2026.

- README, Diagramme und Paketlisten auf WPF als unterstützte Plattform
  korrigiert.
- `HelixToolkit.SharpDX.Core.Wpf` als eingestellt und
  `HelixToolkit.Wpf.SharpDX` als Ersatz dokumentiert.
- WinUI/UWP ausdrücklich als nicht unterstützt und außerhalb von
  Hauptmappe, CI und Packaging markiert.
- Öffentliche WPF-Paketnamen, CLR-Namespaces und der XAML-Namespace bleiben
  vorerst aus Kompatibilitätsgründen unverändert.
- `README.md`, `docs/repository-structure.puml`,
  `docs/repository-structure.svg/HelixToolkit Repository Structure.png`,
  `HelixToolkit.WinUI.nuspec` und `HelixToolkit.UWP.nuspec` wurden
  aktualisiert.
- Aktive Hauptmappen-Projekte haben keine `SharpDX`-Paket- oder
  Assembly-Referenzen mehr. Verbleibende externe SharpDX-References liegen
  nur in den nicht in `HelixToolkit.SharpDX.sln` eingehängten Alt-Demos
  `DeferredShadingDemo`, `GenericMaterialDemo` und `ScreenSpaceDemo`.
- `D2DScreenMenuExample`, `SwapChainRenderingDemo`,
  `ScreenDuplicationDemo` und `MorphTargetAnimationDemo` wurden von
  ungenutzten SharpDX-Paketreferenzen befreit und bauen einzeln mit
  `0` Fehlern.
- `MemoryLeakTester` nutzt keinen SharpDX-`ObjectTracker` mehr; die Demo
  bleibt beim Prozessspeicher-Smoke. Der SharpDX-Object-Tracker wurde nicht
  durch einen Silk.NET-/COM-Object-Tracer ersetzt.
- `dotnet build Source\HelixToolkit.SharpDX.sln --no-restore --no-incremental -m:1 -c Debug -p:UseSharedCompilation=false -clp:ErrorsOnly`
  baut mit `0` Fehlern und `785` Warnungen.
- `dotnet build Source\HelixToolkit.SharpDX.sln --no-restore --no-incremental -m:1 -c Release -p:UseSharedCompilation=false -clp:ErrorsOnly`
  baut mit `0` Fehlern und `864` Warnungen.

### 5. Runtime-Smokes

Status: erledigt am 29. Juni 2026.

- `SimpleDemo` bleibt nach fünf Sekunden stabil und wurde danach beendet.
- `SwapChainRenderingDemo` bleibt nach fünf Sekunden stabil und wurde danach
  beendet.
- `OffScreenRendering` bleibt nach fünf Sekunden stabil und wurde danach
  beendet. Dafür wurde die ViewCube in der Offscreen-Demo deaktiviert und
  `ViewportCore` hängt unsichtbare ViewCube-Nodes nicht mehr beim Start an;
  der Core-Zielpfad kann die ViewCube-WIC-Textur nicht speichern.
- `WinFormsTest` bleibt nach fünf Sekunden stabil und wurde danach beendet.
- `dotnet build Source\HelixToolkit.SharpDX.sln --no-restore --no-incremental -m:1 -c Debug -p:UseSharedCompilation=false -clp:ErrorsOnly`
  baut nach dem Smoke-Fix weiterhin mit `0` Fehlern und `785` Warnungen.
- `dotnet build Source\HelixToolkit.SharpDX.sln --no-restore --no-incremental -m:1 -c Release -p:UseSharedCompilation=false -clp:ErrorsOnly`
  baut nach dem Smoke-Fix weiterhin mit `0` Fehlern und `864` Warnungen.
- Hinweis zur Offscreen-Ausgabe: `OffScreenRendering` ist eine WPF-Demo mit
  Render-Button und schreibt im Startup-Smoke keine Datei. Der Smoke prüft
  daher Start/Stabilität, nicht einen automatisch erzeugten Export.
- Der SharpDX-Object-Tracker bleibt bewusst offen: Es gibt aktuell keinen
  Ersatz für `SharpDX.Diagnostics.ObjectTracker`. Für native Resource-Leak-
  Diagnose wäre ein separater Silk.NET-COM-Handle-Tracker nötig.

## Prüfungen

### Suche

```powershell
rg -n 'projitems|HelixToolkit.Wpf.SharpDX.Shared|HelixToolkit.SharpDX.Shared' Source\SilkCore Source\SilkToolkit Source\SilkAssimp Source\SilkToolkit.slnx
rg -n 'ProjectReference Include=".*HelixToolkit|PackageReference Include="SharpDX|Reference Include="SharpDX' Source\Examples\WPF.SharpDX Source\Examples\SharpDX.Core Source\SilkCore Source\SilkToolkit Source\SilkAssimp -g '*.csproj' -g '*.props' -g '*.targets'
```

Treffer in öffentlichen Namen separat von echten Backend-Typen bewerten. Die
SilkToolkit-Mappe darf keine alten Projekt- oder Shared-Import-Kanten haben.

### Builds und Tests

```powershell
dotnet build Source\HelixToolkit.Native.ShaderBuilder\HelixToolkit.Native.ShaderBuilder.csproj --no-restore --no-incremental -m:1 -clp:ErrorsOnly
dotnet build Source\SilkCore\SilkCore.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
dotnet build Source\SilkToolkit\SilkToolkit.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
dotnet build Source\SilkAssimp\SilkAssimp.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
dotnet build Source\SilkToolkit.slnx --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
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

Der separate Demo-Migrationsblock ist abgearbeitet. Aktueller nächster Schritt
ist die SilkToolkit-Übergabe zu finalisieren:

1. Offene SilkToolkit-Änderungen committen.
2. Runtime-Smokes aus `Source/SilkToolkit.slnx` starten:
   `SimpleDemo`, `FileLoadDemo`, `DeferredShadingDemo`, `ScreenSpaceDemo`,
   `OffScreenRendering`, `WinFormsTest`.
3. Danach entscheiden, ob historische `HelixToolkit.SharpDX.*`-Projektmappen
   weiter gepflegt oder nur noch als Vergleichsstand behalten werden.
