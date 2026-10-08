# Feature Plan: Calendar (Epic 8)

Status: Planned. Implementation starts after the open questions in section 10 are answered.
Classification (per `CLAUDE.md`): **Medium**. It adds a new domain object (`StudySession`), a new
CRUD workflow and a new page with two views. Per the Medium workflow this plan covers the planned
architecture, the affected layers, the required contracts and the database impact.

Epic items:

- [ ] Month view (Monatsansicht)
- [ ] Week view (Wochenansicht)
- [ ] Session display (Session Darstellung)

Section 8 maps each item to its implementation steps.

## 1. Goal

`/calendar` is still a "coming soon" stub. It becomes a calendar that shows the student's planned
study sessions in a **month view** and a **week view**. Sessions are added, edited and deleted
directly in the calendar. The Dashboard's "Today's Sessions" placeholder shows today's sessions.

## 2. Starting Point

- `Calendar.razor` only contains a placeholder text. Its header subtitle is hardcoded to
  "November 2025".
- **No session entity exists yet.** The README roadmap lists "7. Study Sessions — plan and track
  study time" before "8. Calendar", but milestone 7 is not implemented. A calendar cannot show
  sessions that do not exist, so this epic adds the **planning half** of milestone 7: a minimal
  `StudySession` (what, when, how long, where, for which course). Tracking (done flag, actual
  duration, timer) stays in milestone 7 (open question 1).
- **Mockup** (`docs/mockup/StudyHub.html`, `pageCalendar()`):
  - month grid with up to two event chips per day and "+N more";
  - side panel for the selected day with session cards (time · duration, title,
    course code · course name) and an "Add Session" button;
  - previous / next / "Today" buttons and course legend chips;
  - the Dashboard shows "Today's Study Sessions" with a "View calendar" link and a
    "Schedule Session" quick action.

  The mockup has **no week view**; section 5.3 designs it in the mockup's style.
- **Reusable pieces:** `Course.Color` (hex value such as `#2563eb`), `IPageHeaderStateHolder`,
  the modal dialog pattern (`CourseFormDialog`, and `FlashcardDeckFormDialog` with its
  "Belongs to" dropdown), and `TimeProvider` plus an options-based time zone (`StudyDayProvider`,
  `FlashcardStudyOptions`).

## 3. Scope

In scope:

1. **Month view** (section 5.2).
2. **Week view** (section 5.3).
3. **Session display**: session cards, chips and blocks, course colors, the day panel and the
   Dashboard card (section 5.4).
4. **Session CRUD**: add, edit and delete a session in a dialog (section 5.5). Without it there is
   nothing to display.

Out of scope (follow-ups, section 11):

- Recurring sessions (weekly series, e.g. a fixed weekly lecture slot).
- Drag and drop to move or resize sessions.
- Session tracking: mark as done, actual duration, timer, statistics (milestone 7 and Analytics).
- Deadlines, exams and semester start/end markers. There is no deadline entity yet.
- Day view, agenda list view, year view.
- iCal export or import, Google/Outlook sync, reminders and notifications.
- Flashcard due counts per day.

## 4. Decisions (please confirm)

- **The week starts on Monday (ISO 8601).** The mockup starts on Sunday (US style). Germany and
  ISO 8601 use Monday, and the week view shows the ISO week number ("Week 41"), computed with
  `System.Globalization.ISOWeek`. See open question 3.
- **Sessions use local wall-clock time, not UTC.** A session stores `Date` (`DateOnly`),
  `StartTime` (`TimeOnly`) and `DurationMinutes`. A session planned for 09:00 stays at 09:00 across
  daylight saving changes, and the UI needs no time zone conversion. `Semester` already uses
  `DateOnly` the same way.
  - A session ends on the same day: `StartTime + DurationMinutes ≤ 24:00`. This keeps both views
    simple, since a session never spans two cells.
  - Only "today" depends on a time zone. It comes from a new `CalendarOptions.TimeZone`
    (`Calendar:TimeZone`, default `Europe/Berlin`), validated at startup like
    `FlashcardStudyOptions` (COD-003).
- **A session belongs to at most one course or semester, or to neither.** This is the rule
  `FlashcardDeck` already uses (`CK_FlashcardDecks_AtMostOneParent`). A free session such as
  "Exam planning" needs no course.
  - Color: the course color. A semester session or a session without a course uses the accent
    color.
  - An archived course or semester cannot be newly assigned. A session that is already linked
    keeps it and stays visible.
- **Sessions are deleted for real.** A session is a single calendar entry, not an aggregate that
  other data hangs on. This is the same reasoning as for single flashcards. There is no
  archive/restore and no "show archived" filter.
- **Overlapping sessions are allowed.** There is no conflict check. The week view places
  overlapping sessions side by side (section 6.3).
- **The Api returns ready-to-render calendar DTOs.** The server computes the visible period, the
  current date and the sessions per day (with course name, color and overlap lanes). The Razor
  components only render. All calendar logic is then covered by xUnit tests, which matters because
  the test project does not reference `StudyHub.UI`.
- **Limits:** title required, at most 200 characters. Location optional, at most 200 characters.
  Duration from 5 to 720 minutes (12 hours).
- **URL state:** `/calendar?view=week&date=2026-10-08` via `[SupplyParameterFromQuery]`. Without
  parameters the page shows the month that contains today. Reload and browser back keep the view.
- **Labels in English, 24-hour times,** like the rest of the UI. Strings stay hardcoded like
  everywhere else in the app (UIX-002, see section 10).
- **No new dependencies** (DEP-001). The grids are CSS grid plus Bootstrap. No calendar library
  and no JavaScript interop are needed.

## 5. UI

### 5.1 Page shell `/calendar`

- **Toolbar:** period title ("October 2026" or "Week 41 · 5 – 11 Oct 2026"), previous / next
  buttons, "Today", a Month | Week toggle (Bootstrap `btn-group`) and a primary "Add session"
  button.
- **Legend:** chips for the courses that have sessions in the visible period (as in the mockup).
- **Page header:** the subtitle shows the period title instead of the hardcoded "November 2025".
- **Layout** as in the mockup: calendar on the left, a 320 px day panel on the right
  (`cal-layout`). Below the Bootstrap `lg` breakpoint the panel moves under the calendar.

### 5.2 Month view

- Seven columns Monday to Sunday with weekday headers. Four to six rows: the whole weeks that cover
  the month (Monday on or before the 1st to Sunday on or after the last day).
- Days of the neighbouring months are dimmed (`.other`). Today's day number has the accent
  background. The selected day is outlined (`.sel`).
- Each cell shows up to three session chips ("09:00 Graph review"), ordered by start time, and
  "+N more" if there are more.
- A click on a cell or chip selects the day and shows it in the day panel. The selected day
  defaults to today if today is in the visible month, otherwise to the 1st.
- **Day panel:** weekday and date, the day's session cards, an empty state "No sessions scheduled",
  and "Add session" (prefilled with the selected day). A click on a session card opens the edit
  dialog. Chips do not open the dialog directly, since they are too small to hit reliably.

### 5.3 Week view

- Seven day columns Monday to Sunday. Column headers like "Mon 5"; today's column is highlighted.
- A time axis on the left with one row per hour. The default range is 07:00–22:00. It grows to the
  full hour before the earliest start and after the latest end in that week.
- Session blocks are placed by start time and duration (top and height in percent of the visible
  range). Blocks have a minimum height so that 15-minute sessions stay readable. Overlapping
  sessions share the column width by lane (`Lane`, `LaneCount` from the Api, section 6.3).
- A click on a block opens the edit dialog. A click on a day header selects that day in the day
  panel. A click on an empty hour opens "Add session" prefilled with that day and hour.
- On narrow screens the grid scrolls horizontally (minimum column width).

### 5.4 Session display

- **Color:** each chip, block and card sets a CSS custom property `--session-color` to the course
  color (or `var(--accent)`). Chips and blocks get a tinted background
  (`color-mix(in srgb, var(--session-color) 14%, var(--surface))`), a left border and colored
  text. Because the tint mixes with `--surface`, it works in the light and the dark theme.
- **`StudySessionCard`** (day panel and Dashboard): "09:00 – 10:30 · 90 min" in the monospace
  style of the mockup's `.sc-time`, the title, the course or semester name, and the location with a
  pin icon if set.
- **Month chip:** start time and title, cut off with an ellipsis.
- **Week block:** title, time range and, if there is room, the location.
- **Past sessions** (date before today) are shown with reduced opacity.
- **Accessibility:** chips and blocks are `<button>` elements with an `aria-label` such as
  "Graph review, 09:00 – 10:30, Algorithms". Color is never the only carrier of information.

### 5.5 Session dialog `StudySessionFormDialog`

- Modal like `FlashcardDeckFormDialog`. Fields:
  - Title;
  - "Belongs to": none, a course or a semester (archived ones are hidden unless the session is
    already linked to them);
  - Date (`<input type="date">`);
  - Start (`<input type="time" step="300">`);
  - Duration in minutes (number input, step 5, default 60);
  - Location.
- Edit mode has a "Delete" button with a confirmation step.
- Validation errors from the Api are shown in the dialog, as in the other dialogs. After saving or
  deleting, the current view reloads.

### 5.6 Dashboard

The placeholder card "Today's Sessions" shows today's sessions as `StudySessionCard`s, a
"View calendar" link (`/calendar?view=week`) and an empty state "No sessions today" with a
"Schedule a session" link.

## 6. Architecture Impact

The flow is the same as in every other domain:
page → Accessor (`Logic.Integration`) → Controller (`Api`) → Orchestrator (`Business`) →
Domain services and repositories. The code follows the current structure with `*.Contract`
projects, entity records in `Shared` and `*Lifecycle` Domain services (see section 10).

| Layer / project | New or changed types |
| --- | --- |
| `StudyHub.Shared/StudySessions/` | **Entity:** `StudySession` record (`Id`, `Title`, `CourseId?`, `SemesterId?`, `Date`, `StartTime`, `DurationMinutes`, `Location?`, `CreatedAt`, `UpdatedAt`) with constants for the limits. **DTO:** `StudySessionDto` (entity fields plus `EndTime`, `OwnerName?` = course or semester name, `Color?` = course color). **Requests:** `CreateStudySessionRequest`, `UpdateStudySessionRequest`. **Exceptions:** `StudySessionNotFoundException`, `StudySessionValidationException`. `StudySessionErrorCodes`. |
| `StudyHub.Shared/Calendar/` | `CalendarMonthDto` (`Year`, `Month`, `Today`, `Days`), `CalendarWeekDto` (`Start`, `End`, `IsoWeek`, `Today`, `Days`), `CalendarDayDto` (`Date`, `Sessions`), `CalendarSessionDto` (`Session`, `Lane`, `LaneCount`) |
| `StudyHub.Shared/Configuration/` | `CalendarOptions` (`TimeZone`) |
| `StudyHub.Data.Contract` | `IStudySessionRepository`: `GetByIdAsync`, `GetByDateRangeAsync(from, to)`, `AddAsync`, `Update`, `Remove`, `SaveChangesAsync` |
| `StudyHub.Data` | `StudySessionRepository`, `ApplicationDbContext` mapping, migration `AddStudySessions`, DI registration |
| `StudyHub.Logic.Domain(.Contract)` | `StudySessionLifecycle` (create and update rules, section 6.1), `CalendarPeriodProvider` (today in the configured time zone, visible month and week, section 6.2), `StudySessionLaneProcessor` (overlap lanes for the week view, section 6.3). Records `CalendarPeriod` and `StudySessionLane`. |
| `StudyHub.Logic.Business(.Contract)` | `StudySessionOrchestrator` (create, update, delete; checks that the course or semester exists and is not archived), `CalendarOrchestrator` (month, week, today; groups sessions per day, adds owner name, color and lanes), `StudySessionMapper` (entity plus course/semester → `StudySessionDto`, like `FlashcardMapper`) |
| `StudyHub.Logic.Integration/` | `Calendar/ICalendarAccessor` (`GetMonthAsync`, `GetWeekAsync`), `StudySessions/IStudySessionAccessor` (`CreateAsync`, `UpdateAsync`, `DeleteAsync`, including `ProblemDetails` → exception mapping), `IDashboardAccessor.GetSessionsTodayAsync`, registration in `AddStudyHubIntegration` |
| `StudyHub.Api` | `Calendar/CalendarController`, `StudySessions/StudySessionController`, `StudySessions/StudySessionExceptionHandler`, `DashboardController` + `sessions-today`, `AddStudyHubCalendar` (binds `CalendarOptions`, `ValidateOnStart`), `appsettings.json` section `Calendar` |
| `StudyHub.UI` | `Calendar.razor` rewrite; `Components/Shared/`: `CalendarMonthView`, `CalendarWeekView`, `CalendarDayPanel`, `StudySessionCard`, `StudySessionFormDialog` (each with a `.razor.cs` code-behind); `Calendar/CalendarFormatter` (period titles, time ranges, durations); calendar styles in `app.css` (ported from the mockup's `.cal-*` and `.session-card` rules); Dashboard card |

**Dependencies (COD-006, at most seven):**

- `StudySessionOrchestrator`: session repository, `IStudySessionLifecycle`, course repository,
  semester repository (4).
- `CalendarOrchestrator`: session repository, course repository, semester repository,
  `ICalendarPeriodProvider`, `IStudySessionLaneProcessor` (5).
- `DashboardOrchestrator` already has seven dependencies, so it stays unchanged. The new Dashboard
  endpoint calls `ICalendarOrchestrator.GetTodayAsync` from `DashboardController`, the way
  `FlashcardDeckController` already calls several orchestrators.

### API endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `api/calendar/month?year=2026&month=10` | `CalendarMonthDto`; `400` if `month` is not 1–12 (`[Range]` on the query parameters) |
| `GET` | `api/calendar/week?date=2026-10-08` | `CalendarWeekDto` for the week that contains `date` |
| `POST` | `api/study-sessions` | `CreateStudySessionRequest` → `StudySessionDto` |
| `PUT` | `api/study-sessions/{id}` | `UpdateStudySessionRequest` → `StudySessionDto` (route id wins, as in `NoteController`) |
| `DELETE` | `api/study-sessions/{id}` | `204` |
| `GET` | `api/dashboard/sessions-today` | `CalendarDayDto` for today |

Errors: `404` `study_session_not_found`, `400` `study_session_validation_failed`. A missing or
archived course or semester is reported by the existing `CourseExceptionHandler` and
`SemesterExceptionHandler`, as for notes and decks.

### 6.1 Session rules (`StudySessionLifecycle`)

- Title required, at most 200 characters.
- `DurationMinutes` from 5 to 720.
- `StartTime + DurationMinutes ≤ 24:00` (the session ends on the same day).
- At most one of `CourseId` / `SemesterId`.
- Location optional, at most 200 characters; empty becomes `null`.
- Ids with `Guid.CreateVersion7()` and timestamps from `TimeProvider`, as in
  `FlashcardDeckLifecycle`.

### 6.2 Visible periods (`CalendarPeriodProvider`)

- `GetToday()`: the current UTC instant from `TimeProvider`, converted to
  `CalendarOptions.TimeZone`, as a `DateOnly`.
- `GetMonth(year, month)`: from the Monday on or before the 1st to the Sunday on or after the last
  day of the month.
- `GetWeek(date)`: Monday to Sunday of the week that contains `date`.
- Registered as a singleton with the plain options object, like `StudyDayProvider`, so the Domain
  needs no Options package.

### 6.3 Overlap lanes (`StudySessionLaneProcessor`)

Per day: sort sessions by start time (longer first on a tie). Sessions that overlap directly or
through a chain form a group. Each session gets the first lane whose previous session has already
ended; `LaneCount` is the number of lanes its group uses. A session that starts exactly when
another ends does not overlap it.

## 7. Database Impact

One migration, **`AddStudySessions`**, with one new table. Existing tables stay as they are, and no
existing migration is edited (DAT-006).

| Column | Type | Notes |
| --- | --- | --- |
| `Id` | `Guid` | primary key |
| `Title` | text (200) | required |
| `CourseId` | `Guid?` | FK → `Courses`, `Restrict` |
| `SemesterId` | `Guid?` | FK → `Semesters`, `Restrict` |
| `Date` | `DateOnly` | stored by SQLite as ISO text (`yyyy-MM-dd`), so range queries sort correctly |
| `StartTime` | `TimeOnly` | |
| `DurationMinutes` | `int` | |
| `Location` | text (200), nullable | |
| `CreatedAt`, `UpdatedAt` | `DateTime` | |

- Check constraints: `CK_StudySessions_AtMostOneParent` (`CourseId IS NULL OR SemesterId IS NULL`)
  and `CK_StudySessions_Duration` (5–720). The same-day rule stays in the Domain only.
- Indexes: `Date` (range query of the month and week views), `CourseId`, `SemesterId`.
- `Restrict` on both foreign keys, like everywhere else: courses and semesters are only archived,
  never physically deleted.

## 8. Task Checklist (one commit per step)

1. Add this plan (this commit).
2. Shared: entity, DTOs, requests, exceptions, error codes, `CalendarOptions`.
3. Data: repository, `ApplicationDbContext` mapping, migration `AddStudySessions`, repository tests.
4. Domain: `StudySessionLifecycle`, `CalendarPeriodProvider`, `StudySessionLaneProcessor`, tests.
5. Business: `StudySessionOrchestrator`, `CalendarOrchestrator`, `StudySessionMapper`, DI, tests.
6. Api: controllers, exception handler, `CalendarOptions` binding and `appsettings.json`, Dashboard
   endpoint, Api tests.
7. Integration: accessors and registration, accessor tests against the real Api (as for
   flashcards).
8. UI, month view: page shell, toolbar, URL state, month grid, day panel, `StudySessionCard`,
   `CalendarFormatter`, CSS.
9. UI, session dialog: add, edit, delete.
10. UI, week view: time grid, blocks with lanes, click on an empty hour.
11. UI, Dashboard card "Today's Sessions".
12. Docs: `docs/agent-context.md` (map, glossary entry `StudySession`), README (features), plan
    status.

| Epic item | Steps |
| --- | --- |
| Month view (Monatsansicht) | 4 (`CalendarPeriodProvider`), 5, 6, 7, 8 |
| Week view (Wochenansicht) | 4 (`CalendarPeriodProvider`, `StudySessionLaneProcessor`), 5, 6, 7, 10 |
| Session display (Session Darstellung) | 2–7 (data and contracts), 8 (`StudySessionCard`, chips), 9, 10 (blocks), 11 |

## 9. Validation Plan

- `dotnet build StudyHub.slnx` without new warnings, `dotnet test StudyHub.slnx` green.
- **`StudySessionLifecycle`:** durations 4 / 5 / 720 / 721 minutes; 23:00 + 60 minutes is
  accepted, 23:30 + 60 minutes is rejected; course and semester together are rejected; title and
  location limits; timestamps with a fixed `TimeProvider`.
- **`CalendarPeriodProvider`:**
  - February 2027 starts on a Monday and has 28 days, so it shows exactly four rows;
  - November 2026 starts on a Sunday, so it shows six rows (26 Oct – 6 Dec);
  - December 2026 → January 2027 across the year boundary;
  - the week of a Sunday (e.g. 11 Oct 2026 → 5–11 Oct, ISO week 41);
  - "today" just before and after midnight in Europe/Berlin while UTC is still on the previous
    day, including the daylight saving change on 25 Oct 2026.
- **`StudySessionLaneProcessor`:** no overlap, two overlapping sessions, a chain A–B–C where A and C
  do not overlap, sessions that only touch, identical start times.
- **Orchestrators (Moq):** sessions grouped per day and ordered by start time; sessions outside the
  period are not returned; course name and color resolved; semester sessions show the semester name
  and no color; sessions of archived courses stay visible; creating with an unknown or archived
  course or semester fails; updating keeps an already linked archived course; deleting an unknown
  session fails.
- **Data (InMemory):** `GetByDateRangeAsync` includes both boundary days.
- **Api (`WebApplicationFactory`):** `month=13` → `400`; create / update / delete status codes and
  problem details; week endpoint shape.
- **Manual:** start Api and UI; create sessions in both views, including overlapping ones; navigate
  across the year boundary; reload a week URL; check the Dashboard card; check the light and dark
  theme and a narrow window.

## 10. Open Questions, Assumptions, Risks

### Open (please answer before implementation)

1. **Sessions in this epic:** is it OK that Epic 8 brings minimal **planned** sessions (CRUD from
   the calendar), while tracking (done, actual duration, timer) stays with milestone 7 "Study
   Sessions"? Recommendation: yes. Otherwise the calendar has nothing to show.
2. **Recurring sessions:** are they needed now (e.g. "every Monday 10:00")? Recommendation: no.
   Series need their own model (rule, end date, exceptions for single dates) and would roughly
   double this epic. They are additive on top of single sessions later.
3. **Start of the week:** Monday (ISO 8601, German convention) instead of the mockup's Sunday?
   Recommendation: Monday.

### Assumptions

- Single-user operation, as in the whole app: no user id on sessions.
- Time zone Europe/Berlin via `Calendar:TimeZone`.
- Sessions of archived courses or semesters stay visible in the calendar.
- No calendar library and no JavaScript (section 4).

### Risks

- **Wall-clock times** are not tied to an instant. If the app ever needs real instants (sync with
  external calendars, several time zones), sessions need a conversion with the configured time
  zone. An iCal export can still write local times with a `TZID`.
- **Same-day rule:** a session across midnight has to be entered as two sessions.
- **Very short sessions** in the week view get a minimum height and can cover the start of the next
  block. Acceptable for a first version.
- **Two time zone settings** (`Flashcards:TimeZone`, `Calendar:TimeZone`) can drift apart. They can
  be merged into one general setting later; doing it now would change the flashcard feature
  (GOV-005).
- **UIX-002:** UI strings stay hardcoded like in the rest of the app; there is no localization
  infrastructure yet.

### Documentation inconsistencies found while planning

Reported per `CLAUDE.md`, not resolved by guessing:

1. As in `flashcard-study-plan.md`: `CLAUDE.md` and `docs/agent-context.md` place Business
   contracts in `Contracts/` subfolders and entities and repository contracts in `Logic.Domain`.
   The code has `*.Contract` projects, entity records in `Shared` and repository contracts in
   `Data.Contract`. This plan follows the code.
2. ARC-007 / LAY-9 say Business components must not call aggregate repositories directly, but every
   existing orchestrator does. This plan follows the code.
3. COD-008 asks for `Guid.CreateVersion7()`, while `docs/agent-context.md` names `Guid.NewGuid()`.
   Older lifecycles use `NewGuid`, the flashcard lifecycles use `CreateVersion7`. This plan uses
   `CreateVersion7` (the rule).
4. The README sections "Status" and "Current Features" still say that no user-facing features
   exist, although semesters, courses, documents, notes and flashcards are implemented. Step 12
   adds the calendar to the feature list but does not rewrite those sections (GOV-005).
5. The mockup's calendar starts the week on Sunday (open question 3).

## 11. Follow-ups (not part of this feature)

- Recurring sessions (series with exceptions).
- Drag and drop to move and resize sessions.
- Mark sessions as done, actual duration, timer (milestone 7); statistics in Analytics.
- Deadlines and exams in the calendar; semester start and end markers.
- A "now" line in the week view.
- iCal (`.ics`) export.
- Day or agenda view for small screens.
