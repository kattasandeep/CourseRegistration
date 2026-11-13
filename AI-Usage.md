# AI Usage Log

This file tracks how an AI assistant (Claude Code, model Claude Opus 5.5) was
used while building this project. Each entry records the prompt, a summary of
what the assistant did, and any suggestions or changes the developer gave in
response.

## How to add an entry

Copy the template below, increment the number, and fill in each field.

```markdown
### <N>. <Short title>

- **Date:** YYYY-MM-DD
- **Tool / model:** Claude Code / Claude Opus 5.5

**Prompt**

> exact prompt text

**Summary of AI response**

- what was produced or changed

**Files affected**

- `path/to/file`

**Developer suggestions / changes**

- feedback, corrections, or manual edits made afterward (or "None")
```

---

## Log

### 1. Add project documentation

- **Date:** 2026-11-13
- **Tool / model:** Claude Code / Claude Opus 5.5

**Prompt**

> Add a Readme.md file for Instructions to run the application. Design.md to
> explain the design and a AI-Usage.md file to track the prompts being used for
> the assistance. Record the prompts and their summaries and the
> suggestions/Changes I gave in it.

**Summary of AI response**

- Looked at the repository. It holds a fresh .NET 10 console project
  (`CourseRegistration.csproj`) with an empty `Program.cs`.
- Ran `dotnet build` to check the run instructions. The build currently fails
  with `CS5001` (no entry point) because `Program.cs` is empty. The assistant
  documented this instead of changing the code.
- Created `Readme.md`: prerequisites, project layout, build and run steps
  (CLI, Visual Studio, VS Code), and troubleshooting.
- Created `Design.md`: current state, technology choices, an architecture
  outline to fill in, a decisions log, testing strategy, and open questions.
  No features were invented beyond what exists.
- Created this `AI-Usage.md` with a reusable entry template.

**Files affected**

- `Readme.md` (new)
- `Design.md` (new)
- `AI-Usage.md` (new)

**Developer suggestions / changes**

- None. The developer moved on to the next task and asked for the documents to
  be kept up to date as work continues (see entry 2).

### 2. CourseOffering class and test project

- **Date:** 2026-11-13
- **Tool / model:** Claude Code / Claude Opus 5.5

**Prompt**

> Create a CourseOffering class with properties of string coursecode,int
> capacity and a prerequisite coursecode string. Also create a tests project to
> write unit tests. Keep updating the documents as we progress.

**Summary of AI response**

- Created `CourseOffering` in `CourseRegistration/Models/` with read-only
  properties `CourseCode` (`string`), `Capacity` (`int`) and
  `PrerequisiteCourseCode` (`string?`, optional), plus a computed
  `HasPrerequisite`.
- Added validation that the prompt didn't ask for: course code is required,
  capacity must be greater than 0, and a course can't be its own prerequisite.
  Codes are trimmed, and a blank prerequisite is stored as `null`.
- Added `Console.WriteLine("Course Registration");` to the empty `Program.cs`
  so the console project builds. The test project needs it to build.
- Created the `CourseRegistration.Tests` xUnit project (`dotnet new xunit`),
  added it to the solution, referenced the main project, and removed the
  template's placeholder `UnitTest1.cs`.
- Wrote 13 unit tests in `CourseOfferingTests` (valid values, optional
  prerequisite, trimming, and each validation rule). All pass with `dotnet test`.
- Updated `Readme.md` (layout, test commands, expected output, troubleshooting)
  and `Design.md` (domain model, validation rules, decisions 2 to 5, testing
  strategy, open questions).

**Files affected**

- `CourseRegistration/Models/CourseOffering.cs` (new)
- `CourseRegistration/Program.cs` (modified)
- `CourseRegistration.Tests/CourseRegistration.Tests.csproj` (new)
- `CourseRegistration.Tests/Models/CourseOfferingTests.cs` (new)
- `CourseRegistration.slnx` (modified: test project added)
- `Readme.md`, `Design.md`, `AI-Usage.md` (modified)

**Developer suggestions / changes**

- None. The developer moved on to the next feature (entry 3).

### 3. EnrollmentService with waitlist

- **Date:** 2026-11-13
- **Tool / model:** Claude Code / Claude Opus 5.5

**Prompt**

> Create an EnrollmentService class that maintains a Enrollment list along with
> a waitlist. Enrollment list can only enroll upto the capacity of the course
> and any new enrollments will be put in a waitlist. Define the following
> methods in the service. Enroll drop and status. Enroll should enroll a user
> with Name into course after making sure user is not already enrolled.Drop
> will drop and promote the first user in waitlist to the course and status
> will give the current enrolled and waitlisted users

**Summary of AI response**

- Created `EnrollmentService` in `CourseRegistration/Services/`. It takes a
  list of `CourseOffering`s and keeps an enrolled list (up to capacity) and a
  first-in-first-out waitlist for each course.
- `Enroll(courseCode, userName)` checks the user isn't already enrolled. It
  also checks they aren't already waitlisted, which the prompt didn't ask for.
  Then it enrolls them if a seat is free, or adds them to the waitlist.
- `Drop(courseCode, userName)` removes an enrolled user and promotes the first
  waitlisted user. Choice the prompt didn't cover: it also lets a waitlisted
  user leave the waitlist.
- `Status(courseCode)` returns a snapshot of the enrolled and waitlisted users,
  plus capacity and available seats.
- Added result types in `Models/`: `EnrollmentResult` (enum), `DropOutcome`
  (enum), `DropResult` (record with the promoted user) and `EnrollmentStatus`
  (record).
- Choices the AI made (each recorded in `Design.md` decisions 6 to 11):
  - Names and course codes ignore case and surrounding spaces.
  - Expected outcomes are returned as values instead of thrown as exceptions.
  - Blank input and unknown courses throw.
  - Prerequisites are not enforced yet.
  - The service is not thread-safe.
- Wrote 33 unit tests in `EnrollmentServiceTests`. All 46 tests in the
  solution pass.
- Updated `Readme.md` (layout, test filter, usage example) and `Design.md`
  (service design, diagram, invariants, error handling, decisions, coverage,
  open questions).

**Files affected**

- `CourseRegistration/Services/EnrollmentService.cs` (new)
- `CourseRegistration/Models/EnrollmentResult.cs` (new)
- `CourseRegistration/Models/DropResult.cs` (new)
- `CourseRegistration/Models/EnrollmentStatus.cs` (new)
- `CourseRegistration.Tests/Services/EnrollmentServiceTests.cs` (new)
- `Readme.md`, `Design.md`, `AI-Usage.md` (modified)

**Developer suggestions / changes**

- The developer reversed the AI's choice not to enforce prerequisites (Design
  decision 11). They asked for users to be enrolled or waitlisted only when the
  prerequisite is met, based on each user's completed courses (entry 4).

### 4. Enforce prerequisites using users' completed courses

- **Date:** 2026-11-13
- **Tool / model:** Claude Code / Claude Opus 5.5

**Prompt**

> Now on the prerequiste - Enroll/waitlist the user only when the prerequisite
> is satisfied. When defining the users we will have their names and their
> completed course's that we will check against the prerequisites.

**Summary of AI response**

- Added a `User` model (`Models/User.cs`) with `Name` and
  `CompletedCourseCodes`, plus `HasCompleted` and `MeetsPrerequisiteFor`
  helpers. Course codes are matched ignoring case and surrounding spaces.
- Changed `EnrollmentService.Enroll` to take a `User` instead of a name string.
  This is a breaking change to the method signature. `Drop` still takes a name.
- Enroll now returns a new `EnrollmentResult.PrerequisiteNotMet` when the user
  hasn't completed the course's prerequisite. Such users are neither enrolled
  nor waitlisted.
- Check order: already enrolled → already waitlisted → prerequisite → seat or
  waitlist. Because only qualified users reach the waitlist, promotion on
  `Drop` needs no re-check.
- Rewrote `EnrollmentServiceTests` to use `User`. Added 8 prerequisite tests
  and 1 promotion test, and moved blank-name checks to the new `UserTests`
  (16 tests). All 68 tests pass.
- Updated `Readme.md` (layout, usage example with prerequisites) and
  `Design.md` (User model, enroll check flow, invariants, decisions 12 to 15,
  coverage, open questions). Decision 11 is marked as replaced.

**Files affected**

- `CourseRegistration/Models/User.cs` (new)
- `CourseRegistration/Models/EnrollmentResult.cs` (modified: `PrerequisiteNotMet`)
- `CourseRegistration/Services/EnrollmentService.cs` (modified: `Enroll(string, User)`)
- `CourseRegistration.Tests/Models/UserTests.cs` (new)
- `CourseRegistration.Tests/Services/EnrollmentServiceTests.cs` (rewritten)
- `Readme.md`, `Design.md`, `AI-Usage.md` (modified)

**Developer suggestions / changes**

- None. The developer moved on to building the console app (entry 5).

### 5. Interactive console in Program.cs

- **Date:** 2026-11-13
- **Tool / model:** Claude Code / Claude Opus 5.5

**Prompt**

> Modify the Program.cs with the following. We have a single Course .net 201
> with prerequisite of.net101. and capacity of 3.Create a User list of 10 users
> of which 5 have completed the prerequisite. Give instructions with
> Coursename-open Capacity and its prerequisite. We can choose to Enroll
> user,Drop user or get the status .

**Summary of AI response**

- `Program.cs` now sets up course `.NET201` (capacity 3, prerequisite
  `.NET101`) and 10 users. Alice, Bob, Carol, David and Emma have completed
  `.NET101`. Frank, Grace, Henry, Isla and Jack have not, though some have
  other courses. Then it starts the menu.
- Added `ConsoleUi/RegistrationConsole.cs`, a menu loop. Before each menu it
  prints `Course | Open seats: n of 3 | Prerequisite | Waitlist`, then offers
  1. Enroll user, 2. Drop user, 3. Status, 4. Exit.
  - Enroll lists users with their completed courses, eligibility and current
    state. Users are chosen by number or name.
  - Drop lists the enrolled and waitlisted users and shows who was promoted.
  - Status shows the numbered enrolled list and waitlist.
- Choices the AI made that the prompt didn't cover:
  - The menu loop is in its own class with `TextReader`/`TextWriter` so it can
    be tested. Setup stays in `Program.cs` as asked.
  - An Exit option was added. End of input also exits.
  - Users can be chosen by number or name.
  - Course codes are written as `.NET201` / `.NET101`.
- Bug found while testing with piped input: PowerShell adds a byte-order mark,
  which made the first menu choice invalid. Fixed by stripping a leading BOM,
  with a regression test.
- Added 17 tests in `RegistrationConsoleTests` using scripted input. All 85
  tests pass. Also checked by piping a full enroll/drop/status session through
  `dotnet run`.
- Updated `Readme.md` (how to use the app, sample data table, menu options,
  scripted input, troubleshooting) and `Design.md` (console architecture,
  decisions 16 to 19, coverage, open questions).

**Files affected**

- `CourseRegistration/Program.cs` (rewritten)
- `CourseRegistration/ConsoleUi/RegistrationConsole.cs` (new)
- `CourseRegistration.Tests/ConsoleUi/RegistrationConsoleTests.cs` (new)
- `Readme.md`, `Design.md`, `AI-Usage.md` (modified)

**Developer suggestions / changes**

- The developer changed the course capacity from 5 to 3 in `Program.cs` and
  updated the docs to match, so the waitlist can be shown with the sample data.
- The developer ran `/code-review`, which reported 8 findings, and asked for
  one of them to be fixed: Drop echoes the name as typed (entry 6).

### 6. Code review fix: Drop shows the stored name

- **Date:** 2026-11-13
- **Tool / model:** Claude Code / Claude Opus 5.5

**Prompt**

> Drop echoes the name exactly as user typed it, return the name stored on the
> course.

(This came from finding 3 of a `/code-review` run. The run reviewed the whole
source because the folder isn't a git repository.)

**Summary of AI response**

- Added `DroppedUser` to `DropResult`. It holds the dropped user's name as
  stored on the course, or `null` for `NotFound`. It was added as a third
  optional parameter, so existing `new DropResult(outcome)` and
  `new DropResult(outcome, promoted)` calls still compile.
- `EnrollmentService.Drop` now reads the stored name before removing the user,
  for both enrolled and waitlisted users.
- `RegistrationConsole` prints `result.DroppedUser` in the Dropped,
  DroppedAndPromoted and RemovedFromWaitlist messages. Typing `aLiCe` now shows
  "Alice has been dropped".
- Tests: updated 4 exact-match `DropResult` assertions. Added 3 service tests
  (stored name for enrolled and waitlisted users, `null` when not found) and 2
  console tests (stored name for waitlist removal and promotion). Tightened the
  drop-by-name test. All 90 tests pass.
- Checked in the real app with piped input. The first build failed because a
  running copy of `CourseRegistration.exe` (likely the developer's) locked the
  output file. The AI built into a scratch folder instead of stopping that process.
- Docs: fixed the stale Readme note that said the waitlist never fills, and
  the menu example still showing "5 of 5" (capacity is now 3). Updated the
  `DropResult` description, added decision 20, and updated the test counts.
- Not changed, as it wasn't requested: from the same finding, an out-of-range
  number at the Drop prompt (e.g. `7`) is still treated as a name and reported
  as "7 is not enrolled or waitlisted". The other 7 review findings are also
  open.

**Files affected**

- `CourseRegistration/Models/DropResult.cs` (modified: `DroppedUser`)
- `CourseRegistration/Services/EnrollmentService.cs` (modified)
- `CourseRegistration/ConsoleUi/RegistrationConsole.cs` (modified)
- `CourseRegistration.Tests/Services/EnrollmentServiceTests.cs` (modified)
- `CourseRegistration.Tests/ConsoleUi/RegistrationConsoleTests.cs` (modified)
- `Readme.md`, `Design.md`, `AI-Usage.md` (modified)

**Developer suggestions / changes**

- None. The developer chose to settle code review findings 4, 5 and 6 by
  documenting assumptions instead of changing the code (entry 7).

### 7. Document assumptions in the Readme

- **Date:** 2026-11-13
- **Tool / model:** Claude Code / Claude Opus 5.5

**Prompt**

> Update the following assumptions in readme. 1)User name will not be a
> number.2) No two users can have the same name(case-insensitive)3)An users
> completed courses cannot be updated.So we will check for prerequisite while
> entering the enrolled or waitlist and need not check again for promotion.

**Summary of AI response**

- Added an "Assumptions" section to `Readme.md` with the three assumptions and
  why each matters. It states that they are not checked in code.
- Recorded them in `Design.md` as decisions 21 to 23, each linked to the code
  review finding it settles (findings 4, 5 and 6). Updated the related design
  notes and removed the open question about unique user IDs.
- No code changes. Assumption 3 already matches the code: `User` sets its
  completed courses in the constructor and only exposes them read-only.
  Assumptions 1 and 2 are documented, not enforced.

**Files affected**

- `Readme.md`, `Design.md`, `AI-Usage.md` (modified)

**Developer suggestions / changes**

- None. The developer moved on to code review finding 8 (entry 8).

### 8. Code review fix: duplicated name matching in the console

- **Date:** 2026-11-13
- **Tool / model:** Claude Code / Claude Opus 5.5

**Prompt**

> DescribeState repeats the service's private case-insensitive IndexOf logic
> with LINQ tuple tricks, scanning both lists for every user on every list
> render.

(This is code review finding 8, pasted as the request.)

**Summary of AI response**

- Added `Models/UserNames.cs` with a shared `UserNames.Comparer`
  (`OrdinalIgnoreCase`). `EnrollmentService.IndexOf` and the console's
  `FindUser` now both use it, so the name-matching rule is defined once.
- Replaced `RegistrationConsole.DescribeState`, which used a LINQ scan of both
  lists per row, with `BuildStateLabels`. It builds a name → label dictionary
  in one pass over the status snapshot, and each row in the Enroll and Drop
  lists does a single lookup. The output is unchanged.
- Added 2 console tests for the `(enrolled)` / `(waitlist #n)` labels, which
  had no direct coverage before, and 5 `UserNamesTests`. All 97 tests pass,
  and a piped run of the app shows the same labels as before.
- Updated `Readme.md` (layout) and `Design.md` (`UserNames` section, console
  design note, decision 24, test counts and coverage).
- Not changed: finding 7, the extra `Status()` call in the Waitlisted
  message, is related but wasn't requested.

**Files affected**

- `CourseRegistration/Models/UserNames.cs` (new)
- `CourseRegistration/Services/EnrollmentService.cs` (modified)
- `CourseRegistration/ConsoleUi/RegistrationConsole.cs` (modified)
- `CourseRegistration.Tests/Models/UserNamesTests.cs` (new)
- `CourseRegistration.Tests/ConsoleUi/RegistrationConsoleTests.cs` (modified)
- `Readme.md`, `Design.md`, `AI-Usage.md` (modified)

**Developer suggestions / changes**

- None. The developer asked for the work to be pushed to GitHub (entry 9).

### 9. Push to GitHub

- **Date:** 2026-11-13
- **Tool / model:** Claude Code / Claude Opus 5.5

**Prompt**

> Push these changes to https://github.com/kattasandeep/CourseRegistration

**Summary of AI response**

- The local folder wasn't a git repository. The GitHub repo already had one
  commit ("Initial commit") containing a placeholder `README.md`
  (`# CourseRegistration.git`).
- Initialised git with `main` as the branch, added the GitHub repo as
  `origin`, and based the local work on the existing remote commit so the push
  didn't need a force.
- Replaced the placeholder `README.md` with the project's `Readme.md`. Windows
  can't hold both names, which differ only in case.
- Added a standard .NET `.gitignore` (`dotnet new gitignore`) so `bin/`,
  `obj/`, `.vs/` and test results aren't committed.
- Added a "Getting the code" (`git clone`) section to `Readme.md`.
- Committed everything in one commit and pushed to `origin/main`.

**Files affected**

- `.gitignore` (new)
- `README.md` (removed placeholder) → `Readme.md`
- `Readme.md`, `AI-Usage.md` (modified)

**Developer suggestions / changes**

- _None yet._
