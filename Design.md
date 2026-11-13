# Design

This document describes the design of the Course Registration application.
It records the current state and the decisions made so far, and is updated as
features are implemented.

## 1. Purpose

A console application that lets users register for courses.

## 2. Current state

| Area | Status |
|------|--------|
| Solution / projects | `CourseRegistration` (console app) and `CourseRegistration.Tests` (xUnit) |
| Target framework | .NET 10 (`net10.0`) for both projects |
| Language features | Implicit usings and nullable reference types enabled |
| Entry point | `Program.cs` sets up course `.NET201` (capacity 3, prerequisite `.NET101`) and 10 users, then runs the menu |
| Console UI | `RegistrationConsole`: interactive menu for enroll, drop, status and exit |
| Domain model | `CourseOffering`, `User`, plus result types `EnrollmentResult`, `DropResult`, `EnrollmentStatus` |
| Services | `EnrollmentService`: enroll (with prerequisite check), drop and status, with a per-course waitlist |
| Persistence | None. Enrollment state lives in memory for the life of the service. |
| Tests | 97 unit tests (13 `CourseOffering`, 16 `User`, 5 `UserNames`, 42 `EnrollmentService`, 21 `RegistrationConsole`), all passing |

## 3. Technology choices

| Choice | Reason |
|--------|--------|
| .NET 10 console app | Simple to build and run, with no hosting setup. A good starting point before deciding on a UI or API. |
| Nullable reference types on | Catches null-handling bugs at compile time. Used to mark the prerequisite as optional (`string?`). |
| Implicit usings on | Less boilerplate for common namespaces. |
| `.slnx` solution format | The newer, readable XML solution format. |
| xUnit for tests | The default .NET test template, widely used, and supports data-driven tests (`[Theory]` / `[InlineData]`). |

## 4. Architecture

### 4.1 Projects

```
CourseRegistration.slnx
├── CourseRegistration          (console app)
│   ├── Program.cs              composition root: sample data + start the menu
│   ├── ConsoleUi/              interactive menu (RegistrationConsole)
│   ├── Models/                 domain classes and result types
│   └── Services/               business logic (EnrollmentService)
└── CourseRegistration.Tests    (xUnit)  ──references──▶ CourseRegistration
    ├── ConsoleUi/              tests mirror the folder of the class under test
    ├── Models/
    └── Services/
```

Dependencies point one way: `Program` → `ConsoleUi` → `Services` → `Models`.
`Models` depends on nothing. Business rules live only in `Models` and
`Services`. The console only gathers input and shows results.

Test files mirror the folder and namespace of the code they test, for example
`CourseRegistration.Models.CourseOffering` is tested by
`CourseRegistration.Tests.Models.CourseOfferingTests`.

### 4.2 Domain model

#### `CourseOffering` (`CourseRegistration/Models/CourseOffering.cs`)

A course that students can register for.

| Property | Type | Description |
|----------|------|-------------|
| `CourseCode` | `string` | Unique code for the course, e.g. `CS101`. Required. |
| `Capacity` | `int` | Maximum number of students. Must be greater than zero. |
| `PrerequisiteCourseCode` | `string?` | Code of the course that must be completed first, or `null` if none. |
| `HasPrerequisite` | `bool` (computed) | `true` when `PrerequisiteCourseCode` is not `null`. |

**Validation rules** (checked in the constructor):

| Rule | On failure |
|------|------------|
| `CourseCode` must not be null, empty or whitespace | `ArgumentException` |
| `Capacity` must be greater than 0 | `ArgumentOutOfRangeException` |
| `PrerequisiteCourseCode` must not equal `CourseCode` (case-insensitive) | `ArgumentException` |

**Normalisation:** leading and trailing whitespace is trimmed from both codes.
An empty or whitespace prerequisite is stored as `null` (no prerequisite).

**Design notes:**

- Properties are read-only (get-only, set in the constructor), so an offering is
  always in a valid state once created.
- Only a single prerequisite is supported, as requested. Supporting several
  would mean changing the property to a collection.
- The prerequisite is stored as a course code, not a reference to another
  `CourseOffering`. This keeps the class simple and avoids needing the other
  offering to exist first. Checking that the prerequisite code refers to a real
  course is left to a future service or catalog layer.

#### `User` (`CourseRegistration/Models/User.cs`)

A person who can enroll, with the courses they have already completed.

| Member | Type | Description |
|--------|------|-------------|
| `Name` | `string` | The user's name. Required, trimmed. Identifies the user within a course. |
| `CompletedCourseCodes` | `IReadOnlySet<string>` | Codes of completed courses. Compared ignoring case. |
| `HasCompleted(courseCode)` | `bool` | True if the code is in the completed set (ignores case and surrounding spaces). |
| `MeetsPrerequisiteFor(course)` | `bool` | True if the course has no prerequisite, or the user has completed it. |

**Validation and normalisation:** a blank name throws `ArgumentException`.
Completed course codes are trimmed. Blank entries are ignored, and duplicates
(including different case) are stored once.

**Design notes:**

- The prerequisite rule lives on `User` (`MeetsPrerequisiteFor`), so it can be
  tested on its own and reused outside the service.
- Completed courses are a read-only set. The user's history can't be changed
  after creation, and lookups are fast.

#### `UserNames` (`CourseRegistration/Models/UserNames.cs`)

A static class holding `Comparer` (`StringComparer.OrdinalIgnoreCase`), the
one rule for deciding whether two user names refer to the same user.
`EnrollmentService` (roster lookups) and `RegistrationConsole` (finding a user,
state labels) both use it. Changing how names match means changing only this file.

#### Result types (`CourseRegistration/Models/`)

| Type | Kind | Purpose |
|------|------|---------|
| `EnrollmentResult` | enum | `Enrolled`, `Waitlisted`, `AlreadyEnrolled`, `AlreadyWaitlisted`, `PrerequisiteNotMet` |
| `DropOutcome` | enum | `Dropped`, `DroppedAndPromoted`, `RemovedFromWaitlist`, `NotFound` |
| `DropResult` | record | `Outcome`, `PromotedUser` (who got the freed seat, or `null`) and `DroppedUser` (the dropped user's name as stored on the course, or `null` when not found) |
| `EnrollmentStatus` | record | `CourseCode`, `Capacity`, `Enrolled`, `Waitlisted`, computed `AvailableSeats` |

### 4.3 Services

#### `EnrollmentService` (`CourseRegistration/Services/EnrollmentService.cs`)

Manages enrollment for a fixed set of courses, passed to the constructor. For
each course it keeps a **roster**:

- **Enrolled list:** never holds more users than the course `Capacity`, kept in
  the order users got a seat.
- **Waitlist:** first in, first out. Users join at the end once the course is full.

```
                  ┌─────────── CourseRoster (one per course) ───────────┐
Enroll(code,name) │  Enrolled  [Alice, Bob]        (max = Capacity)     │
  ──────────────▶ │  Waitlist  [Carol, Dave]       (FIFO, unbounded)    │
                  └──────────────────────────────────────────────────────┘
Drop(code,"Bob")  →  Enrolled [Alice, Carol]   Waitlist [Dave]   (Carol promoted)
```

**Methods:**

| Method | Behaviour | Returns |
|--------|-----------|---------|
| `Enroll(courseCode, user)` | If the user is already enrolled or waitlisted, nothing changes. If they haven't completed the course's prerequisite, nothing changes. Otherwise they get a seat if one is free, or join the end of the waitlist. | `EnrollmentResult` |
| `Drop(courseCode, userName)` | If enrolled: removes them, then moves the first waitlisted user into the seat. If waitlisted: removes them from the waitlist. Otherwise nothing changes. The result reports the stored name of the dropped user, whatever case was typed. | `DropResult` |
| `Status(courseCode)` | Returns a snapshot of the enrolled and waitlisted users. | `EnrollmentStatus` |

**Enroll checks, in order:**

```
Enroll(courseCode, user)
  1. Course known?                  no  → KeyNotFoundException
  2. Already enrolled?              yes → AlreadyEnrolled
  3. Already waitlisted?            yes → AlreadyWaitlisted
  4. Prerequisite completed?        no  → PrerequisiteNotMet   (not enrolled, not waitlisted)
  5. Seat free?                     yes → Enrolled
                                    no  → Waitlisted
```

The duplicate checks (2, 3) come before the prerequisite check (4), so a user
already on a course is told so rather than getting a prerequisite error.

**Rules and invariants:**

- Only users who meet the prerequisite are ever enrolled or waitlisted. A
  user's completed courses can't be updated (decision 23), so a user promoted
  from the waitlist always meets it, and `Drop` doesn't need to re-check.
- `Enrolled.Count <= Capacity` at all times.
- A user appears at most once per course, across both lists.
- While the waitlist has users, the course is full. A drop always refills the seat.
- The same user can enroll in different courses.

**Errors:**

| Situation | Handling |
|-----------|----------|
| Already enrolled / already waitlisted / prerequisite not met / user not found on drop | Returned as a result value. These are normal outcomes the UI should report. |
| Null `User` passed to `Enroll` | `ArgumentNullException` |
| Blank user name (on `Drop`, or when creating a `User`) or blank course code | `ArgumentException` |
| Unknown course code | `KeyNotFoundException` |
| Duplicate course codes passed to the constructor | `ArgumentException` |

**Design notes:**

- **Expected outcomes are results, not exceptions.** A caller such as the
  console UI can `switch` on the result and show a message without try/catch.
  Exceptions are kept for programming errors and bad input.
- **Names and course codes ignore case and surrounding spaces.** `"alice"`,
  `" Alice "` and `"Alice"` are the same user. The name is stored as first
  entered (trimmed).
- **Lists, not `Queue<T>`, for the waitlist.** A waitlisted user can drop from
  the middle of the line, which `Queue<T>` doesn't support. Rosters are small,
  so the O(n) removal doesn't matter.
- **`Status` returns copies.** Callers can't change the service's internal
  state through the returned lists, and a snapshot doesn't change after it's taken.
- **`Enroll` takes a `User`; `Drop` takes a name.** Enrolling needs the
  user's completed courses for the prerequisite check. Dropping only needs to
  find them on the roster, which stores names.
- **Users are identified by name** within a course. It is assumed that no two
  users share a name, ignoring case (decision 22). This isn't checked in code.
- **The prerequisite must be completed, not just enrolled in.** Being enrolled
  in (or waitlisted for) the prerequisite course in this service doesn't count.
- **Not thread-safe.** Fine for a single-user console app. Concurrent use (for
  example behind a web API) would need locking.

### 4.4 Console application

#### `Program.cs`: setup

Builds everything the app needs, then hands control to the menu:

| Item | Value |
|------|-------|
| Course | `.NET201`, capacity 3, prerequisite `.NET101` |
| Users (eligible) | Alice, Bob, Carol, David, Emma: each has completed `.NET101` (some also have SQL100 / JS100) |
| Users (not eligible) | Frank, Henry, Jack (no completed courses); Grace (SQL100); Isla (JS100) |

Some ineligible users have other completed courses, which shows that only the
specific prerequisite counts.



#### `RegistrationConsole` (`CourseRegistration/ConsoleUi/RegistrationConsole.cs`)

A menu loop for one course. Before each menu it prints a summary line:

```
Course: .NET201 | Open seats: 3 of 3 | Prerequisite: .NET101 | Waitlist: 0
```

| Option | Flow |
|--------|------|
| 1. Enroll user | Lists all users (number, name, completed courses, eligible or not, current enrolled/waitlist state). Takes a number or name and calls `EnrollmentService.Enroll`. Prints a message for each `EnrollmentResult`, including waitlist position. |
| 2. Drop user | Lists users currently enrolled or waitlisted. Takes a number (from that list) or name and calls `Drop`. Prints the outcome, including who was promoted. |
| 3. Status | Prints the numbered enrolled list (`n of capacity`) and waitlist, or `(none)`. |
| 4. Exit | Also `q` / `exit`. End of input (Ctrl+Z / Ctrl+D, or the end of piped input) exits too. |

**Design notes:**

- **Testable I/O.** The class takes a `TextReader` and `TextWriter`. `Program`
  passes `Console.In`/`Console.Out`, and tests pass `StringReader`/`StringWriter`
  with scripted input.
- **Setup kept in `Program.cs`, menu in its own class.** The developer asked
  for the change in `Program.cs`, so the sample course and users live there.
  The loop was moved out so it can be unit-tested.
- **No business logic in the UI.** Eligibility shown in the list comes from
  `User.MeetsPrerequisiteFor`. Every enroll and drop goes through
  `EnrollmentService`, and messages are mapped from its result values.
- **Select by number or name.** Numbers are quicker to type. Names (ignoring
  case) are handy when you know who you want. In Drop, a number refers to the
  drop list shown, not the full user list.
- **State labels built once per list.** The `(enrolled)` / `(waitlist #n)`
  labels come from a name → label dictionary built in one pass over the
  status snapshot the list already fetched. Each row is a single lookup, rather
  than a search through both lists.
- **Messages use stored names.** Drop messages print `DropResult.DroppedUser`
  (e.g. `Alice`), not the text typed at the prompt (e.g. `aLiCe`).
- **Byte-order mark stripped.** PowerShell adds a BOM (U+FEFF) at the start of
  piped input, which made the first `1` invalid. `Prompt` strips a leading BOM.

### 4.5 Planned components

_To be filled in as the application is built:_

- **User IDs**: add a unique ID to `User` if names aren't unique enough.
- **Data storage**: in-memory today; file-based or a database if state must survive restarts.
- **Configurable data**: load courses and users from a file instead of `Program.cs`.

## 5. Key decisions log

Newest last.

| # | Date | Decision | Alternatives considered | Rationale |
|---|------|----------|-------------------------|-----------|
| 1 | 2026-11-13 | Start as a .NET 10 console application | Web API, desktop UI | Lowest setup cost; the core logic can move to another front end later. |
| 2 | 2026-11-13 | Use xUnit in a separate `CourseRegistration.Tests` project | NUnit, MSTest | Default .NET template, good support for data-driven tests. |
| 3 | 2026-11-13 | `CourseOffering` is read-only and validates in its constructor | Settable properties with no validation | Invalid offerings (blank code, zero capacity) can never exist. |
| 4 | 2026-11-13 | Prerequisite is an optional `string?` holding a course code | Reference to another `CourseOffering`; a list of prerequisites | Matches the request for a single prerequisite code; simplest model. |
| 5 | 2026-11-13 | `Program.cs` prints a banner | Leave empty | The console project must have an entry point to build, which the test project needs. |
| 6 | 2026-11-13 | One `EnrollmentService` manages many courses, each with its own roster | One service instance per course | One place to look up any course; matches how a UI would use it. |
| 7 | 2026-11-13 | Expected outcomes (already enrolled, not found) are returned as enums/records | Throw exceptions; return `bool` | Easy for callers to handle; a `bool` couldn't tell "waitlisted" from "enrolled" or say who was promoted. |
| 8 | 2026-11-13 | User names and course codes ignore case and surrounding spaces | Exact match | Prevents `Alice` and `alice` from taking two seats. |
| 9 | 2026-11-13 | Dropping a waitlisted user removes them from the waitlist | Only enrolled users can drop | Users need a way to leave the waitlist. |
| 10 | 2026-11-13 | Waitlist stored as a `List<string>` | `Queue<string>`, `LinkedList<string>` | Supports removal from the middle; simple. |
| 11 | 2026-11-13 | Prerequisites not enforced on enroll | Enforce now | No data yet on which courses a user has completed. **Replaced by decision 12.** |
| 12 | 2026-11-13 | Enforce prerequisites: users who haven't completed the prerequisite are neither enrolled nor waitlisted (developer requirement) | Waitlist them anyway; check only on promotion | Requested by the developer. Checking up front keeps the waitlist free of users who could never be promoted. |
| 13 | 2026-11-13 | Add a `User` model with `Name` and `CompletedCourseCodes`; `Enroll` takes a `User` | Pass completed courses as an extra `Enroll` parameter; look them up in a separate store | Matches "users have names and completed courses"; keeps the user's data together. |
| 14 | 2026-11-13 | Duplicate checks run before the prerequisite check | Prerequisite first | An enrolled user re-enrolling gets the clearer `AlreadyEnrolled` message. |
| 15 | 2026-11-13 | Prerequisite logic lives in `User.MeetsPrerequisiteFor` | Inside `EnrollmentService` | Testable on its own and reusable. |
| 16 | 2026-11-13 | Single course `.NET201` (capacity 3, prerequisite `.NET101`) and 10 hard-coded users, 5 eligible (developer requirement) | Load from a file | Requested by the developer; enough to demo every feature. |
| 17 | 2026-11-13 | Menu loop in `RegistrationConsole` using `TextReader`/`TextWriter`; setup in `Program.cs` | Everything in `Program.cs` using `Console` directly | The menu can be unit-tested with scripted input. |
| 18 | 2026-11-13 | Users chosen by list number or name | Name only; number only | Numbers are fast; names avoid looking up the list. |
| 19 | 2026-11-13 | Strip a leading byte-order mark from input | Ignore | Found during a scripted run: PowerShell's pipe made the first menu choice invalid. |
| 20 | 2026-11-13 | `DropResult` returns `DroppedUser`, the stored name; the console prints it (developer request, from code review) | Console looks the name up in the status before dropping | Drop messages showed the name as typed (`aLiCe`). Returning it from the service gives every caller the correct name in one place. |
| 21 | 2026-11-13 | **Assumption:** a user name is never a number (developer decision, from code review finding 4) | Try name before number; reject numeric names in `User` | Numeric input at the prompts always means a list position, with no ambiguity. Documented in the Readme, not enforced. |
| 22 | 2026-11-13 | **Assumption:** no two users have the same name, ignoring case (developer decision, from code review finding 5) | Add a unique user ID; reject duplicates in `Program.cs` / the service | Names are enough to identify users in this app. Documented in the Readme, not enforced. Resolves the open question about unique IDs. |
| 23 | 2026-11-13 | **Assumption:** a user's completed courses can't be updated, so the prerequisite is checked only when entering the enrolled list or waitlist, not on promotion (developer decision, from code review finding 6) | Re-check on promotion; store `User` objects on the waitlist | Matches `User` in code (`CompletedCourseCodes` is a read-only set set in the constructor). Promotion stays simple. |
| 24 | 2026-11-13 | One shared `UserNames.Comparer` for name matching; console state labels from a dictionary built once per render (developer request, from code review finding 8) | Expose lookup methods on `EnrollmentService` or `EnrollmentStatus` | The matching rule was copied in the service and the console and could drift apart. The dictionary replaces a LINQ scan of both lists per row with one pass. |

## 6. Testing strategy

- **Framework:** xUnit, in `CourseRegistration.Tests`.
- **Scope:** unit tests for domain classes, covering valid construction, each
  validation rule and edge cases (whitespace, case sensitivity).
- **Naming:** `Method_Condition_ExpectedResult`, for example
  `Constructor_WithNonPositiveCapacity_Throws`.
- **Data-driven tests:** `[Theory]` with `[InlineData]` for rules checked
  against several inputs.
- **Console tests:** drive `RegistrationConsole` with scripted lines through a
  `StringReader`, then check both the printed messages and the service's state.

### Current coverage

| Class | Test class | Tests | What's covered |
|-------|-----------|-------|----------------|
| `CourseOffering` | `CourseOfferingTests` | 13 | Valid values, no prerequisite, blank prerequisite, trimming, missing course code, non-positive capacity, self-prerequisite |
| `User` | `UserTests` | 16 | Construction, trimming, blank and duplicate completed codes, blank name, `HasCompleted` (case and spaces), `MeetsPrerequisiteFor` with/without a prerequisite |
| `UserNames` | `UserNamesTests` | 5 | Case-insensitive equality; same hash code for names differing only in case |
| `RegistrationConsole` | `RegistrationConsoleTests` | 21 | Summary line, exit, end of input, BOM input, invalid option; enroll by number/name, eligibility labels, enrolled/waitlist state labels in the enroll and drop lists, prerequisite rejection, waitlist position, already enrolled, unknown user; drop when empty, drop with promotion, drop by name, not found, messages show stored names (enrolled, waitlisted, with promotion); status lists and empty status |
| `EnrollmentService` | `EnrollmentServiceTests` | 42 | Constructor checks; `DroppedUser` is the stored name (enrolled, waitlisted) and `null` when not found; enroll up to capacity, waitlist order, duplicate enroll/waitlist (case and spaces), null user, unknown course; prerequisite met, not met, no completed courses, not waitlisted when full and unmet, waitlisted when full and met, matching ignores case and spaces, course without prerequisite; drop with/without promotion, drop from waitlist, not found, re-enroll after drop, promotion on a prerequisite course; status on a new course, snapshot isolation, invalid course codes |

## 7. Open questions

- Should course codes follow a fixed format (e.g. letters then digits)?
- Course codes are now matched ignoring case in `EnrollmentService` too. Is
  that the rule everywhere?
- Should a user be blocked from enrolling in a course they have already completed?
- Should completing a course in this system add it to the user's completed courses?
- Should a course allow more than one prerequisite?
- Should the waitlist have a size limit?
- Should a user be notified (or have to confirm) when promoted from the waitlist?
- Should the sample data include more eligible users than seats, so the
  waitlist can be shown without editing `Program.cs`?
- Should the menu support more than one course?
- What are the core use cases (register, drop, list courses, view a student's schedule)?
- Is persistence needed between runs?
- Are there user roles (student vs. administrator)?
