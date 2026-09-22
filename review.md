# Architektur-Review – StudyHub

Grundlage: manuelles Code-Review der aktuellen Architektur (Backend-API, Frontend-Integration,
UI-Services, Razor-Komponenten, Persistenzschicht), abgeglichen mit dem Ist-Stand des Codes auf
Branch `Refactor`. Es wurde noch kein Produktionscode geändert – dies ist ausschließlich die
Dokumentation des Reviews.

---

## 0. Vorab-Befund: Inkonsistente / fehlende Dokumentation

Gemäß `CLAUDE.md` ("If documentation conflicts with the current implementation, report the
inconsistency instead of making assumptions") wird dieser Befund hier gemeldet statt stillschweigend
aufgelöst:

- `CLAUDE.md` listet `docs/roadmap.md` und `docs/adrs/` als Kern-Dokumentation – **beide existieren
  nicht** im Repository. Die Listung rausschmeißen. 
- `docs/agent-rule-catalog.md` nennt sich selbst `Canonical source: AGENTS.md`, und **alle**
  Dateien unter `agents/` (`architect`, `orchestrator`, `reviewer`, `implementer`, `consolidator`,
  `verification`) verlangen explizit "Read the nearest applicable `AGENTS.md` files" – eine
  `AGENTS.md`-Datei existiert **nirgends** im Repository. Überall wo eine AGENTS.md verwendet wird, soll die CLAUDE md referenziert werden. 
- `docs/architecture.md`, `docs/agent-context.md` und `docs/agent-rule-catalog.md` beschreiben
  augenscheinlich ein **anderes Projekt**: Namensraum `Polipol.PA.*` / `Aplauncher`, Projektname
  "AppLauncher", Domänenbegriffe `Application`/`Group`/`ApplicationGroup` (SID-basierte
  Zugriffsgruppen), WPF-Frontend, MSTest, `Guid.CreateVersion7()`. Nichts davon trifft auf StudyHub
  zu (Blazor, Semester/Kurse/Dokumente, xUnit, wie in `CLAUDE.md` beschrieben). Das soll in der Tat zu unserem Projektnamen geändert werden.

**Interessant dabei:** Genau diese "fremden" Dokumente beschreiben bereits sehr genau die Zielarchitektur,
die im Folgenden gefordert wird (Controller statt Minimal APIs, Accessor-Namensmuster, Contract/
Implementation-Trennung, Razor-Code-Behind als Pflicht via `ARC-008`, verbotene generische
Suffixe wie `Service` via `NAM-005`, Repository-Platzierung in der Data-Schicht via `LAY-9`/`ARC-007`).
Es scheint, dass diese Dateien aus einer Vorlage/einem Schwesterprojekt übernommen wurden, ohne
für StudyHub angepasst zu werden – der aktuelle StudyHub-Code hält sich an keine dieser bereits
niedergeschriebenen Regeln.

**Offene Entscheidung (siehe Abschnitt 4):** Sollen diese Dateien als generischer Firmenstandard
bestehen bleiben und lediglich um StudyHub-spezifische/aktuelle Inhalte ergänzt werden (`agent-context.md`,
fehlende `AGENTS.md`, `roadmap.md`, `adrs/`), oder sollen sie ersetzt werden? Diese Entscheidung wurde
hier bewusst nicht eigenmächtig getroffen.Ja, das sind Dateien von anderen Projekten. Sie sollen hier in der StudyHub Domäne Verwendung finden. Also die ensprechenden Fragmente aus alten Projekten mit StudyHub spezifischen Informationen ersetzen.

---

## 1. Befunde

### 1.1 Minimal APIs → Controller + Orchestratoren

**Ist-Zustand:** `src/UI/StudyHub.Api/*/​*Endpoints.cs` (`SemesterEndpoints`, `CourseEndpoints`,
`DocumentEndpoints`, `DashboardEndpoints`) nutzen Minimal-API-Routen (`MapGet`, `MapPost`, `MapPut`),
die direkt gegen die Business-Contracts (`ISemesterManagement`, `ICourseManagement`, …) aufgerufen
werden. Es gibt keine Controller-Klassen und keine dedizierte Orchestrator-Schicht zwischen
API-Endpunkt und Business-Use-Case.

**Ziel:** ASP.NET Core Controller (`SemesterController`, `CourseController`, …) als reine
Transport-Schicht (Routing, Model-Binding, HTTP-Statuscodes), die einen Orchestrator aus der
Business-Schicht aufrufen. Der Orchestrator kapselt die eigentliche Workflow-Logik und ruft
Domain- und Data-Schicht auf. Deckt sich mit `API-003`/`API-004`
(`docs/agent-rule-catalog.md`).

### 1.2 Contract-Projekte innerhalb der Logic-Schicht

**Ist-Zustand:** DTOs, Interfaces und Exceptions liegen unstrukturiert neben der Implementierung im
selben Projekt/Ordner, z. B. `StudyHub.Logic.Business/Courses/`:
`CourseDto.cs`, `ICourseManagement.cs`, `CourseManagement.cs`, `CourseNotFoundException.cs`,
`DuplicateCourseNameException.cs` – alles im selben Ordner, keine Trennung zwischen dem, was ein
Konsument braucht ("Contract"), und der eigentlichen Implementierung.

**Ziel:** Contract (Interfaces, DTOs, Delegates, Exceptions) klar von der Implementierung trennen –
z. B. eigener `Contracts/`-Unterordner bzw. eigenes `*.Contract`-Projekt pro Komponente, analog zu
`docs/architecture.md` §6/§9.2.

### 1.3 Frontend → Controller-Kommunikation über Accessors statt einem "fetten" API-Client

**Ist-Zustand:** `SemesterApiClient` (`src/UI/StudyHub.UI/Semesters/SemesterApiClient.cs`) und
`CourseApiClient` implementieren jeweils **direkt das volle Business-Interface**
(`ISemesterManagement` bzw. `ICourseManagement`) über HTTP – inklusive aller CRUD-Operationen einer
Domäne **und** der Übersetzung von `ProblemDetails` in Business-Exceptions
(`SemesterNotFoundException`, `DuplicateSemesterNameException`, …) in derselben Klasse. Damit
spricht das Frontend exakt denselben Vertrag wie der Server – eine einzige, große Klasse pro
Domäne übernimmt Transport, Fehler-Mapping und Vertragskonformität gleichzeitig.

**Bewertung:** Das ist genau der kritisierte "eine fette API-Client-Klasse pro Domäne"-Ansatz. Er
bläht einzelne Dateien auf und zwingt jede neue Operation in dieselbe Klasse.

**Ziel:** Schlanke, pro Anwendungsfall/Zugriffsart geschnittene Accessor-Klassen in der
UI-Integrationsschicht (z. B. `SemesterAccessor` mit kleinen, fokussierten Methoden oder mehrere
kleinere Accessors), die reine HTTP-Zugriffslogik kapseln. Das entlastet die Ordnerstruktur im UI
und entkoppelt das Frontend von der serverseitigen Form des Business-Contracts.

### 1.4 Namensgebung der UI-Services

**Ist-Zustand:** `src/UI/StudyHub.UI/Services/` enthält `ThemeService`, `SidebarStateService`,
`PageHeaderService` – durchgängig der generische `*Service`-Suffix.

**Ziel:** Rollenbasiertes Namensmuster ("Berufsnamensmuster") statt generischer Suffixe – z. B.
`Accessor`, `Provider`, `StateHolder`, `Formatter`, `Orchestrator`. Deckt sich mit `NAM-005`
(`docs/agent-rule-catalog.md`), das `Service`/`Helper`/`Utility` als "Generic bucket suffixes"
explizit ausschließt.

### 1.5 Vermischung von Datenzugriff und Logik in UI-Services

**Ist-Zustand:** `ThemeService.cs` kombiniert JS-Interop-Datenzugriff
(`jsRuntime.InvokeAsync<string>("studyHubTheme.get")`) mit Zustandshaltung (`Theme`-Property) und
Event-Publishing (`Changed`) in einer einzigen Klasse.

**Ziel:** Datenzugriff (z. B. ein kleiner `ThemeAccessor` für den JS-Interop-Aufruf) von
Zustands-/Änderungsbenachrichtigungslogik trennen, damit jede Klasse eine Verantwortung hat.

### 1.6 Razor-Komponenten ohne Code-Behind

**Ist-Zustand:** Alle Seiten unter `Components/Pages/*.razor` und `Components/Shared/*.razor`
verwenden `@code`-Blöcke direkt in der `.razor`-Datei. Nur die Layout-Komponenten (`MainLayout.razor`,
`NavMenu.razor`) haben eine begleitende `.razor.cs`-Datei.

**Ziel:** Jede `.razor`-Komponente erhält eine gleichnamige `.razor.cs`-Code-Behind-Klasse; `@code`
wird vermieden. Diese Regel steht bereits (aber unbeachtet) als `ARC-008` in
`docs/agent-rule-catalog.md`.

### 1.7 Repository-Platzierung

**Ist-Zustand:** `SemesterRepository`, `CourseRepository`, `DocumentRepository` liegen im Projekt
`StudyHub.Infrastructure` (`src/Infrastructure/StudyHub.Infrastructure/...`). Das Projekt
`StudyHub.Data` enthält dagegen nur `ApplicationDbContext` und die EF-Migrationen.

**Ziel:** Repositories gehören in die Data-Schicht (`StudyHub.Data`), zusammen mit `DbContext` und
`IEntityTypeConfiguration`-Klassen. `StudyHub.Infrastructure` sollte für tatsächliche
Infrastruktur-Belange (externe Services, Dateisystem, E-Mail, KI-Provider-Adapter o. Ä.) reserviert
bleiben – nicht für Persistenz.

### 1.8 DTOs, Datenklassen, Exceptions und Options → Shared

**Ist-Zustand:** `src/Shared/StudyHub.Shared` ist **komplett leer** (nur die `.csproj`-Datei). Alle
DTOs (`CourseDto`, `SemesterDto`, `DocumentDto`, …) und Exceptions (`CourseNotFoundException`,
`SemesterArchivedException`, …) liegen stattdessen in `StudyHub.Logic.Business` bzw.
`StudyHub.Logic.Domain`. Es existieren noch keine Options-Klassen, aber auch kein vorgesehener Ort
dafür.

**Ziel:**
- Gemeinsame Datenklassen und Exceptions, die über mehrere Komponenten/Schichten hinweg gebraucht
  werden, wandern nach `StudyHub.Shared`.
- Komponenten-lokale DTOs, die nur innerhalb einer Domäne verwendet werden, bleiben im jeweiligen
  Contract-Bereich der Logic-Schicht (siehe 1.2) – nicht alles pauschal nach Shared verschieben.
- Options-Klassen (Konfiguration) bekommen einen eigenen Bereich unter Shared, z. B.
  `StudyHub.Shared/Configuration/` bzw. `Configuration.Contract`-artig benannt.

---

## 2. Zielworkflow

```
UI (Blazor Pages, thin, mit Code-Behind)
  → Accessor (StudyHub.UI/<Domäne>/Accessors, reine HTTP-Zugriffslogik)
    → Controller (StudyHub.Api/<Domäne>, reines Transport-Mapping)
      → Orchestrator / Business (StudyHub.Logic.Business, Workflow-Orchestrierung)
        → Domain (StudyHub.Logic.Domain, Entitäten, Invarianten)
        → Data (StudyHub.Data, Repositories, EF Core)

Quer dazu: StudyHub.Shared (DTOs, Exceptions, Options/Configuration-Contracts),
referenzierbar von allen Schichten.
```

Wichtig: Business-Contracts (`ISemesterManagement` etc.) sollten **nicht** 1:1 vom Frontend als
HTTP-Client implementiert werden (siehe 1.3) – Accessor und Controller/Orchestrator dürfen
unterschiedlich geschnittene Schnittstellen haben.

---

## 3. Einordnung nach CLAUDE.md-Workflow

Nach der in `CLAUDE.md` definierten Klassifizierung ist diese Umstellung eine **"Large"**-Änderung
(betrifft API-Muster, Contracts, DI-Registrierung und ein quer liegendes UI-Muster über mehrere
Domänen hinweg). Entsprechend gilt: **Implementierungsplan vorschlagen und Bestätigung abwarten,
bevor Code geschrieben wird.** Dieses Dokument ist der erste Schritt (Ist-Analyse); es wurde noch
keine Implementierung vorgenommen.

Empfehlung für die Umsetzung, falls gewünscht: schrittweise pro Domäne (z. B. zuerst Semester als
Referenzimplementierung, dann Courses und Documents nachziehen), damit Build und Tests nach jedem
Schritt grün bleiben.

---

## 4. Offene Fragen für den Nutzer

1. Sollen `docs/architecture.md`, `docs/agent-context.md` und `docs/agent-rule-catalog.md` an
   StudyHub angepasst werden (Domänenbegriffe, Tech-Stack), oder ist der aktuelle Inhalt bewusst ein
   generischer, projektübergreifender Firmenstandard, der so bleiben soll? Die sollen angepasst werden
2. Soll eine `AGENTS.md`-Datei angelegt werden? Sie wird von `docs/agent-rule-catalog.md` als
   kanonische Quelle und von allen `agents/*`-Dateien als Pflichtlektüre referenziert, existiert aber
   nicht. ALle referenzen an eine agents md sollen mit claude md ersetzt werden.
3. Sollen `docs/roadmap.md` und `docs/adrs/` (von `CLAUDE.md` als Kern-Dokumentation gelistet)
   nachgezogen werden? nein, die listing wegmachen
4. In welcher Reihenfolge/mit welchem Umfang soll die eigentliche Code-Umstellung (Abschnitt 1–2)
   angegangen werden? entscheide du das.

---

## 5. Code-Review: PR #18 „Feature/markdown notes"

Grundlage: eigenständiges Review des Diffs `main...feature/markdown-notes`
(https://github.com/WinkelhoffT/LedgerApp/pull/18, Head-Commit `648ef9b`), zusätzlich zum bereits
vom Nutzer selbst abgegebenen GitHub-Review. Die dort bereits genannten Punkte (Titel-Normalisierung
im Repository statt in der Domäne, Konstanten in eigene Klassen auslagern, Trennung von Model und
Logik in `Note`/`NoteDocument`) werden hier nicht wiederholt, sondern nur um zusätzliche, eigenständig
gefundene Befunde ergänzt.

### 5.1 Positiv: Architektur folgt jetzt dem Zielbild aus Abschnitt 1–2

Die Notes-Feature-Schicht ist die erste, die dem in Abschnitt 2 festgelegten Zielworkflow tatsächlich
folgt: `NoteController` (reines Transport-Mapping) → `NoteOrchestrator` (Business-Contract via
`INoteOrchestrator`) → `INoteRepository`/`Note` (Domain) bzw. `NoteRepository` (Data), plus
`NoteAccessor`/`INoteAccessor` in `StudyHub.Logic.Integration` statt eines fetten API-Clients in der
UI (vgl. 1.3). Auch Code-Behind (`Notes.razor.cs`) statt `@code`-Block (vgl. 1.6) ist umgesetzt.

### 5.2 Korrektheit

1. **Fehlende Fehlerbehandlung bei Archive/Restore/Attach/Detach lässt den Blazor-Server-Circuit
   abstürzen.** `ArchiveSelectedAsync`/`RestoreSelectedAsync`
   (`src/UI/StudyHub.UI/Components/Pages/Notes.razor.cs:508-534`) sowie
   `AttachAsync`/`DetachAsync` in `NoteAttachmentPicker.razor.cs` rufen den jeweiligen Accessor ohne
   `try`/`catch`/`finally` auf. Anders als `SaveAsync` (das ein `finally { IsSaving = false; }` hat)
   bleibt `IsSaving` bei einem Fehler dauerhaft `true` (Buttons bleiben deaktiviert), und eine nicht
   abgefangene Exception in einem Blazor-Server-Eventhandler beendet den Circuit – der Nutzer sieht
   „An unhandled error has occurred, reload". Tritt z. B. auf, wenn dieselbe Notiz in zwei Tabs offen
   ist und in Tab B bereits archiviert/gelöscht wurde, während in Tab A auf „Archive" geklickt wird.

2. **`SaveAsync` fängt `NoteNotFoundException` nicht ab**
   (`Notes.razor.cs:474-501`). Die Catch-Liste deckt `DuplicateNoteTitleException`,
   `NoteValidationException`, `NoteArchivedException` sowie Course-/Semester-Fehler ab, aber nicht
   den Fall, dass die Notiz zwischen Laden und Speichern in einem anderen Tab bereits entfernt wurde
   (`NoteOrchestrator.UpdateAsync` wirft dann `NoteNotFoundException`). Gleicher
   Circuit-Absturz wie oben.

3. **DB-eindeutiger Index auf `Notes.Title` ist case-sensitive, die Anwendungslogik prüft aber
   case-insensitive.** Migration `20260916201326_AddNote.cs` legt `IX_Notes_Title` als `UNIQUE` auf
   eine reine `TEXT`-Spalte ohne `COLLATE NOCASE` an; SQLite vergleicht `TEXT`-Spalten standardmäßig
   binär/case-sensitive. `NoteRepository.ExistsByTitleAsync` (und `GetIdsByTitlesAsync`) vergleichen
   dagegen über `.ToLower()` bzw. `StringComparer.OrdinalIgnoreCase` – case-insensitive. Die
   Datenbank erzwingt die eigentlich gewollte Invariante ("Titel eindeutig, unabhängig von
   Groß-/Kleinschreibung") also nicht: Bei zwei nahezu gleichzeitigen Create-Requests mit
   unterschiedlicher Groß-/Kleinschreibung ("Foo" / "foo") kann die App-seitige Prüfung durch die
   Race Condition umgangen werden, und die DB lässt beide Zeilen zu. Danach ist auch unklar, welcher
   der beiden Titel bei `GetIdsByTitlesAsync` (das einen `Dictionary` mit `OrdinalIgnoreCase` baut)
   für die Wiki-Link-Auflösung gewinnt.

4. **Wiki-Link-Backlinks veralten, wenn das Ziel erst nach der Quelle angelegt wird.** Schreibt man
   `[[Future Note]]` in eine Notiz, bevor „Future Note" existiert, wird der Link laut Plan-Dokument
   bewusst nur als reiner Text angezeigt (kein Auto-Create). Sobald „Future Note" später angelegt
   wird, rendert die *Vorschau* der alten Notiz den Link aber automatisch als klickbar (weil
   `PreviewHtml`/`TitleToNoteId` bei jedem Aufruf live gegen `NoteList` auflöst) – während die
   `NoteLinks`-Tabelle (und damit die Backlinks-Anzeige auf „Future Note") nicht nachgezogen wird,
   weil `ResolveLinksAsync` nur beim Create/Update der Quelle läuft. Vorschau und Backlinks-Panel
   können dadurch dauerhaft auseinanderlaufen, bis die alte Notiz erneut gespeichert wird. Im
   Plan-Dokument nicht als bekannte Einschränkung dokumentiert.

### 5.3 Performance / Wartbarkeit

5. **`PreviewHtml` und `TitleToNoteId` sind ungecachte Properties, die bei jedem Render mehrfach neu
   berechnet werden** (`Notes.razor.cs:144-212`). `HeadingOutline` liest `PreviewHtml`, das Markup
   selbst liest `PreviewHtml` erneut – zwei volle Markdig-Durchläufe pro Render. Zusätzlich wird
   `TitleToNoteId` (ein `ToDictionary(...)` über die komplette `NoteList`) für **jeden** `[[Title]]`-
   Treffer im `Regex.Replace`-Delegate neu aufgebaut, nicht einmal pro Aufruf. Bei mehreren Wiki-Links
   in einer Notiz und einer wachsenden Notizliste macht das jeden Tastendruck im Editor spürbar
   teurer, als es sein müsste.

6. **Wiki-Link-Regex/-Extraktion ist dupliziert.** `NoteOrchestrator.WikiLinkPattern`
   (`\[\[(.+?)\]\]`) in der Business-Schicht und `Notes.WikiLinkPattern` in der Razor-Komponente
   implementieren unabhängig voneinander dieselbe Parsing-Logik. Ändert sich die Link-Syntax (z. B.
   `[[Titel|Alias]]`), muss das an zwei Stellen synchron gehalten werden; verpasst man eine, zeigt die
   Vorschau Links an, die serverseitig nie in `NoteLinks` landen (oder umgekehrt). Verstößt gegen
   CLAUDE.md „Keep Razor components thin" / „Move business logic into Business layer" – die
   Extraktion gehört an eine Stelle, auf die beide Seiten zugreifen (z. B. Shared).

7. **`highlightCode` in `notes-editor.js` kann denselben Codeblock mehrfach mit einem „Copy"-Button
   versehen** (`wwwroot/js/notes-editor.js:78-99`). Der Kommentar im Code geht davon aus, dass die
   `<pre>`-Knoten bei jedem Render neu erzeugt werden, weil Blazor `MarkupString` „wholesale" ersetzt.
   Das stimmt aber nur, wenn sich der `PreviewHtml`-String tatsächlich ändert – bleibt er gleich (z. B.
   weil nur `ShowArchived` umgeschaltet oder ein anderer Filter geändert wird, während dieselbe Notiz
   offen ist), rendert Blazor die Region nicht neu, `OnAfterRenderAsync` ruft `highlightCode` aber
   trotzdem bedingungslos auf. Jeder solche Render hängt einen weiteren „Copy"-Button an denselben,
   unveränderten `<pre>`-Block.

### 5.4 Priorisierung

Vor dem Merge sollten mindestens **5.2.1** und **5.2.2** behoben werden (Blazor-Server-Circuit-Abstürze
sind ein direkter Produktionsausfall für die gesamte offene Session, nicht nur einen Klick). **5.2.3**
(Collation) ist ein Datenintegritätsproblem, das in einer Single-User-App selten, aber real auftreten
kann – einfach zu fixen (`.UseCollation("NOCASE")` auf der Spalte bzw. im Index). 5.2.4, 5.3.5–5.3.7
sind nicht blockierend, aber günstig genug, um in diesem PR mitzunehmen, bevor weitere Features auf
der duplizierten Link-Logik bzw. den ungecachten Properties aufbauen.
