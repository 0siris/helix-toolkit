# Migrationsplan: SharpDX → Silk.NET (DirectX 11)

## Überblick

Dieser Plan beschreibt die schrittweise Migration der HelixToolkit SharpDX-Rendering-Engine von **SharpDX** nach **Silk.NET**. DirectX 11 als zugrunde liegende API bleibt unverändert. Jede Phase beginnt mit einem **Pre-State Check** und endet mit einem **Post-State Gate**, das den validierten Zustand sichert.

---

## Pre-Migration: Baseline testen & sichern

### Aktuelle Test-Situation

| Test-Datei | Zeilen | Test-Typ | GPU-Required |
|------------|--------|----------|--------------|
| `EffectsManagerTests.cs` | 33 | Engine-Initialisierung, Dispose-Validierung (SharpDX.Diagnostics.ObjectTracker) | Ja |
| `SceneNodeTests.cs` | 44 | HitTest auf Box-Mesh (Vektor-Mathematik) | Teilweise |
| `CrossSectionMeshNodeTests.cs` | 87 | CrossSection HitTest mit Reflection-basierten Planes | Teilweise |

**Gesamt:** ~160 Zeilen Testcode, 5 Tests.

### Pre-State Gate: Baseline sicherstellen

Bevor die Migration beginnt muss der aktuelle Zustand validiert werden:

```
Pre-State Gate #0 — Baseline
──────────────────────────────
[Z] Projekt HelixToolkit.SharpDX.Core kompiliert (Debug + Release)
[Z] Projekt Helixtoolkit.SharpDX.Core.Tests kompiliert
[Z] Core-Tests laufen durch (oder bekannten Ausfall dokumentieren)
[Z] Alle #if SHARPDX-Präprozessor-Anweisungen gezählt und gelistet
[Z] Alle SharpDX-Import-Pakete in csproj verzeichnet
[Z] DisposeObject-Vererbungshierarchie vollständig erfasst (~15 Klassen)
[Z] Buffer/Texture/Shader Proxy-Klassen identifiziert (~60 Dateien)
[Z] Math-Typen (SharpDX.Mathematics) usage quantifiziert
[Z] Direct2D/DirectWrite-Nutzung gezählt (~19 Dateien)
```

---

## Migrationsphasen mit Test-Gates

### Phase 1: NuGet-Pakete & Define-Constants aktualisieren

**Ziel:** Kompilierfehler als "Leitfaden" für die weitere Migration erzeugen.

#### Dateien zu ändern
| Datei | Änderung |
|-------|----------|
| `Source/Helixtoolkit.SharpDX.Core.csproj` | SharpDX-Packages → Silk.NET-Packages (5 Pakete) |
| `Source/Directory.Build.props` (falls SHARPDX-Def) | SHARPDX → SILKNET in Debug/Release Configurations |

#### Package-Ersatzmatrix

```xml
<!-- ENTFERNEN -->
<PackageReference Include="SharpDX.D3DCompiler" Version="4.2.0" />
<PackageReference Include="SharpDX.Direct2D1" Version="4.2.0" />
<PackageReference Include="SharpDX.Direct3D11" Version="4.2.0" />
<PackageReference Include="SharpDX.DXGI" Version="4.2.0" />
<PackageReference Include="SharpDX.Mathematics" Version="4.2.0" />

<!-- HINZUFÜGEN -->
<PackageReference Include="Silk.NET.Direct3D11" Version="2.50.*" />
<PackageReference Include="Silk.NET.DXGI" Version="2.50.*" />
<PackageReference Include="Silk.NET.Direct3D.Compilers" Version="2.50.*" />
<PackageReference Include="Silk.NET.Direct2D" Version="2.50.*" />
<PackageReference Include="Silk.NET.Maths" Version="2.50.*" />
```

#### Pre-State Gate #1a — Vorheriger Zustand validieren

```
Pre-State Gate #1a — Package-Baseline
───────────────────────────────────────
[Z] Aktuelle SharpDX-PackageVersions: 4.2.0 (alle 5 Pakete)
[Z] Alle SharpDX-Importe in .csproj gezählt und protokolliert
[Z] Projekt buildbar mit aktueller SDK-Version (10.0.300)
[Z] Test-Projekte referenzieren korrekt HelixToolkit.SharpDX.Core
```

#### Post-State Gate #1b — Nach Paket-Ersatz

```
Post-State Gate #1b — Kompilierfehler als Guide
─────────────────────────────────────────────────
[ ] Projekt kompiliert NICHT mehr (erwartet: SharpDX-Typen nicht aufgelöst)
[ ] Alle #if SHARPDX-Präprozessor-Anweisungen zu #if SILKNET geändert
[ ] Silk.NET-Pakete korrekt referenziert, keine Version-Konflikte
```

---

### Phase 2: Namespace-Aliase & Using-Directives systematisch ersetzen

**Ziel:** Alle SharpDX-Typreferenzen durch Silk.NET-Typen ersetzen. Der Compiler zeigt an welcher Code angepasst werden muss.

#### Globale Replace-Regeln (für alle ~396 .cs-Dateien)

| Ersetze | Durch | Anmerkung |
|---------|-------|-----------|
| `using SDX11 = SharpDX.Direct3D11;` | `using SDX11 = Silk.NET.Direct3D11;` | Alias beibehalten, minimiert Boilerplate |
| `SharpDX.DXGI.Format` | `Silk.NET.DXGI.Format` | Direkter Ersatz |
| `SharpDX.Vector2/3/4` | `Silk.NET.Maths.Vector{N}<float>` | Konversion-Layer nötig (s.u.) |
| `SharpDX.Matrix` | `Silk.NET.Maths.Matrix<float>` | 4x4 Matrix |
| `SharpDX.Direct3D.FeatureLevel` | `Silk.NET.Direct3D11.Enums.FeatureLevel` | Enum-Pfad ändert sich |

#### Konversions-Layer für Math-Typen (neue Datei)

```csharp
// Neue Datei: Source/Helixtoolkit.SharpDX.Shared/Utilities/MathsCompat.cs

namespace HelixToolkit.SharpDX.Core {
    /// <summary>
    /// Extension methods und Helper-Klassen für Silk.NET.Maths Typ-Konvertierung.
    /// </summary>
    public static class MathsExtensions {
        // System.Numerics → Silk.NET.Maths Konvertierungen
        // Silk.NET.Maths → System.Numerics Konvertierungen
        
        // Analog für Vector2, Vector4, Matrix4x4, Quaternion
    }
}
```

#### Pre-State Gate #2a — Vorheriger Zustand validieren

```
Pre-State Gate #2a — Namespace-Baseline
───────────────────────────────────────
[Z] Alle using-Direktiven mit SharpDX gezählt (insgesamt ~X Verwendungen)
[Z] SharpDX.Mathematics-Typen isoliert identifiziert (~Y Verwendungen)
[Z] SDX11-Alias-Verwendungen dokumentiert (~Z Stellen)
```

#### Post-State Gate #2b — Nach Namespace-Ersatz

```
Post-State Gate #2b — Namespace-Migration abgeschlossen
───────────────────────────────────────────────────────
[ ] Alle SharpDX-Namespace-Referenzen ersetzt (0 verbleibend)
[ ] Silk.NET.Maths Konversions-Layer implementiert und getestet
[ ] Projekt kompiliert mit Silk.NET-Typen, keine SharpDX-Abhängigkeiten mehr in .cs-Dateien
```

---

### Phase 3: DisposeObject-Adapter für Silk.NET

**Ziel:** SharpDX ComObject-Disposal durch Silk.NET-Ressourcenverwaltung ersetzen.

#### Dateien zu ändern
| Datei | Änderung |
|-------|----------|
| `Utilities/DisposeObject.cs` (183 Zeilen) | Basis-Klasse anpassen, ComObject → Silk vtbl-Pattern |
| `Utilities/Buffers/StateProxy.cs` (~22 Zeilen) | `where StateType : ComObject` entfernen |

#### Problemstellung

SharpDX nutzt `ComObject` mit COM Ref-Counting. Silk.NET nutzt **Vtbl-basierte** Schnittstellen — keine ComObjects, kein Ref-Counting. Alle Ressourcen müssen manuell freigegeben werden.

#### Lösung: Adapter-Klasse für Silk.NET Resources (neue Datei)

```csharp
// Neue Datei: Source/Helixtoolkit.SharpDX.Shared/Utilities/SilkResourceProxy.cs

namespace HelixToolkit.SharpDX.Core {
    /// <summary>
    /// Wrapper for Silk.NET Direct3D11 resources that implements IDisposable.
    /// Ersetzt SharpDX ComObject-Disposal durch vtbl-basiertes Management.
    /// </summary>
    public class SilkResourceProxy : IDisposable {
        private readonly IntPtr _nativePtr;
        private bool disposed = false;

        protected virtual void OnDispose() {
            // Release via vtbl method oder Marshal.ReleaseComObject (falls COM)
        }

        public void Dispose() {
            if (!disposed) {
                OnDispose();
                disposed = true;
            }
        }
    }
}
```

#### Pre-State Gate #3a — Vorheriger Zustand validieren

```
Pre-State Gate #3a — DisposeObject-Baseline
─────────────────────────────────────────────
[Z] Alle ~15 Klassen die von DisposeObject erben identifiziert
[Z] SharpDX ComObject-Instantiierungspunkte gezählt (~N Stellen)
[Z] DisposeObject.Dispose() und OnDispose(bool) Override-Analyse abgeschlossen
```

#### Post-State Gate #3b — Nach Adapter-Implementierung

```
Post-State Gate #3b — Disposal-Migration abgeschlossen
───────────────────────────────────────────────────────
[ ] SilkResourceProxy implementiert und in allen Proxy-Klassen verwendet
[ ] Alle DisposeObject-Subklassen migriert (~15 Klassen)
[ ] Keine SharpDX ComObject-Abhängigkeiten mehr im Code
```

---

### Phase 4: Buffer-Proxies migrieren

**Ziel:** Vertex-, Index- und Constant Buffers an Silk.NET API anpassen.

#### Dateien zu ändern
| Datei | Zeilen | Status |
|-------|--------|--------|
| `Utilities/Buffers/BufferProxy.cs` (IBufferProxy, BufferProxyBase) | ~130 | Hauptänderung |
| `Utilities/Buffers/ElementsBufferProxy.cs` | ~? | DataStream → MapSubresource |
| `Core/Buffers/*` (GeometryBuffers etc.) | mehrere | Buffer-Erstellung anpassen |

#### API-Vergleich: Buffer-Erstellung

| SharpDX | Silk.NET |
|---------|----------|
| `new Buffer(device, desc)` | `device.CreateBuffer(bufferDesc, out buffer)` |
| `DataStream.From(data)` + `UpdateSubresource()` | `DeviceContext.MapSubresource(buffer, 0, MapFlags.Write, ...)` → Write → Unmap |
| `ResourceUsage.Dynamic` / `BindFlags.VertexBuffer` | `Silk.NET.Direct3D11.ResourceUsage.Dynamic`, `BindingFlags.VertexBuffer` |

#### Pseudocode: Dynamic Vertex Buffer (Silk.NET)

```csharp
// Vorher (SharpDX)
using var dataStream = DataStream.From(data);
buffer = new Buffer(device, new BufferDescription {
    Size = data.Length,
    Usage = ResourceUsage.Dynamic,
    DrawFlags = BindFlags.VertexBuffer,
    CpuAccessFlags = CpuAccessFlags.Write,
    StructureByteStride = elementSize
});

// Nachher (Silk.NET)
var desc = new Silk.NET.Direct3D11.BufferDescription {
    SizeInBytes = data.Length,
    Usage = Silk.NET.Direct3D11.ResourceUsage.Dynamic,
    BindingFlags = Silk.NET.Direct3D11.BindingFlags.VertexBuffer,
    CpuAccessFlags = Silk.NET.Direct3D11.CpuAccessFlags.Write,
    StructureByteStride = elementSize
};
device.CreateBuffer(desc, out buffer);

using var context = device.ImmediateContext;
context.MapSubresource(buffer, 0, MapSubresourceFlags.Write, 0, out DataBox box);
Marshal.Copy(dataArray, 0, box.DataPointer, data.Length);
context.UnmapSubresource(buffer, 0);
```

#### Pre-State Gate #4a — Vorheriger Zustand validieren

```
Pre-State Gate #4a — Buffer-Baseline
─────────────────────────────────────
[Z] Alle Buffer-Erstellungen identifiziert (~N Stellen)
[Z] DataStream-Verwendungen gezählt
[Z] BindFlags- und ResourceUsage-Kombinationen dokumentiert
```

#### Post-State Gate #4b — Nach Buffer-Migration

```
Post-State Gate #4b — Buffer-Proxies abgeschlossen
───────────────────────────────────────────────────
[ ] IBufferProxy.Buffer-Eigenschaft gibt Silk.NET-Typ zurück
[ ] Alle DataStream-Verwendungen durch MapSubresource ersetzt
[ ] Buffer-Tests (falls vorhanden) bestanden
```

---

### Phase 5: Texture2D, RTV, SRV, DSV migrieren

**Ziel:** Render Targets, Depth Stencil und Shader Resource Views an Silk.NET anpassen.

#### Dateien zu ändern
| Datei | Zeilen | Status |
|-------|--------|--------|
| `Render/RenderBuffers/DX11Texture2DRenderBufferProxy.cs` | ~? | Hauptänderung |
| `Utilities/Buffers/ShaderResourceViewProxy.cs` (~650 Zeilen) | Große Änderung |
| `DX11RenderBufferBase.cs` (~640 Zeilen) | RTV/DSV-Initialisierung |

#### API-Vergleich: Texture & Views

| SharpDX | Silk.NET |
|---------|----------|
| `new Texture2D(device, desc)` | `device.CreateTexture2D(textureDesc, out texture2D)` |
| `new RenderTargetView(device, resourceView)` | `device.CreateRenderTargetView(resource, rtvDesc, out rtv)` |
| `new ShaderResourceView(device, texture)` | `device.CreateShaderResourceView(texture, srvDesc, out srv)` |
| `Texture2D.FromSwapChain<Texture2D>(sc, 0)` | `swapChain.GetBuffer<ID3D11Texture2D>(0)` + CreateTexture2D |

#### Pre-State Gate #5a — Vorheriger Zustand validieren

```
Pre-State Gate #5a — Texture-Baseline
─────────────────────────────────────
[Z] Alle Texture2D-Erstellungen gezählt (~N Stellen)
[Z] RTV/SRV/DSV-Erstellungspunkte identifiziert (~M Stellen)
[Z] SwapChain BackBuffer-Handling analysiert
```

#### Post-State Gate #5b — Nach Texture-Migration

```
Post-State Gate #5b — Texture & View-Proxies abgeschlossen
───────────────────────────────────────────────────────────
[ ] IShaderResourceViewProxy implementiert Silk.NET-Typen
[ ] Alle Texture2D-Erstellungen auf Silk.NET.CreateTexture2D umgestellt
[ ] BackBuffer-Handling über SwapChain.GetBuffer() migriert
```

---

### Phase 6: ShaderReflection → DXC Migration

**Ziel:** Kompilierte `.cso`-Shader laden und Reflection-Daten (Constant Buffers, Texture Bindings, UAVs) extrahieren.

#### Dateien zu ändern
| Datei | Zeilen | Status |
|-------|--------|--------|
| `Shaders/ShaderReflector.cs` | 101 | Hauptänderung |
| `ConstantBufferDescription.cs` | ~? | Reflection-Daten parsen |
| `UWPShaderReader.cs` / ShaderByteCode-Loader | ~? | Eventuell unverändert (embedded Resources) |

#### Problemstellung & Optionen

SharpDX nutzt `ShaderReflection(byteCode)` um DXBC-Sections zu parsen. Silk.NET hat keine eingebaute Reflection-API für DXBC.

| Option | Aufwand | Risiko | Empfehlung |
|--------|---------|--------|------------|
| A: Eigenes DXBC-Parsing | Mittel (~200 Zeilen) | Niedrig | **Empfohlen** |
| B: DXC mit `/FC` + JSON-Ausgabe | Hoch | Mittel | Zu aufwendig |
| C: SharpDX.D3DCompiler als "Legacy" nur für Reflection | Niedrig | Mittel | Temporär, nicht ideal |

#### Option A — Eigenes DXBC-Parsing (empfohlen)

Die `.cso`-Dateien haben standard DXBC-Struktur mit sequenziellen Chunks:
```
┌───────────────────┐
│ d3db (signature)  │ → Shader signature, entry points
│ d3di (info)       │ → Reflection info (CB layouts, bindings) ← WICHTIG
│ d3df (flow)       │ → Control flow graph
│ d3dh (header)     │ → Compiler version, profile
└───────────────────┘
```

Die `ShaderReflector.Parse(byteCode)`-Methode muss:
1. DXBC-Chunks manuell parsen (4-Byte Magic + Size + Data)
2. `d3di`-Chunk lesen und D3D11_SIGNATURE_PARAMETER Struktur extrahieren
3. Constant Buffer Layouts aus d3di Section identifizieren

#### Pre-State Gate #6a — Vorheriger Zustand validieren

```
Pre-State Gate #6a — ShaderReflection-Baseline
───────────────────────────────────────────────
[Z] SharpDX.D3DCompiler.ShaderReflection API-Nutzung dokumentiert
[Z] DXBC-Section-Parsing Logik nachvollzogen (~100 Zeilen)
[Z] Alle .cso-Dateien (103 Stück) aufgelistet und validiert
```

#### Post-State Gate #6b — Nach ShaderReflection-Migration

```
Post-State Gate #6b — DXC/Reflection abgeschlossen
───────────────────────────────────────────────────
[ ] ShaderByteCode-Loader funktioniert mit .cso-Dateien (unverändert)
[ ] ConstantBufferMappings, TextureMappings etc. korrekt gefüllt
[ ] FeatureLevel-Parsing funktioniert wie vorher
```

---

### Phase 7: Direct2D / DirectWrite migrieren

**Ziel:** 2D Overlay Rendering, Text Metrics und Sprite-Texting.

#### Dateien mit Direct2D1/DirectWrite-Nutzung (~19 Dateien)
| Kategorie | Dateien |
|-----------|---------|
| ImagePacker | `TextInfoExtPacker.cs`, `ImagePacker.cs`, `SpritePackerBase.cs` |
| Core2D Text | `TextRenderCore2D.cs`, `FrameStatisticsRenderCore.cs` |
| Extensions | `BitmapExtensions.cs`, `AnimationExtensions.cs` |
| Models | `Figure.cs`, `BezierSegment.cs`, `ArcSegment.cs`, `LineSegment.cs`, `Segment.cs` |
| Utilities | `ScreenCapture.cs`, `WICHelper.cs` |

#### API-Vergleich

| SharpDX | Silk.NET |
|---------|----------|
| `SharpDX.Direct2D1.Device` | `Silk.NET.Direct2D.ID2D1Device` |
| `SharpDX.Direct2D1.Factory` | `Silk.NET.Direct2D.D2D1Factory1` |
| `SharpDX.DirectWrite.Factory` | `Silk.NET.Direct2D.IDWriteFactory` (via Direct2D Package) |

#### Pre-State Gate #7a — Vorheriger Zustand validieren

```
Pre-State Gate #7a — Direct2D/DirectWrite-Baseline
───────────────────────────────────────────────────
[Z] Alle SharpDX.Direct2D1-Verwendungen gezählt (~N Stellen)
[Z] Alle SharpDX.DirectWrite-Verwendungen gezählt (~M Stellen)
[Z] WIC/Bitmap Loading-Pfade analysiert
```

#### Post-State Gate #7b — Nach Direct2D-Migration

```
Post-State Gate #7b — Direct2D/DirectWrite abgeschlossen
───────────────────────────────────────────────────────
[ ] Alle D2D1-Factory-Konstruktionen durch Silk.NET äquivalente ersetzt
[ ] DirectWrite TextMetrics und Font-Rendering angepasst
[ ] WIC/Bitmap-Ladefunktionen funktionieren (System.Windows.Media oder eigenlayer)
```

---

### Phase 8: SharpDX.Toolkit.Graphics-Layer

**Ziel:** Das ~28 Dateien Utility-Layer ersetzen.

#### Status pro Datei
| Kategorie | Dateien | Änderung |
|-----------|---------|----------|
| DDS Utilities | `DDS.cs`, `DDSHelper.cs`, `DDSFlags.cs` | **Keine** — reines Byte-Parsing |
| Texture Wrapper | `Texture*.cs` (1D/2D/3D/Cube) | Silk.NET Direct3D11 wrapping |
| Image/Data | `Image.cs`, `ImageDescription.cs` | **Unverändert** (Datenstruktur) |
| Pixel Buffer | `PixelBuffer*.cs` | Silk.NET DataBox-Äquivalent |
| WIC Helper | `WICHelper.cs` | System.Windows.Media/BitmapDecoder |

#### Pre-State Gate #8a — Vorheriger Zustand validieren

```
Pre-State Gate #8a — Toolkit.Graphics-Baseline
───────────────────────────────────────────────
[Z] Alle SharpDX.Toolkit.Graphics-Verwendungen in anderen Dateien gezählt
[Z] DDS-Parsing als "unverändert" identifiziert
[Z] Texture.Load()-Pfade dokumentiert
```

#### Post-State Gate #8b — Nach Toolkit-Migration

```
Post-State Gate #8b — Toolkit.Layer abgeschlossen
─────────────────────────────────────────────────
[ ] SharpDX.Toolkit.Graphics-Namespaces durch Silk.NET ersetzt
[ ] DDSHelper.cs unverändert und funktionsfähig
[ ] Texture.Load() über Silk.NET Direct3D11 implementiert
```

---

### Phase 9: Device & SwapChain-Erstellung (platform-spezifisch)

**Ziel:** Plattform-spezifische RenderPanels (UWP/WinUI/WPF) an Silk.NET Direct3D11 anpassen. **Außerhalb der Core-Phase**, aber Interfaces müssen vorbereitet werden.

#### Dateien zu ändern
| Panel | Datei | Änderung |
|-------|-------|----------|
| UWP | `Helixtoolkit.UWP/CommonDX/HelixtoolkitRenderPanel.cs` | SwapChain-Erstellung, IDXGISwapChain1 |
| WinUI 3 | `Helixtoolkit.WinUI/CommonDX/HelixtoolkitRenderPanel.cs` | Gleiche Änderungen wie UWP |
| WPF | `Helixtoolkit.Wpf.SharpDX/DX11RenderControl.xaml.cs` | HWND-basierte SwapChain-Erstellung |

#### API-Vergleich: Device & SwapChain

| SharpDX | Silk.NET |
|---------|----------|
| `new Device(dxgiDevice, flags)` | `Device.Create(dxc: dxc, flags: ..., out device)` |
| `device.QueryInterface<Device2>()` | `device.GetExtensionDevice<IDXGIDevice12>()` |
| `new SwapChain1(factory, device, ref desc)` | `factory.CreateSwapChain(device, swapChainDesc, out swapChain)` |
| `Texture2D.FromSwapChain<Texture2D>(sc, 0)` | `swapChain.GetBuffer<ID3D11Texture2D>(0)` + `device.CreateTexture2D()` |

#### Wichtige Unterschiede Silk.NET:
- `Factory2` → `Factory4` (moderne DXGI-Version)
- `GetParent<T>()` entfällt; explizite COM-QI-Calls über `QueryInterface` oder `GetParent` mit Cast
- SwapChain für XAML `SwapChainPanel` erfordert WinRT/COM-Interop über `ISwapChainPanelNative`

#### Pre-State Gate #9a — Vorheriger Zustand validieren

```
Pre-State Gate #9a — Device/SwapChain-Baseline
───────────────────────────────────────────────
[Z] Alle Device-Erstellungen identifiziert (~N Stellen, platform-spezifisch)
[Z] SwapChain-Patterns für UWP/WinUI/WPF dokumentiert
[Z] ISwapChainPanelNative-Interop analysiert
```

#### Post-State Gate #9b — Nach Device/SwapChain-Migration (Follow-up)

```
Post-State Gate #9b — Platform-Spezifische Rendering-Erstellung abgeschlossen
─────────────────────────────────────────────────────────────────────────────
[ ] UWP RenderPanel: SwapChain mit Silk.NET DXGI Factory erstellt
[ ] WinUI 3 RenderPanel: Gleiche Migration wie UWP
[ ] WPF DX11RenderControl: HWND-basierte SwapChain-Erstellung migriert
```

---

## Test-Gates Detail-Spezifikation

### Gate-Validierungsmatrix

| Phase | Pre-State Check | Post-State Validation | Test-Fokus |
|-------|----------------|----------------------|-----------|
| **Baseline** | Baseline bauen + dokumentieren | — | Alles funktioniert vor Migration |
| **#1** Pakete | SharpDX-Pakete referenziert | Kompiliert NICHT (erwartet) | Package-Ersatz vollständig |
| **#2** Namespaces | SharpDX-Imports gezählt | 0 SharpDX-Imports, Silk.NET-Typen aufgelöst | Namespace-Migration vollständig |
| **#3** Disposal | DisposeObject-Hierarchie erfasst | Alle ~15 Subklassen migriert | Keine Memory Leaks |
| **#4** Buffer | Buffer-Erstellungen gezählt | IBufferProxy.Buffer → Silk.NET-Typ | Buffer-Performance unverändert |
| **#5** Texture | RTV/SRV/DSV-Pfade identifiziert | Alle Views über CreateX() erstellt | Render Targets funktionieren |
| **#6** Shader | DXBC-Parsing nachvollzogen | Reflection-Daten korrekt extrahiert | Shader-Bindings valide |
| **#7** Direct2D | D2D1/DWrite-Verwendungen gezählt | Text/Overlay Rendering funktioniert | 2D-UI-Komponenten stabil |
| **#8** Toolkit | Texture.Load()-Pfade dokumentiert | Silk.NET-Äquivalent implementiert | Texture Loading valid |
| **#9** Platform | Device/SwapChain-Patterns erfasst | Alle RenderPanels initialisieren | GPU-Rendering funktioniert |

### Automatisierte Test-Ausführung pro Gate

```bash
# Core-Bibliothek kompiliert
dotnet build Source/Helixtoolkit.SharpDX.Core/Helixtoolkit.SharpDX.Core.csproj /p:Configuration=Debug

# Tests kompiliert (falls Zielplattform verfügbar)
dotnet build Source/Helixtoolkit.SharpDX.Core.Tests/Helixtoolkit.SharpDX.Core.Tests.csproj /p:Configuration=Debug

# Core-Tests ausführen (GPU-relevante Tests fallen ggf. durch — dokumentieren)
dotnet test Source/Helixtoolkit.SharpDX.Core.Tests/Helixtoolkit.SharpDX.Core.Tests.csproj --no-build --verbosity minimal
```

### Bekannte Test-Herausforderungen

1. **GPU-Abhängigkeit:** `EffectsManagerTests` nutzt `SharpDX.Diagnostics.ObjectTracker.FindActiveObjects()` — muss durch Silk.NET äquivalent ersetzt oder mockbar gemacht werden
2. **STA-Anforderung:** `SceneNodeTests` und `CrossSectionMeshNodeTests` verwenden `[Apartment(ApartmentState.STA)]` — Headless-Validierung ohne UI-Thread erforderlich
3. **Vektorkonvertierung:** SharpDX.Vector3 ↔ Silk.NET.Maths.Vector3<float> — Tests müssen Konvertierungs-Layer abdecken

---

## Risikobewertung & Schätzung

### Risiko-Matrix

| Bereich | Risiko | Aufwand | Blockiert andere Phasen? |
|---------|--------|---------|--------------------------|
| **Phase 1-2** Pakete + Namespaces | Niedrig | ~3 Tage | Nein (kompiliert kurz nicht) |
| **Phase 3** DisposeObject Adapter | **Hoch** | ~2 Tage | Ja — alle Proxy-Klassen |
| **Phase 4** Buffer Proxies | Mittel | ~5 Tage | Teilweise (abhängig von Phase 3) |
| **Phase 5** Texture/RTV/SRV | Mittel | ~5 Tage | Teilweise (abhängig von Phase 4) |
| **Phase 6** ShaderReflection/DXC | Mittel | ~2–3 Tage | Nein (Shaders laden ohne Reflection) |
| **Phase 7** Direct2D/DirectWrite | Mittel | ~3 Tage | Nein — nur Text Overlay |
| **Phase 8** Toolkit.Graphics | Niedrig | ~1–2 Tage | Ja (abhängig von Phase 5) |
| **Phase 9** Device/SwapChain | **Hoch** | ~4–6 Tage | Ja — alle Render-Phasen |

### Gesamt-Abschätzung

```
Core-Phase (Phasen 1–8):     3–4 Wochen (eine Person, parallel möglich)
Platform-Follow-up (Phase 9): +1–2 Wochen
Test-Gate Validierung:       +1 Woche (manuelle GPU-Tests)
─────────────────────────────
Gesamt:                       5–7 Wochen
```

---

## Entscheidungspunkte vor Start

Bevor die Migration beginnt sind folgende Entscheidungen zu treffen:

| # | Entscheidung | Optionen | Empfehlung |
|---|-------------|----------|------------|
| D1 | ShaderReflection | A: Eigenes DXBC-Parsing (~200 Zeilen)<br>C: SharpDX.D3DCompiler als Legacy | **A** (langfristig sauberer) |
| D2 | Math-Typen | Voll auf Silk.NET.Maths oder Konversions-Layer zu System.Numerics | Konversions-Layer (weniger Risiko) |
| D3 | DirectX 9 Support in Wpf.SharpDX | Bleibt bestehen oder nur DX11? | Nur DX11 für Core-Migration |
| D4 | Versionierung | Major Bump (2.0) oder Minor mit Deprecated-Warning für SharpDX-Pfade | Major Bump (API-Bruch unvermeidbar) |

---

## Implementierungsreihenfolge (empfohlen)

```
Woche 1:  Phase 1 → Gate #1b (Pakete + Defines)
          Phase 2 → Gate #2b (Namespaces + Maths-Layer) [parallel möglich]

Woche 2:  Phase 3 → Gate #3b (DisposeObject Adapter)
          Phase 4 → Gate #4b (Buffer Proxies, Teil 1–2)

Woche 3:  Phase 5 → Gate #5b (Texture/RTV/SRV Proxies)
          Phase 6 → Gate #6b (ShaderReflection/DXC) [parallel zu Phase 5]

Woche 4:  Phase 7 → Gate #7b (Direct2D/DirectWrite)
          Phase 8 → Gate #8b (Toolkit.Graphics Layer)

Woche 5–6: Platform-Follow-up (Phase 9) → Gate #9b (Device/SwapChain)
           Manuelle GPU-Validierung in Beispiel-Projekten
```

---

## Post-Migration Checklist

```
[ ] Keine SharpDX-PackageReferences mehr in Helixtoolkit.SharpDX.Core.csproj
[ ] Keine using SharpDX.* Direktiven in Helixtoolkit.SharpDX.Shared/*.cs (außer Legacy-Kommentaren)
[ ] Alle 103 .cso-Shaderdateien laden und Reflection-Daten extrahieren
[ ] DisposeObject-Pattern funktioniert ohne Memory Leaks (~15 Klassen validiert)
[ ] Buffer/Texture-Performance vergleichbar mit SharpDX-Baseline
[ ] UWP, WinUI und WPF RenderPanels initialisieren korrekt
[ ] Alle 3 Core-Tests (EffectsManager, SceneNode, CrossSectionMeshNode) bestanden
[ ] Beispiel-Projekte rendern korrekt (Mesh, Billboard, Line, Text Overlay)
```

---

*Dokument erstellt am: 2026-05-17*  
*Version: 1.0 — Migrationsplan mit Test-Gates für SharpDX → Silk.NET Migration*
