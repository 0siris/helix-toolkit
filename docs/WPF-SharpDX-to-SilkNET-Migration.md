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
- Indirect Draw/Dispatch ist im `DeviceContextProxy` bewusst als nicht unterstützt markiert, bis native Argument-Buffer-Wrapper existieren. Deferred Command Lists laufen über native Silk.NET-Wrapper.
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

Zuletzt gemessen nach dem Texture2D-/RTV-/DSV-Schnitt: Restore ist erfolgreich, Build scheitert mit `1456` Compilefehlern. Die Reduktion kommt durch die migrierten `DeviceContextProxy`-Teilbereiche, die ersten nativen Buffer-/Resource-/Texture-Wrapper und die native SRV-/UAV-/RTV-/DSV-Bindung; die verbleibenden Fehler liegen weiterhin in noch nicht migrierten SharpDX-Namespace-, Shader-, Buffer-Model-, RenderContext-, D2D/DWrite/WIC- und Utility-Schichten.

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

Zusätzliche Prüfung der portierten Texture-/RTV-/DSV-Kante:

```powershell
rg -n "using SharpDX|global::SharpDX\.Direct3D\.ShaderResourceViewDimension\.Texture2D|SharpDX\.Direct3D11|SharpDX\.DXGI" Source\HelixToolkit.SharpDX.Shared\Native Source\HelixToolkit.SharpDX.Shared\Utilities\Buffers\ShaderResourceViewProxy.cs Source\HelixToolkit.SharpDX.Shared\Render\RenderBuffers\ColorBufferPool.cs Source\HelixToolkit.SharpDX.Shared\Render\RenderBuffers\DX11RenderBufferBase.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'D3DResourceHandles.cs|D3DDeviceHandles.cs|D3DViewHandles.cs|ShaderResourceViewProxy.cs|ColorBufferPool.cs|DX11RenderBufferBase.cs'
```

Ergebnis: Keine Buildfehler in `D3DResourceHandles.cs`, `D3DDeviceHandles.cs`, `D3DViewHandles.cs`, `ShaderResourceViewProxy.cs` oder `ColorBufferPool.cs`. `DX11RenderBufferBase.cs` meldet weiter die bekannte Legacy-`SharpDX.Direct3D11`-/D2D-Kante in der `HelixToolkit.SharpDX.Core`-Kompilation; die neu angeschlossenen Offscreen-Color-/Depth-Textures laufen aber ueber `IDevice3DResources.NativeDeviceResources`, `ShaderResourceViewDimension.Texture2D` ist in diesen Pfaden der eigene Enum-Wert.

Zusätzliche Prüfung der portierten State-Wrapper-Kante:

```powershell
rg -n "SharpDX|global::SharpDX|ComObject|deviceContext\.OutputMerger|deviceContext\.Rasterizer" Source\HelixToolkit.SharpDX.Shared\Native\D3DStateHandles.cs Source\HelixToolkit.SharpDX.Shared\Utilities\Buffers\StateProxy.cs Source\HelixToolkit.SharpDX.Shared\ShaderManager\StatePool.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_States.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_ShaderResources.cs Source\HelixToolkit.SharpDX.Shared\DefaultShaders\DefaultStates.cs Source\HelixToolkit.SharpDX.Shared\DefaultShaders\DefaultSamplers.cs Source\HelixToolkit.SharpDX.Shared\Interface\IPoolManagers.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'StateProxy.cs|StatePool.cs|DeviceContextProxy_States.cs|DeviceContextProxy_ShaderResources.cs|D3DStateHandles.cs|D3DDeviceHandles.cs|DefaultStates.cs|DefaultSamplers.cs|TechniqueDescription.cs|ColorStripeMaterialCore.cs|VolumeTextureMaterial.cs|GenericMaterialVariable.cs|ShaderPass.cs|DefaultEffectsManager.cs|DeviceContextProxy_InputAssembler.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: Keine SharpDX-Backend-Treffer in der portierten State-Kante; nur Legacy-Namespace-Namen wie `HelixToolkit.Wpf.SharpDX` bleiben sichtbar. Der gefilterte Build meldet keine Fehler in den State-/Sampler-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `1225` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in D2D/DWrite, nicht migrierten Direct3D11-Dateien, Shader-/InputLayout- und weiteren Utility-Flächen.

Zusätzliche Prüfung der portierten Shader-/InputLayout-Kante:

```powershell
rg -n "SharpDX\.Direct3D11|global::SharpDX\.Direct3D11|SharpDX\.D3DCompiler|global::SharpDX\.D3DCompiler|using SharpDX\.Direct3D|ComObject" Source\HelixToolkit.SharpDX.Shared\Native\D3DShaderHandles.cs Source\HelixToolkit.SharpDX.Shared\Native\D3DDeviceHandles.cs Source\HelixToolkit.SharpDX.Shared\DefaultShaders\DefaultGeometryShaders.cs Source\HelixToolkit.SharpDX.Shared\Shaders\VertexShader.cs Source\HelixToolkit.SharpDX.Shared\Shaders\PixelShader.cs Source\HelixToolkit.SharpDX.Shared\Shaders\ComputeShader.cs Source\HelixToolkit.SharpDX.Shared\Shaders\DomainShader.cs Source\HelixToolkit.SharpDX.Shared\Shaders\HullShader.cs Source\HelixToolkit.SharpDX.Shared\Shaders\GeometryShader.cs Source\HelixToolkit.SharpDX.Shared\Shaders\ShaderBase.cs Source\HelixToolkit.SharpDX.Shared\Shaders\ShaderDescription.cs Source\HelixToolkit.SharpDX.Shared\Shaders\ShaderReflector.cs Source\HelixToolkit.SharpDX.Shared\Shaders\ConstantBufferDescription.cs Source\HelixToolkit.SharpDX.Shared\Shaders\InputLayoutDescription.cs Source\HelixToolkit.SharpDX.Shared\Shaders\InputLayoutProxy.cs Source\HelixToolkit.SharpDX.Shared\Shaders\MappingProxy.cs Source\HelixToolkit.SharpDX.Shared\ShaderManager\ShaderPool.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_ShaderResources.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_InputAssembler.cs Source\HelixToolkit.SharpDX.Shared\Interface\IShaderReflector.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'D3DShaderHandles.cs|D3DDeviceHandles.cs|VertexShader.cs|PixelShader.cs|ComputeShader.cs|DomainShader.cs|HullShader.cs|GeometryShader.cs|ShaderBase.cs|ShaderDescription.cs|ShaderReflector.cs|ConstantBufferDescription.cs|InputLayoutDescription.cs|InputLayoutProxy.cs|ShaderPool.cs|DeviceContextProxy_ShaderResources.cs|DeviceContextProxy_InputAssembler.cs|IShaderReflector.cs|DefaultGeometryShaders.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: Native Shader-Wrapper, Shader-Erzeugung, InputLayout-Erzeugung, Shader-/ConstantBuffer-Bindings und `ShaderReflector` laufen in dieser Kante ohne SharpDX-D3D11-/D3DCompiler-Typen. `Technique` und `IRenderTechnique.Device` bleiben eine separate Public-API-/EffectsManager-Legacy-Kante. Der gefilterte Build meldet keine Fehler in den portierten Shader-/InputLayout-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `1153` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in D2D/DWrite und weiteren noch nicht migrierten SharpDX-Flächen.

Zusätzliche Prüfung der portierten Deferred-Context-/CommandList-Kante:

```powershell
rg -n "SharpDX\.Direct3D11|global::SharpDX\.Direct3D11|using Device = SharpDX|FinishCommandList|ExecuteCommandList|CreateDeferredContext|CommandList" Source\HelixToolkit.SharpDX.Shared\Native\D3DDeviceHandles.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextPool.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_DrawCalls.cs Source\HelixToolkit.SharpDX.Shared\Render\Renderer\DeferredContextRenderer.cs Source\HelixToolkit.SharpDX.Shared\Render\Renderer\RenderTaskScheduler.cs Source\HelixToolkit.SharpDX.Shared\ShaderManager\EffectsManager.cs Source\HelixToolkit.SharpDX.Shared\Render\RenderBuffers\DX11RenderBufferBase.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'D3DDeviceHandles.cs|DeviceContextPool.cs|DeviceContextProxy_DrawCalls.cs|DeferredContextRenderer.cs|RenderTaskScheduler.cs|DX11RenderBufferBase.cs|EffectsManager.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -p:BuildProjectReferences=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'D3DDeviceHandles.cs|DeviceContextPool.cs|DeviceContextProxy_DrawCalls.cs|DeferredContextRenderer.cs|RenderTaskScheduler.cs|DX11RenderBufferBase.cs|EffectsManager.cs|Bool32|CommandList|CreateDeferredContext|ExecuteCommandList|FinishCommandList'
```

Ergebnis: `DeviceContextPool`, `DeferredContextRenderer` und `RenderTaskScheduler` sind von SharpDX-CommandList-/Deferred-Context-Typen gelöst. `DeviceContextProxy.FinishCommandList` und `ExecuteCommandList` werfen nicht mehr fuer die Deferred-Kante, sondern verwenden native `ID3D11CommandList`-Wrapper. Der normale gefilterte Build bleibt durch bekannte `SharpDX.Core`-Referenzfehler in `IEffectsManager`, `EffectsManager`, `DX11RenderBufferBase` und D2D/DWrite/WIC blockiert; der WPF-only Build ohne ProjectReferences meldet keine Fehler in den geänderten Deferred-Dateien und stoppt nur wegen der fehlenden vorgelagerten Core-Metadatendatei. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `1144` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in D2D/DWrite.

Zusätzliche Prüfung der portierten Technique-Device-Kante:

```powershell
rg -n "SharpDX\.Direct3D11|using Device = SharpDX|Device1 Device|SharpDX.Direct3D11.Device" Source\HelixToolkit.SharpDX.Shared\Interface\IRenderTechnique.cs Source\HelixToolkit.SharpDX.Shared\Shaders\Technique.cs
rg -n "new Technique\([^\n]*,\s*(Device|device|device1)" Source\HelixToolkit.SharpDX.Shared Source\HelixToolkit.Wpf.SharpDX -g "*.cs"
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'IRenderTechnique.cs|Technique.cs|EffectsManager.cs|INativeDeviceResources.cs|D3DDeviceHandles.cs|SilkD3DDeviceResources.cs|CS005|CS006|CS0122'
```

Ergebnis: `IRenderTechnique.Device` und `Technique.Device` liefern jetzt `Native.SilkD3DDevice`. `Technique` speichert keinen SharpDX-Device-Konstruktorparameter mehr, und `EffectsManager` erzeugt Techniques über die native Resource-Grenze. `INativeDeviceResources`, `SilkD3DDevice`, `SilkD3DDeviceContext`, `SilkDriverType` und `SilkFeatureLevel` sind für diese öffentliche Interface-Kante sichtbar; Raw-COM-Handles und interne Shader-/InputLayout-Erzeugung bleiben assembly-intern. Der gefilterte Build zeigt keine neue Technique-/Native-Accessibility-Kante; er bleibt an den bekannten `IEffectsManager`-/D2D-/DWrite-/WIC-/SharpDX-Core-Fehlern blockiert. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `1137` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in D2D/DWrite.

Zusätzliche Prüfung der portierten RenderHost-Device-Kante:

```powershell
rg -n "SharpDX\.Direct3D11\.Device|using Device = SharpDX\.Direct3D11|global::SharpDX\.Direct3D\.FeatureLevel|manager\.Device\.ImmediateContext|new ShaderResourceViewProxy\(deviceResources\.Device\)" Source\HelixToolkit.SharpDX.Shared\Interface\IRenderHost.cs Source\HelixToolkit.SharpDX.Shared\Render\RenderHost\RenderHostBase.cs Source\HelixToolkit.SharpDX.Shared\Render\Renderer\ImmediateContextRenderer.cs Source\HelixToolkit.SharpDX.Shared\Model\Scene\BoneSkinMeshNode.cs Source\HelixToolkit.SharpDX.Shared\Model\Material\Variables\ColorStripeMaterialVariable.cs Source\HelixToolkit.SharpDX.Shared\Model\Material\VolumeTextureMaterial.cs
rg -n "manager\.Device\.ImmediateContext|manager\.Device\.ImmediateContext1" Source\HelixToolkit.SharpDX.Shared
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `IRenderHost.Device` und `DX11RenderHostBase.Device` liefern jetzt `Native.SilkD3DDevice`; `IRenderHost.FeatureLevel`/`DX11RenderHostBase.FeatureLevel` verwenden den eigenen `FeatureLevel`-Typ. `ImmediateContextRenderer` vergleicht gegen `FeatureLevel.Level_11_0`. Die einfachen `manager.Device.ImmediateContext*`-Aufrufer in `BoneSkinMeshNode` nutzen `manager.NativeDeviceResources`, und die ColorStripe-/Volume-Texture-Pfade geben native Ressourcen an `ShaderResourceViewProxy` weiter. Der gefilterte Build zeigt in den Ziel-Dateien nur noch die bekannte Direct2D-Legacy-Kante in `IRenderHost`/`RenderHostBase`; keine neuen Fehler in `ImmediateContextRenderer.cs`, `BoneSkinMeshNode.cs`, `ColorStripeMaterialVariable.cs` oder `VolumeTextureMaterial.cs`. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `1129` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in D2D/DWrite und weiteren nicht migrierten SharpDX-Flächen.

Zusätzliche Prüfung der portierten RenderCore-Device-/CubeMap-/Volume-Kante:

```powershell
rg -n "SharpDX\.Direct3D11\.Device|using Device = SharpDX|global::SharpDX\.Direct3D\.ShaderResourceViewDimension\.TextureCube|EffectsManager\.Device|Resource\.Dimension|QueryInterface<Texture3D>|new RenderTargetView\(|new DepthStencilView\(" Source\HelixToolkit.SharpDX.Shared\Core\Abstract\RenderCore.cs Source\HelixToolkit.SharpDX.Shared\Core\DynamicCubeMapCore.cs Source\HelixToolkit.SharpDX.Shared\Model\Material\Variables\VolumeMaterialVariable.cs Source\HelixToolkit.SharpDX.Shared\Utilities\Buffers\ShaderResourceViewProxy.cs Source\HelixToolkit.SharpDX.Shared\Native\D3DViewHandles.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `RenderCore.Device` liefert jetzt `Native.SilkD3DDevice`. OIT-, ShadowMap-, SkyBox-/SkyDome- und DynamicCubeMap-Texture-Erzeugung laufen über den nativen `ShaderResourceViewProxy`-Device-Pfad. `DynamicCubeMapCore` verwendet den eigenen `ShaderResourceViewDimension.TextureCube`-Wert, erzeugt Cube-Face-RTVs/DSVs über `SilkD3DDevice.CreateRenderTargetView/CreateDepthStencilView` und führt Command Lists über den aktuellen `DeviceContextProxy` aus. `VolumeMaterialVariable` liest Texture3D-Abmessungen aus der nativen `Texture3DDescription`; der Raw-Pixel-Volume-Pfad erzeugt dafür eine native Texture3D plus SRV. Der gefilterte Build meldet keine Fehler in den portierten RenderCore-/CubeMap-/Volume-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `1110` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in D2D/DWrite und anderen nicht migrierten SharpDX-Flächen.

Zusätzliche Prüfung der portierten RenderContext-/Geometry-Offscreen-Kante:

```powershell
rg -n "using (global::)?SharpDX\.(Direct3D11|Direct3D);|GetOffScreen(RT|DS|Texture)\([^\r\n]*global::SharpDX\.DXGI\.Format|public ShaderResourceViewProxy GetOffScreen.*SharpDX\.DXGI" Source\HelixToolkit.SharpDX.Shared\Core\Abstract\GeometryRenderCore.cs Source\HelixToolkit.SharpDX.Shared\Core\CrossSectionMeshRenderCore.cs Source\HelixToolkit.SharpDX.Shared\Core\MeshRenderCore.cs Source\HelixToolkit.SharpDX.Shared\Core\TopMostMeshRenderCore.cs Source\HelixToolkit.SharpDX.Shared\Core\VolumeRenderCore.cs Source\HelixToolkit.SharpDX.Shared\Core\Sprite2DRenderCore.cs Source\HelixToolkit.SharpDX.Shared\Core\ScreenSpacedMeshRenderCore.cs Source\HelixToolkit.SharpDX.Shared\Core\PostEffects\PostEffectBlurCore.cs Source\HelixToolkit.SharpDX.Shared\Core\PostEffects\PostEffectMeshOutlineBlurCore.cs Source\HelixToolkit.SharpDX.Shared\Core\SSAOCore.cs Source\HelixToolkit.SharpDX.Shared\Render\RenderContext.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `RenderContext.GetOffScreenTexture/GetOffScreenRT/GetOffScreenDS` nehmen jetzt den eigenen `Format`-Alias auf Silk.NET-DXGI-Formate. Die Geometry-/ScreenSpaced-/Volume-/Sprite-RenderCore-Imports haengen nicht mehr an `SharpDX.Direct3D11`/`SharpDX.Direct3D`, und die direkten Offscreen-Aufrufer in Volume, SSAO und Blur/Outline verwenden Silk.NET-Formatwerte. `Bool4`, `FrustumCameraParams` und `ViewportF` sind als kleine Shared-Datenstrukturen im Toolkit vorhanden; `DeviceContextProxy` besitzt native `SetViewport`-/`SetScissorRectangle`-Overloads fuer `ViewportF`. Der gefilterte Build meldet keine Fehler in den portierten RenderContext-/Geometry-/Offscreen-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `1063` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in D2D/DWrite.

Zusätzliche Prüfung der portierten `IEffectsManager.Device`-/3D-Device-Kante:

```powershell
rg -n "manager\.Device|EffectsManager\.Device|\.Device\.ImmediateContext|\.Device\.DeviceRemovedReason|IDevice3DResources\.Device|Device Device|using Device = SharpDX\.Direct3D11|using SharpDX\.Direct3D11" Source\HelixToolkit.SharpDX.Shared\Interface\IEffectsManager.cs Source\HelixToolkit.SharpDX.Shared\ShaderManager\EffectsManager.cs Source\HelixToolkit.SharpDX.Shared\Core\ScreenCloneRenderCore.cs Source\HelixToolkit.Wpf.SharpDX.Shared\Controls\DX11ImageSourceRenderHost.cs Source\HelixToolkit.SharpDX.Shared\Utilities\ScreenCapture.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `IDevice3DResources.Device` und `EffectsManager.Device` liefern jetzt `Native.SilkD3DDevice`. `EffectsManager` hält das SharpDX-D3D11-Device nur noch als explizite `LegacyDevice`-Übergangskante fuer noch nicht migrierte ScreenClone-/ScreenCapture-/D3DImage-Pfade; `ConstantBufferPool` und `TextureResourceManager` bleiben bis zu ihrer eigenen Buffer-/Texture-Kante auf diesem Legacy-Device. Der gefilterte Build meldet keine Fehler in `IEffectsManager.cs`, `EffectsManager.cs`, `ScreenCloneRenderCore.cs`, `DX11ImageSourceRenderHost.cs` oder `ScreenCapture.cs`. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `1059` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in D2D/DWrite, Core-Buffer-Modellen und nicht migrierten SharpDX-Math-/DXGI-Flächen.

Zusätzliche Prüfung der portierten BufferModel-/Batching-InputAssembler-Kante:

```powershell
rg -n "using (global::)?SharpDX\.(Direct3D11|Direct3D|DXGI);|global::SharpDX\.DXGI\.Format|SharpDX\.Direct3D11\.VertexBufferBinding|Format\.R32_UInt|Format\.Unknown|global::SharpDX\.Vector3" Source\HelixToolkit.SharpDX.Shared\Core\Buffers Source\HelixToolkit.SharpDX.Shared\Core\Batching Source\HelixToolkit.SharpDX.Shared\Interface\IGeometryBufferModel.cs Source\HelixToolkit.SharpDX.Shared\Render\DeviceContextProxy\DeviceContextProxy_InputAssembler.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `VertexBufferBinding` ist ein eigener nativer Binding-Typ, `SilkD3DDeviceContext`/`DeviceContextProxy_InputAssembler` binden Vertex- und Index-Buffer über Silk.NET `IASetVertexBuffers`/`IASetIndexBuffer`. Die zentralen Geometry-/Elements-/Mesh-/Line-/Point-/Billboard-/Sprite-/BoneSkin-BufferModels sowie die statischen Mesh-Batching-Pfade verwenden keine SharpDX-D3D11-/D3D-/DXGI-Imports mehr. `IGeometryBufferModel.CopySkinnedToArray` verwendet jetzt den zentralen `Vector3`-Alias statt `global::SharpDX.Vector3`. Der gefilterte Build meldet keine Fehler in den portierten Buffer-/Batching-/InputAssembler-/Interface-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `1022` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in D2D/DWrite, ScreenClone/Interop, weiteren Direct3D11-Dateien und qualifizierten SharpDX-Math-/Utility-Typen.

Zusätzliche Prüfung der portierten RenderHost-/Core2D-D2D-Resource-Grenze:

```powershell
rg -n "SharpDX\.Direct2D1|global::SharpDX\.Direct2D1|SharpDX\.DirectWrite|global::SharpDX\.DirectWrite|SharpDX\.WIC|global::SharpDX\.WIC" Source\HelixToolkit.SharpDX.Shared\Interface\IEffectsManager.cs Source\HelixToolkit.SharpDX.Shared\Interface\IRenderHost.cs Source\HelixToolkit.SharpDX.Shared\Render\RenderContext2D.cs Source\HelixToolkit.SharpDX.Shared\Core2D\Device2DProxy.cs Source\HelixToolkit.SharpDX.Shared\Utilities\Buffers\BitmapProxy.cs Source\HelixToolkit.SharpDX.Shared\ShaderManager\EffectsManager.cs Source\HelixToolkit.SharpDX.Shared\Render\RenderHost\RenderHostBase.cs Source\HelixToolkit.SharpDX.Shared\Render\RenderBuffers\DX11RenderBufferBase.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'D2DResourceHandles.cs|D3DResourceHandles.cs|D3DMathTypes.cs|IEffectsManager.cs|IRenderHost.cs|RenderContext2D.cs|Device2DProxy.cs|BitmapProxy.cs|EffectsManager.cs|RenderHostBase.cs|DX11RenderBufferBase.cs|EventArguments.cs'
```

Ergebnis: `IDevice2DResources`, `IRenderHost.Device2D`, `DX11RenderHostBase.Device2D`, `CreateRenderContext2D(...)`, `RenderContext2D`, `D2DTargetProxy` und `BitmapProxy` expose keine SharpDX-Direct2D-/DirectWrite-/WIC-Typen mehr. `D2DResourceHandles.cs` kapselt die minimalen eigenen Handles fuer `D2DFactory`, `D2DDevice`, `D2DDeviceContext`, `WICImagingFactory`, `DirectWriteFactory`, `D2DBitmap` und Bitmap-Properties; `Size2`, `Matrix3x2` und der eigene `DriverType` schliessen kleine oeffentliche Legacy-Typkanten. Die konkrete D2D-, DWrite- und WIC-Renderer-/Loader-Implementierung bleibt bewusst offen. Der gefilterte Build meldet keine Fehler in den portierten D2D-Grenzdateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `930` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in den noch nicht portierten konkreten Core2D-/DWrite-/WIC-Dateien, weiteren Direct3D11-/DXGI-Schichten, ScreenClone/Interop und qualifizierten SharpDX-Math-/Utility-Typen.

Zusätzliche Prüfung der portierten Core2D-/Scene2D-D2D-Typfläche:

```powershell
rg -n "SharpDX\.Direct2D1|SharpDX\.DirectWrite|SharpDX\.WIC|global::SharpDX\.Direct2D1|global::SharpDX\.DirectWrite|global::SharpDX\.WIC|using SharpDX;|\bD2D\." Source\HelixToolkit.SharpDX.Shared\Core2D Source\HelixToolkit.SharpDX.Shared\Model\Scene2D -g "*.cs"
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'Core2D|Scene2D|D2DResourceHandles.cs|D3DMathTypes.cs|Vector3DExtensions.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: Die konkreten Core2D-RenderCores, Segment-/Figure-Modelle und Scene2D-Nodes hängen nicht mehr an `SharpDX.Direct2D1`, `SharpDX.DirectWrite`, `SharpDX.WIC` oder unqualifizierten `using SharpDX`-Imports. `D2DResourceHandles.cs` stellt dafür eigene Brush-, StrokeStyle-, Geometry-, Text- und Bitmap-Wrapper sowie No-op-Zeichenmethoden auf `D2DDeviceContext` bereit; `D3DMathTypes.cs` enthält die benötigten `Size2F`-/`RectangleF`-Hilfstypen. `ImageNode2D.OnLoadImage(...)` ist bewusst nur ein Platzhalter, da WIC-/Bitmap-Decoding eine separate Kante bleibt. Der gefilterte Build meldet keine Fehler in den portierten Core2D-/Scene2D-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `763` Compilefehlern gemessen; die ersten Fehlergruppen liegen jetzt vor allem in verbleibenden Direct3D11-/DXGI-Imports, Extensions/Bitmap-/Animation-WIC-Flächen, Billboard-DWrite, RenderBuffer-/SwapChain-Interop und qualifizierten SharpDX-Math-Typen.

Zusätzliche Prüfung der portierten Billboard-/BitmapExtensions-/ImagePacker-DWrite-/WIC-Kante:

```powershell
rg -n "SharpDX\.Direct2D1|SharpDX\.DirectWrite|SharpDX\.WIC|global::SharpDX\.Direct2D1|global::SharpDX\.DirectWrite|global::SharpDX\.WIC|\bWicRenderTarget\b|\bRenderTarget\b|\bD2D\." Source\HelixToolkit.SharpDX.Shared\Extensions\BitmapExtensions.cs Source\HelixToolkit.SharpDX.Shared\Extensions\AnimationExtensions.cs Source\HelixToolkit.SharpDX.Shared\Utilities\ImagePacker Source\HelixToolkit.SharpDX.Shared\Model\Geometry\BillboardSingleText3D.cs Source\HelixToolkit.SharpDX.Shared\Model\Geometry\BillboardText3D.cs -g "*.cs"
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'BitmapExtensions.cs|AnimationExtensions.cs|ImagePacker|BillboardSingleText3D.cs|BillboardText3D.cs|D2DResourceHandles.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `BitmapExtensions`, `AnimationExtensions`, `SpritePackerBase`, `ImagePacker`, `TextInfoExtPacker`, `BillboardSingleText3D` und `BillboardText3D` verwenden in dieser Kante keine SharpDX-Direct2D-/DirectWrite-/WIC-Typen mehr. `D2DResourceHandles.cs` enthält dafür minimale Gradient-/Brush-/Bitmap-Wrapper; `BitmapExtensions.ToMemoryStream(...)` erzeugt einen einfachen Managed-BMP-Placeholder-Stream, damit Billboard-Texture-Streams weiterhin eine deterministische Ausgabe haben. Echtes Text-/Gradient-Rendering, WIC-Encoding und Image-Decoding bleiben bewusst separate Arbeiten. Der gefilterte Build meldet keine Fehler in den portierten Bitmap-/Billboard-/ImagePacker-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `725` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in verbleibenden Direct3D11-/DXGI-Imports, ScreenCapture-/ScreenClone-/Interop-Code, TextureLoader/WICHelper und qualifizierten SharpDX-Math-/Utility-Typen.

Zusätzliche Prüfung der portierten Material-/Scene-State-Signatur-Kante:

```powershell
rg -n "using SharpDX\.Direct3D11|using SharpDX\.DXGI|using global::SharpDX\.Direct3D11|global::SharpDX\.DXGI\.Format|global::SharpDX\.Direct2D1" Source\HelixToolkit.SharpDX.Shared\Model\Material Source\HelixToolkit.SharpDX.Shared\Model\Scene Source\HelixToolkit.SharpDX.Shared\Interface Source\HelixToolkit.SharpDX.Shared\Render\RenderHost\DefaultRenderHost.cs -g "*.cs"
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'DiffuseMaterialCore.cs|PhongMaterialCore.cs|PBRMaterialCore.cs|TextureModel.cs|VolumeTextureMaterial.cs|GeometryNode.cs|MeshNode.cs|LineNode.cs|BillboardNode.cs|IRenderer.cs|IRenderCoreParams.cs|DefaultRenderHost.cs|BatchedMeshNode.cs|PointNode.cs|ParticleStormNode.cs|ScreenQuadNode.cs|ViewBoxNode.cs|VolumeTextureNode.cs|ITextureModelRepository.cs|IMaterial.cs|PBRMaterialVariable.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: Die einfachen MaterialCore-, SceneNode- und Interface-State-Signaturen verwenden in dieser Kante keine SharpDX-D3D11-/DXGI-Imports oder qualifizierten `global::SharpDX.DXGI.Format`-Werte mehr. `RenderParameter.RenderTargetView`, `RenderParameter.DepthStencilView`, `RenderParameter.CurrentTargetTexture` und `RenderParameter2D.RenderTarget` zeigen auf eigene View-/Resource-/Bitmap-Typen; `DefaultRenderHost.OnRender(...)` erzeugt das RenderTargetView-Array mit dem eigenen `RenderTargetView`. `VolumeTextureRawDataMaterialCore.LoadRAWFile(...)` verwendet den eigenen `Format`. Der gefilterte Build meldet keine Fehler in den portierten Material-/Scene-/Interface-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `697` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in nicht portierten RenderCore-/PostEffect-/ScreenClone-Dateien, RenderBuffer-/SwapChain-Interop, TextureLoader/WICHelper, SharpDX.Toolkit-Graphics und qualifizierten SharpDX-Math-/Utility-Typen.

Zusätzliche Prüfung der RenderCore-/PostEffect-D3D11-/DXGI-Signatur-Kante:

```powershell
rg -n "SharpDX\.(Direct3D11|Direct3D|DXGI)|global::SharpDX\.(Direct3D11|Direct3D|DXGI)|using SharpDX\.(Direct3D11|Direct3D|DXGI)|using global::SharpDX\.(Direct3D11|Direct3D|DXGI)" Source\HelixToolkit.SharpDX.Shared\Core\DrawScreenQuadCore.cs Source\HelixToolkit.SharpDX.Shared\Core\ParticleRenderCore.cs Source\HelixToolkit.SharpDX.Shared\Core\Lights\LightCoreBase.cs Source\HelixToolkit.SharpDX.Shared\Core\MorphTargetUploaderCore.cs Source\HelixToolkit.SharpDX.Shared\Core\PostEffects Source\HelixToolkit.SharpDX.Shared\ShaderManager\BufferPool.cs Source\HelixToolkit.SharpDX.Shared\ShaderManager\ConstantBufferPool.cs Source\HelixToolkit.SharpDX.Shared\ShaderManager\TextureResourceManager.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'DrawScreenQuadCore|ParticleRenderCore|LightCoreBase|MorphTargetUploaderCore|PostEffectFXAA|PostEffectBlurCore|PostEffectBoomCore|PostEffectMeshXRay|PostEffectMeshXRayGrid|BufferPool.cs|ConstantBufferPool.cs|TextureResourceManager.cs|D3DResourceHandles.cs|D3DViewHandles.cs|D3DStateHandles.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: Die einfachen RenderCore- und PostEffect-Dateien `DrawScreenQuadCore`, `ParticleRenderCore`, `LightCoreBase`, `MorphTargetUploaderCore`, `PostEffectFXAA`, `PostEffectBlurCore`, `PostEffectBloomCore`, `PostEffectMeshXRayCore` und `PostEffectMeshXRayGridCore` verwenden in dieser Kante keine SharpDX-D3D11-/D3D-/DXGI-Imports mehr. `MorphTargetUploaderCore` erzeugt seine SRVs direkt ueber die native Buffer-Resource, und die kleinen ShaderManager-Pool-Signaturen sind von `SharpDX.Direct3D11.Device` gelöst, ohne `EffectsManager.LegacyDevice` selbst zu migrieren. Der gefilterte Build meldet keine Fehler in den portierten RenderCore-/PostEffect-/Pool-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `675` Compilefehlern gemessen; die ersten Fehlergruppen liegen weiterhin in ScreenClone-/ScreenCapture-/Interop-Code, RenderBuffer-/SwapChain-Interop, TextureLoader/WICHelper, SharpDX.Toolkit-Graphics und qualifizierten SharpDX-Math-/Utility-Typen.

Zusätzliche Prüfung der RenderBuffer-/SwapChain-D3D11-/DXGI-Signatur-Kante:

```powershell
rg -n "SharpDX\.(Direct3D11|Direct3D|DXGI)|global::SharpDX\.(Direct3D11|Direct3D|DXGI)|using SharpDX\.(Direct3D11|Direct3D|DXGI)|using global::SharpDX\.(Direct3D11|Direct3D|DXGI)" Source\HelixToolkit.SharpDX.Shared\Render\RenderBuffers Source\HelixToolkit.SharpDX.Shared\Render\RenderHost\SwapChainRenderHost.cs Source\HelixToolkit.SharpDX.Shared\Utilities\DepthStencilFormatHelper.cs Source\HelixToolkit.SharpDX.Shared\Native\DXGISwapChainHandles.cs Source\HelixToolkit.SharpDX.Shared\Native\D3DDeviceHandles.cs Source\HelixToolkit.SharpDX.Shared\Native\D3DResourceHandles.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'DX11RenderBufferBase.cs|DX11Texture2DRenderBufferProxy.cs|DX11SwapChainRenderBufferProxy.cs|DX11SwapChainCompositionRenderBufferProxy.cs|SwapChainRenderHost.cs|DepthStencilFormatHelper.cs|DXGISwapChainHandles.cs|D3DDeviceHandles.cs|D3DResourceHandles.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `DX11RenderBufferBase`, `DX11Texture2DRenderBufferProxy`, `DX11SwapChainRenderBufferProxy`, `DX11SwapChainCompositionRenderBufferProxy`, `SwapChainRenderHost` und `DepthStencilFormatHelper` verwenden in dieser Kante keine SharpDX-D3D11-/D3D-/DXGI-Imports mehr. `Native\DXGISwapChainHandles.cs` stellt eigene minimale SwapChain-/Present-/Description-Typen bereit. Der Texture2D-RenderBuffer erzeugt seine BackBuffer-Texture über `ShaderResourceViewProxy(DeviceResources, Texture2DDescription)` als native Texture2D mit SRV/RTV, und die SwapChain-RenderBuffer erzeugen vorerst native BackBuffer-Texture-Platzhalter über denselben Pfad. Echte Silk.NET-DXGI-SwapChain-Erzeugung, DXGI-Factory/Adapter-Ownership und D3DImage-Surface-Interop bleiben separate Kanten. Der gefilterte Build meldet keine Fehler in den portierten RenderBuffer-/SwapChain-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `648` Compilefehlern gemessen; die ersten Fehlergruppen liegen jetzt in ScreenClone-/ScreenCapture-Code, `SharpDX.Toolkit.Graphics`, TextureLoader/WICHelper und qualifizierten SharpDX-Math-/Utility-Typen.

Zusätzliche Prüfung der ScreenClone-/ScreenCapture-/D3DImage-Interop-Signatur-Kante:

```powershell
rg -n "SharpDX\.(Direct3D11|DXGI|WIC)|global::SharpDX\.(Direct3D11|DXGI|WIC|Result|IO|DataRectangle)|LegacyDevice" Source\HelixToolkit.SharpDX.Shared\Core\ScreenCloneRenderCore.cs Source\HelixToolkit.SharpDX.Shared\Utilities\ScreenCapture.cs Source\HelixToolkit.Wpf.SharpDX.Shared\Controls\DX11ImageSource.cs Source\HelixToolkit.Wpf.SharpDX.Shared\Controls\DX11ImageSourceRenderHost.cs Source\HelixToolkit.SharpDX.Shared\Extensions\IViewportExtensions.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'ScreenCloneRenderCore.cs|ScreenCapture.cs|DX11ImageSource.cs|DX11ImageSourceRenderHost.cs|IViewportExtensions.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `ScreenCloneRenderCore`, `ScreenCapture`, `DX11ImageSource`, `DX11ImageSourceRenderHost` und der Screenshot-Aufrufer in `IViewportExtensions` verwenden in dieser Kante keine SharpDX-D3D11-/DXGI-/WIC-Typen und keine `EffectsManager.LegacyDevice`-Abfrage mehr. `ScreenCloneRenderCore` bleibt als öffentliche `IScreenClone`-No-op-Grenze erhalten; echte Silk.NET-DXGI-Desktop-Duplication wird separat portiert. `ScreenCapture.CaptureTexture(...)` arbeitet auf `DeviceContextProxy` und nativen `Texture2D`-Wrappern; WIC-Datei-/Stream-Encoding liefert im migrierten Pfad vorerst kontrolliert `false`. `DX11ImageSource` hält native `Texture2D`-RenderTargets, setzt aber noch keine echte `IDirect3DSurface9`-BackBuffer-Bridge. Der gefilterte Build meldet keine Fehler in den portierten ScreenClone-/ScreenCapture-/D3DImage-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `617` Compilefehlern gemessen; die ersten Fehlergruppen liegen jetzt in `DefaultVertexShaders`, `ContextSharedResource`, `LightsBufferModel`, `EffectsManager`, `SharpDX.Toolkit.Graphics`, TextureLoader/WICHelper und qualifizierten SharpDX-Math-/Utility-Typen.

Zusätzliche Prüfung der einfachen Shader-/ResourceManager-D3D11-/DXGI-Restkante:

```powershell
rg -n "SharpDX\.(Direct3D11|Direct3D|DXGI|Toolkit)|global::SharpDX\.(Direct3D11|Direct3D|DXGI|Toolkit)|using SharpDX|using global::SharpDX|LegacyDevice|Factory1|Adapter\b|DeviceCreationFlags|GetSupportedFeatureLevel|ToLegacyDriverType" Source\HelixToolkit.SharpDX.Shared\DefaultShaders\DefaultVertexShaders.cs Source\HelixToolkit.SharpDX.Shared\Model\ContextSharedResource.cs Source\HelixToolkit.SharpDX.Shared\Model\Lights\LightsBufferModel.cs Source\HelixToolkit.SharpDX.Shared\ShaderManager\EffectsManager.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'DefaultVertexShaders.cs|ContextSharedResource.cs|LightsBufferModel.cs|EffectsManager.cs|D3DShaderHandles.cs|ConstantBufferPool.cs|TextureResourceManager.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `DefaultVertexShaders`, `ContextSharedResource`, `LightsBufferModel` und die einfache `EffectsManager`-Device-/Pool-Erzeugung verwenden in dieser Kante keine SharpDX-D3D11-/D3D-/DXGI-/Toolkit-Typen mehr. Die Default-InputLayouts verwenden die eigenen `InputElement`-/`InputClassification`-Wrapper; `LightsBufferModel` verwendet native `MapMode`/`MapFlags`; `EffectsManager` erzeugt nur noch `NativeDeviceResources` über `SilkD3D11DeviceFactory` und gibt `ConstantBufferPool`/`TextureResourceManager` das native `SilkD3DDevice`. Die echte DXGI-Adapterauswahl bleibt offen, weil `SilkD3D11DeviceFactory.CreateDefault(...)` aktuell noch keinen `IDXGIAdapter` übernimmt. Der gefilterte Build meldet keine Fehler in den portierten Shader-/ResourceManager-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `607` Compilefehlern gemessen; die ersten Fehlergruppen liegen jetzt in `SharpDX.Toolkit.Graphics`, TextureLoader/WICHelper, Shared-WPF-Geometrie-Imports im Core-Projekt und qualifizierten SharpDX-Math-/Utility-Typen.

Zusätzliche Prüfung der `SharpDX.Toolkit.Graphics`-Texturcontainer-Kante:

```powershell
rg -n "using SharpDX|using global::SharpDX|global::SharpDX\.(Direct3D11|Direct3D|DXGI|IO|Multimedia|WIC)|SharpDX\.(Direct3D11|Direct3D|DXGI|IO|Multimedia|WIC)|NativeFileStream|NativeFileMode|NativeFileAccess" Source\HelixToolkit.SharpDX.Shared\SharpDX.Toolkit Source\HelixToolkit.SharpDX.Shared\Utilities\TextureLoader.cs
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'SharpDX.Toolkit\\|SharpDX.Toolkit\\Graphics|TextureLoader.cs|DDS.cs|DDSHelper.cs|DepthFormat.cs|GraphicsResource.cs|Image.cs|ImageDescription.cs|PixelBuffer.cs|PixelFormat.cs|Texture.cs|Texture1D.cs|Texture1DBase.cs|Texture2D.cs|Texture2DBase.cs|Texture3D.cs|Texture3DBase.cs|TextureCube.cs|TextureDescription.cs|WICHelper.cs|Component.cs'
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: Die Legacy-Toolkit-Texturcontainer `Texture`, `Texture1D`, `Texture2D`, `Texture3D`, `TextureCube`, `GraphicsResource`, `TextureDescription`, `PixelBuffer` und `Image` verwenden in dieser Kante keine SharpDX-D3D11-/DXGI-/IO-/WIC-/Multimedia-Typen mehr. Die Wrapper erzeugen native Texture-Resources über `SilkD3DDevice`, `TextureLoader` akzeptiert native Devices und gibt native Resources/SRVs zurück. `DDSHelper` und `WICHelper` sind vorerst explizite `NotSupportedException`-Grenzen; echtes DDS-/WIC-Decoding und Encoding bleibt eine separate Loader-Kante. Der gefilterte Build meldet keine Fehler in den portierten Toolkit-/TextureLoader-Dateien. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `346` Compilefehlern gemessen; die ersten Fehlergruppen liegen jetzt in qualifizierten SharpDX-Math-Referenzen (`StLReader`, `LineBuilder`, `Interfaces`) und Shared-WPF-Geometrie-Imports im `HelixToolkit.SharpDX.Core`-Buildpfad.

Zusätzliche Prüfung der qualifizierten SharpDX-Math- und Shared-Geometrie-Kante:

```powershell
rg -n "using SharpDX|using global::SharpDX|global::SharpDX\.(BoundingBox|BoundingSphere|Matrix|Vector)" Source\HelixToolkit.SharpDX.Shared\Utilities\ImportExport Source\HelixToolkit.SharpDX.Shared\Utilities\LineBuilder.cs Source\HelixToolkit.SharpDX.Shared\Interface\Interfaces.cs Source\HelixToolkit.SharpDX.Shared\Utilities\Octrees Source\HelixToolkit.SharpDX.Shared\Model\Scene
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: Die ersten qualifizierten Math-Blocker in `StLReader`, `ObjReader`, `OffReader`, `LineBuilder`, `Interfaces`, `CMOReader`, Octree-/Scene-Hilfstypen und `BoundingSphereExtensions` sind von lokalen SharpDX-Aliasen gelöst. Die Shared-Geometrie-Dateien verwenden im `SILKNET`-Pfad die SharpDX/Core-Branch-Struktur mit `Silk.NET.Maths`-Aliases statt WPF-Geometrietypen. `SilkNetLegacyTypes` ergänzt Übergangsformen für `Color`, `PlaneIntersectionType`, `ContainmentType`, `DoubleKeyDictionary`, `Ray`, `Plane`, `Collision` und eine SharpDX-förmige `BoundingBox`-Oberfläche (`Minimum`, `Maximum`, `Merge`, `FromSphere`, `Contains`, `Intersects`, `GetCorners`). Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `918` Compilefehlern gemessen. Die Fehlerzahl ist nicht direkt mit `346` vergleichbar, weil der Compiler nach den gelösten Deklarationsblockern tiefer in Core-/Shared-Geometrie-Methoden kommt; die ersten Fehlergruppen liegen jetzt konsistent in `SharpDX.Toolkit.Graphics` (`FormatHelper`, `Utilities`, DXGI-`Format`-Enum-Namen) und weiteren Silk-Math-API-Unterschieden wie `Normalize`, `LengthSquared`, `Vector2.Multiply`, `Vector3.Subtract`, `BoundingSphere.Merge` und `Vector3.Transform*`.

Zusätzliche Prüfung der `SharpDX.Toolkit.Graphics`-Format-/Utilities-Kante:

```powershell
rg -n "FormatHelper\.|Utilities\.|Format\.[A-Za-z0-9_]+" Source\HelixToolkit.SharpDX.Shared\SharpDX.Toolkit\Graphics
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly 2>&1 | Select-String -Pattern 'SharpDX.Toolkit\\Graphics|ToolkitCompatibility.cs|PixelFormat.cs|Texture1DBase.cs|Texture2DBase.cs|Texture3DBase.cs|D3DResourceHandles.cs|D3DStateHandles.cs'
```

Ergebnis: `SharpDX.Toolkit.Graphics` besitzt jetzt eine `SILKNET`-Kompatibilitätsschicht für die alten SharpDX-DXGI-Formatnamen, `FormatHelper` und `Utilities`. `PixelFormat` konvertiert direkt zu/von `Silk.NET.DXGI.Format`, sodass die Toolkit-Texture-Container weiter native `Texture1D`/`Texture2D`/`Texture3D`-Descriptions befüllen können. Der Gesamtbuild bleibt rot und wurde nach diesem Schnitt mit `792` Compilefehlern gemessen. Der gefilterte Build zeigte danach noch einen Toolkit-Restfehler in `Texture.cs` (`Format` nach `int`), der durch eine explizite `Format`-Konvertierung behoben wurde; eine erneute Buildmessung war wegen Approval-/Usage-Limit in dieser Sitzung nicht möglich. Die verbleibenden gefilterten Fehler liegen jetzt in `Native\D3DResourceHandles.cs` und `Native\D3DStateHandles.cs`, wo fixed buffer/pointer-Zugriffe noch als `unsafe` markiert werden müssen. Die breite Restkante liegt weiterhin in Silk-Math-API-Unterschieden wie `Normalize`, `Dot`, `Cross`, `Transform*`, `Vector2.Multiply`, `Vector3.Min/Max` und Matrix-/Bounding-Frustum-Kompatibilität.

Zusätzliche Prüfung der nativen fixed-buffer-/Pointer-Kante:

```powershell
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `D3DResourceHandles.ToDataBox(MappedSubresource)` und `D3DStateHandles.ToSilkDesc(SamplerStateDescription)` kapseln ihre Pointer- bzw. fixed-buffer-Zugriffe jetzt in `unsafe`-Methodenkontexten. Der Build meldet keine Fehler mehr in `D3DResourceHandles.cs` oder `D3DStateHandles.cs`; der Gesamtstand sinkt auf `786` Compilefehler. Die ersten Fehlergruppen liegen nun in Silk-Math-Kompatibilität (`Normalize`, `Dot`, `Cross`, `Transform*`, `Vector2.Multiply`, `Vector3.Min/Max`, Matrix- und Bounding-Frustum-Oberflächen) sowie einer kleinen `TextFormat`-Signaturabweichung.

Zusätzliche Prüfung der ersten zentralen Silk-Math-Kompatibilitätskante:

```powershell
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `SilkNetMathCompatibility.cs` stellt zentrale `SilkMath`-Operationen für Normalize, Dot, Cross, Min/Max, Clamp, Lerp, Add/Subtract, DistanceSquared, Transform/TransformCoordinate/TransformNormal, Matrix-Invertierung sowie Translation/Achsenrotation bereit. Mutierende SharpDX-Aufrufe wie `vector.Normalize()`, `vector.LengthSquared()` und `matrix.Invert()` werden über globale `SILKNET`-Extensions weiter unterstützt. Controller-, Kamera-, Ray/Plane-, Bounding-, Batching-, Licht-, SSAO-, LineBuilder-, Buffer- und erste Scene-Callsites verwenden die neue Schicht. Der Gesamtbuild sinkt von `786` auf `601` Compilefehler; `SilkNetMathCompatibility.cs` selbst meldet keine Fehler. Die erste Restkante liegt nun bei einer echten `BoundingFrustum`-Kompatibilitätsform statt des aktuellen `Box3D<float>`-Aliases, gefolgt von Color/Color4-Oberflächen und weiteren Matrix-Fabrikmethoden (`Scaling`, `RotationQuaternion`, `Translation`, Achsenrotationen).

Zusätzliche Prüfung der erweiterten Math-/DXGI-Kompatibilitätskante:

```powershell
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `BoundingFrustum` besitzt jetzt eine eigene Legacy-Oberfläche mit sechs normalisierten Frustum-Planes. `Color`/`Color4`, Matrix-Fabriken, LH/RH-Kamera- und Projektionsmatrizen, `Rectangle`/`RectangleF`, `Matrix3x2`, Zufallsvektoren und die im Rendering verwendeten DXGI-Formatnamen sind an Silk.NET angepasst. `SampleDescription` unterstützt wieder den bisherigen Zwei-Argument-Konstruktor. Der Gesamtbuild sinkt von `601` auf `183` Compilefehler. Die verbleibenden Hauptgruppen liegen in BoundingSphere-/Ray-Schnitt- und Merge-Operationen, noch nicht umgestellten Geometrie-/Octree-`Transform*`-Aufrufen, Matrix-Decomposition, einzelnen nativen View-/DeviceContext-Details und Legacy-Exception-/Importer-Helfern.

Zusätzliche Prüfung der BoundingSphere-/Ray-/BoundingBox-Kompatibilitätskante:

```powershell
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: `BoundingSphereExtensions` stellt `FromBox`, `Merge` und Ray-Schnittprüfung bereit. `RayExtensions` unterstützt Box-/Sphere-Schnittprüfungen für `ref`- und Wertargumente. Die lokale `BoundingBox`-Oberfläche besitzt zusätzlich `Size`, Wertgleichheit und Hashing. Die betroffenen Geometry-, Billboard-, Batching- und Particle-Callsites verwenden diese Kompatibilität. Der Gesamtbuild sinkt von `183` auf `154` Compilefehler. Die größte verbleibende Math-Gruppe besteht aus statischen SharpDX-förmigen `Vector2`-/`Vector3`-Aufrufen (`TransformCoordinate`, `TransformNormal`, `Dot`, `Cross`, `Length`) in Geometry- und Octree-Pfaden; daneben bleiben Matrix-Decomposition, native View-/DeviceContext-Details und Legacy-Exception-/Importer-Helfer offen.

Zusätzliche Prüfung der Vector-/Geometry-/Octree-Transform-Kante:

```powershell
dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly
```

Ergebnis: Geometry-, Billboard-, Scene-, Viewport-, Shared-Geometry- und Octree-Pfade verwenden für `TransformCoordinate`, `TransformNormal`, `Transform`, `Dot`, `Cross`, `Normalize`, `Clamp` und `DistanceSquared` die zentrale `SilkMath`-Schicht. Silk-Vektorlängen werden über die `Length`-Property gelesen. Matrix-Decomposition und Quaternion-Winkel sind zentral über `System.Numerics` adaptiert. `Half4` verwendet echte 16-Bit-Float-Komponenten und unterstützt die bisherige Float-/Vector4-Zuweisung. Der Gesamtbuild sinkt von `154` auf `28` Compilefehler. Die verbleibenden Gruppen liegen in nativen Device-/View-Details, RenderCore-State-Signaturen, Importer-Helfern und RenderHost-Exception-/DXGI-Kompatibilität.

Stand dieses Implementierungsschnitts:

- Die Silk.NET-Device-Erzeugung ist als interner Parallelpfad vorhanden.
- Die Ownership für `ID3D11Device` und `ID3D11DeviceContext` läuft über `Silk.NET.Core.Native.ComPtr<T>`.
- `EffectsManager` hat eine erste native Resource-Grenze.
- Der Immediate-Renderpfad kann den neuen nativen Kontext erreichen.
- Draw-, InputAssembler-, Viewport-, Output-Merger-/Target-, ResourceUpdate- und ShaderResource-/UAV-Teile des `DeviceContextProxy` laufen teilweise über Silk.NET.
- `Texture1D`, `Texture2D` und `Texture3D` sind als native Resource-Wrapper vorbereitet; `ShaderResourceViewProxy` erzeugt fuer native Device-Resource-Kontexte Texture2D-Resources sowie echte SRV/RTV/DSV-Views.
- Blend-, DepthStencil-, Rasterizer- und Sampler-States sind als native Wrapper vorbereitet; `StatePoolManager`, `StateProxy`, `DeviceContextProxy_States` und Sampler-Bindings laufen in dieser Kante über Silk.NET.
- Vertex-, Pixel-, Compute-, Domain-, Hull- und Geometry-Shader sowie InputLayouts sind als native Wrapper vorbereitet; `ShaderPoolManager`, `ShaderReflector`, `InputLayoutProxy` und Shader-/ConstantBuffer-Bindings laufen in dieser Kante ohne SharpDX-D3D11-/D3DCompiler-Typen.
- `DeviceContextPool` erzeugt native Deferred Contexts, und `DeferredContextRenderer`/`RenderTaskScheduler` verwenden native Command Lists.
- `IRenderTechnique.Device`/`Technique.Device` sind von SharpDX-Device-Typen gelöst und zeigen auf `Native.SilkD3DDevice`.
- `IRenderHost.Device`, `DX11RenderHostBase.Device` und die einfachen RenderHost-/Material-Device-Aufrufer sind von SharpDX-D3D11-Device-Typen gelöst und verwenden `NativeDeviceResources`.
- `RenderCore.Device` und einfache RenderCore-Texture-/CubeMap-/Volume-Texture-Pfade sind von SharpDX-D3D11-Device-Typen gelöst und verwenden native Texture/SRV/RTV/DSV-Wrapper.
- `RenderContext` und die einfache Geometry-/Offscreen-RenderCore-Kante sind von SharpDX-D3D11-Imports und SharpDX-DXGI-Formatparametern gelöst.
- `IEffectsManager.Device`/`IDevice3DResources.Device` sind von SharpDX-D3D11-Device-Typen gelöst; die verbleibenden SharpDX-D3D11-Device-Verwendungen sind explizite Legacy-Interop-/ResourceManager-Kanten.
- Die zentrale BufferModel-/Batching-InputAssembler-Kante ist von SharpDX-D3D11-/D3D-/DXGI-Typen gelöst; `DeviceContextProxy` besitzt native Vertex-/Index-Buffer-Bindings.
- Die RenderHost-/Core2D-D2D-Resource-Grenze ist von SharpDX-Direct2D-/DirectWrite-/WIC-Signaturen gelöst; die konkrete 2D-Renderer-, Text-, WIC- und Bitmap-Loading-Implementierung bleibt eine eigene Portierungskante.
- Die konkrete Core2D-/Scene2D-D2D-Typfläche ist von SharpDX-Direct2D-/DirectWrite-/WIC-Imports gelöst; echte Direct2D-Zeichnung und WIC-Decoding sind weiter Platzhalter bzw. separate Kanten.
- Die Billboard-/BitmapExtensions-/ImagePacker-DWrite-/WIC-Kante ist von SharpDX-Direct2D-/DirectWrite-/WIC-Imports gelöst; Managed-BMP-Streams sind vorerst Platzhalter für spätere echte Text-/Bitmap-Encoding-Implementierung.
- Die einfache Material-/Scene-State-Signatur-Kante ist von SharpDX-D3D11-/DXGI-Imports gelöst; Material- und SceneNode-Properties verwenden die eigenen State-/Format-/View-Typen.
- Die einfache RenderCore-/PostEffect-D3D11-/DXGI-Signatur-Kante ist von SharpDX-D3D11-/D3D-/DXGI-Imports gelöst; die verbleibenden Treffer in diesem Umfeld liegen in bewusst ausgeklammerten ScreenClone-/RenderBuffer-/SwapChain-/EffectsManager-Interop-Pfaden.
- Die RenderBuffer-/SwapChain-D3D11-/DXGI-Signatur-Kante ist von SharpDX-D3D11-/D3D-/DXGI-Imports gelöst; Offscreen-BackBuffer-Textures werden nativ erzeugt, echte DXGI-SwapChain- und D3DImage-Surface-Interop bleiben noch Platzhalter bzw. separate Kanten.
- Die ScreenClone-/ScreenCapture-/D3DImage-Interop-Signatur-Kante ist von SharpDX-D3D11-/DXGI-/WIC-Typen und `LegacyDevice`-Abfragen gelöst; echte Desktop-Duplication, WIC-Encoding und D3D9Ex-BackBuffer-Interop bleiben separate Implementierungskanten.
- Die einfache Shader-/ResourceManager-D3D11-/DXGI-Restkante ist von SharpDX-D3D11-/D3D-/DXGI-/Toolkit-Typen gelöst; `EffectsManager` erzeugt keine SharpDX-D3D11-Device-Instanz mehr.
- Die `SharpDX.Toolkit.Graphics`-Texturcontainer-Kante ist von SharpDX-D3D11-/DXGI-/IO-/WIC-/Multimedia-Typen gelöst; DDS-/WIC-Load/Save-Backends bleiben vorerst kontrollierte Platzhalter.
- Die erste qualifizierte SharpDX-Math- und Shared-Geometrie-Kante ist gelöst; der Compiler erreicht jetzt die nächste breite Compatibility-Front in Toolkit-`Format`/`Utilities` und generischen Silk-Math-Methodenunterschieden.
- Die `SharpDX.Toolkit.Graphics`-Format-/Utilities-Kante ist auf lokale SharpDX-kompatible `Format`-, `FormatHelper`- und `Utilities`-Typen gelegt; die direkte native Format-Konvertierung bleibt erhalten.
- Die nativen fixed-buffer-/Pointer-Kontexte in `D3DResourceHandles` und `D3DStateHandles` sind korrekt als `unsafe` gekapselt.
- Eine zentrale Silk-Math-Kompatibilitätsschicht deckt die häufigsten Vector2/3/4- und Matrix-Operationen ab; die ersten Controller-, Geometry-, Extension-, Buffer- und Scene-Callsites sind darauf migriert.
- `BoundingFrustum`, Color/Color4, Matrix-Fabriken, DXGI-Formatnamen und die grundlegende 2D-Math-Oberfläche sind migriert.
- BoundingSphere-/Ray-Schnitt- und Merge-Operationen sowie die grundlegende BoundingBox-Wertoberfläche sind migriert.
- Die statischen Vector-/Geometry-/Octree-Transform-Aufrufe sowie Matrix-Decomposition und Quaternion-Winkel sind migriert.
- Der nächste Umbau muss die nativen Device-/View- und RenderCore-State-Signaturabweichungen schließen.

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

1. Native View-/DeviceContext-Details sowie RenderCore-State-Typabweichungen bereinigen.
2. Legacy-Exception-, String-Parsing- und Importer-Helfer portieren.
3. RenderHost-DXGI-Fehlerbehandlung und verbleibende Utility-Kompatibilität schließen.
4. TextureLoader-/WICHelper-WIC-Pfade separat portieren oder, wo möglich, durch WPF/BCL-Imaging ersetzen.
5. Echte Silk.NET-DXGI-SwapChain-Factory/Adapter-Ownership, Desktop-Duplication und D3DImage-BackBuffer-Interop ergänzen.
6. Tests von `SharpDX.Diagnostics.ObjectTracker` und SharpDX-Math-Typen entkoppeln.

## Phase 0: Baseline und Inventar

Ziel: Den aktuellen Zustand messbar sichern, bevor SharpDX entfernt wird.

### Fortschritt

Status: Teilweise erledigt.

- Supported Scope und Nicht-Scope sind in diesem Dokument abgegrenzt.
- SharpDX-PackageReferences und zentrale SharpDX-Code-Referenzen wurden für den aktuellen Migrationsschnitt geprüft.
- Der aktuelle Buildstatus ist dokumentiert: Nach dem State-Wrapper-Schnitt scheitert `dotnet build Source\HelixToolkit.Wpf.SharpDX\HelixToolkit.Wpf.SharpDX.csproj --no-restore --no-incremental -m:1 -p:UseSharedCompilation=false -clp:ErrorsOnly` mit `1225` Fehlern.
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
- Erste native Wrapper existieren für RenderTargetView, DepthStencilView, ShaderResourceView, UnorderedAccessView, Resource, Buffer sowie Texture1D/2D/3D.
- Offene Arbeit: Ownership-Regeln noch vollständig dokumentieren und Shader-, State-, DXGI-, D3D9- und D2D/DWrite/WIC-Objekte aus SharpDX-`ComObject`-Semantik lösen.

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
- Texture-/View-Erzeugung ist fuer Offscreen-RenderBuffer teilweise migriert: native Texture1D/2D/3D-Wrapper, Texture2D-Erzeugung sowie native RTV/DSV/SRV-Erzeugung in `ShaderResourceViewProxy` und `ColorBufferPool`.
- State-Erzeugung und State-Bindings sind teilweise migriert: native Blend-/DepthStencil-/Rasterizer-/Sampler-Wrapper, `StatePoolManager`, `StateProxy`, `DeviceContextProxy_States` und Sampler-Bindings.
- Shader-/InputLayout-Erzeugung ist teilweise migriert: native Shader- und InputLayout-Wrapper, `ShaderPoolManager`, `ShaderReflector`, `InputLayoutProxy` sowie Shader-/ConstantBuffer-Bindings.
- Deferred-Kontexte und Command Lists sind teilweise migriert: `DeviceContextPool`, `DeviceContextProxy.FinishCommandList`, `DeviceContextProxy.ExecuteCommandList`, `DeferredContextRenderer` und `RenderTaskScheduler` verwenden native Silk.NET-D3D11-Wrapper.
- Technique-Device-Kante ist teilweise migriert: `IRenderTechnique.Device` und `Technique.Device` liefern `Native.SilkD3DDevice`, `Technique` hat keinen SharpDX-Device-Konstruktorparameter mehr.
- RenderHost-Device-Kante ist teilweise migriert: `IRenderHost.Device` und `DX11RenderHostBase.Device` liefern `Native.SilkD3DDevice`, `IRenderHost.FeatureLevel` verwendet den eigenen `FeatureLevel`-Typ, und einfache Material-/BoneSkin-Aufrufer verwenden `NativeDeviceResources`.
- RenderCore-Device-Kante ist teilweise migriert: `RenderCore.Device` liefert `Native.SilkD3DDevice`; OIT-, ShadowMap-, SkyBox-/SkyDome-, DynamicCubeMap- und Volume-Texture-Pfade verwenden native Texture/SRV/RTV/DSV-Erzeugung, soweit sie keine DDS/WIC/Stream-Texture-Loader brauchen.
- BufferModel-/Batching-InputAssembler-Kante ist teilweise migriert: `VertexBufferBinding`, Vertex-/Index-Buffer-Bindings, zentrale Geometry-/Elements-/Mesh-/Line-/Point-/Billboard-/Sprite-/BoneSkin-BufferModels und statische Mesh-Batching-Pfade verwenden native Buffer-/Format-/Topology-Typen.
- Offene Arbeit: konkrete Core2D-/DWrite-/WIC-Implementierungen, echte SwapChain-/D3DImage-BackBuffer-Interop, Desktop-Duplication und verbleibende EffectsManager-/Texture-/Utility-Typen hängen noch an SharpDX-Typen.

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

Status: In Arbeit.

- D2D/DWrite/WIC bleiben eine der dominanten Fehlergruppen im aktuellen Build.
- Die Toolkit-Texture-Wrapper sind auf native D3D11-Resources umgestellt; echte DDS-/WIC-Decoding- und Encoding-Backends bleiben offen.

### Aufgaben

- Direct2D device/factory/context über Silk.NET.Direct2D erstellen.
- DirectWrite-Zugriffe ersetzen oder über passende Windows/Silk.NET-Interop-Schicht kapseln.
- WIC-Helper ersetzen:
  - Für WPF bevorzugt WPF Imaging APIs nutzen, wenn keine native WIC-Semantik nötig ist.
  - Native WIC nur dort kapseln, wo GPU-Texture-Upload oder ScreenCapture es verlangt.
- `SharpDX.Toolkit.Graphics`-Layer bereinigen:
  - DDS-Parsing behalten.
  - Texture-Wrapper auf Silk.NET resource creation umstellen. (erledigt)
  - PixelBuffer/DataBox-Äquivalente sauber kapseln. (teilweise erledigt)
- ScreenCapture mit Silk.NET staging textures und WPF/Windows encoding validieren.

### Gate

- Text/Billboard Rendering funktioniert.
- 2D Overlay-Beispiel funktioniert.
- DDS und Standard-Bildformate laden.
- Screenshot/Viewport export funktioniert für D3DImage- und SwapChain-Pfad.

## Phase 8: Shader Reflection und Shader-Pipeline

Ziel: Bestehende `.cso`-Shader weiterverwenden und Reflection-Daten ohne SharpDX laden.

### Fortschritt

Status: In Arbeit.

- `ShaderReflector` nutzt `D3DReflect` über `d3dcompiler_47.dll`, weil `Silk.NET.Direct3D.Compilers` 2.23.0 keine D3D11-Reflection-Wrapper bereitstellt.
- Shader creation für alle sechs Shader-Stages und InputLayout-Erzeugung laufen über Silk.NET.Direct3D11.
- Shader-Pools und `DeviceContextProxy`-Shader-/ConstantBuffer-Bindings sind auf native Handles umgestellt.
- Offene Arbeit: konkrete Core2D-/DWrite-/WIC-Implementierungen, Legacy-Device-Interop-Kanten und weiter entfernte Buffer-/Utility-Aufrufer hängen noch an SharpDX-Typen.

### Aufgaben

- Shader bytecode loading aus embedded resources beibehalten.
- `ShaderReflector` auf `D3DReflect` umstellen.
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
