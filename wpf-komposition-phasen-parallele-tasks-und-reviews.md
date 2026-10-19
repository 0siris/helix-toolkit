# WPF-Komposition: Phasen, parallele Tasks und Reviews

## Verbindliche Anforderungen

- **Standard:** `EnableSwapChainRendering = false`, entsprechend dem ursprünglichen HelixToolkit. `true` aktiviert den bestehenden HWND-Pfad.
- Auswahl ausschließlich vor der ersten erfolgreichen Präsentationsinitialisierung; danach abweichende Änderungen eindeutig ablehnen. Reload und Template-Neuaufbau behalten die Auswahl.
- Ein gemeinsamer D3D12-Renderer ohne duplizierte Szenen-, Material- oder Effektlogik. Bestehende öffentliche APIs und Namespaces erhalten.
- **Geringe Heap-Belastung:** Neue Steuerungs- und Renderpfade erzeugen nach dem Aufwärmen keine vermeidbaren Allokationen pro Tick/Frame. Puffer, Listen, Delegates und Kontexte wiederverwenden; LINQ, Closures, Boxing und neue Task-/Event-Objekte im häufigen Pfad vermeiden. Messungen trennen Initialisierung, Kapazitätswachstum und explizites Capture vom stabilen Rendering.
- **WPF rendert bedarfsgesteuert:** Ohne Änderungsbedarf keine Szenenvorbereitung, GPU-Renderarbeit, Interop-Kopie oder erneute Bildveröffentlichung.
- `MinimumUpdateCount` bleibt standardmäßig **6**: sechs Frames insgesamt pro isolierter Invalidierung. `0` und negative Werte erlauben weiterhin einen angeforderten Frame; negative Werte werden historisch auf null begrenzt.
- Laufende Animationen, Partikel, Desktop-Capture und `ContinuousRenderNode` dürfen ausdrücklich weitere Frames anfordern.
- Das heutige HWND-Verhalten bleibt erhalten. Historisch verwendeten beide Präsentationswege eine Renderbedarfsprüfung.
- Normale WPF-Ticks dürfen zur Kameraaktualisierung sowie zur begrenzten Prüfung laufender Arbeit bestehen bleiben. Permanente GPU-Ausgabe ist daraus nicht abzuleiten.
- Keine regelmäßigen CPU-Readbacks für die neue Präsentation und kein stiller HWND-Ersatzpfad.
- Gezielte sichtbare Testfenster sind freigegeben. Keine Änderungen an MeshMeister-Anwendungscode und kein Push.
- Bestehende MSAA- und hochauflösende Capture-Abweichungen werden dokumentiert; ihre vollständige Rendererimplementierung gehört nicht zu diesem Präsentationsumbau.

### Arbeitsteilung und Reviews

- Ein Integrationsverantwortlicher **I**, höchstens drei gleichzeitig schreibende Subagenten **A/B/C**, anschließend unabhängiger Reviewer **R**.
- Schreibende Tasks verwenden getrennte Worktrees vom selben freigegebenen Phasencommit. Jede Datei besitzt genau einen verantwortlichen Bearbeiter.
- I besitzt zentrale Viewport-Dateien, Projektdateien, Dokumentation und generierte Ausgabe.
- Gemeinsame Schnittstellen vor parallelen Implementierungen festlegen. Änderungen außerhalb des Dateibesitzes mit I koordinieren.
- Builds, GPU-Prüfungen und sichtbare Tests führt I seriell auf dem integrierten Stand aus.
- Delegation über `delegate_task` mit `isolation: "worktree"`; Provider und Modell aus dem aktuellen Orchestrator-Katalog.
- **Jede Phase endet mit Integration, Prüfungen, unabhängigem Review und lokalem Commit. Danach auf deine ausdrückliche Freigabe warten.**
- Das unabhängige Review enthält ab Phase 2 ausdrücklich einen Heap-/Allokationscheck mit Messnachweisen und Grenzen; der ruhende Pfad ist gesondert zu prüfen.

## Phase 0 – Arbeitsbasis und historische Verträge

**Ziel:** Geprüfte Ausgangsbasis und vollständige Zuordnung der Review-Findings.

- **P0.1 · I · Repository aktualisieren:** Arbeitsbaum, Index und Submodule prüfen; Branch per Fast-forward auf `72557068603fbe3017dc045782e24631970a4fb1` bringen. Bei Divergenz stoppen. Fremde Änderungen einschließlich MeshMeisters uncommittierter Dokumentation erhalten.
- **P0.2 · A · Historische Verträge bestätigen:** Original `c59fcc424` und Altpfad `6215e5c0b^` anhand der Review-Fundstellen prüfen: Renderbedarf, DPI-Abschaltung, Canvas, Frontbuffer, Eingabe und Ressourcenbesitz.
- **P0.3 · B · Integrationsstellen erfassen:** Gemeinsamen Renderdurchlauf, Invalidierungsquellen, EffectsManager-Ereignisse, kontinuierliche Bildquellen und gemeinsam genutzte Szenen zuordnen.
- **P0.4 · C · Prüf- und Fähigkeitsbestand erfassen:** Bestehende Tests zuordnen; HWND-spezifische Tests mit künftig explizitem `true` markieren. MSAA, angeforderte Capture-Auflösung und Software-/WARP-Unterstützung als bestehende Grenzen erfassen.
- **P0.5 · I · Baseline und Aufgaben festhalten:** Build und Standardtests ausführen; Ergebnisse, Schnittstellenverantwortung und Dateibesitz dokumentieren. Gespeicherten Phasenplan und OKF-Verweise konsistent halten.

**Parallel:** P0.2–P0.4 nach P0.1, ausschließlich lesend.

**Review:** Ausgangscommit und vorhandene Änderungen stimmen; jedes Finding besitzt einen Task oder eine ausdrücklich dokumentierte Grenze.

**Haltepunkt:** Freigabe für Phase 1.

## Phase 1 – GPU-Interop nachweisen

**Ziel:** Funktionierende GPU-Übergabe vor dem Rendererumbau.

- **P1.1 · A · Native Probe:** D3D12-BGRA-Textur mit NT-Shared-Handle in D3D11 öffnen; GPU-Kopie in eine separat mit D3D9Ex geteilte BGRA-Textur; Veröffentlichung über `D3DImage`. Native Komponenten und eigene Tests gehören A.
- **P1.2 · C · WPF-Probe:** Nach Bereitstellung der nativen Probe Farbwechsel, Alpha, wiederholte Veröffentlichung, Resize und Freigabe auf STA prüfen. Eigene WPF-Testdateien gehören C.
- **P1.3 · B · Synchronisationsreview:** Parallel Ressourcen- und Handle-Besitz, GPU-Abschluss, WPF-Lock und sichere Wiederverwendung untersuchen. Keine Rendereränderungen.
- **P1.4 · I · Hardwareprüfung:** Silk-D3D9-Paketreferenz ergänzen und Probe seriell auf der GTX 1070 prüfen. Formate, Kopien und Grenzen dokumentieren.

**Verbindliche Ergänzungen:**

- Brückenadapter aus dem tatsächlich verwendeten D3D12-Gerät ableiten. DXGI- und D3D9-Indexnummern nicht gleichsetzen; LUIDs vergleichen.
- Konfigurierte Adapterauswahl ausdrücklich berücksichtigen und prüfen.
- NT-Handles und ältere Sharing-Handles getrennt behandeln.
- Historisches `enableSoftwareFallback: true` berücksichtigen; daraus keine ungeprüfte WARP-/Remote-Desktop-Zusage ableiten.
- Keine konkurrierenden Schreibzugriffe auf veröffentlichte Oberflächen. Normale Dispatcher-Ticks warten nicht blockierend auf die GPU.
- Zwei wechselnde Legacy-Sharing-Ziele halten die an WPF gebundene Oberfläche unverändert. Erst nach GPU-Abschluss wird das fertige andere Ziel unter kurzem WPF-Lock veröffentlicht; vor Wiederverwendung muss das vorherige Ziel abgebunden sein.
- Jede normal zurückgekehrte `D3DImage.TryLock`-Anfrage wird gemäß geprüftem .NET-10-Verhalten mit `Unlock` ausgeglichen, auch bei `false`. Dann erfolgen weder Backbuffer-Wechsel noch Dirty-Publikation.

**Review:** Bildübergabe, Alpha, Synchronisation und Freigabe nachgewiesen. Bei Scheitern mit reproduzierbarem Befund stoppen.

**Haltepunkt:** Freigabe für Phase 2.

## Phase 2 – Gemeinsamer Renderer und Renderbedarf

**Ziel:** Stabile Schnittstellen für die parallele Implementierung.

- **P2.1 · I · Verträge festlegen:** Minimale Zielbeschreibung aus Farbressource, RTV, Größe und Format. Renderanforderung, GPU-Einreichung, Fertigstellung und WPF-Veröffentlichung getrennt definieren.
- **P2.2 · B · Renderer extrahieren:** Gemeinsame Szenenvorbereitung, Renderpässe und Ressourcen aus `D3D12PresentationSurface` herauslösen. Bestehenden HWND-Pfad produktiv über diese Grenze führen.
- **P2.3 · A · Renderbedarfssteuerung:** Separaten internen Zustand samt Tests implementieren. Bestehende Anforderungen vor der Framevorbereitung verbrauchen; später eintreffende Invalidierungen für Folgeframes erhalten. Unterschiedliche Invalidierungsarten für Bild, Szenengraph und Renderables berücksichtigen.
- **P2.4 · C · Anschlussreview:** WPF-Anschluss, kontinuierliche Quellen, DPI und Recovery gegen die festgelegten Verträge prüfen.
- **P2.5 · I · Integration und HWND-Gate:** Ergebnisse integrieren; Capture, RenderHost, Frame-Ereignisse und bisheriges HWND-Verhalten prüfen.

**Parallel:** P2.2/P2.3 nach P2.1 mit getrenntem Dateibesitz; P2.4 lesend parallel.

**Abnahme:**

- `MinimumUpdateCount`: tatsächliche Frames zählen, keine Busy-Ticks oder bloßen Veröffentlichungswiederholungen.
- Neue Invalidierung startet das Folgeframe-Budget erneut.
- Invalidierungen während Kamera-/Node-Update, GPU-Arbeit und Abschlussereignissen bleiben erhalten.
- Laufende GPU-Arbeit schützt Commands, Bindings und Ressourcen; bestehender endlicher Timeout bleibt erhalten.
- Ruhender WPF-Pfad führt keine Renderarbeit aus.
- Ausstehende Veröffentlichung und Recovery bleiben auch ohne neue Szenenänderung bearbeitbar.
- Bedarfspflege und ruhende Tick-Prüfung allokieren nach dem Aufwärmen nicht; gemeinsame Renderlisten wachsen nur bei tatsächlichem Kapazitätsbedarf. Allokationen der realen Szenenvorbereitung und des HWND-Frames messen und vermeidbare neue Kosten ausschließen.

**Haltepunkt nach Review:** Freigabe für Phase 3.

## Phase 3 – Drei Implementierungspakete parallel

**Gemeinsame Basis:** Phase-2-Commit `6f36703f3` und der anschließend geprüfte P3.0-Vertragscommit. Die Freigabe für P3.0 startet die Implementierungspakete noch nicht.

- **P3.0 · I · Anschlussverträge vor Parallelstart:** [Verbindlichen Architekturvertrag](knowledge/architecture/wpf-composition-contracts.md) festlegen und unabhängig prüfen: genau ein D3D12-Besitzer, geliehenes gemeinsames Ziel, explizite Einreichungsserials, Render-/Kopie-/Publikationszustände, Pixel-/Alpha-/Zwischenformatvertrag, Recovery-Generationen und Szenenlebensdauer, Scene2D-Wecksignal und Heap-Gates. Dokumentationsschritt ohne produktive Quelländerungen.
- **P3.0-Abnahme:** F14–F20 sind konkreten Besitzern und Prüffällen zugeordnet; keine parallelen Schreiber besitzen dieselbe Datei. Bei Signaturänderungen übernimmt I den gemeinsamen Vertragscommit vor abhängigen Implementierungen. Danach Haltepunkt vor P3.1–P3.4.

- **P3.1 · A · Produktive GPU-Brücke:** Tatsächliches D3D12-Gerät des Präsentationsbesitzers leihen; gemeinsame Quelle/RTV als `D3D12FrameTarget` und zwei D3D9-Zielslots besitzen. LUID-Zuordnung, ein kanonischer Ressourcenwrapper, Zustandsregistrierung, Handle-Lebensdauer und native Fehlerpfade. Keine eigene zweite D3D12-Queue oder Renderlogik. Besitz: native Interop-Dateien, eng erforderliche Core-Factory für Sharing/Adoption und eigene Tests.
- **P3.2 · B · Ausgabe und dynamische Quellen:** WPF-Profil mit vormultipliziertem BGRA und konsistenten Blend-/Effekt-/Zwischenformaten herstellen. Luma darf Coverage-Alpha nicht verlieren; FXAA, Scene2D und Capture getrennt prüfen. Feste RGBA-Postprocess-Ziele und Pass-Keys auf Formatverträglichkeit prüfen. `ContinuousRenderNode`, Partikel, Desktop-Capture und Scene2D-Invalidierung an Folgeanforderungen/Host-Benachrichtigung anschließen. Vermeidbare Allokationen in berührten Pass-Cache-Abfragen beseitigen. Besitz: betroffene Core-/Shaderdateien außer A zugeordneter Sharing-Factory und eigene Tests.
- **P3.3 · C · WPF-Canvas:** `DPFCanvas` und ImageSource mit Layout, `Stretch.Fill`, DPI, Sichtbarkeit, Resize, Frontbuffer-Verlust und sicherer Veröffentlichung implementieren. Fertigen Slot bei fehlgeschlagenem Lock behalten; keine neue Szene/Kopie für Publikationswiederholung. Tatsächliche Bindung auch bei Teilfehlern verfolgen; generationengebundene Callbacks und Unload-Bereinigung. Besitz: Canvas-/ImageSource-Dateien und eigene Tests.
- **P3.4 · I · Integration:** Ein Präsentationsbesitzer für Gerät/Queue/Context/Fence und gemeinsamen Renderer; explizite Einreichung statt HWND-`PresentedFrames` als Budgetnachweis. Einmalige GPU-Abschlussereignisse, Recovery, Capture-Normalisierung, Scene2D-Host-Abonnement und gemeinsame Renderer-/Target-Anpassungen integrieren. Besitz: zentrale Viewport-/Host-/FrameRenderer-/Target-Dateien, Projekte/Shaderregistrierungen und Dokumentation. Builds und fokussierte Prüfungen seriell ausführen.

**Parallel:** P3.1–P3.3 erst nach geprüftem Vertragscommit und Freigabe; P3.4 integriert seriell. C dupliziert weder Szenenlogik noch Viewport-Renderloop. Gemeinsam benötigte Signaturen werden über I koordiniert.

**Zusätzliche Verträge:**

- Ein gemeinsames Quellbild, zwei wechselnde Ziele und höchstens ein Frame im Transport: `Idle → Rendering → Copying → ReadyToPublish → Idle`. Neue Invalidierungen bleiben im Koordinator; keine Frameobjekt-Warteschlange.
- Budget nur bei akzeptierter GPU-Einreichung verbrauchen; GPU-Abschluss, Kopierabschluss und WPF-Veröffentlichung getrennt zählen. D3D11-Lesezugriff und unveröffentlichte/gebundene Slots verhindern Wiederverwendung.
- Recovery/Polling/Publikation bleiben bei leerem Szenenbudget erreichbar. Normale Ticks warten nicht auf GPU/Query und erzeugen keine Task-/Token-Objekte oder Callback-Bursts.
- WPF veröffentlicht ausschließlich neue, vollständig fertige Bilder. Eine erforderliche Wiederanbindung nach Frontbuffer-Verlust ist davon getrennt.
- Das in Phase 1 geprüfte Wechseln unveränderlicher Präsentationsoberflächen auch für Software-Snapshots/Fallback beibehalten; keinen über Dispatcher-Callbacks laufenden Schreibzugriff auf die gebundene Oberfläche einführen.
- Frontbuffer-Rückkehr, Reload und Resize können eine Wiederherstellung auch bei sauberer Szene anfordern.
- `EnableDpiScale` im Canvas berücksichtigen: effektive Renderauflösung bei deaktivierter Skalierung mit Faktor `1`; Eingabe bleibt in korrekten DIP-Koordinaten.
- Kontinuierliche Quellen fordern nur während ihrer aktiven Nutzung weitere Arbeit an. Capture-Timeout und Quellenneustart müssen ohne Kameraeingabe weiterlaufen können.
- Bestehender Desktop-Capture-Readback wird nicht mit einem neuen CPU-Präsentationsweg verwechselt.
- Kanonische Capture-Darstellung und vormultiplizierte WPF-Ausgabe getrennt halten; keine doppelte Kanalvertauschung oder Alpha-Multiplikation.
- Vor Ressourcenfreigabe WPF vom Backbuffer lösen und GPU-/Interop-Nutzung abschließen.
- Unload von endgültigem Dispose unterscheiden: wiederverwendbare Szene und extern bereitgestellte Ressourcen erhalten, Subscriptions und ausstehende Dispatcher-Arbeit sicher behandeln.

**Review:** Sichere Ressourcenlebensdauer, korrekte Farben/Alpha, funktionierende dynamische Quellen und anschließende Rückkehr zum Ruhezustand.

**Haltepunkt:** Freigabe für Phase 4.

## Phase 4 – Viewport, Invalidierung und Kompatibilität

**Ziel:** Beide Wege über den echten Viewport integrieren und die gefundenen Vertragslücken schließen.

### Integrationswelle

- **P4.1 · I · Viewport anschließen:** Default `false`, Initialisierungssperre, Template-Anbindung und `EnableDpiScale` ergänzen. Alle bildrelevanten Property-Änderungen einschließlich Original-Aliasnamen invalidieren zuverlässig. Reine Diagnose-/Statusänderungen erzeugen keinen unbeabsichtigten Render-Dauerlauf.
- **P4.2 · B · Gemeinsam genutzte Szenen:** Invalidierungen an alle angeschlossenen Viewports verteilen und gemeinsame Nodes bis zum letzten Verbraucher erhalten. Besitz: ModelContainer-/gemeinsame Szenenverwaltung und notwendige Rendereranpassungen; zentrale Viewport-Dateien bleiben bei I.
- **P4.3 · I · Host-Verträge integrieren:** EffectsManager-Ereignisse `InvalidateRender`, `DisposingResources` und `Reinitialized` abonnieren und abmelden; Dispatcher- und GPU-sichere Verarbeitung gewährleisten. Externen Manager nicht entsorgen. `EnableD2DRendering` tatsächlich auswerten und Änderungen invalidieren.

P4.1 und P4.2 dürfen mit getrennten Dateien parallel laufen. Danach integriert I P4.3 und erstellt den gemeinsamen Testausgang.

### Testwelle

- **P4.4 · A · Host-/Frame-Tests:** Echte RenderHost-Identität, RenderContext, FlattenedScene, Capture und genau einmal ausgelöste Abschlussereignisse prüfen, einschließlich leerer und übersprungener Frames. Managerwechsel/-neustart und gemeinsame Szenen einschließen.
- **P4.5 · B · HWND-Regressionen:** Native Tests und gezielt HWND-basierte Beispiele ausdrücklich auf `true` setzen. Native Eingaberoute und bisheriges Verhalten absichern.
- **P4.6 · C · WPF-Verhalten:** Default, XAML, Initialisierungssperre, DPI, Eingabe, Renderbedarf, 2D-Auswahl und Wiederanlauf testen.
- **P4.7 · I · Sichtbare Abnahme:** Bedienbares WPF-Control ohne Popup über echter D3D12-Ausgabe; ZIndex, Clipping, VisualBrush, Blur und Adorner prüfen.

**Parallel:** P4.4–P4.6 vom gemeinsamen Integrationscommit; getrennte Testdateien.

**Verbindliche Prüffälle:**

- `MinimumUpdateCount`: `6`, `1`, `0`, negativ und Änderung zur Laufzeit; statische Szene kommt zur Ruhe.
- Änderungen an Hintergrund, Kamera, Geometrie, Material, Licht, FXAA/SSAO und Effektmanager aktualisieren eine ruhende Szene.
- Während Kamera-/Node-Update, GPU-Arbeit und aus einem Abschlussereignis invalidieren; jede neue Anforderung erzeugt zuverlässig einen Folgeframe.
- `ContinuousRenderNode`, Partikel und Capture laufen weiter; Entfernen beziehungsweise Deaktivieren beendet die Folgeanforderungen.
- Zwei WPF-Viewports und ein WPF/HWND-Paar teilen eine Szene: beide aktualisieren sich; Unload eines Verbrauchers beeinträchtigt den anderen nicht.
- Zwei Viewports teilen einen EffectsManager: Austausch, Ressourcenfreigabe, Reinitialisierung und Schließen eines Viewports lassen den anderen funktionsfähig.
- Frontbuffer-Rückkehr, Reload, Resize und Manager-Reinitialisierung funktionieren ohne zusätzliche Benutzeraktion.
- DPI bei 200 %, jeweils mit und ohne Skalierung: erwartete Pixelauflösung bei unveränderten Picking-Koordinaten.
- `EnableD2DRendering=false/true` schaltet Scene2D einschließlich Capture; normale WPF-Controls und der separat gesteuerte 3D-ViewCube bleiben unabhängig.
- Hit-Test-Priorität: 2D-Overlay vor ViewCube vor Szenenpicking. Konsumierte Eingabe erzeugt keine zusätzliche Kamerageste; abgeschaltetes Szenenpicking deaktiviert nicht Overlay und ViewCube.
- Touch-Selektion erfolgt trotz Mouse-Promotion genau einmal; Manipulationen funktionieren und bereinigen fehlendes TouchUp.
- Abgeleitete Viewports erhalten Preview/Bubble genau einmal; CameraTarget und ZoomRectangle bleiben bei DPI/Resize korrekt positioniert und verschwinden nach Gestenende.
- Capture prüft getrennte Rot-/Blauwerte, Alpha `0/0.5/1`, Ausgabeformate und angeforderte Bildabmessungen; Skalierung nicht als echtes Neurendern ausweisen.
- Laden, Unload/Reload und Template-Neuaufbau erzeugen keine mehrfachen Renderloop-/Ereignisregistrierungen. Gleiche WPF-Rendering-Zeitstempel werden nicht mehrfach verarbeitet.

**Review und Haltepunkt:** Freigabe für Phase 5. `ViewportWidgetOverlay` bleibt erhalten.

## Phase 5 – Gesamtabnahme und Übergabe

- **P5.1 · I · Qualitätsgate:** Formatierung, ReSharper-Diagnostik, Rider-Build und Standardtests. Hardware-/DX12- und sichtbare Prüfungen getrennt und seriell.
- **P5.2 · A · Renderingaudit:** Schatten, OIT, SSAO, FXAA, Postprocessing, Screen-Space/2D, dynamische Quellen und GPU-Kollisionen für beide Wege gegen die Ergebnisse prüfen.
- **P5.3 · B · Lebensdaueraudit:** Minimieren, Reload, Frontbuffer-/Geräteverlust, Monitorwechsel, Managerwechsel, gemeinsame Verbraucher und Ressourcenfreigabe prüfen.
- **P5.4 · C · Dokumentationsaudit:** Standardwerte, historische Verträge, unterstützte Konfigurationen, Capture und ausdrücklich verbleibende Grenzen abgleichen. MSAA und lediglich skalierte Capture-Auflösung nicht als wiederhergestellte Fähigkeiten ausweisen.
- **P5.5 · I · Leistung und Abschluss:** Identische Szene/Kamera/Auflösung vergleichen; CPU-/GPU-Zeiten, Eingabelatenz, Speicher und Kopien dokumentieren. Zusätzlich Ruhezustand gegen aktive Animation messen: keine zusätzlichen Render-/Kopie-/Veröffentlichungsframes nach dem Einpendeln.

**Parallel:** P5.2–P5.4 ausschließlich lesend. Messungen und Werkzeugläufe seriell durch I.

OKF-Konzepte, Index und Wissenslog aktualisieren; Graphify lokal regenerieren; Dokumentationsprüfung und `git diff --check` abschließen.

**Abschlussreview:** WPF-Komposition funktioniert sichtbar und bedarfsgesteuert, dynamische Inhalte laufen korrekt, HWND-Regressionsgate besteht, Grenzen und lokale Commits sind vollständig dokumentiert.

## Review-Findings und Nachverfolgung

Die folgenden Befunde stammen aus der statischen Prüfung des Originals `c59fcc424`, des letzten Altpfads `6215e5c0b^` und des vorgesehenen Implementierungsstands `725570686`. Sie sind keine Aussage über bereits ausgeführte Implementierungs- oder Hardwaretests.

- **F01 · Renderoptionen invalidieren nicht zuverlässig:** Hintergrundfarbe, FXAA/SSAO und weitere Optionen werden heute teilweise nur während ohnehin laufender Frames gelesen. Ruhende Viewports müssen durch relevante Property-Änderungen aufwachen. **Tasks:** P0.3, P2.3, P4.1, P4.6.
- **F02 · ContinuousRenderNode ist wirkungslos:** Der heutige `InvalidRendererCore` ist leer; im Original forderte er weitere Frames an. **Tasks:** P3.2, P4.6, P5.2.
- **F03 · Desktop-Capture braucht Folgeanforderungen:** Auch nach Timeout oder Quellenneustart muss die aktive Quelle ohne fremde Eingabe weiter geprüft werden. **Tasks:** P2.1, P3.2, P4.6.
- **F04 · Neue Invalidierungen dürfen nicht verloren gehen:** Anforderungen aus Updates, GPU-Wartezeit oder Abschlussereignissen dürfen nicht durch das Abschließen eines älteren Frames gelöscht werden. **Tasks:** P2.3, P4.4, P4.6.
- **F05 · MinimumUpdateCount ist bisher ohne Verbraucher:** Initialwert, Änderungen zur Laufzeit und historisches Verhalten für null/negative Werte wieder funktional anschließen. Sechs bedeutet sechs Frames insgesamt. **Tasks:** P2.3, P4.1, P4.6.
- **F06 · EffectsManager-Ereignisse fehlen:** Renderinvalidierung und Ressourcen-Reinitialisierung müssen ruhende Hosts erreichen; externe Ressourcenbesitzer nicht eigenmächtig entsorgen. **Tasks:** P4.3, P4.4, P5.3.
- **F07 · Gemeinsame Szenen brauchen mehrere Verbraucher:** Historische Invalidierungsverteilung und Lebensdauer bis zum letzten Host fehlen im heutigen renderables-only Container. **Tasks:** P4.2, P4.4, P5.3.
- **F08 · Adapterindizes sind keine gemeinsame Identität:** D3D12-Gerätewahl und D3D9Ex-Gerät dürfen nicht unabhängig oder anhand vermeintlich gleicher Ordinalwerte verbunden werden. **Tasks:** P1.1, P1.3, P1.4, P3.1.
- **F09 · EnableDpiScale fehlt als öffentlicher Vertrag:** Historischen Auflösungs-/Leistungsschalter additiv wiederherstellen und von Eingabekoordinaten trennen. **Tasks:** P3.3, P4.1, P4.6.
- **F10 · EnableD2DRendering wird ignoriert:** Scene2D-Ausgabe muss tatsächlich schaltbar sein; Propertyänderungen müssen einen ruhenden Viewport aktualisieren. **Tasks:** P4.3, P4.6.
- **F11 · Lifecycle und Eingabe brauchen konkrete Regressionen:** Wiederanlauf einer sauberen Szene, Unload/Dispose, Overlay-/ViewCube-Priorität, Touch-Promotion, eigene Viewports und Adorner ausdrücklich absichern. Bereits vorhandene korrekte Eingaberouten wiederverwenden. **Tasks:** P3.3, P4.6, P4.7, P5.3.
- **F12 · Capture- und WPF-Pixelverträge unterscheiden sich:** Die bisherige R/B-Vertauschung nicht blind auf bereits BGRA-/vormultiplizierte Interop-Ausgabe anwenden. **Tasks:** P3.2, P4.4.
- **F13 · Bestehende Paritätsgrenzen separat ausweisen:** MSAA ist derzeit ohne produktive Wirkung; `RenderBitmap(width, height)` skaliert aktuell das vorhandene Bild statt in der Zielauflösung neu zu rendern. Beides wird nicht durch den Präsentationsumbau stillschweigend als gelöst bezeichnet. **Tasks:** P0.4, P5.4.

Die folgenden Architektur-Findings stammen aus dem Review von `6f36703f3`. Sie sind Anschlussverträge für P3/P4 und keine neu nachgewiesenen Phase-2-Regressionen. Der [P3.0-Vertrag](knowledge/architecture/wpf-composition-contracts.md) definiert Besitz, Ablauf und Abnahme.

- **F14 · Produktive Brücke hat noch keinen gemeinsamen Gerätebesitz:** Probe besitzt eigenes Gerät/Quelle; produktiv genau ein D3D12-Präsentationsbesitzer, Brücke leiht dieses Gerät und besitzt Quelle/Slots. **Tasks:** P3.0, P3.1 (A), P3.4 (I). **Nachweis:** tatsächliches gemeinsames Gerät/Ziel, LUID und ein Wrapper/Zustandstracker je Allokation.
- **F15 · Einreichung/Kopie/Publikation brauchen getrennte Identität:** HWND-`PresentedFrames` ist kein WPF-Einreichungsnachweis. Explizite Serial und Transportzustände; WPF-Lock-Wiederholung erzeugt keine neuen Render-/Kopieframes. **Tasks:** P3.0, P3.1 (A), P3.3 (C), P3.4 (I), P4.4. **Nachweis:** echte GPU-/Kopier-Stalls, Lock-Fehlschlag, Slot-Unveränderlichkeit und einmalige Ereignisse.
- **F16 · Alpha geht bereits im Luma-Pass verloren:** Nicht nur FXAAs finales Alpha korrigieren. WPF-Profil mit konsistenten Coverage-/Blend-/Scene2D-Verträgen und expliziter Capture-Normalisierung. **Tasks:** P3.0, P3.2 (B), P3.4 (I), P4.4. **Nachweis:** Alpha 0/0.5/1, überlappende transparente Geometrie, FXAA an/aus, Scene2D und getrennte Capture-Pixel.
- **F17 · Transport-Recovery berührt Szenen-Attachment:** Rebind, Resize, Unload, Gerätewechsel und Dispose trennen; Generationen schützen vor alten Callbacks. Anwendungsszene/externen Manager erhalten und fremde Verbraucher nicht detachieren. **Tasks:** P3.0, P3.3 (C), P3.4 (I), P4.2/P4.3, P5.3. **Nachweis:** Rebind ohne Neurendern, Reload und veraltete Callbacks; mehrere Verbraucher erst mit P4 freigeben.
- **F18 · Scene2D-Dirty-Flags wecken keinen ruhenden Host:** Core-Benachrichtigung und abonnierte Verbraucher explizit verbinden; kein Dauerloop als Ersatz. **Tasks:** P3.0, P3.2 (B), P3.4 (I), P4.3/P4.6. **Nachweis:** Scene2D-Änderung nach leerem Budget führt zu neuem Bild und anschließender Ruhe.
- **F19 · Probe-Testtreiber und Pass-Cache sind keine Null-Allokationsvorlage:** Produktive Polls mit retained state und gecachten Callbacks; vermeidbare Kosten vor Pass-Cache-Treffern in berührten Pfaden beheben. **Tasks:** P3.0, P3.2 (B), P3.3 (C), P3.4 (I), P5.5. **Nachweis:** getrennte Warm-Messungen für Idle, echte Meshframes, Kopie/Publikation/Lock-Retry sowie aktive Scene2D/Effekte.
- **F20 · Postprocess-Zwischenziele sind fest RGBA:** `SilkD3D12PostProcessResources.TargetFormat` und zugehörige Pass-/Kopierpfade auf BGRA-Verträglichkeit prüfen. Kein ungeprüfter CopyResource zwischen inkompatiblen Formatfamilien. **Tasks:** P3.0, P3.2 (B), P3.4 (I), P5.2. **Nachweis:** passende Ressourcen-/PSO-Formate für FXAA, Bloom, Outline und weitere betroffene Pässe.

### Zusätzliche Findings aus Implementierung und P3-Prüfung

- **F21 · Laufzeit-Profilprüfung kann alte Shader-Bindungen brechen:** Ein neuer b0-Zugriff in `psScreenDup` entfernte das WARP-Gerät bei bestehenden direkten Nutzern ohne zuvor benötigten b0-Puffer. Betroffene Pixelstufen als additive Profilvarianten kompilieren und nur im vormultiplizierten Passkatalog austauschen; alte Module behalten ihre Bindungsverträge. **Tasks:** P3.2, P3.4. **Nachweis:** unveränderte direkte Shader-Tests, Profil-Pixeltests und vollständiges Manifest mit 136 Modulen.
- **F22 · Freigabe und Capture brauchen Fehleridentität:** D3D11-Geräteverlust darf Freigabe nicht dauerhaft verhindern; erfolgreicher Preflight wird nicht wiederholt. Capture muss eine neue abgeschlossene Serial nachweisen. **Task:** P3.4. **Nachweis:** wiederholbarer echter WPF-Lock-Fehlschlag, Dispose-Retry und Ablehnung alter Capture-Pixel.
- **F23 · StopRendering muss nach allen Rückrufen gelten:** GPU-Abschluss-, Kamera-, Szenen- und Timer-Rückrufe können den Zustand ändern. Angenommenen Transport abschließen, Berechtigung vor neuer Einreichung erneut prüfen und abgebrochene Aufzeichnungen sicher verwerfen. **Task:** P3.4. **Nachweis:** Stop vor/aus Abschluss, aus Node-Update und aus suspendiertem Transport mit wiederhergestellter Sichtbarkeit.
- **F24 · Heap-Nachweis braucht echte häufige Pfade:** Delegates, Polygon-Enumeratoren, Clip-Arrays und Metadatenzugriffe bei konstanten Spans gesondert prüfen. Framework-Kosten beim fehlgeschlagenen WPF-Lock getrennt messen. **Tasks:** P3.2, P3.4, P5.5. **Nachweis:** echte Draw-/Publikationszähler, ruhende und aktive Messungen sowie Scene2D mit Layout und Clipping.
- **F25 · Allokationszähler kann durch Hintergrund-GC verfälscht werden:** Der dokumentierte .NET-10-Befund `dotnet/runtime#134724` passt zu sporadischen Zusatzbytes im parallelen Testlauf. Nur den Core-Testhost mit nicht gleichzeitigem GC qualifizieren, effektive Einstellung und positive Allokationskontrolle prüfen; Null-Byte-Grenzen beibehalten. **Tasks:** P3.4, P5.5. **Grenze:** Zuordnung des ursprünglichen Intervalls bleibt eine begründete Vermutung; keine Änderung der Produkt-GC-Einstellungen.

Historische Referenzstellen, jeweils per `git show <Revision>:<Pfad>` prüfbar:

- `c59fcc424:Source/HelixToolkit.SharpDX.Shared/Render/RenderHost/RenderHostBase.cs`: Zeilen 244–253 für EffectsManager-Ereignisse, 708–779 für Invalidierung und Framebudget.
- `c59fcc424:Source/HelixToolkit.SharpDX.Shared/Model/Scene/ContinuousRenderNode.cs`: Zeilen 51–53 für kontinuierliche Renderanforderungen.
- `c59fcc424:Source/HelixToolkit.SharpDX.Shared/Core/ScreenCloneRenderCore.cs`: Zeilen 238 und 303 für Capture-Folgeanforderungen.
- `c59fcc424:Source/HelixToolkit.SharpDX.SharedModel/Controls/ModelContainer3DX.cs`: Zeilen 309–340 für Invalidierungsverteilung, 645–681 für gemeinsame Lebensdauer.
- `c59fcc424:Source/HelixToolkit.Wpf.SharpDX.Shared/Controls/Viewport3DX.Properties.cs`: Zeilen 1105–1112 für Scene2D, 1297–1304 für das Framebudget und 1329–1335 für DPI-Skalierung.
- `c59fcc424:Source/HelixToolkit.Wpf.SharpDX.Shared/Controls/DX11ImageSourceRenderHost.cs`: Zeile 88 für die historische Adapterwahl und Zeilen 120–167 für Frontbuffer-Recovery.
- `c59fcc424:Source/HelixToolkit.Wpf.SharpDX.Shared/Controls/DPFCanvas.cs`: Zeilen 194–232 für Unload und Renderloop.
- `c59fcc424:Source/HelixToolkit.Wpf.SharpDX.Shared/Controls/Viewport3DX.cs`: Zeilen 872–915 für Adorner und 1715–1738 für Eingabepriorität.
- `c59fcc424:Source/HelixToolkit.Wpf.SharpDX.Shared/Extensions/ViewportExtensions.cs`: Zeilen 431–439 für Rendering bei angeforderter Capture-Auflösung.

## Phasenbericht und Freigabe

### Stand 2026-10-19 · Phase 1 abgeschlossen

- P0.1: Fast-forward auf `72557068603fbe3017dc045782e24631970a4fb1` abgeschlossen; fremde Änderungen im MeshMeister-Checkout erhalten.
- Der historische Reviewbestand P0.2–P0.4 ist über F01–F13 zugeordnet. Native HWND-Testanpassungen folgen erst mit dem neuen Standard in P4.5.
- P0.5: Rider-Build nach Korrektur von sieben vorhandenen Import-/Namespacefehlern in vier Beispieldateien erfolgreich.
- Standard-Baseline: **617 bestanden, 8 fehlgeschlagen, 625 insgesamt**. Die vorhandenen Fehler betreffen glTF-Materialien (2), Ressourcen-Maintenance (2), zufällige Postprocess-Samples (1) und Octree-Annahmen in Eingabe-/Frame-Tests (3). Einzelheiten im [Wissensstand](knowledge/backlog/d3d12-wpf-composition.md).
- Die ausdrückliche Freigabe „starte Phase 1“ wird für den isolierten Interop-Nachweis umgesetzt. Das allgemeine Baseline-Testgate bleibt als offener Befund sichtbar.
- **P1.1 erledigt:** Native GPU-Brücke mit einer D3D12-Quelle und zwei wechselnden D3D9Ex-Zielen; LUID-Abgleich, explizite Adapterwahl, getrennte Handle-Typen und begrenzter Teardown.
- **P1.2 erledigt:** STA-Proben für Farbwechsel, Alpha, Veröffentlichung, unveränderliches gebundenes Bild, WPF-Snapshot, Resize und Freigabe. Quellloses Testbild misst für die weiße Ausgangs-/Endkontrolle das Fenster; tatsächliche Bildprüfungen messen immer das Image.
- **P1.3 erledigt:** Separates Synchronisationsreview führte zu unveränderlichen gebundenen Oberflächen und ausgeglichenen `TryLock`-Aufrufen. Unabhängiges Integrationsreview und abschließendes Delta-Review melden keine weiteren Blocker.
- **P1.4 erledigt:** GTX 1070, Treiber `32.0.15.6094`, DXGI-Adapter `0`, LUID `0x000000000001456C`; Standard- und explizite Auswahl stimmen überein. **Alle drei neuen Hardwaretests bestanden, keine übersprungen.** Sichtbare Pixel bestätigen RGB und Alpha `0/0.5/1`; auch der alte Bildinhalt während einer neuen Kopie bleibt korrekt.
- Integrierter Implementierungsstand: `54bb684c7`, vorheriger Integrationspunkt `81ebc066b`; ausschließlich lokale Commits, kein Push.
- Abschließender Rider-Build erfolgreich. Standardtests unverändert **617/625 bestanden**, dieselben acht vorhandenen Fehler; keine neuen fehlschlagenden Testnamen.
- Graphify nach finaler Formatierung erfolgreich: 1.013 Dateien, 9.691 Knoten, 16.858 Kanten, 2.847 Communities. Die bestehenden 17 partiellen Parser-Ergebnisse bleiben dokumentiert.
- Grenzen: ein GPU-/Treiber-/Desktop-Setup, kurze Beobachtung; keine Zusage für WARP, RDP, zweiten Hardwareadapter, injizierte GPU-Stalls oder Geräteverlust. Der Testtreiber arbeitet nur auf Anforderung; die produktive Viewport-Bedarfssteuerung folgt in Phase 2.
- **Haltepunkt erreicht: Phase 2 startet erst nach ausdrücklicher Freigabe.** Ausführlicher [Phasenbericht und Befunde](knowledge/backlog/d3d12-wpf-composition.md).

### Stand 2026-10-19 · Phase 2 abgeschlossen

- **P2.1–P2.5 erledigt:** Gemeinsamer `D3D12FrameRenderer` mit geliehenem Werttyp-Ziel; HWND verwendet ihn produktiv. Demand-Koordinator und interner Viewport-Verbraucher integriert. Öffentliches HWND-Verhalten bleibt kontinuierlich; produktiver WPF-Canvas und öffentliche Auswahl folgen in P3/P4.
- Parallele Implementierung in getrennten Worktrees, separates Anschlussreview, frühes Core-/Demand-Review und zwei unabhängige Integrationsreviews mit ausdrücklichem Heap-Fokus.
- Reviewbefunde behoben: direkte Kandidatenliste vor Nutzung geleert; offene Command-Liste nach fehlgeschlagener Vorbereitung. Fehlerpfad verwirft ungesendete Commands samt nativen Ressourcen/Zuständen; echter HWND-Retry prüft Mesh-Pixel und erhaltenen Bedarf.
- Letzter geprüfter Implementierungscommit `d4042daee`; finales Delta-Review ohne offene Blocker. Rider-Build erfolgreich.
- **57 neue Fälle bestanden.** Gesamtsuite: **674/682 bestanden**, dieselben acht bekannten Baseline-Fehler; keine neuen fehlschlagenden Testnamen.
- Heap-Messungen nach Warm-up: Koordinator aktiv/ruhend **0 Bytes**, 1.000 ruhende echte Viewport-Aufrufe **0 Bytes**, 1.000 Zwei-Node-Vorbereitungen samt Cleanup **0 Bytes**. 128 tatsächlich eingereichte HWND-Mesh-Frames: **1.747,81 Bytes/Frame**, Regressiongrenze **2.048 Bytes/Frame**.
- Messgrenzen: Initialisierung, Kapazitätswachstum, Capture, Fehler-Recovery, aktive Scene2D und optionale Effektpässe gesondert; keine pauschale Null-Allokationsaussage für vollständiges Rendering. Hardware-/DX12-Proben aus P1 wurden nicht erneut ausgeführt.
- Einzelheiten, Prüfartefakte und Reviewantworten im [Phasenbericht](knowledge/backlog/d3d12-wpf-composition.md).
- Graphify nach finalen Quelländerungen erfolgreich: 1.021 Dateien, 9.841 Knoten, 17.265 Kanten, 2.884 Communities; bestehende 17 partielle Parser-Ergebnisse bleiben dokumentiert.
- **Haltepunkt erreicht: Phase 3 startet erst nach ausdrücklicher Freigabe.**

### Stand 2026-10-19 · P3.0 abgeschlossen

- Architekturvertrag und Taskbesitz in `1913dab60` festgelegt; F14–F20 mit Abnahmekriterien zugeordnet.
- Ein D3D12-Besitzer, geliehenes Ziel, explizite Frame-Serials und getrennte Render-/Kopie-/Publikationszustände verbindlich. WPF-Premultiplikation, Luma/FXAA-Coverage, Zwischenformate und Capture-Normalisierung getrennt definiert.
- Recovery-Generationen, Unload/Attachment-Grenzen, Scene2D-Wecksignal und Heap-Prüfungen festgelegt; A/B/C/I haben getrennte Dateiverantwortung.
- Unabhängiges Vertragsreview ohne offene Pflichtkorrekturen. Fünf Markdown-Dateien und 44 lokale Links/Quellenverweise geprüft; `git diff --check` erfolgreich.
- Ausschließlich Dokumentation geändert. Keine neuen Build-, GPU-, Test- oder Graphify-Ergebnisse behauptet.
- **Haltepunkt erreicht: P3.1–P3.4 sind vorbereitet und starten nach ausdrücklicher Freigabe.**

### Stand 2026-10-19 · Phase 3 abgeschlossen

- Nach ausdrücklicher Freigabe parallel implementiert und integriert: produktive GPU-Brücke, Core-Ausgabeprofil,
  passiver WPF-Canvas und interner Präsentationsbesitzer mit gemeinsamer Renderpipeline.
- **P3.1–P3.4 erledigt:** ein gemeinsamer D3D12-Besitzer, GPU-Brücke mit zwei unveränderlichen WPF-Zielslots,
  bedarfsgesteuerter Transport mit Serials, korrekte BGRA-/Alpha-Ausgabe und explizites Capture.
- Unabhängige Reviews mit Heap-Fokus; Owner-Runde 6, Core-Runde 5 und finales Messungsreview ohne offene
  Pflichtkorrekturen. Findings F14–F25 umgesetzt beziehungsweise mit klaren Folgeaufgaben/Grenzen dokumentiert.
- Shader-Kompatibilität durch 13 additive Profilmodule; bestehende Bindungsverträge und benutzerdefinierte
  Shader-Reader bleiben erhalten. Neues Manifest umfasst 136 Module.
- Letzter Implementierungsstand `1d0c59efe`; Rider-Build erfolgreich. **82 neue Fälle bestanden**, darunter
  **27 ausdrückliche Hardwarefälle**. Standardtests **729/737 bestanden**, exakt dieselben acht Baseline-Fehler.
- Warm-Messungen: Idle und native Scene2D-Vorbereitung **0 Bytes**; echter WPF-Mesh **184 Bytes/Frame**,
  Mesh mit FXAA/Scene2D **208 Bytes/Frame**. 1.000 echte Lock-Retries: Framework und Owner jeweils 32.000 Bytes.
- Exakte Core-Messung isoliert den bekannten möglichen Hintergrund-GC-Zählerfehler nur im Testhost;
  positive Kontrolle 64 Bytes, unveränderte Null-Byte-Grenzen. Keine Änderung der Produkt-GC-Konfiguration.
- Einzelheiten, Messgrenzen, Reviewantworten und TRX-Präfixe im
  [Phasenbericht](knowledge/backlog/d3d12-wpf-composition.md). Geräteverlust/RDP/zweiter Adapter und mehrere
  gemeinsame Verbraucher sind damit noch nicht vollständig nachgewiesen.
- Graphify lokal erfolgreich: **1.038 Dateien, 10.230 Knoten, 18.893 Kanten, 2.756 Communities**;
  dieselben 17 partiellen Parser-Ergebnisse. Staging entfernt, erzeugte Daten nicht von Hand geändert.
- **Haltepunkt erreicht: Phase 4 und der öffentliche Backend-Schalter mit Default `false` starten erst nach
  ausdrücklicher Freigabe.**

Jeder Phasenbericht enthält:

- Phasenstatus, integrierten Commit und erledigte Task-IDs.
- Wesentliche Änderungen und zugehörige Review-Findings.
- Bestandene, fehlgeschlagene und nicht ausgeführte Prüfungen.
- Behobene Reviewbefunde und verbleibende Grenzen.
- Nächste Phase mit ausdrücklichem Halt bis zu deiner Freigabe.
