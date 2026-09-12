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
