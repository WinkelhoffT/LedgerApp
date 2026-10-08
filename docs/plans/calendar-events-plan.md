# Feature Plan: Exams and Deadlines in the Calendar

Status: Planned.
Classification (per `CLAUDE.md`): **Medium**. It adds a new domain object (`CalendarEvent`), a CRUD
workflow, calendar display and a Dashboard card. This plan covers the planned architecture, the
affected layers, the required contracts and the database impact. It builds on
`calendar-plan.md`, which listed "deadlines and exams" as a follow-up.

## 1. Goal

Besides planned study sessions, the calendar shows fixed dates a student works towards:

- **Exams** (Prüfungen), usually with a start time, a duration and a room, or all-day if the time is
  not known yet;
- **Deadlines** (Abgaben), a date with an optional due time such as 23:59.

They are added, edited and deleted in the calendar, appear in the month view, the week view and the
day panel, and the Dashboard's "Upcoming Deadlines" placeholder lists the next ones with a
countdown ("in 12 days").

## 2. Decisions

Settled with the user before planning:

1. **Own entity, not a session type.** Exams and deadlines are not study time, so they must not
   count as study time once milestone 7 tracks it; they can be all-day and have a countdown.
2. **Two kinds: Exam and Deadline.** No free "other" kind.
3. **Dashboard:** the "Upcoming Deadlines" card shows the next exams and deadlines with a
   countdown.

Defaults taken in this plan:

- **Name:** `CalendarEvent` with `CalendarEventKind { Exam = 1, Deadline = 2 }` (0 is not a kind, so a
  missing value is rejected). UI labels say "Exam" and "Deadline".
- **Owner:** at most one of a course or a semester, or neither, as for sessions and decks. An
  archived course or semester cannot be newly assigned; an existing link is kept.
- **Time:**
  - an exam is either all-day (no time) or has a start time **and** a duration (5–720 minutes,
    ending by midnight, as for sessions);
  - a deadline has a date and an optional due time, never a duration.
  Date and time are local wall-clock values like sessions; start times are kept to the minute.
- **Delete for real**, like sessions. No archive.
- **Limits:** title required, at most 200 characters; location optional, at most 200 characters.

## 3. Scope

In scope:

1. CRUD for exams and deadlines from the calendar (dialog).
2. Month view: event chips before session chips, with a kind icon; they count towards the three
   chips per day.
3. Week view: an all-day row under the day headers for all-day exams and deadlines; timed exams as
   blocks in the time grid, sharing the overlap lanes with sessions.
4. Day panel: event cards (kind badge, time or "All day" / "Due 23:59", title, owner, location)
   above the session cards; buttons "Add session" and "Add exam or deadline".
5. Dashboard card "Upcoming exams & deadlines": the next five from today on, with "Today",
   "Tomorrow" or "in N days".

Out of scope (follow-ups): recurring events, reminders/notifications, exam results or grades,
study plans generated towards an exam, semester start/end markers, iCal export.

## 4. Architecture Impact

Same flow as the calendar: page → Accessor (`Logic.Integration`) → Controller (`Api`) →
Orchestrator (`Business`) → Domain services and repositories.

| Layer / project | New or changed types |
| --- | --- |
| `StudyHub.Shared/CalendarEvents/` | **Entity:** `CalendarEvent` record (`Id`, `Kind`, `Title`, `CourseId?`, `SemesterId?`, `Date`, `StartTime?`, `DurationMinutes?`, `Location?`, `CreatedAt`, `UpdatedAt`) with limits. `CalendarEventKind`. **DTOs:** `CalendarEventDto` (entity fields plus `EndTime?`, `OwnerName?`, `Color?`), `UpcomingCalendarEventDto` (`Event`, `DaysUntil`). **Requests:** `CreateCalendarEventRequest`, `UpdateCalendarEventRequest`. **Exceptions:** `CalendarEventNotFoundException`, `CalendarEventValidationException`. `CalendarEventErrorCodes`. |
| `StudyHub.Shared/Calendar/` | `CalendarEventEntryDto` (`Event`, `Lane`, `LaneCount`); `CalendarDayDto` gains `Events` |
| `StudyHub.Data.Contract` | `ICalendarEventRepository`: `GetByIdAsync`, `GetByDateRangeAsync(from, to)`, `GetUpcomingAsync(from, count)`, `AddAsync`, `Update`, `Remove`, `SaveChangesAsync` |
| `StudyHub.Data` | `CalendarEventRepository`, mapping, migration `AddCalendarEvents`, DI |
| `StudyHub.Logic.Domain(.Contract)` | `CalendarEventLifecycle` (rules of section 2). The lane and hour rules become time-slot based so they cover sessions and timed exams: `IStudySessionLaneProcessor` → `ICalendarLaneProcessor` over `CalendarTimeSlot` (`Id`, `StartTime`, `DurationMinutes`) returning `CalendarLane` (`Id`, `Lane`, `LaneCount`); `ICalendarPeriodProvider.GetWeekHours` takes time slots. |
| `StudyHub.Logic.Business(.Contract)` | `CalendarEventOrchestrator` (create, update, delete, upcoming), `CalendarEventMapper`; `CalendarOrchestrator` also loads events per period, places timed exams in the lanes and widens the week hours for them |
| `StudyHub.Logic.Integration/` | `CalendarEvents/ICalendarEventAccessor` (create, update, delete with `ProblemDetails` mapping), `IDashboardAccessor.GetUpcomingEventsAsync` |
| `StudyHub.Api` | `CalendarEvents/CalendarEventController` (`api/calendar-events`), `CalendarEventExceptionHandler`, `DashboardController` + `upcoming-events` |
| `StudyHub.UI` | `CalendarEventCard`, `CalendarEventFormDialog`; month chips, week all-day row and exam blocks, day panel, toolbar button; Dashboard card; `CalendarFormatter` (event time, countdown) |

**Dependencies (COD-006):** `CalendarEventOrchestrator`: event repository, lifecycle, course
repository, semester repository, period provider (5). `CalendarOrchestrator`: session repository,
event repository, course repository, semester repository, period provider, lane processor (6).

### API endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `api/calendar-events` | `CreateCalendarEventRequest` → `CalendarEventDto` |
| `PUT` | `api/calendar-events/{id}` | `UpdateCalendarEventRequest` → `CalendarEventDto` (route id wins) |
| `DELETE` | `api/calendar-events/{id}` | `204` |
| `GET` | `api/dashboard/upcoming-events` | the next five `UpcomingCalendarEventDto` from today on |

The month and week endpoints keep their routes; their days now also carry `Events`.
Errors: `404` `calendar_event_not_found`, `400` `calendar_event_validation_failed`; course and
semester errors as before.

## 5. Database Impact

One migration, **`AddCalendarEvents`**, with one new table `CalendarEvents`: `Id`, `Kind` (int),
`Title` (200), `CourseId?` and `SemesterId?` (FK, `Restrict`), `Date`, `StartTime?`,
`DurationMinutes?`, `Location?` (200), `CreatedAt`, `UpdatedAt`.

- Check constraints: `CK_CalendarEvents_AtMostOneParent`, `CK_CalendarEvents_Duration` (null or
  5–720), `CK_CalendarEvents_DurationNeedsStart` (no duration without a start time). The kind
  specific rules stay in the Domain.
- Indexes: `Date`, `CourseId`, `SemesterId`.
- Existing tables and migrations stay unchanged.

## 6. Task Checklist (one commit per step)

1. Add this plan.
2. Shared: entity, kind, DTOs, requests, exceptions, error codes; `CalendarDayDto.Events`.
3. Data: repository, mapping, migration, repository tests.
4. Domain: `CalendarEventLifecycle`; time-slot based lanes and week hours; tests.
5. Business: `CalendarEventOrchestrator`, `CalendarEventMapper`, events in `CalendarOrchestrator`, DI,
   tests.
6. Api: controller, exception handler, Dashboard endpoint, tests.
7. Integration: accessor, Dashboard accessor, tests against the real Api.
8. UI: event chips, all-day row, exam blocks, event cards, dialog, toolbar and panel buttons.
9. UI: Dashboard card "Upcoming exams & deadlines".
10. Docs: `agent-context.md`, README, plan status.

## 7. Validation Plan

- `dotnet build StudyHub.slnx` without new warnings; `dotnet test StudyHub.slnx` with no new
  failures.
- **Lifecycle:** all-day exam; timed exam needs a duration; exam with a duration but no time is
  rejected; exam ending after midnight is rejected; deadline with and without due time; deadline
  with a duration is rejected; unknown kind, missing title, too long title/location, course and
  semester together are rejected.
- **Lanes:** a timed exam and an overlapping session share the lanes; week hours widen for an early
  exam.
- **Orchestrators (Moq):** events grouped per day, all-day first; owner name and color; archived
  course rules as for sessions; upcoming events from today on with `DaysUntil`, at most five.
- **Data (InMemory):** date range includes both boundaries; upcoming ordered by date and time.
- **Api:** status codes and problem details; days carry events; Dashboard endpoint.
- **Manual:** create an all-day exam, a timed exam overlapping a session and a deadline; check
  month, week, day panel and Dashboard in both themes and on a narrow screen.

## 8. Risks

- `CalendarDayDto` gains a field, so every consumer of the month and week endpoints gets the events;
  all consumers live in this repository.
- The lane processor is renamed and generalized; it was added on this branch and is not merged yet.
- Timed exams in the week view use the same minimum block height as sessions.
