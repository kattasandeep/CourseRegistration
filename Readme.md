# Course Registration

A .NET console application for course registration.

> **Status:** Working console app for a single course (`.NET201`) with
> enroll, drop and status, a waitlist and prerequisite checks, all covered by
> unit tests.

## Prerequisites

| Tool | Version | Check with |
|------|---------|------------|
| .NET SDK | 10.0 or later | `dotnet --version` |
| IDE (optional) | Visual Studio 2022 17.14+ / VS Code with C# Dev Kit / Rider | — |

The solution uses the new XML solution format (`CourseRegistration.slnx`), which
needs a recent SDK or IDE.

## Project layout

```
CourseRegistration/
├── CourseRegistration.slnx              # Solution file
├── CourseRegistration/
│   ├── CourseRegistration.csproj        # Console app, net10.0
│   ├── Program.cs                       # Entry point: sample course and users
│   ├── ConsoleUi/
│   │   └── RegistrationConsole.cs       # Interactive menu
│   ├── Models/
│   │   ├── CourseOffering.cs            # Course code, capacity, prerequisite
│   │   ├── User.cs                      # Name and completed courses
│   │   ├── UserNames.cs                 # Shared rule for comparing user names
│   │   ├── EnrollmentResult.cs          # Outcome of Enroll
│   │   ├── DropResult.cs                # Outcome of Drop (+ dropped/promoted user)
│   │   └── EnrollmentStatus.cs          # Enrolled and waitlisted snapshot
│   └── Services/
│       └── EnrollmentService.cs         # Enroll / Drop / Status with waitlist
├── CourseRegistration.Tests/
│   ├── CourseRegistration.Tests.csproj  # xUnit test project, net10.0
│   ├── ConsoleUi/
│   │   └── RegistrationConsoleTests.cs  # Menu tests with scripted input
│   ├── Models/
│   │   ├── CourseOfferingTests.cs       # Unit tests for CourseOffering
│   │   ├── UserTests.cs                 # Unit tests for User
│   │   └── UserNamesTests.cs            # Unit tests for UserNames
│   └── Services/
│       └── EnrollmentServiceTests.cs    # Unit tests for EnrollmentService
├── Readme.md                            # This file
├── Design.md                            # Architecture and design notes
└── AI-Usage.md                          # Log of AI-assistant prompts and changes
```

## Getting the code

```bash
git clone https://github.com/kattasandeep/CourseRegistration.git
cd CourseRegistration
```

## Running the application

From the repository root:

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run
dotnet run --project CourseRegistration
```

## Using the application

The app starts with one course and ten users already set up:

| Course | Capacity | Prerequisite |
|--------|----------|--------------|
| `.NET201` | 3 | `.NET101` |

| # | User | Completed courses | Can enroll in .NET201? |
|---|------|-------------------|------------------------|
| 1 | Alice | .NET101 | Yes |
| 2 | Bob | .NET101, SQL100 | Yes |
| 3 | Carol | .NET101 | Yes |
| 4 | David | .NET101, JS100 | Yes |
| 5 | Emma | .NET101 | Yes |
| 6 | Frank | none | No |
| 7 | Grace | SQL100 | No |
| 8 | Henry | none | No |
| 9 | Isla | JS100 | No |
| 10 | Jack | none | No |

Each time the menu appears, the line above it shows the course, open seats,
prerequisite and waitlist size:

```
=== Course Registration ===

Course: .NET201 | Open seats: 3 of 3 | Prerequisite: .NET101 | Waitlist: 0
  1. Enroll user
  2. Drop user
  3. Status
  4. Exit
Choose an option (1-4):
```

| Option | What it does |
|--------|--------------|
| **1. Enroll user** | Lists all users with their completed courses, whether they're eligible, and whether they're already enrolled or waitlisted. Enter a user's number or name. Eligible users get a seat, or join the waitlist if the course is full. Ineligible users are refused. |
| **2. Drop user** | Lists users who are enrolled or waitlisted. Enter a number or name. Dropping an enrolled user promotes the first person on the waitlist. |
| **3. Status** | Shows the enrolled and waitlisted users in order. |
| **4. Exit** | Quits. You can also type `q` or `exit`, or end input with Ctrl+Z, Enter (Windows) or Ctrl+D (macOS/Linux). |

Names ignore case, so `alice` works as well as `Alice`.

To change the course or users, edit `CourseRegistration/Program.cs`.

> **Note:** With the sample data, 5 users are eligible and there are 3 seats.
> The 4th and 5th eligible users to enroll go on the waitlist, and dropping an
> enrolled user promotes the first of them.

### Scripted input

The app reads from standard input, so you can pipe in a list of choices.
For example, enroll Alice (1, 1), show status (3), then exit (4):

```powershell
"1`n1`n3`n4" | dotnet run --project CourseRegistration
```

## Assumptions

The application relies on the following assumptions about user data. They
are not checked in code, so data that breaks them may behave unexpectedly.

| # | Assumption | Why it matters |
|---|------------|----------------|
| 1 | **A user name is never a number** (e.g. no user called `2`). | At the Enroll and Drop prompts, input that is a number selects by list position. A user named `2` could not be picked by name. |
| 2 | **No two users have the same name, ignoring case** (e.g. not both `Alex` and `alex`). | Users are identified by name. Two users with the same name would share one seat, and either could drop the other. |
| 3 | **A user's completed courses cannot be updated** once the user is created. | The prerequisite is checked once, when a user is enrolled or added to the waitlist. Since completed courses can't change, a waitlisted user still meets the prerequisite when promoted, so it is not checked again. |

## Running the tests

The tests use [xUnit](https://xunit.net/). From the repository root:

```bash
# Run all tests in the solution
dotnet test

# Show each test name and result
dotnet test --logger "console;verbosity=detailed"

# Run only the CourseOffering tests
dotnet test --filter "FullyQualifiedName~CourseOfferingTests"

# Run only the EnrollmentService tests
dotnet test --filter "FullyQualifiedName~EnrollmentServiceTests"

# Collect code coverage (output in CourseRegistration.Tests/TestResults)
dotnet test --collect:"XPlat Code Coverage"
```

In Visual Studio, use **Test > Test Explorer**. In VS Code, use the **Testing**
panel from C# Dev Kit.

## Using the EnrollmentService from code

The service can also be used directly, without the console menu:

```csharp
using CourseRegistration.Models;
using CourseRegistration.Services;

var service = new EnrollmentService(
[
    new CourseOffering("CS101", capacity: 10),
    new CourseOffering("CS201", capacity: 2, prerequisiteCourseCode: "CS101"),
]);

var alice = new User("Alice", ["CS101"]);
var bob   = new User("Bob",   ["CS101", "MATH100"]);
var carol = new User("Carol", ["CS101"]);
var dave  = new User("Dave");              // no completed courses

service.Enroll("CS201", alice);   // Enrolled
service.Enroll("CS201", bob);     // Enrolled
service.Enroll("CS201", carol);   // Waitlisted (course is full)
service.Enroll("CS201", dave);    // PrerequisiteNotMet (hasn't completed CS101)
service.Enroll("CS201", alice);   // AlreadyEnrolled

var drop = service.Drop("CS201", "Alice");
// drop.Outcome == DroppedAndPromoted, drop.DroppedUser == "Alice", drop.PromotedUser == "Carol"
// (DroppedUser is the stored name, so Drop("CS201", "ALICE") also gives "Alice")

var status = service.Status("CS201");
// status.Enrolled   == ["Bob", "Carol"]
// status.Waitlisted == []
```

`Enroll` takes a `User` so it can check completed courses. `Drop` takes the
user's name.

### From Visual Studio

1. Open `CourseRegistration.slnx`.
2. Set **CourseRegistration** as the startup project.
3. Press **F5** to run with debugging, or **Ctrl+F5** to run without.

### From VS Code

1. Open the repository folder.
2. Install the **C# Dev Kit** extension if prompted.
3. Run `dotnet run --project CourseRegistration` in the integrated terminal, or use **Run > Start Debugging**.

## Configuration

The application has no configuration files or external dependencies (no
database, no NuGet packages). The test project uses these NuGet packages, which
`dotnet restore` downloads: `xunit`, `xunit.runner.visualstudio`,
`Microsoft.NET.Test.Sdk` and `coverlet.collector`.

## Troubleshooting

**The menu says `'1' is not a valid option` when I typed 1**

Check for extra characters in piped input. The app strips a leading
byte-order mark, which PowerShell adds when piping, but other hidden
characters would still be rejected.

**`dotnet test` finds no tests**

Run `dotnet build` first and check that it succeeds, then make sure you are
running the command from the repository root (where `CourseRegistration.slnx` is).

**`The current .NET SDK does not support targeting .NET 10.0`**

Install the .NET 10 SDK from <https://dotnet.microsoft.com/download>.
