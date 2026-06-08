# WPF Migration: SharpDX zu Silk.NET DirectX

Dieses Dokument konsolidiert die bisherigen Entwürfe:

- `docs/SharpDX-to-SilkNET-Migration.md`
- `docs/SilkNet-Migration-Draft.md`

Es ersetzt diese Dateien als Arbeitsgrundlage für die eigentliche Migration. Die alten Dateien bleiben als historische Drafts erhalten.

## Zielbild

HelixToolkit soll für den DirectX-Renderer intern von SharpDX auf Silk.NET migriert werden. Unterstützt wird nur WPF. DirectX 11 bleibt das Rendering-Backend.

Die bestehende öffentliche WPF-API bleibt zunächst kompatibel:

- Namespace `HelixToolkit.Wpf.SharpDX` bleibt erhalten.
- XAML Namespace `http://helix-toolkit.org/wpf/SharpDX` bleibt erhalten.
- Controls wie `Viewport3DX`, `DPFCanvas`, `DX11ImageSource` und `DPFSurfaceSwapChain` bleiben erhalten.
- Intern dürfen keine SharpDX-Pakete und keine SharpDX-Typen mehr im supported Scope verwendet werden.

Der Name `SharpDX` in der öffentlichen API ist damit ein Legacy-Kompatibilitätsname, nicht mehr die technische Backend-Implementierung.

## Entscheidungen

| Thema | Entscheidung |
| --- | --- |
| Plattformen | Nur WPF wird supported. UWP, WinUI und nicht-WPF SharpDX.Core-Beispiele werden aus aktiven Build- und CI-Gates entfernt. |
| WPF Renderpfade | Sowohl `D3DImage` als auch `HwndHost`/SwapChain bleiben erhalten. |
| D3DImage Interop | WPF `D3DImage` benötigt weiter eine `IDirect3DSurface9`; diese wird mit `Silk.NET.Direct3D9` bereitgestellt. |
| Math-Typen | `Silk.NET.Maths` wird das primäre Modell für Vektoren, Matrizen, Quaternionen und geometrische Math-Typen im migrierten DirectX-Scope. Bestehende Helix-Typen bleiben dort erhalten, wo sie bereits fachliche Semantik tragen. |
| Public API | Namespace- und XAML-Kompatibilität bleiben, SharpDX-Typen in Signaturen werden aber migriert. |
| Shader Reflection | Kein eigener DXBC-Parser. Reflection wird über `D3DReflect` aus `Silk.NET.Direct3D.Compilers` umgesetzt. |
| Assimp | `HelixToolkit.Wpf.SharpDX.Assimp` wird in der ersten Migration mit migriert. |
| Paketversionen | Silk.NET-Pakete werden fest auf `2.23.0` gepinnt. Keine Wildcards. |

## Supported Scope

### Zielprojekte

- `Source/HelixToolkit.Wpf.SharpDX/HelixToolkit.Wpf.SharpDX.csproj`
- `Source/HelixToolkit.Wpf.SharpDX.Assimp/HelixToolkit.Wpf.SharpDX.Assimp.csproj`
- `Source/HelixToolkit.SharpDX.Core/HelixToolkit.SharpDX.Core.csproj`, soweit es für WPF weiter als Rendering-Core genutzt wird
- `Source/HelixToolkit.SharpDX.Core.Assimp/HelixToolkit.SharpDX.Core.Assimp.csproj`, soweit es vom WPF-Assimp-Paket benötigt wird
- `Source/HelixToolkit.Wpf.SharpDX.Tests/HelixToolkit.Wpf.SharpDX.Tests.csproj`
- `Source/HelixToolkit.SharpDX.Core.Tests/HelixToolkit.SharpDX.Core.Tests.csproj`, soweit diese Tests den WPF-Core-Scope absichern

### Validierungsbeispiele

Mindestens diese WPF.SharpDX-Beispiele sollen als manuelle Smoke-Tests dienen:

- `Source/Examples/WPF.SharpDX/SimpleDemo`
- `Source/Examples/WPF.SharpDX/FileLoadDemo`
- `Source/Examples/WPF.SharpDX/BillboardDemo`
- `Source/Examples/WPF.SharpDX/D2DScreenMenuExample`
- `Source/Examples/WPF.SharpDX/SwapChainRenderingDemo`

### Nicht-Scope

- UWP Rendering
- WinUI Rendering
- `Source/Examples/SharpDX.Core/*`
- UWP/WinUI-Beispiele
- Neue Plattform-Backends neben WPF
- Umbenennung der öffentlichen WPF-Namespaces auf `SilkNET`

## Warum D3DImage weiter D3D9 braucht

Der WPF-Default-Pfad benötigt `D3DImage`, damit normale WPF-Elemente über dem gerenderten 3D-Inhalt liegen können. `D3DImage.SetBackBuffer(...)` akzeptiert als Interop-BackBuffer eine `D3DResourceType.IDirect3DSurface9`-Surface. Das ist eine WPF-API-Grenze, keine SharpDX-Einschränkung.

Der Zielpfad ist daher:

1. D3D11 rendert mit Silk.NET in eine shared texture.
2. Die shared texture wird über ihren shared handle auf der D3D9Ex-Seite geöffnet.
3. Die daraus erzeugte `IDirect3DSurface9` wird an `D3DImage.SetBackBuffer(...)` übergeben.
4. `D3DImage.Lock()`, `AddDirtyRect(...)` und `Unlock()` treiben die WPF-Composition an.

Der `HwndHost`/SwapChain-Pfad braucht diesen D3D9-Bridge-Schritt nicht, kann aber keine normalen WPF-Elemente im selben visuellen Layer über dem DirectX-Inhalt compositen.

## Ziel-Abhängigkeiten

SharpDX-Pakete im supported Scope werden entfernt:

```xml
<PackageReference Include="SharpDX.D3DCompiler" Version="4.2.0" />
<PackageReference Include="SharpDX.Direct2D1" Version="4.2.0" />
<PackageReference Include="SharpDX.Direct3D11" Version="4.2.0" />
<PackageReference Include="SharpDX.Direct3D9" Version="4.2.0" />
<PackageReference Include="SharpDX.DXGI" Version="4.2.0" />
<PackageReference Include="SharpDX.Mathematics" Version="4.2.0" />
```

Silk.NET-Pakete werden fest gepinnt:

```xml
<PackageReference Include="Silk.NET.Direct3D11" Version="2.23.0" />
<PackageReference Include="Silk.NET.Direct3D9" Version="2.23.0" />
<PackageReference Include="Silk.NET.DXGI" Version="2.23.0" />
<PackageReference Include="Silk.NET.Direct2D" Version="2.23.0" />
<PackageReference Include="Silk.NET.Direct3D.Compilers" Version="2.23.0" />
<PackageReference Include="Silk.NET.Maths" Version="2.23.0" />
```

`Silk.NET.Maths` ist damit nicht nur ein Binding-Hilfstyp, sondern die bevorzugte Math-Basis für den neuen WPF-DirectX-Scope. `System.Numerics` bleibt nur für bestehende externe API-Grenzen, Framework-Interop oder punktuelle Performance-/BCL-Kompatibilität im Einsatz.

## Fortschritt

### 2026-06-07

Umgesetzt:

- Planentscheidung geändert: `Silk.NET.Maths` ist das primäre Math-Modell für den migrierten DirectX-Scope.
- SharpDX-PackageReferences im definierten WPF-Scope entfernt:
  - `Source/HelixToolkit.SharpDX.Core/HelixToolkit.SharpDX.Core.csproj`
  - `Source/HelixToolkit.Wpf.SharpDX/HelixToolkit.Wpf.SharpDX.csproj`
  - `Source/HelixToolkit.Wpf.SharpDX.Assimp/HelixToolkit.Wpf.SharpDX.Assimp.csproj`
  - `Source/HelixToolkit.SharpDX.Core.Assimp/HelixToolkit.SharpDX.Core.Assimp.csproj`
  - `Source/HelixToolkit.Wpf.SharpDX.Tests/HelixToolkit.Wpf.SharpDX.Tests.csproj`
- Silk.NET-PackageReferences mit Version `2.23.0` hinzugefügt:
  - `Silk.NET.Direct3D11`
  - `Silk.NET.Direct3D9`
  - `Silk.NET.DXGI`
  - `Silk.NET.Direct2D`
  - `Silk.NET.Direct3D.Compilers`
  - `Silk.NET.Maths`
- `SHARPDX` Defines in den Zielprojekten auf `SILKNET` umgestellt.
- Zentrale erste Math-Alias-Datei eingeführt:
  - `Source/HelixToolkit.SharpDX.Shared/SilkNetMathAliases.cs`
  - eingebunden über `Source/HelixToolkit.SharpDX.Shared/HelixToolkit.SharpDX.Shared.projitems`
- Erste Native-/COM-Ownership-Schicht für Silk.NET angelegt:
  - `Source/HelixToolkit.SharpDX.Shared/Native/D3DDeviceHandles.cs`
  - `Source/HelixToolkit.SharpDX.Shared/Native/INativeDeviceResources.cs`
  - `Source/HelixToolkit.SharpDX.Shared/Native/SilkD3DDeviceResources.cs`
  - `Source/HelixToolkit.SharpDX.Shared/Native/SilkD3D11DeviceFactory.cs`
- Die neuen D3D11-Wrapper halten echte `Silk.NET.Core.Native.ComPtr<T>`-Handles für `ID3D11Device` und `ID3D11DeviceContext`.
- `SilkD3D11DeviceFactory.CreateDefault(...)` erzeugt ein D3D11-Gerät mit `Silk.NET.Direct3D11.D3D11.CreateDevice`, BGRA-Support und einer Feature-Level-Kette `11_1 -> 11_0 -> 10_1 -> 10_0`.
- `IEffectsManager`/`IDevice3DResources` expose zusätzlich `NativeDeviceResources`.
- `EffectsManager` erzeugt die Silk.NET-D3D11-Device-Resources parallel zum bestehenden SharpDX-Pfad und gibt sie im Dispose-Pfad wieder frei.
- `SilkD3DDeviceContext` kapselt erste direkte `ID3D11DeviceContext`-Aufrufe:
  - `ClearState`
  - `Flush`
  - `Draw`, `DrawAuto`, `DrawIndexed`, `DrawInstanced`, `DrawIndexedInstanced`
  - `Dispatch`
  - `IASetPrimitiveTopology`/`IAGetPrimitiveTopology`
  - `RSSetViewports`
  - `RSSetScissorRects`
- `DeviceContextProxy` verwendet für diese Basisaufrufe den nativen `SilkD3DDeviceContext`.
- `DeviceContextProxy_InputAssembler` ist auf einen minimalen nativen Kern reduziert: `PrimitiveTopology` wird über Silk.NET gesetzt/gelesen, `InputLayout` bleibt bis zum nativen InputLayout-Wrapper nur als Tracking-Punkt vorhanden.
- `RenderHostBase` und `ImmediateContextRenderer` erzeugen ihren Immediate-`DeviceContextProxy` aus `EffectsManager.NativeDeviceResources`.
- Indirect Draw/Dispatch und Deferred Command Lists sind im `DeviceContextProxy` bewusst als nicht unterstützt markiert, bis native Buffer- und CommandList-Wrapper existieren.
- Erste native View-Handle-Schicht ergänzt:
  - `Source/HelixToolkit.SharpDX.Shared/Native/D3DViewHandles.cs`
  - `RenderTargetView`
  - `DepthStencilView`
  - `ShaderResourceView`
  - `UnorderedAccessView`
  - `DepthStencilClearFlags`
- `SilkNetMathAliases.cs` um `Int4 = Silk.NET.Maths.Vector4D<int>` ergänzt.
- `SilkD3DDeviceContext` kapselt jetzt zusätzlich Output-Merger- und Clear-Aufrufe:
  - `OMSetRenderTargets`
  - `OMGetRenderTargets`
  - `OMSetRenderTargetsAndUnorderedAccessViews`
  - `OMGetRenderTargetsAndUnorderedAccessViews`
  - `ClearRenderTargetView`
  - `ClearDepthStencilView`
  - `ClearUnorderedAccessViewUint`
  - `ClearUnorderedAccessViewFloat`
- `DeviceContextProxy_Targets` ist von SharpDX-Usings gelöst und verwendet für RenderTargets, DepthStencil, UAVs und Clear-Aufrufe den nativen Silk.NET-Kontext.
- Erste native Resource- und Buffer-Handle-Schicht ergänzt:
  - `Source/HelixToolkit.SharpDX.Shared/Native/D3DResourceHandles.cs`
  - `Resource`
  - `Buffer`
  - `BufferDescription`
  - `DataBox`
  - `DataStream`
  - `ResourceRegion`
  - `BindFlags`, `CpuAccessFlags`, `ResourceUsage`, `ResourceOptionFlags`, `MapMode`, `MapFlags`
- `SilkNetMathAliases.cs` um `Format = Silk.NET.DXGI.Format` ergänzt.
- `SilkD3DDevice` erstellt jetzt native D3D11-Buffer.
- `SilkD3DDeviceContext` kapselt jetzt zusätzlich Resource- und Buffer-Aufrufe:
  - `MapSubresource`
  - `UnmapSubresource`
  - `UpdateSubresource`
  - `CopyResource`
  - `CopySubresourceRegion`
  - `ResolveSubresource`
  - `CopyStructureCount`
  - `GenerateMips`
  - `SOSetTargets`
- `DeviceContextProxy_ResourceUpdate` verwendet für `Resource`/`Buffer` jetzt den nativen Silk.NET-Kontext.
- `DeviceContextProxy_Targets` unterstützt Stream-Output-Bindings jetzt über native `Buffer`-Wrapper.
- `BufferProxy`, `ConstantBufferProxy` und `ElementsBufferProxy` verwenden im migrierten Pfad den nativen `Buffer`-Wrapper statt SharpDX-Buffer.
- `ConstantBufferProxy` erzeugt seinen nativen Buffer lazy beim ersten Upload, weil alte Pool-Aufrufer noch keine native Device-Grenze übergeben.
- Native SRV-/UAV-Description-Typen ergänzt:
  - `ShaderResourceViewDescription`
  - `UnorderedAccessViewDescription`
  - `ShaderResourceViewDimension`
  - `UnorderedAccessViewDimension`
  - `UnorderedAccessViewBufferFlags`
- `SilkD3DDevice` erstellt jetzt native `ShaderResourceView`- und `UnorderedAccessView`-Wrapper.
- `SilkD3DDeviceContext` kapselt jetzt Shader-Resource- und Compute-UAV-Bindings:
  - `VSSetShaderResources`
  - `HSSetShaderResources`
  - `DSSetShaderResources`
  - `GSSetShaderResources`
  - `PSSetShaderResources`
  - `CSSetShaderResources`
  - `CSSetUnorderedAccessViews`
- `DeviceContextProxy_ShaderResources` ist von SharpDX-Stage-APIs gelöst und bindet SRVs/UAVs über den nativen Silk.NET-Kontext.
- `ShaderResourceViewProxy` ist auf einen nativen View-Container reduziert. Texture-Loading- und RenderTarget-/DepthStencil-Erzeugung bleiben Platzhalter bis zur Texture-Resource-Portierung.
- `UAVBufferViewProxy` unterstützt native Buffer-SRV-/UAV-Erzeugung über `DeviceContextProxy`; alte Device-Constructoren bleiben vorübergehend als Übergang ohne native Erzeugung erhalten.
- `StructuredBufferProxy` erzeugt bei Buffer-Wechsel wieder eine native `ShaderResourceViewProxy`.
- `ParticleRenderCore` verwendet für UAV-Descriptions jetzt den zentralen `Format`-Alias statt `global::SharpDX.DXGI.Format`.

Aktueller Validierungsstand:

```powershell
rg -n 'PackageReference Include="SharpDX' Source\HelixToolkit.SharpDX.Core Source\HelixToolkit.Wpf.SharpDX Source\HelixToolkit.Wpf.SharpDX.Assimp Source\HelixToolkit.SharpDX.Core.Assimp Source\HelixToolkit.Wpf.SharpDX.Tests Source\HelixToolkit.SharpDX.Core.Tests
```

Ergebnis: Keine SharpDX-PackageReferences im definierten WPF-Scope.

```powershell
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Zuletzt gemessen nach dem Shader-Resource-/UAV-Schnitt: Restore ist erfolgreich, Build scheitert mit `1474` Compilefehlern. Die Reduktion kommt durch die migrierten `DeviceContextProxy`-Teilbereiche, die ersten nativen Buffer-/Resource-Wrapper und die native SRV-/UAV-Bindung; die verbleibenden Fehler liegen weiterhin in noch nicht migrierten SharpDX-Namespace-, Shader-, Buffer-Model-, RenderContext-, D2D/DWrite/WIC- und Utility-Schichten.

Zusätzliche Prüfung nach Einführung der Native-Schicht:

```powershell
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore -clp:ErrorsOnly | Select-String -Pattern 'Native\\|SilkD3D|ComPtr.cs|D3DDeviceHandles|SilkD3D11DeviceFactory|SilkD3DDeviceResources|INativeDeviceResources'
```

Ergebnis: Keine Treffer für die neuen Native-Dateien; die Fehlerkante bleibt bei den bestehenden SharpDX-Referenzen.

Eine zusätzliche Filterprüfung auf `NativeDeviceResources`, `IEffectsManager`, `EffectsManager` und `SilkD3D*` erzeugt ebenfalls keine Treffer. Die Fehlerkante liegt weiter in den noch nicht migrierten Shader-, Buffer-, RenderContext-, D2D/DWrite/WIC- und Utility-Schichten.

Zusätzliche Prüfung der portierten Basisdateien:

```powershell
rg -n "using SharpDX|global::SharpDX|SharpDX\." Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_DrawCalls.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_InputAssembler.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_Viewport.cs
```

Ergebnis: Keine Backend-Treffer in den portierten Basisdateien. Treffer mit `HelixToolkit.SharpDX.Core` bleiben Legacy-Namespace-Kompatibilität und sind keine SharpDX-Backend-Nutzung.

Zusätzliche Prüfung der portierten Output-Merger-/View-Dateien:

```powershell
rg -n "using SharpDX|global::SharpDX|SharpDX\.Direct|SharpDX\.DXGI|SharpDX\.Mathematics|SharpDX\.Diagnostics" Source\HelixToolkit.SharpDX.Shared\Native\D3DViewHandles.cs Source\HelixToolkit.SharpDX.Shared\Native\D3DDeviceHandles.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_Targets.cs Source\HelixToolkit.SharpDX.Shared\SilkNetMathAliases.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'D3DViewHandles|D3DDeviceHandles|DeviceContextProxy_Targets|SilkNetMathAliases|HelixToolkit.SharpDX.Shared.projitems'
```

Ergebnis: Keine SharpDX-Treffer in den bearbeiteten Dateien und keine Buildfehler, die auf diese Dateien gefiltert wurden.

Zusätzliche Prüfung der portierten Resource-/Buffer-Dateien:

```powershell
rg -n "using SharpDX|global::SharpDX|SharpDX\.Direct|SharpDX\.DXGI|SharpDX\.Mathematics|SharpDX\.Diagnostics" Source\HelixToolkit.SharpDX.Shared\Native\D3DResourceHandles.cs Source\HelixToolkit.SharpDX.Shared\Native\D3DDeviceHandles.cs Source\HelixToolkit.SharpDX.Shared\Native\D3DViewHandles.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_ResourceUpdate.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_Targets.cs Source\HelixToolkit.SharpDX.Shared\Utilities\Buffers\BufferProxy.cs Source\HelixToolkit.SharpDX.Shared\Utilities\Buffers\ConstantBufferProxy.cs Source\HelixToolkit.SharpDX.Shared\Utilities\Buffers\ElementsBufferProxy.cs Source\HelixToolkit.SharpDX.Shared\SilkNetMathAliases.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'Native\\D3DResourceHandles\.cs|Native\\D3DDeviceHandles\.cs|Native\\D3DViewHandles\.cs|DeviceContextProxy\\DeviceContextProxy_ResourceUpdate\.cs|DeviceContextProxy\\DeviceContextProxy_Targets\.cs|Utilities\\Buffers\\BufferProxy\.cs|Utilities\\Buffers\\ConstantBufferProxy\.cs|Utilities\\Buffers\\ElementsBufferProxy\.cs|SilkNetMathAliases\.cs|HelixToolkit\.SharpDX\.Shared\.projitems'
```

Ergebnis: Keine SharpDX-Treffer in den bearbeiteten Resource-/Buffer-Dateien und keine Buildfehler, die auf diese Dateien gefiltert wurden.

Zusätzliche Prüfung der portierten Shader-Resource-/UAV-Dateien:

```powershell
rg -n "using SharpDX|global::SharpDX|SharpDX\.Direct|SharpDX\.DXGI|SharpDX\.Mathematics|SharpDX\.Diagnostics" Source\HelixToolkit.SharpDX.Shared\Native\D3DViewHandles.cs Source\HelixToolkit.SharpDX.Shared\Native\D3DDeviceHandles.cs Source\HelixToolkit.SharpDX.Shared\Utilities\Buffers\ShaderResourceViewProxy.cs Source\HelixToolkit.SharpDX.Shared\Utilities\Buffers\UAVBufferViewProxy.cs Source\HelixToolkit.SharpDX.Shared\Utilities\Buffers\ElementsBufferProxy.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_ShaderResources.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'Native\\D3DViewHandles\.cs|Native\\D3DDeviceHandles\.cs|Utilities\\Buffers\\ShaderResourceViewProxy\.cs|Utilities\\Buffers\\UAVBufferViewProxy\.cs|Utilities\\Buffers\\ElementsBufferProxy\.cs|DeviceContextProxy\\DeviceContextProxy_ShaderResources\.cs|ParticleRenderCore\.cs'
```

Ergebnis: Keine SharpDX-Treffer in den portierten Shader-Resource-/UAV-Dateien und keine Buildfehler, die auf diese Dateien gefiltert wurden.

Stand dieses Implementierungsschnitts:

- Die Silk.NET-Device-Erzeugung ist als interner Parallelpfad vorhanden.
- Die Ownership für `ID3D11Device` und `ID3D11DeviceContext` läuft über `Silk.NET.Core.Native.ComPtr<T>`.
- `EffectsManager` hat eine erste native Resource-Grenze.
- Der Immediate-Renderpfad kann den neuen nativen Kontext erreichen.
- Draw-, InputAssembler-, Viewport-, Output-Merger-/Target-, ResourceUpdate- und ShaderResource-/UAV-Teile des `DeviceContextProxy` laufen teilweise über Silk.NET.
- Der nächste Umbau muss deshalb bei Texture-Wrappern, Texture-Resource-Erzeugung, State-Wrappern und Shader-/InputLayout-Wrappern ansetzen.

Nächste offene Migrationskante:

- Ca. `900` konkrete SharpDX-Code-Referenzen über Namespaces oder qualifizierte Backend-Typen verbleiben in Shared/WPF/Test-Code.
- Ca. `1894` reine Texttreffer auf `SharpDX` verbleiben inklusive öffentlicher Legacy-Namespace-Namen wie `HelixToolkit.Wpf.SharpDX`.
- Die ersten Fehlergruppen sind native API-Typen und Namespaces:
  - `SharpDX.Direct3D11`
  - `SharpDX.DXGI`
  - `SharpDX.Direct2D1`
  - `SharpDX.DirectWrite`
  - `SharpDX.WIC`
  - `SharpDX.Direct3D`
- Danach folgen qualifizierte SharpDX-Math- und Utility-Typen, die nicht durch die zentrale Alias-Datei abgedeckt werden, z. B. `global::SharpDX.BoundingSphere`, `global::SharpDX.DXGI.Format` oder D3D-State-Descriptions.

Pragmatische Reihenfolge für die nächsten Commits:

1. Texture-Wrapper und Texture-Resource-Erzeugung migrieren, damit SRV/RTV/DSV nicht nur als Handle-Typen existieren.
2. State-Wrapper und `DeviceContextProxy_States` migrieren.
3. `DeviceContextPool` und Deferred Command Lists mit nativen Kontexten neu aufsetzen oder im WPF-Scope vorübergehend deaktivieren.
4. D3D11/DXGI Typen in Shader-, Buffer-, RenderContext- und RenderBuffer-Schichten auf Silk.NET umstellen.
5. `IRenderTechnique`, `Technique` und Shader-Pools von SharpDX-`Device` auf die native Resource-Grenze umstellen.
6. D2D/DWrite/WIC separat portieren oder, wo möglich, durch WPF/BCL-Imaging ersetzen.
7. Qualifizierte SharpDX-Math-Referenzen auf `Silk.NET.Maths` und Helix-Typen migrieren.
8. Tests von `SharpDX.Diagnostics.ObjectTracker` und SharpDX-Math-Typen entkoppeln.

## Phase 0: Baseline und Inventar

Ziel: Den aktuellen Zustand messbar sichern, bevor SharpDX entfernt wird.

### Fortschritt

Status: Teilweise erledigt.

- Supported Scope und Nicht-Scope sind in diesem Dokument abgegrenzt.
- SharpDX-PackageReferences und zentrale SharpDX-Code-Referenzen wurden für den aktuellen Migrationsschnitt geprüft.
- Der aktuelle Buildstatus ist dokumentiert: Nach dem Shader-Resource-/UAV-Schnitt scheitert `dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly` mit `1474` Fehlern.
- Offene Inventararbeit: vollständige Testbaseline, detaillierte Zählung nach D3D9/D3D11/DXGI/D2D/DWrite/WIC/D3DCompiler und vollständige Liste aller Resource-Owner.

### Aufgaben

- Build- und Teststatus dokumentieren.
- Alle SharpDX-PackageReferences im supported Scope erfassen.
- Alle `using SharpDX`, `global::SharpDX` und SharpDX-Alias-Verwendungen zählen.
- D3D11-, D3D9-, DXGI-, D2D-, DWrite-, WIC- und D3DCompiler-Nutzungen getrennt erfassen.
- `DisposeObject`-Vererbung und alle nativen Resource-Owner auflisten.
- Buffer-, Texture-, Shader-, State- und RenderBuffer-Proxies identifizieren.
- `D3DImage`- und SwapChain-Pfade separat dokumentieren.

### Commands

```powershell
dotnet build Source\HelixToolkit.SharpDX.Core\HelixToolkit.SharpDX.Core.csproj
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj
dotnet test Source\HelixToolkit.SharpDX.Core.Tests\HelixToolkit.SharpDX.Core.Tests.csproj
dotnet test Source\HelixToolkit.Wpf.SharpDX.Tests\HelixToolkit.Wpf.SharpDX.Tests.csproj
rg -n "PackageReference Include=\"SharpDX|using SharpDX|global::SharpDX" Source
```

### Gate

- Baseline-Builds sind dokumentiert.
- Bekannte Testausfälle sind mit Fehlerbild dokumentiert.
- Supported Scope ist eindeutig gegen Nicht-Scope abgegrenzt.

## Phase 1: Build-Scope auf WPF reduzieren

Ziel: Nur noch der WPF-Scope entscheidet über den Migrationserfolg.

### Fortschritt

Status: Dokumentiert, aber noch nicht vollständig in Build-/CI-Struktur umgesetzt.

- WPF-Zielprojekte, WPF.Assimp und relevante Tests sind als supported Scope festgelegt.
- UWP, WinUI und nicht-WPF SharpDX.Core-Beispiele sind als Nicht-Scope markiert.
- Offene Arbeit: aktive Solutions, lokale Build-Gates und CI-Gates noch konsequent auf die definierte WPF-Zielmatrix reduzieren.

### Aufgaben

- Aktive Lösungen und CI-Gates auf WPF-Zielprojekte reduzieren.
- UWP, WinUI und nicht-WPF SharpDX.Core-Beispiele aus aktiven Builds entfernen.
- Nicht migrierte Plattformen in Dokumentation oder Projektstruktur als unsupported markieren.
- WPF.SharpDX-Beispiele für Smoke-Tests festlegen.

### Gate

- WPF-Zielmatrix baut vor der Backend-Migration noch mit SharpDX.
- UWP/WinUI/SharpDX.Core-only Beispielprojekte blockieren keine WPF-Migration mehr.
- CI oder lokaler Validierungsablauf nutzt nur die definierte WPF-Zielmatrix.

## Phase 2: Dependencies und Defines umstellen

Ziel: SharpDX als Paketquelle aus dem supported Scope entfernen und Silk.NET einziehen.

### Fortschritt

Status: Weitgehend erledigt.

- SharpDX-PackageReferences sind im definierten WPF-Scope entfernt.
- Silk.NET-PackageReferences sind mit Version `2.23.0` ergänzt.
- Backend-Defines wurden in den Zielprojekten von `SHARPDX` auf `SILKNET` umgestellt.
- Gate-Prüfung für SharpDX-PackageReferences im supported Scope liefert keine Treffer.
- Offene Arbeit: Testprojekte noch vollständig von SharpDX.Diagnostics/ObjectTracker entkoppeln und restliche Compilefehler aus Code-Typreferenzen schrittweise abbauen.

### Aufgaben

- SharpDX-PackageReferences aus WPF, WPF.Assimp, Core, Core.Assimp und relevanten Tests entfernen.
- Silk.NET-PackageReferences mit Version `2.23.0` hinzufügen.
- `SHARPDX`-Define prüfen:
  - Als öffentlicher Legacy-Name darf es nur bleiben, wenn es keine Backend-Entscheidung mehr ausdrückt.
  - Backend-spezifische Bedingungen auf `SILKNET` oder neutrale Namen migrieren.
- Testprojekte von SharpDX.Diagnostics/ObjectTracker entkoppeln.

### Gate

- `rg -n "PackageReference Include=\"SharpDX" <supported-scope>` liefert keine Treffer.
- Restliche Kompilierfehler stammen aus Code-Typreferenzen, nicht aus fehlenden NuGet-Paketen.

## Phase 3: Math- und Public-Type-Migration

Ziel: SharpDX-Math-Typen aus öffentlichen und internen Datenmodellen entfernen.

### Fortschritt

Status: Begonnen.

- `Silk.NET.Maths` ist als primäres Math-Modell festgelegt.
- Eine zentrale Alias-Datei `SilkNetMathAliases.cs` ist eingebunden und enthält erste Zieltypen, unter anderem `Int4 = Silk.NET.Maths.Vector4D<int>`.
- `Format` ist als `Silk.NET.DXGI.Format` zentral verfügbar, damit DXGI-Typen nicht über SharpDX zurückkommen.
- Offene Arbeit: qualifizierte SharpDX-Math-Referenzen, Bounds-/HitTest-Typen und Assimp-Konverter sind noch nicht systematisch migriert.

### Aufgaben

- `SharpDX.Vector2`, `Vector3`, `Vector4`, `Matrix`, `Quaternion`, `Color4`, `BoundingBox`, `BoundingSphere`, `BoundingFrustum`, `Ray`, `Plane` ersetzen.
- Bevorzugte Zieltypen:
  - `Silk.NET.Maths.Vector2D<T>`
  - `Silk.NET.Maths.Vector3D<T>`
  - `Silk.NET.Maths.Vector4D<T>`
  - `Silk.NET.Maths.Matrix4X4<T>`
  - `Silk.NET.Maths.Quaternion<T>`
  - `Silk.NET.Maths.Box3D<T>`, `Sphere<T>`, `Plane<T>` oder Helix-eigene Typen, je nach API-Bedarf
  - bestehende Helix-eigene Typen für Bounds, Rays und Hit-Tests, wenn vorhanden
- Konvertierungshelfer für WPF- und Assimp-Grenzen bereitstellen.
- Methodennamen wie `ToSharpDXVector3` in Assimp-Helpern umbenennen, z. B. zu `ToVector3`.
- Keine neuen SharpDX-Type-Shims einführen.

### Gate

- Kein SharpDX-Math-Typ verbleibt in supported Source-Dateien.
- Core-Tests für Hit-Test, Bounds, CrossSection und Assimp-Konvertierung laufen oder haben dokumentierte GPU-unabhängige Restfehler.

## Phase 4: COM- und Resource-Lifetime-Schicht

Ziel: SharpDX `ComObject`-Disposal durch explizite Silk.NET-COM-Ownership ersetzen.

### Fortschritt

Status: In Arbeit.

- Erste native Ownership-Grenze ist vorhanden: `INativeDeviceResources`, `SilkD3DDeviceResources`, `SilkD3DDevice` und `SilkD3DDeviceContext`.
- `ID3D11Device`, `ID3D11DeviceContext`, Views, Resource und Buffer werden in den neuen Pfaden über `Silk.NET.Core.Native.ComPtr<T>` gehalten.
- Erste native Wrapper existieren für RenderTargetView, DepthStencilView, ShaderResourceView, UnorderedAccessView, Resource und Buffer.
- Offene Arbeit: Ownership-Regeln noch vollständig dokumentieren und Texture-, Shader-, State-, DXGI-, D3D9- und D2D/DWrite/WIC-Objekte aus SharpDX-`ComObject`-Semantik lösen.

### Aufgaben

- Zentrale Wrapper für native COM-Objekte einführen, z. B. `SilkComObject<T>` oder `ComPtr<T>`.
- Ownership-Regeln dokumentieren:
  - Wer erstellt, released.
  - Wann QueryInterface-Ergebnisse released werden.
  - Wann Borrowed-Pointer nicht released werden.
- `DisposeObject`, `Disposer` und alle Resource-Proxies auf diese Regeln ausrichten.
- `StateProxy`, Buffer-/Texture-/Shader-Proxies und Pools von SharpDX `ComObject` Constraints entkoppeln.
- Device-lost und Dispose-Reihenfolge für SwapChain, BackBuffer, RenderTargets und DeviceContext absichern.

### Gate

- Alle nativen D3D/DXGI/D2D/D3D9-Objekte im supported Scope haben eindeutige Dispose-Ownership.
- Kein `SharpDX.ComObject`, `IsDisposed`, `NativePointer` oder `QueryInterface<T>` aus SharpDX verbleibt.

## Phase 5: D3D11 Core-Rendering migrieren

Ziel: Der WPF-Rendering-Core läuft auf Silk.NET.Direct3D11 und Silk.NET.DXGI.

### Fortschritt

Status: In Arbeit.

- `SilkD3D11DeviceFactory.CreateDefault(...)` erzeugt ein D3D11-Gerät über Silk.NET.Direct3D11.
- `EffectsManager` erstellt und hält parallel native Silk.NET-D3D11-Device-Resources.
- `RenderHostBase` und `ImmediateContextRenderer` erreichen den nativen Immediate-Kontext.
- `DeviceContextProxy` ist in diesen Bereichen teilweise auf Silk.NET umgestellt:
  - Draw Calls
  - Input Assembler für `PrimitiveTopology`
  - Viewport und Scissor Rects
  - Output-Merger RenderTargets/UAVs und Clear-Aufrufe
  - ResourceUpdate für `Resource`/`Buffer`
  - Stream-Output-Bindings über native Buffer
- Buffer-Proxies sind teilweise migriert: `BufferProxy`, `ConstantBufferProxy` und `ElementsBufferProxy`.
- Shader-Resource-/UAV-Bindings sind teilweise migriert: native SRV-/UAV-Descriptions, native SRV-/UAV-Erzeugung, `DeviceContextProxy_ShaderResources`, `ShaderResourceViewProxy`, `UAVBufferViewProxy` und `StructuredBufferProxy`.
- Offene Arbeit: Texture-Erzeugung, Shader-/State-Pools, InputLayouts, `DeviceContextPool`, Deferred-Kontexte, RenderBuffer und viele Buffer-Modelle hängen noch an SharpDX-Typen.

### Aufgaben

- `EffectsManager` migrieren:
  - Adapter-Auswahl über DXGI.
  - D3D11 device creation.
  - WARP/HARDWARE-Auswahl.
  - Feature-Level-Prüfung.
  - Device removed/reset handling.
- `DeviceContextPool` und `DeviceContextProxy` migrieren:
  - Draw calls.
  - Input assembler state.
  - Resource updates.
  - Shader resources.
  - Render targets.
  - Viewports.
  - State binding.
- Buffer-Proxies migrieren:
  - Vertex buffers.
  - Index buffers.
  - Constant buffers.
  - Dynamic map/unmap updates.
- Texture- und View-Proxies migrieren:
  - `Texture2D`
  - `ShaderResourceView`
  - `RenderTargetView`
  - `DepthStencilView`
  - `UnorderedAccessView`
- Shader- und State-Pools migrieren:
  - Vertex, pixel, geometry, hull, domain und compute shaders.
  - Input layouts.
  - Blend, rasterizer, depth-stencil und sampler states.

### Gate

- Core-Rendering-Projekte bauen ohne SharpDX.
- `DefaultEffectsManager` kann ein D3D11-Device erstellen und sauber disposen.
- Simple geometry kann in einem WPF-Smoke-Test gerendert werden.

## Phase 6: WPF Interop migrieren

Ziel: Beide WPF-Renderpfade funktionieren ohne SharpDX.

### Fortschritt

Status: Noch offen.

- D3DImage- und SwapChain-Pfade sind fachlich beschrieben.
- Noch keine D3D9Ex-/DXGI-Interop-Portierung mit Silk.NET umgesetzt.

### D3DImage-Pfad

Betroffene Kernbereiche:

- `DX11ImageSource`
- `DX11ImageSourceRenderHost`
- `DPFCanvas`
- `DPFSurfaceSwapChain.D3DImageExt`

Aufgaben:

- D3D9Ex-Device mit `Silk.NET.Direct3D9` erstellen.
- D3D11 shared texture handle über Silk.NET.DXGI abfragen.
- Shared texture als D3D9 texture/surface öffnen.
- `IDirect3DSurface9`-Pointer an `D3DImage.SetBackBuffer(...)` übergeben.
- FrontBuffer-Changed und Software-Fallback-Verhalten erhalten.
- `AddDirtyRect`/`Unlock`-Pfad unverändert in WPF halten.

### SwapChain/HwndHost-Pfad

Betroffene Kernbereiche:

- `DPFSurfaceSwapChain`
- `RenderControl`
- `SwapChainRenderHost`
- `DX11SwapChainRenderBufferProxy`

Aufgaben:

- SwapChain über Silk.NET.DXGI Factory erstellen.
- ResizeBuffers, Present und DeviceRemoved handling migrieren.
- `EnableSwapChainRendering=true` beibehalten.
- DPI scaling und parent window lifetime erhalten.

### Gate

- Default `Viewport3DX` rendert über `D3DImage`.
- WPF-Elemente können über dem 3D-Inhalt liegen.
- `EnableSwapChainRendering=true` rendert über HwndHost/SwapChain.
- Resize, DPI change, unload/reload und window close funktionieren in beiden Pfaden.

## Phase 7: Direct2D, DirectWrite, WIC und Texturen

Ziel: 2D Overlay, Text, Bitmap-/Texture-Loading und ScreenCapture laufen ohne SharpDX.

### Fortschritt

Status: Noch offen.

- D2D/DWrite/WIC bleiben eine der dominanten Fehlergruppen im aktuellen Build.
- Texture-Wrapper sind noch nicht portiert; nur Resource-/Buffer-Grundlagen sind vorhanden.

### Aufgaben

- Direct2D device/factory/context über Silk.NET.Direct2D erstellen.
- DirectWrite-Zugriffe ersetzen oder über passende Windows/Silk.NET-Interop-Schicht kapseln.
- WIC-Helper ersetzen:
  - Für WPF bevorzugt WPF Imaging APIs nutzen, wenn keine native WIC-Semantik nötig ist.
  - Native WIC nur dort kapseln, wo GPU-Texture-Upload oder ScreenCapture es verlangt.
- `SharpDX.Toolkit.Graphics`-Layer bereinigen:
  - DDS-Parsing behalten.
  - Texture-Wrapper auf Silk.NET resource creation umstellen.
  - PixelBuffer/DataBox-Äquivalente sauber kapseln.
- ScreenCapture mit Silk.NET staging textures und WPF/Windows encoding validieren.

### Gate

- Text/Billboard Rendering funktioniert.
- 2D Overlay-Beispiel funktioniert.
- DDS und Standard-Bildformate laden.
- Screenshot/Viewport export funktioniert für D3DImage- und SwapChain-Pfad.

## Phase 8: Shader Reflection und Shader-Pipeline

Ziel: Bestehende `.cso`-Shader weiterverwenden und Reflection-Daten ohne SharpDX laden.

### Fortschritt

Status: Noch offen.

- Entscheidung für `D3DReflect` über `Silk.NET.Direct3D.Compilers` ist dokumentiert.
- Shader creation, Reflection, InputLayout und Shader-Pools hängen noch an SharpDX-Typen.

### Aufgaben

- Shader bytecode loading aus embedded resources beibehalten.
- `ShaderReflector` auf `D3DReflect` über `Silk.NET.Direct3D.Compilers` umstellen.
- Reflection-Daten in bestehende Modelle übertragen:
  - Constant buffers.
  - Variables.
  - Textures.
  - Samplers.
  - UAVs.
  - Input signatures, soweit für input layouts benötigt.
- Shader creation für alle Shader-Stages auf Silk.NET.Direct3D11 migrieren.
- Fehlerpfade für ungültige oder fehlende `.cso`-Ressourcen dokumentieren.

### Gate

- Alle embedded `.cso`-Shader können geladen werden.
- Reflection-Daten entsprechen dem bisherigen Mapping.
- Materialien und RenderTechniken initialisieren ohne SharpDX.D3DCompiler.

## Phase 9: Assimp-WPF migrieren

Ziel: Import/Export-Workflows bleiben im WPF-Scope erhalten.

### Fortschritt

Status: Noch offen.

- WPF.Assimp ist im supported Scope enthalten und die SharpDX-PackageReferences sind entfernt.
- Assimp-Math-Konverter, Material-/Texture-Mapping und Builds sind noch nicht migriert.

### Aufgaben

- `HelixToolkit.SharpDX.Assimp.Shared` von SharpDX-Math-Typen entkoppeln.
- Konverter auf `Silk.NET.Maths` und Helix-Typen umstellen.
- Material- und Texture-Mapping an neue Texture-Modelle anpassen.
- `HelixToolkit.Wpf.SharpDX.Assimp` ohne SharpDX-PackageReferences bauen.
- Core.Assimp nur soweit migrieren, wie es für WPF.Assimp benötigt wird.

### Gate

- `FileLoadDemo` lädt mindestens OBJ mit Material/Texture.
- Assimp Importer erzeugt Geometrie, Materialien und Animationen ohne SharpDX-Typen.
- Export-Tests laufen oder bekannte Format-/GPU-Abhängigkeiten sind dokumentiert.

## Phase 10: Cleanup, Umbenennung nur intern und Final Gates

Ziel: Supported Scope ist SharpDX-frei und WPF-funktional.

### Fortschritt

Status: Begonnen.

- SharpDX-PackageReferences im definierten WPF-Scope sind entfernt.
- Öffentliche Legacy-Namen bleiben bewusst erhalten.
- Offene Arbeit: SharpDX-Code-Referenzen, Kommentare, README-/Package-Metadaten und finale Gates sind noch nicht abgeschlossen.

### Aufgaben

- Alle SharpDX-PackageReferences im supported Scope entfernen.
- Alle SharpDX-Namespaces im supported Scope entfernen.
- Kommentare aktualisieren, die SharpDX als technische Abhängigkeit beschreiben.
- Projekt-Tags und README-Hinweise korrigieren:
  - Public compatibility name bleibt.
  - Backend ist Silk.NET DirectX.
- Alte Migration-Drafts optional mit Hinweis auf diese Datei verlinken.
- NuGet-/Package-Metadaten für Major-Breaking-Change vorbereiten.

### Final Gate

```powershell
rg -n "PackageReference Include=\"SharpDX" Source\HelixToolkit.SharpDX.Core Source\HelixToolkit.Wpf.SharpDX Source\HelixToolkit.Wpf.SharpDX.Assimp Source\HelixToolkit.SharpDX.Core.Assimp Source\HelixToolkit.*.Tests
rg -n "using SharpDX|global::SharpDX|SharpDX\." Source\HelixToolkit.SharpDX.Shared Source\HelixToolkit.Wpf.SharpDX.Shared Source\HelixToolkit.SharpDX.Assimp.Shared Source\HelixToolkit.Wpf.SharpDX Source\HelixToolkit.Wpf.SharpDX.Assimp
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj
dotnet build Source\HelixToolkit.Wpf.SharpDX.Assimp\HelixToolkit.Wpf.SharpDX.Assimp.csproj
dotnet test Source\HelixToolkit.Wpf.SharpDX.Tests\HelixToolkit.Wpf.SharpDX.Tests.csproj
```

Erwartung:

- Keine SharpDX-Treffer im supported Scope.
- WPF Library und WPF Assimp bauen.
- WPF Tests laufen oder GPU-/Umgebungsabhängigkeiten sind explizit dokumentiert.
- Manuelle Smoke-Tests bestätigen beide WPF-Renderpfade.

## Testmatrix

| Bereich | Test |
| --- | --- |
| Device init | `DefaultEffectsManager` erstellen und disposen |
| Resource lifetime | Wiederholtes Erstellen/Disposen ohne wachsende native Ressourcen |
| D3DImage | Default `Viewport3DX`, WPF Overlay über 3D-Inhalt |
| SwapChain | `EnableSwapChainRendering=true`, Resize, DPI, Present |
| Geometry | Mesh, line, point, billboard |
| Materials | Phong, PBR, diffuse texture, environment map |
| 2D Overlay | Text, image, frame statistics, D2D screen menu |
| Import | OBJ/MTL über Assimp/FileLoadDemo |
| Export | Bestehende OBJ/Assimp Export-Tests |
| Capture | Viewport export und screen capture |
| Device lost | Device removed/reset recovery, soweit lokal reproduzierbar |

## Risiken

| Risiko | Bewertung | Gegenmaßnahme |
| --- | --- | --- |
| D3DImage/D3D9Ex interop | Hoch | Früh in Phase 6 prototypisieren, nicht ans Ende schieben. |
| COM lifetime mit Silk.NET | Hoch | Zentrale Wrapper und klare Ownership-Regeln vor Resource-Portierung einführen. |
| Direct2D/DirectWrite/WIC-Abdeckung | Mittel | WPF Imaging APIs bevorzugen, native Interop nur wo nötig. |
| Shader Reflection | Mittel | `D3DReflect` nutzen statt DXBC selbst zu parsen. |
| Public API SharpDX-Typen | Hoch | Systematisch mit Tests und klarer Breaking-Change-Dokumentation migrieren. |
| UWP/WinUI Restcode | Mittel | Aus Build-Scope entfernen, damit unsupported Code die Migration nicht blockiert. |

## Post-Migration Checklist

- [x] SharpDX-PackageReferences im definierten WPF-Scope entfernt.
- [ ] Supported Scope baut ohne SharpDX-PackageReferences.
- [ ] Supported Scope enthält keine SharpDX-Code-Referenzen.
- [ ] Beide WPF-Renderpfade funktionieren.
- [ ] `D3DImage` erhält seine `IDirect3DSurface9` über Silk.NET.Direct3D9.
- [ ] Shader Reflection läuft über `D3DReflect`.
- [ ] Assimp Import/Export ist migriert.
- [ ] Texture loading und screen capture funktionieren.
- [ ] WPF.SharpDX Beispiele aus der Smoke-Test-Liste laufen.
- [ ] Breaking Changes für SharpDX-Math-Typen sind dokumentiert.

## Referenzen

- Microsoft WPF `D3DImage`: https://learn.microsoft.com/dotnet/api/system.windows.interop.d3dimage
- Microsoft `D3DImage.SetBackBuffer`: https://learn.microsoft.com/dotnet/api/system.windows.interop.d3dimage.setbackbuffer
- Microsoft WPF and Direct3D9 Interoperation: https://learn.microsoft.com/dotnet/desktop/wpf/advanced/wpf-and-direct3d9-interoperation
- Microsoft `D3DReflect`: https://learn.microsoft.com/windows/win32/api/d3dcompiler/nf-d3dcompiler-d3dreflect
- Silk.NET.Direct3D11 NuGet: https://www.nuget.org/packages/Silk.NET.Direct3D11
- Silk.NET.Direct3D9 NuGet: https://www.nuget.org/packages/Silk.NET.Direct3D9
- Silk.NET.Direct2D NuGet: https://www.nuget.org/packages/Silk.NET.Direct2D
- Silk.NET.Direct3D.Compilers NuGet: https://www.nuget.org/packages/Silk.NET.Direct3D.Compilers
