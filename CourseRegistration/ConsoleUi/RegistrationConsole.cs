using CourseRegistration.Models;
using CourseRegistration.Services;

namespace CourseRegistration.ConsoleUi;

/// <summary>
/// Interactive menu for a single course: enroll a user, drop a user, or show status.
/// Reads from and writes to the given streams so it can be driven by tests.
/// </summary>
public sealed class RegistrationConsole
{
    private readonly EnrollmentService _service;
    private readonly CourseOffering _course;
    private readonly IReadOnlyList<User> _users;
    private readonly TextReader _input;
    private readonly TextWriter _output;

    public RegistrationConsole(
        EnrollmentService service,
        CourseOffering course,
        IReadOnlyList<User> users,
        TextReader input,
        TextWriter output)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _course = course ?? throw new ArgumentNullException(nameof(course));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    /// <summary>Runs the menu until the user chooses Exit or input ends.</summary>
    public void Run()
    {
        _output.WriteLine("=== Course Registration ===");

        while (true)
        {
            _output.WriteLine();
            WriteCourseSummary();
            _output.WriteLine("  1. Enroll user");
            _output.WriteLine("  2. Drop user");
            _output.WriteLine("  3. Status");
            _output.WriteLine("  4. Exit");

            var choice = Prompt("Choose an option (1-4)");
            if (choice is null)
                return; // input ended

            switch (choice.Trim().ToLowerInvariant())
            {
                case "1":
                    Enroll();
                    break;
                case "2":
                    Drop();
                    break;
                case "3":
                    ShowStatus();
                    break;
                case "4":
                case "q":
                case "exit":
                    _output.WriteLine("Goodbye.");
                    return;
                default:
                    _output.WriteLine($"'{choice.Trim()}' is not a valid option. Enter 1, 2, 3 or 4.");
                    break;
            }
        }
    }

    private void WriteCourseSummary()
    {
        var status = _service.Status(_course.CourseCode);
        var prerequisite = _course.PrerequisiteCourseCode ?? "None";

        _output.WriteLine(
            $"Course: {status.CourseCode} | Open seats: {status.AvailableSeats} of {status.Capacity} | " +
            $"Prerequisite: {prerequisite} | Waitlist: {status.Waitlisted.Count}");
    }

    private void Enroll()
    {
        _output.WriteLine();
        _output.WriteLine("Users:");
        var stateLabels = BuildStateLabels(_service.Status(_course.CourseCode));

        for (var i = 0; i < _users.Count; i++)
        {
            var user = _users[i];
            var completed = user.CompletedCourseCodes.Count == 0
                ? "none"
                : string.Join(", ", user.CompletedCourseCodes);
            var eligibility = user.MeetsPrerequisiteFor(_course) ? "eligible" : "prerequisite not met";
            var state = stateLabels.GetValueOrDefault(user.Name, string.Empty);

            _output.WriteLine($"  {i + 1,2}. {user.Name,-8} completed: {completed,-20} [{eligibility}]{state}");
        }

        var entry = Prompt("Enter user number or name to enroll");
        if (string.IsNullOrWhiteSpace(entry))
        {
            _output.WriteLine("No user entered.");
            return;
        }

        var selected = FindUser(entry);
        if (selected is null)
        {
            _output.WriteLine($"No user matches '{entry.Trim()}'.");
            return;
        }

        var result = _service.Enroll(_course.CourseCode, selected);
        _output.WriteLine(result switch
        {
            EnrollmentResult.Enrolled =>
                $"{selected.Name} is enrolled in {_course.CourseCode}.",
            EnrollmentResult.Waitlisted =>
                $"{_course.CourseCode} is full. {selected.Name} is on the waitlist at position " +
                $"{_service.Status(_course.CourseCode).Waitlisted.Count}.",
            EnrollmentResult.AlreadyEnrolled =>
                $"{selected.Name} is already enrolled in {_course.CourseCode}.",
            EnrollmentResult.AlreadyWaitlisted =>
                $"{selected.Name} is already on the waitlist for {_course.CourseCode}.",
            EnrollmentResult.PrerequisiteNotMet =>
                $"{selected.Name} cannot enroll: prerequisite {_course.PrerequisiteCourseCode} not completed.",
            _ => $"Unexpected result: {result}.",
        });
    }

    private void Drop()
    {
        var status = _service.Status(_course.CourseCode);
        var onCourse = status.Enrolled.Concat(status.Waitlisted).ToList();

        _output.WriteLine();
        if (onCourse.Count == 0)
        {
            _output.WriteLine("No users are enrolled or waitlisted, so there is nobody to drop.");
            return;
        }

        _output.WriteLine("Enrolled and waitlisted users:");
        var stateLabels = BuildStateLabels(status);
        for (var i = 0; i < onCourse.Count; i++)
            _output.WriteLine($"  {i + 1,2}. {onCourse[i]}{stateLabels[onCourse[i]]}");

        var entry = Prompt("Enter number or name to drop");
        if (string.IsNullOrWhiteSpace(entry))
        {
            _output.WriteLine("No user entered.");
            return;
        }

        var name = int.TryParse(entry.Trim(), out var number) && number >= 1 && number <= onCourse.Count
            ? onCourse[number - 1]
            : entry.Trim();

        var result = _service.Drop(_course.CourseCode, name);
        _output.WriteLine(result.Outcome switch
        {
            DropOutcome.Dropped =>
                $"{result.DroppedUser} has been dropped from {_course.CourseCode}.",
            DropOutcome.DroppedAndPromoted =>
                $"{result.DroppedUser} has been dropped from {_course.CourseCode}. " +
                $"{result.PromotedUser} was promoted from the waitlist and is now enrolled.",
            DropOutcome.RemovedFromWaitlist =>
                $"{result.DroppedUser} has been removed from the waitlist for {_course.CourseCode}.",
            DropOutcome.NotFound =>
                $"{name} is not enrolled or waitlisted in {_course.CourseCode}.",
            _ => $"Unexpected result: {result.Outcome}.",
        });
    }

    private void ShowStatus()
    {
        var status = _service.Status(_course.CourseCode);

        _output.WriteLine();
        _output.WriteLine($"Status for {status.CourseCode}:");
        _output.WriteLine($"  Enrolled ({status.Enrolled.Count} of {status.Capacity}):");
        WriteNumbered(status.Enrolled);
        _output.WriteLine($"  Waitlisted ({status.Waitlisted.Count}):");
        WriteNumbered(status.Waitlisted);
    }

    private void WriteNumbered(IReadOnlyList<string> names)
    {
        if (names.Count == 0)
        {
            _output.WriteLine("    (none)");
            return;
        }

        for (var i = 0; i < names.Count; i++)
            _output.WriteLine($"    {i + 1}. {names[i]}");
    }

    private User? FindUser(string entry)
    {
        var text = entry.Trim();

        if (int.TryParse(text, out var number) && number >= 1 && number <= _users.Count)
            return _users[number - 1];

        return _users.FirstOrDefault(u => UserNames.Comparer.Equals(u.Name, text));
    }

    /// <summary>
    /// Builds a name → label lookup (" (enrolled)" or " (waitlist #n)") in one pass over the status,
    /// so each list row is a single dictionary lookup.
    /// </summary>
    private static Dictionary<string, string> BuildStateLabels(EnrollmentStatus status)
    {
        var labels = new Dictionary<string, string>(UserNames.Comparer);

        foreach (var name in status.Enrolled)
            labels[name] = " (enrolled)";

        for (var i = 0; i < status.Waitlisted.Count; i++)
            labels[status.Waitlisted[i]] = $" (waitlist #{i + 1})";

        return labels;
    }

    private string? Prompt(string message)
    {
        _output.Write($"{message}: ");
        var line = _input.ReadLine();
        if (line is null)
        {
            _output.WriteLine();
            return null;
        }

        // Piped or redirected input can start with a byte-order mark; drop it so "1" still matches.
        return line.TrimStart('﻿');
    }
}
