# Feature Plan: Analytics – Dashboard Statistics (Epic 12)

Status: Implemented on `feature-analytics`. Section 12 lists how the open questions were decided
and where the implementation differs from this plan.
Classification (per `CLAUDE.md`): **Medium**. It adds a new page, a new business workflow
(statistics over study sessions, flashcard reviews and practice exam attempts) and extends an
existing domain object (`StudySession` gets completion tracking). Per the Medium workflow this
plan covers the planned architecture, the affected layers, the required contracts and the
database impact. Because it defines what "study time" means and changes a stored entity, the
definitions in section 4 are listed for confirmation (GOV-003).

Epic items:

- [x] Study time (Lernzeit)
- [x] Progress (Fortschritt)
- [x] Charts

Section 8 maps each item to its implementation steps.

## 1. Goal

`/analytics` is still a "coming soon" stub, and the Dashboard's three stat tiles show hardcoded
numbers ("24.5h", "18", "32 Tasks completed"). This epic makes both real:

- **Study time:** how much the student actually studied today, this week and in the last weeks,
  and the learning streak.
- **Progress:** per course of the active semester, how far the flashcards are learned, how the
  latest practice exam went and how much time went into the course.
- **Charts:** the numbers above as bar chart, line chart, heatmap, streak strip and progress bars,
  on the Analytics page and (a subset) on the Dashboard.

## 2. Starting Point

- **Pages:** `Analytics.razor` only sets the page header ("Your productivity at a glance") and shows
  a placeholder. `Dashboard.razor.cs` holds a static `Stats` array with three fake tiles.
- **Study sessions are planned only.** `StudySession` has date, start, planned duration and an
  optional course or semester, but no "done" flag and no actual duration. `calendar-plan.md`
  explicitly deferred tracking to "milestone 7 and Analytics". A planned session says nothing
  about whether the student studied.
- **Flashcard review log:** `FlashcardReview` stores one row per answer with `ReviewedAt` (UTC,
  indexed), rating and state before. It has **no time taken** (Anki stores this; StudyHub does
  not). Reviews are deleted together with their card (cascade, as in Anki).
- **Practice exam attempts:** `PracticeExamAttempt` has `StartedAt`, `SubmittedAt`, `GradedAt`,
  `MaxPoints`, `AwardedPoints`; the exam has `DurationMinutes` and a `CourseId` or a `DeckId`.
- **Flashcard state:** `Flashcard` has `State` (New, Learning, Review, Relearning) and
  `IntervalDays`; `FlashcardDeck` has an optional `CourseId` / `SemesterId`.
- **Existing building blocks:** `IActiveSemesterProvider`, `ISemesterProgressCalculator` (time
  progress of the semester, already on the Dashboard), `IStudyDayProvider` (study day starts at
  04:00 Europe/Berlin, `Flashcards` options), `ICalendarPeriodProvider` (ISO weeks, `Calendar`
  options), `CalendarFormatter`, `StudySessionCard`, `StudySessionFormDialog`.
- **Mockup** (`docs/mockup/StudyHub.html`):
  - `pageAnalytics()`: four KPI tiles (study hours this week "+12%", learning streak "Best: 21",
    flashcards reviewed this week, exams upcoming in the next 6 weeks); "Weekly Activity" line
    chart (last 8 weeks); "Course Progress" bars per course in the course color; "Learning Streak"
    with a Monday-to-Sunday strip and a "Study heatmap · last 5 weeks"; "Upcoming Exams" list.
  - `pageDashboard()`: stat tiles and a "Study Statistics · Hours per day · this week" bar chart.
  - The charts are plain CSS bars (`.bars`) and one SVG path (`lineChart()`), no library.
- **No chart library** is referenced, and `bootstrap.min.css` is linked but not served (see
  `calendar-plan.md`, section 12), so styles go to `app.css` like the calendar's.

## 3. Scope

In scope:

1. **Study time** (section 4.1–4.5): a definition from three sources, completion tracking for
   study sessions, totals per day and week, streak.
2. **Progress** (section 4.6): per course of the active semester.
3. **Charts** (section 4.7, section 5): Razor components with inline SVG and CSS.
4. **Dashboard:** real stat tiles and a "This week" bar chart instead of the placeholders.

Out of scope (follow-ups, section 11):

- A live timer or Pomodoro mode to record study time while studying.
- Study goals (e.g. weekly target hours per course) and "on track" deltas.
- Measuring the answer time on the flashcard study page (section 4.1 estimates it instead).
- Anki-style flashcard statistics (forecast, retention, ease and interval distributions).
- Activity from notes and documents (there is no edit or read log).
- Date range picker, comparison of semesters, CSV export.
- Stored or cached aggregates (everything is computed per request, section 10).

## 4. Decisions (please confirm)

### 4.1 What counts as study time

Study time is built from **time intervals** of three sources. Everything else (notes, documents,
planned but not completed sessions, unfinished exam attempts) does not count.

| Source | Interval | Course |
| --- | --- | --- |
| **Completed study session** (new, section 4.4) | Planned date and start time, converted from wall-clock time to UTC with `Calendar:TimeZone`, length = actual duration | The session's course; none for a semester session or a session without owner |
| **Flashcard answer** | Ends at `ReviewedAt`; length = time since the previous answer (any deck), **capped at 60 s**; the first answer without a previous one counts 60 s | The deck's course, if any |
| **Submitted practice exam attempt** | `StartedAt` → `SubmittedAt`, capped at twice the exam's `DurationMinutes` | The exam's course, or the course of its source deck |

Why the flashcard estimate works: the time between two answers is the time the next card was on
screen (question, "Show answer", rating) — exactly what Anki records as "time taken". Anki caps it
at 60 s by default ("maximum answer seconds"), so a pause between two cards counts at most one
minute here as well. This needs **no schema change and also covers the existing review log**.
Measuring the time on the study page is the more exact alternative (open question 2).

Why the exam cap: an attempt without time limit can stay open overnight; twice the planned
duration keeps such an attempt from counting as a whole day.

### 4.2 Overlaps are counted once

A student who marks the session "Algorithms flashcards 09:00–10:30" as done and answers 300 cards
in that window studied 90 minutes, not 90 plus ~40. Rule (Domain, `StudyTimeProcessor`): sort all
intervals by start (sessions before answers and attempts on a tie). The part of an interval that is
already covered by an earlier interval is dropped. Overlapping time therefore goes to the course of
the activity that started first.

### 4.3 Days and weeks

- **Day = study day** (`IStudyDayProvider`, 04:00 Europe/Berlin), the same boundary as the
  flashcard daily limits. Studying until 01:00 counts for the evening before, which keeps a streak
  intact for night owls. An interval that crosses 04:00 is split between the two days.
- **Week = ISO week, Monday to Sunday** of study days (`ICalendarPeriodProvider.GetWeek`), like the
  calendar. "This week" is the week of today's study day.
- **Window:** statistics are computed over the last **365 study days** (streaks, 8-week chart,
  heatmap, semester totals clipped to the window). Older data is not loaded.

### 4.4 Completion tracking for study sessions

- `StudySession` gets `CompletedAt` (UTC, `DateTime?`) and `ActualDurationMinutes` (`int?`). Both
  `null` = not done; both set = done.
- **Mark as done** is possible when the session's date is today or earlier (calendar "today").
  Future sessions cannot be completed.
- The actual duration defaults to the planned duration and can be changed: 5 to 720 minutes, and
  start + actual duration ≤ 24:00 (the existing same-day rule).
- **Undo** resets both fields.
- Editing a done session keeps the completion. Moving a done session to a future date is rejected
  ("Undo 'done' first"), so no future study time can exist.
- Deleting a done session removes its study time (sessions are deleted for real); the delete
  confirmation says so.

### 4.5 Learning streak

- A study day **counts** when it has study time (any of the three sources).
- **Current streak:** consecutive counted days ending today. If today has no study time yet, the
  streak ends yesterday — it only breaks when a whole day is missed.
- **Longest streak:** the longest run within the 365-day window.

### 4.6 Progress per course

For each non-archived course of the active semester (the semester `IActiveSemesterProvider`
returns):

- **Flashcards:** cards of the non-archived decks linked to the course, in Anki's four buckets:
  - New;
  - Learning (Learning + Relearning);
  - Young (Review, interval < 21 days);
  - Mature (Review, interval ≥ 21 days).

  **Learned** = (Young + Mature) / all cards, rounded to whole percent; no value if the course has
  no cards. Decks linked only to a semester or to nothing are not part of any course row.
- **Practice exams:** the latest and the best result (percent of `MaxPoints`) of the graded
  attempts at the course's non-archived practice exams (exam linked to the course, or to a deck of
  the course), and the number of graded attempts.
- **Study time:** the course's minutes since the semester start (clipped to the window).

There is **no combined "course progress %"**: mixing cards, exam results and time into one number
would be arbitrary and hard to explain. The progress bar shows the flashcard buckets, the numbers
next to it show the rest (open question 3). Without an active semester the card shows the
Dashboard's empty state.

### 4.7 Charts without a library

- **No new dependency** (DEP-001). The mockup's charts are CSS bars and one SVG path, so they are
  Razor components with inline SVG/CSS. No JavaScript interop.
- **The Api returns ready-to-render series:** minutes per day and per week, heat level 0–4 per day,
  `IsToday` / `IsFuture` flags. All rules (estimates, overlaps, days, streaks, levels, buckets) are
  in the Domain and covered by xUnit tests. The UI only scales values to the chart height and
  formats text (`AnalyticsFormatter`), as `CalendarFormatter` does.
- **Heat levels** (fixed, so colors mean the same every week): 0 = no study time, 1 = under 30 min,
  2 = under 1 h, 3 = under 2 h, 4 = 2 h or more.
- **Colors** use the existing CSS variables: `--accent` for study time, the course color for course
  rows (`--session-color` pattern from the calendar), heatmap cells as
  `color-mix(in srgb, var(--accent) N%, var(--surface))`, so the light and dark theme both work.
- **Accessibility:** every chart has `role="img"` and an `aria-label` summary (e.g. "Study time this
  week: 6 h 30 min, most on Thursday with 2 h 10 min"). Bars, points and heatmap cells carry a
  `<title>` with the exact value (also the hover tooltip). Color is never the only carrier: values
  are also printed as text.

### 4.8 KPI comparisons

- The mockup's "+12%" is shown as an **absolute difference to the same days of last week**
  ("+1.5 h vs. last week"): this week Monday to today against last week Monday to the same
  weekday. A full previous week would make every Monday look bad, and a percentage breaks when
  last week was zero.
- The fourth KPI tile is **"Sessions done this week: 5 of 7"** (planned sessions of this week up
  to today) instead of the mockup's "Exams upcoming", which the Dashboard already lists
  (open question 5).
- The Dashboard's "Tasks completed" tile has no data source (there is no task entity). It becomes
  **"Flashcards reviewed this week"** (open question 6).

### 4.9 Further decisions

- **Labels in English, 24-hour times,** hardcoded like the rest of the UI (UIX-002, section 10).
- **Durations** are shown as "6 h 30 min" in tooltips and lists and as "24.5 h" in KPI tiles.
- **No caching** (ADR01-001): statistics are computed per request from a few projected queries.

## 5. UI

### 5.1 Analytics page `/analytics`

Layout as in the mockup (`an-grid-4`, `an-grid-2`), one column below the `lg` breakpoint.

1. **KPI row** (`StatTile` ×4):
   - Study time · this week, with the difference to last week;
   - Learning streak · days, with "Best: N";
   - Flashcards reviewed · this week, with the difference to last week;
   - Sessions done · this week, "5 of 7".
2. **Row 2:**
   - "This week" — `StudyTimeBarChart`: one bar per day Monday to Sunday, today highlighted,
     future days dimmed, tooltip with the exact time and the number of answers.
   - "Weekly study time · last 8 weeks" — `StudyTimeLineChart`: SVG line with area as in the
     mockup, x labels "W35 … W42" (ISO week), current week labelled "Now".
3. **Row 3:**
   - "Course progress" — `CourseProgressList`: per course color dot, name, a stacked
     `FlashcardProgressBar` (mature in course color, young lighter, learning lightest, new as
     track), "72 % learned", latest/best practice exam result and study time this semester. Empty
     states: no active semester; a course without cards ("No flashcards yet").
   - "Learning streak" — `StudyStreakStrip` (Monday to Sunday of this week, studied days filled)
     and `StudyHeatmap` (last 5 ISO weeks, 7 columns Monday to Sunday, future cells empty).
4. **Empty state** when the window has no study time at all: "No study time recorded yet" with
   links "Mark a session as done" (`/calendar`) and "Study flashcards" (`/flashcards`).

### 5.2 Dashboard

- The three placeholder tiles become `StatTile`s with real values: study time this week (with
  difference), learning streak, flashcards reviewed this week.
- New card "This week" with the `StudyTimeBarChart` and a "View analytics" link — the mockup's
  "Study Statistics" card.
- The "Today's Sessions" card gets the "Mark as done" button through `StudySessionCard` (5.3).

### 5.3 Session tracking in the calendar

- **`StudySessionCard`** (day panel and Dashboard): a done session shows a check icon and
  "Done · 75 min"; a session of today or earlier that is not done shows a small "Mark as done"
  button, which completes it with the planned duration in one click.
- **`StudySessionFormDialog`:** for a session dated today or earlier, a "Done" checkbox and an
  "Actual duration (min)" field (step 5, prefilled with the planned duration).
- **Month chips and week blocks** of done sessions get a check mark and the `aria-label` suffix
  ", done".

## 6. Architecture Impact

The flow is the same as in every other domain:
page → Accessor (`Logic.Integration`) → Controller (`Api`) → Orchestrator (`Business`) →
Domain services and repositories. The code follows the current structure with `*.Contract`
projects and entity records in `Shared` (see section 10).

| Layer / project | New or changed types |
| --- | --- |
| `StudyHub.Shared/StudySessions/` | `StudySession` + `CompletedAt?`, `ActualDurationMinutes?`; `StudySessionDto` + the same fields and `IsCompleted`; **Request:** `CompleteStudySessionRequest` (`ActualDurationMinutes`) |
| `StudyHub.Shared/Analytics/` | **DTOs:** `StudyTimeStatisticsDto` (`Today`, `ThisWeekMinutes`, `LastWeekToDateMinutes`, `ReviewsThisWeek`, `ReviewsLastWeekToDate`, `SessionsDoneThisWeek`, `SessionsPlannedThisWeek`, `CurrentStreakDays`, `LongestStreakDays`, `Days`, `Weeks`, `Heatmap`), `StudyDayDto` (`Date`, `Minutes`, `ReviewCount`, `IsToday`, `IsFuture`), `StudyWeekDto` (`WeekStart`, `IsoWeek`, `Minutes`), `StudyHeatmapDayDto` (`Date`, `Minutes`, `Level`, `IsFuture`), `CourseProgressOverviewDto` (`HasActiveSemester`, `SemesterName?`, `Courses`), `CourseProgressDto` (`CourseId`, `Name`, `Color`, `StudyMinutes`, `Flashcards`, `LatestExamPercent?`, `BestExamPercent?`, `GradedAttempts`), `FlashcardProgressDto` (`New`, `Learning`, `Young`, `Mature`, `Total`, `LearnedPercent?`). **Read-model rows:** `FlashcardReviewActivity` (`ReviewedAt`, `CourseId?`), `PracticeExamAttemptActivity` (`StartedAt`, `SubmittedAt`, `ExamDurationMinutes`, `CourseId?`), `PracticeExamResult` (`CourseId?`, `GradedAt`, `AwardedPoints`, `MaxPoints`), `FlashcardDeckProgressCounts` (`DeckId`, `CourseId?`, `New`, `Learning`, `Young`, `Mature`) with the constant `MatureIntervalDays = 21` |
| `StudyHub.Data.Contract` | **New read-model repository** `IStudyAnalyticsRepository` (query only, LAY-9): `GetSessionsAsync(from, to)`, `GetReviewActivitiesAsync(fromUtc, toUtc)`, `GetSubmittedAttemptActivitiesAsync(fromUtc, toUtc)`, `GetDeckProgressCountsAsync()`, `GetGradedResultsAsync()` |
| `StudyHub.Data` | `StudyAnalyticsRepository` (projections only, `AsNoTracking`, grouping in SQL where possible), `StudySession` mapping (new columns, check constraints), migration `AddStudySessionCompletion`, DI registration |
| `StudyHub.Logic.Domain(.Contract)` | `StudySessionLifecycle` + `Complete`, `ResetCompletion` and the update rule (4.4); `StudyTimeProcessor` (intervals of the three sources, 60 s cap, exam cap, overlap clipping, split by study day, minutes per day and per course, heat level — 4.1–4.3, 4.7); `StudyStreakProvider` (current and longest streak, 4.5); `CourseProgressProcessor` (learned percent, latest and best exam result per course, 4.6). Records `StudyActivities` (input), `StudyTimeDay` (`Date`, `Minutes`, `ReviewCount`, `MinutesByCourse`), `StudyStreak` (`Current`, `Longest`) |
| `StudyHub.Logic.Business(.Contract)` | `StudySessionOrchestrator` + `CompleteAsync`, `ResetCompletionAsync`; `StudyTimeOrchestrator` (`GetStatisticsAsync`: loads the window, calls the Domain, assembles the week, 8 weeks, heatmap and KPI values); `CourseProgressOrchestrator` (`GetOverviewAsync`); `StudySessionMapper` (new fields) |
| `StudyHub.Logic.Integration/` | `Analytics/IAnalyticsAccessor` (`GetStudyTimeAsync`, `GetCourseProgressAsync`); `IStudySessionAccessor` + `CompleteAsync`, `ResetCompletionAsync`; `IDashboardAccessor` + `GetStudyTimeAsync`; registration in `AddStudyHubIntegration` |
| `StudyHub.Api` | `Analytics/AnalyticsController`; `StudySessionController` + completion actions; no new exception handler (validation and not-found reuse `StudySessionExceptionHandler`) |
| `StudyHub.UI` | `Analytics.razor` rewrite; `Components/Shared/`: `StatTile`, `StudyTimeBarChart`, `StudyTimeLineChart`, `StudyHeatmap`, `StudyStreakStrip`, `CourseProgressList`, `FlashcardProgressBar` (each with `.razor.cs`); `Analytics/AnalyticsFormatter` (durations, differences, aria labels, bar heights, SVG points); changes to `StudySessionCard`, `StudySessionFormDialog`, `CalendarMonthView`, `CalendarWeekView`, `Dashboard`; chart styles in `app.css` (ported from the mockup's `.bars`, `.line-chart`, `.heatmap`, `.streak-days`, `.progress-legend-item`, `.an-grid-*`) |

**Why a read-model repository:** the statistics need small projections over four aggregates
(sessions, reviews joined to card and deck, attempts joined to exam and deck, card counts per deck).
Spreading them over the aggregate repositories would give the orchestrators more than seven
dependencies (COD-006) and load whole entities. LAY-9 allows "dedicated read-model repositories …
consumed by Business Logic when explicitly designed for that purpose"; `IStudyAnalyticsRepository`
has no mutation methods.

**Dependencies (COD-006, at most seven):**

- `StudyTimeOrchestrator`: `IStudyAnalyticsRepository`, `IStudyDayProvider`,
  `ICalendarPeriodProvider`, `IStudyTimeProcessor`, `IStudyStreakProvider` (5).
- `CourseProgressOrchestrator`: `IStudyAnalyticsRepository`, `ISemesterRepository`,
  `ICourseRepository`, `IActiveSemesterProvider`, `IStudyDayProvider`, `IStudyTimeProcessor`,
  `ICourseProgressProcessor` (7).
- `StudySessionOrchestrator`: unchanged dependencies; completion uses `IStudySessionLifecycle`,
  which gets "today" from `ICalendarPeriodProvider`.
- `DashboardOrchestrator` stays unchanged (already seven dependencies). The Dashboard reads study
  time from the analytics endpoint through `IDashboardAccessor.GetStudyTimeAsync`.
- `StudyTimeProcessor` is a singleton with `CalendarOptions` (session wall-clock → UTC) and
  `IStudyDayProvider`, like `StudyDayProvider` with its options.

### API endpoints

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `api/analytics/study-time` | `StudyTimeStatisticsDto` (Analytics page and Dashboard) |
| `GET` | `api/analytics/course-progress` | `CourseProgressOverviewDto` |
| `PUT` | `api/study-sessions/{id}/completion` | `CompleteStudySessionRequest` → `StudySessionDto` (mark as done or change the actual duration) |
| `DELETE` | `api/study-sessions/{id}/completion` | → `StudySessionDto` (undo) |

Errors: `404` `study_session_not_found`; `400` `study_session_validation_failed` for a future
session, an actual duration outside 5–720 minutes or past 24:00.

### 6.1 Study time (`StudyTimeProcessor`)

1. Build intervals (UTC, with optional course) from completed sessions, answers and submitted
   attempts as in 4.1. Session wall-clock times are converted with `Calendar:TimeZone`; a start in
   a daylight-saving gap moves forward like `StudyDayProvider.GetStart`.
2. Sort by start (sessions first on a tie) and clip every interval to the part after the end of
   everything before it (4.2).
3. Split each remaining interval at the study-day starts (`IStudyDayProvider.GetStart`) and add
   its seconds to the day and course. Count answers per day for `ReviewCount`.
4. Return whole minutes per day (rounded at the end, not per interval).

### 6.2 Assembling the statistics (`StudyTimeOrchestrator`)

Only selection and mapping, no rules (NAM-002): today = `IStudyDayProvider.GetCurrent().Date`;
this week, the last 8 weeks and the 5 heatmap weeks from `ICalendarPeriodProvider.GetWeek`; days
after today are `IsFuture`; the "to date" sums of last week cover Monday to the same weekday.

## 7. Database Impact

One migration, **`AddStudySessionCompletion`**. It adds two nullable columns to `StudySessions`;
existing rows stay "not done". No existing migration is edited (DAT-006), no other table changes.

| Column | Type | Notes |
| --- | --- | --- |
| `CompletedAt` | `DateTime?` | UTC instant the session was marked as done |
| `ActualDurationMinutes` | `int?` | 5–720 |

- Check constraints: `CK_StudySessions_Completion` (`CompletedAt` and `ActualDurationMinutes` are
  both null or both set) and `CK_StudySessions_ActualDuration` (null or 5–720). The same-day rule
  and "not in the future" stay in the Domain.
- Indexes: none new. `StudySessions.Date` and `FlashcardReviews.ReviewedAt` are already indexed;
  attempts are few and filtered by `SubmittedAt` after the join with their exam.
- The read-model queries only read existing tables.

## 8. Task Checklist (one commit per step)

1. Add this plan (this commit).
2. Shared: session completion fields and request, analytics DTOs and read-model rows.
3. Data: `StudySession` mapping, check constraints, migration `AddStudySessionCompletion`,
   `StudyAnalyticsRepository`, repository tests.
4. Domain: `StudySessionLifecycle` completion rules; `StudyTimeProcessor`, `StudyStreakProvider`,
   `CourseProgressProcessor`; tests.
5. Business: `StudySessionOrchestrator` completion, `StudyTimeOrchestrator`,
   `CourseProgressOrchestrator`, `StudySessionMapper`, DI; tests.
6. Api: completion actions, `AnalyticsController`; Api tests.
7. Integration: `IAnalyticsAccessor`, session and Dashboard accessor methods, registration;
   accessor tests against the real Api (as for the calendar).
8. UI, session tracking: `StudySessionCard`, `StudySessionFormDialog`, done marks in the month and
   week views.
9. UI, charts: `StatTile`, `StudyTimeBarChart`, `StudyTimeLineChart`, `StudyHeatmap`,
   `StudyStreakStrip`, `FlashcardProgressBar`, `AnalyticsFormatter`, CSS.
10. UI, Analytics page: KPI row, chart cards, `CourseProgressList`, empty states.
11. UI, Dashboard: real stat tiles and the "This week" card.
12. Docs: `docs/agent-context.md` (map, glossary: study time, streak, session completion), README
    (features, roadmap), plan status.

| Epic item | Steps |
| --- | --- |
| Study time (Lernzeit) | 2–7 (tracking, `StudyTimeProcessor`, `StudyStreakProvider`, endpoint), 8 (mark as done), 10 and 11 (KPI tiles) |
| Progress (Fortschritt) | 2–7 (`CourseProgressProcessor`, read model, endpoint), 10 (`CourseProgressList`) |
| Charts | 5 (chart series in `StudyTimeOrchestrator`), 9 (components), 10 (Analytics page), 11 (Dashboard) |

## 9. Validation Plan

- `dotnet build StudyHub.slnx` without new warnings, `dotnet test StudyHub.slnx` green,
  `dotnet csharpier format .` clean.
- **`StudyTimeProcessor`:**
  - answer gaps of 10 s, 60 s and 61 s (→ 60 s); the first answer counts 60 s;
  - a completed session counts its actual, not its planned duration; an uncompleted one counts 0;
  - an attempt of a 60-minute exam submitted after 3 h counts 120 min; an unsubmitted one 0;
  - a session 09:00–10:30 with answers inside counts 90 min; answers that start inside and end
    after the session add only the part after 10:30; equal starts prefer the session;
  - reviews between 03:50 and 04:10 are split between two study days;
  - a session on the daylight-saving change (25 Oct 2026, 28 Mar 2027) is converted correctly;
  - course attribution: deck without course, semester session, exam from a deck of a course;
  - heat level boundaries 0 / 1 / 29 / 30 / 59 / 60 / 119 / 120 minutes.
- **`StudyStreakProvider`:** no days → 0; today studied; today not yet studied but yesterday →
  streak continues; one missed day breaks it; longest vs. current; a streak as long as the window.
- **`CourseProgressProcessor`:** bucket sums and learned percent (rounding); no cards → no value;
  latest vs. best result; ungraded attempts ignored; archived exams and decks ignored.
- **`StudySessionLifecycle`:** complete a future session → rejected; actual 4 / 5 / 720 / 721;
  23:00 + 60 accepted, 23:30 + 60 rejected; undo; moving a done session to the future → rejected;
  editing a done session keeps the completion.
- **Orchestrators (Moq):** this week Monday to Sunday with `IsFuture`; last-week-to-date sum on a
  Wednesday; 8 weeks across a year boundary (ISO week 53 of 2026 → week 1 of 2027); 35 heatmap days
  Monday-aligned; sessions done/planned this week; no active semester → empty overview; archived
  courses excluded.
- **Data (InMemory):** review activities carry the deck's course; mature boundary 20 / 21 days;
  archived decks excluded from progress counts; only submitted attempts and graded results;
  completion columns round-trip.
- **Api (`WebApplicationFactory`):** analytics shapes; completion `200` / `400` / `404`.
- **Manual:** start Api and UI; mark sessions as done (card button and dialog); study a deck and
  submit a practice exam; compare the Analytics page and Dashboard with the expected numbers; check
  light and dark theme, a narrow window, and the screen-reader labels of the charts.

## 10. Open Questions, Assumptions, Risks

### Open questions (please answer before implementation)

1. **Planned sessions:** count study time only for sessions explicitly marked as done
   (recommended), or count every past planned session automatically unless it is marked as
   skipped? Automatic counting needs no clicks but turns plans into "facts".
2. **Flashcard time:** estimate it from the review log (recommended, section 4.1), measure it on
   the study page (new nullable column on `FlashcardReviews`, `AnswerFlashcardRequest` gets the
   elapsed time; old reviews stay without time), or not count flashcards as study time at all?
3. **Course progress:** separate indicators — flashcard buckets, exam results, time
   (recommended) — or one combined percentage (needs a weighting the user can understand)?
4. **Day boundary:** study day starting 04:00 (recommended, matches flashcards) or midnight
   (matches the calendar)?
5. **Fourth KPI tile:** "Sessions done this week" (recommended) instead of the mockup's "Exams
   upcoming", which the Dashboard already shows?
6. **Dashboard "Tasks completed" tile:** replace with "Flashcards reviewed this week"
   (recommended, no task entity exists) or remove it?

### Assumptions

- Single-user operation, as in the whole app: no user id on any statistic.
- Time zone Europe/Berlin via `Flashcards:TimeZone` (days) and `Calendar:TimeZone` (session times).
- Time on archived courses still counts in the totals; archived courses only drop out of the
  course progress list.
- The read-model repository is acceptable as LAY-9 describes it (see inconsistency 3).

### Risks

- **Estimate error for flashcards:** at most one minute per pause; a card left on screen for ten
  minutes counts one minute. Accepted, as in Anki.
- **History shrinks when data is deleted:** deleting a card removes its reviews (cascade, as in
  Anki), deleting a done session removes its time. Statistics are always recomputed from current
  data.
- **Performance:** every request reads up to 365 days of answers (projected to timestamp and
  course). Fine for one user on SQLite; a long-term heavy user may need stored daily aggregates
  later (not an in-memory cache, ADR01-001).
- **Two time zone settings** (`Flashcards:TimeZone`, `Calendar:TimeZone`) can drift apart, as
  already noted in `calendar-plan.md`.
- **Study day vs. calendar day:** between 00:00 and 04:00 the calendar already shows the new date,
  while the statistics still count for the previous day.
- **UI formatting is not unit-tested:** the test project does not reference `StudyHub.UI`. All rules
  are therefore in the Domain; the UI only scales and formats.
- **UIX-002:** UI strings stay hardcoded like in the rest of the app; there is no localization
  infrastructure yet.

### Documentation inconsistencies found while planning

Reported per `CLAUDE.md`, not resolved by guessing:

1. **Epic number:** the request calls Analytics "Epic 12", the README roadmap lists it as milestone
   11 ("Analytics — learning analytics and progress dashboards"). This plan uses the request's
   number in the title and does not renumber the roadmap.
2. As in the earlier plans: `CLAUDE.md` and `docs/agent-context.md` place Business contracts in
   `Contracts/` subfolders and entities and repository contracts in `Logic.Domain`. The code has
   `*.Contract` projects, entity records in `Shared` and repository contracts in `Data.Contract`.
   This plan follows the code.
3. LAY-9 allows read-model repositories for Business Logic, while the rule catalog's "Deferred
   decisions" still lists "Whether read-model repositories may be consumed directly by Business
   Logic" as open, and ARC-007 forbids repository calls from Business in general (which every
   existing orchestrator does). This plan relies on LAY-9.
4. NAM-005 does not list `Calculator`, but `SemesterProgressCalculator` exists. The new Domain
   types use `Processor` and `Provider`.
5. `DashboardOrchestrator.GetSemesterProgressAsync` takes "today" from `DateTime.UtcNow` (UTC
   date), while newer code uses `TimeProvider` with a configured time zone. Not changed here
   (GOV-005); listed as a follow-up.
6. `docs/agent-context.md` says session tracking "is not implemented yet"; step 12 updates it.

## 11. Follow-ups (not part of this feature)

- Live timer / Pomodoro on a session that records the actual start and duration.
- Measured flashcard answer time (open question 2, if not chosen now).
- Study goals per week or per course, with "on track" indicators.
- Anki-style flashcard statistics: forecast, true retention, interval and ease distribution.
- Practice exam result trend per course (line chart of graded attempts).
- Planned vs. done time per week as a second line in the weekly chart.
- Date range selection and semester comparison.
- `DashboardOrchestrator` "today" via the configured time zone (inconsistency 5).
- One shared time zone setting for flashcards and calendar.

## 12. Implementation Notes

### Open questions (section 10)

The plan was confirmed for implementation without separate answers, so every open question was
decided as recommended:

1. Only sessions explicitly marked as done count as study time.
2. Flashcard time is estimated from the review log (gap to the previous answer, at most 60 s).
3. Course progress shows separate indicators: flashcard buckets, practice exam results, study time.
4. The day boundary is the study day start (04:00 Europe/Berlin).
5. The fourth KPI tile is "Sessions done · this week".
6. The Dashboard's "Tasks completed" tile became "Flashcards reviewed this week".

### Differences from the plan

- **Commit split:** the completion fields of `StudySession` and `StudySessionDto` went into the
  Data commit (step 3) together with the migration, not into the Shared commit. The SQLite tests
  run the real migrations, and EF Core refuses to migrate a model with pending changes, so the
  fields and the migration have to arrive together. The Shared commit holds the new analytics
  types and `CompleteStudySessionRequest`.
- **Completing a session that is already done** changes its actual duration and keeps the instant
  it was first marked as done (`CompletedAt`).
- **Actual duration on update:** the same-day rule is checked for the actual duration on every
  change, so moving a done session to a start where its actual duration would pass midnight is
  rejected, like moving it to a future date.
- **Calendar date vs. study day:** between 00:00 and 04:00 the calendar date is one day ahead of
  the study day, so the orchestrators read sessions up to the calendar date after today. Answers
  and attempts are read by the instants of the study days. The first answer of the 365-day window
  counts 60 s even if an earlier answer lies just before the window (at most one minute once).
- **"Sessions done this week"** counts the sessions dated from Monday to today (study day); a
  session planned for later today is already part of "planned".
- **Additional DTO fields and constants:** `StudyTimeStatisticsDto.HasStudyTime` (drives the empty
  state), `StudyTimeStatisticsDto.WindowDays`/`WeekCount`/`HeatmapWeekCount`,
  `StudyHeatmapDayDto.MaxLevel` and `CourseProgressOverviewDto.Empty`.
- **Additional types:** `CourseExamResults` (Domain.Contract, result of
  `ICourseProgressProcessor.GetExamResults`); `GetFlashcardProgress` returns the Shared
  `FlashcardProgressDto` directly, as `IStudyQueueProvider.GetCounts` does. `StudyInterval`
  (internal record of `StudyTimeProcessor`). In the UI: `DoneIcon` (check mark of a done session),
  `StatIcon` with `StatIconKind` (replaces the Dashboard's inline icon markup), and `ChartPoint`.
- **Rounding:** whole minutes per day and per course and day (`MidpointRounding.AwayFromZero`);
  course totals are sums of the daily minutes. Percentages are rounded the same way, so 199 of
  200 learned cards show as 100 %.
- **Session dialog:** "Done" and "Actual duration" are also offered for a new session dated today
  or earlier; the session is created and then completed, and deleted again if completing fails,
  so a retry does not add it twice. Unticking "Done" undoes it before the update, so the session
  can then move to the future. A done session keeps the controls when its date moves to the
  future, and the Api rejects the change until "Done" is unticked. The delete button of a done
  session asks "Delete with its study time?".
- **Session card:** "Mark as done" is shown only where the page handles the result (day panel and
  Dashboard), and the card holds the main content and the button side by side, since a button
  cannot contain another button.
- **Analytics empty state:** without any study time the page shows the empty state and the course
  progress card (flashcards and exam results can exist without study time), not the charts.
- **Responsive layout:** the four KPI tiles use two columns below 1100 px and one below 640 px, as
  in the mockup; the chart rows use one column below the `lg` breakpoint.
- **Tests:** the analytics repository tests run against SQLite in memory with the real migrations
  (as `PracticeExamRepositoryTests` do) instead of the InMemory provider, so the joins, the
  grouping and the new check constraints are checked as in production. The Api tests use the
  real clock and complete a session dated yesterday in the calendar time zone, which lies in the
  statistics at any time of day.

### Found while implementing

- The Blazor error banner (`#blazor-error-ui`) is visible on every page, because no stylesheet
  hides it (`lib/bootstrap/dist/css/bootstrap.min.css` returns 404, see `calendar-plan.md`,
  section 12, and `app.css` has no rule for it). Not changed here (GOV-005).
