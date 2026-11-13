using CourseRegistration.ConsoleUi;
using CourseRegistration.Models;
using CourseRegistration.Services;

namespace CourseRegistration.Tests.ConsoleUi;

public class RegistrationConsoleTests
{
    private static readonly CourseOffering Course = new(".NET201", 2, ".NET101");

    private static readonly List<User> Users =
    [
        new("Alice", [".NET101"]),   // 1
        new("Bob", [".NET101"]),     // 2
        new("Carol", [".NET101"]),   // 3
        new("Frank"),                // 4 - prerequisite not met
    ];

    /// <summary>Runs the console with the given input lines and returns the service and all output.</summary>
    private static (EnrollmentService Service, string Output) Run(params string[] inputLines)
    {
        var service = new EnrollmentService([Course]);
        var input = new StringReader(string.Join(Environment.NewLine, inputLines));
        var output = new StringWriter();

        new RegistrationConsole(service, Course, Users, input, output).Run();

        return (service, output.ToString());
    }

    [Fact]
    public void Run_ShowsCourseOpenSeatsAndPrerequisite()
    {
        var (_, output) = Run("4");

        Assert.Contains("Course: .NET201 | Open seats: 2 of 2 | Prerequisite: .NET101", output);
        Assert.Contains("1. Enroll user", output);
        Assert.Contains("2. Drop user", output);
        Assert.Contains("3. Status", output);
        Assert.Contains("Goodbye.", output);
    }

    [Fact]
    public void Run_EndOfInput_ExitsWithoutError()
    {
        var (_, output) = Run();

        Assert.Contains("Choose an option", output);
    }

    [Fact]
    public void Run_InputStartingWithByteOrderMark_IsAccepted()
    {
        var (service, _) = Run("﻿1", "1", "4");

        Assert.Equal(["Alice"], service.Status(".NET201").Enrolled);
    }

    [Fact]
    public void Run_InvalidOption_ShowsMessageAndContinues()
    {
        var (_, output) = Run("9", "4");

        Assert.Contains("'9' is not a valid option", output);
        Assert.Contains("Goodbye.", output);
    }

    [Fact]
    public void Enroll_ByNumber_EnrollsEligibleUser()
    {
        var (service, output) = Run("1", "1", "4");

        Assert.Contains("Alice is enrolled in .NET201.", output);
        Assert.Equal(["Alice"], service.Status(".NET201").Enrolled);
        Assert.Contains("Open seats: 1 of 2", output);
    }

    [Fact]
    public void Enroll_ByName_IgnoresCase()
    {
        var (service, output) = Run("1", "bob", "4");

        Assert.Contains("Bob is enrolled in .NET201.", output);
        Assert.Equal(["Bob"], service.Status(".NET201").Enrolled);
    }

    [Fact]
    public void Enroll_ListMarksEligibility()
    {
        var (_, output) = Run("1", "", "4");

        Assert.Contains("Alice", output);
        Assert.Contains("[eligible]", output);
        Assert.Contains("[prerequisite not met]", output);
    }

    [Fact]
    public void Enroll_ListShowsEnrolledAndWaitlistState()
    {
        // Enroll Alice, Bob (full), Carol (waitlisted); then open the enroll list and cancel.
        var (_, output) = Run("1", "1", "1", "2", "1", "3", "1", "", "4");

        var lastList = output[output.LastIndexOf("Users:", StringComparison.Ordinal)..];
        Assert.Matches(@"Alice .*\[eligible\] \(enrolled\)", lastList);
        Assert.Matches(@"Bob .*\[eligible\] \(enrolled\)", lastList);
        Assert.Matches(@"Carol .*\[eligible\] \(waitlist #1\)", lastList);
        Assert.Matches(@"Frank .*\[prerequisite not met\]\r?\n", lastList);
    }

    [Fact]
    public void Drop_ListShowsEnrolledAndWaitlistState()
    {
        // Enroll Alice, Bob (full), Carol (waitlisted); then open the drop list and cancel.
        var (_, output) = Run("1", "1", "1", "2", "1", "3", "2", "", "4");

        Assert.Contains("1. Alice (enrolled)", output);
        Assert.Contains("2. Bob (enrolled)", output);
        Assert.Contains("3. Carol (waitlist #1)", output);
    }

    [Fact]
    public void Enroll_UserWithoutPrerequisite_IsRejected()
    {
        var (service, output) = Run("1", "4", "4");

        Assert.Contains("Frank cannot enroll: prerequisite .NET101 not completed.", output);
        Assert.Empty(service.Status(".NET201").Enrolled);
    }

    [Fact]
    public void Enroll_WhenFull_AddsToWaitlistWithPosition()
    {
        var (service, output) = Run("1", "1", "1", "2", "1", "3", "4");

        Assert.Contains("Carol is on the waitlist at position 1.", output);
        Assert.Equal(["Carol"], service.Status(".NET201").Waitlisted);
    }

    [Fact]
    public void Enroll_SameUserTwice_ReportsAlreadyEnrolled()
    {
        var (_, output) = Run("1", "1", "1", "1", "4");

        Assert.Contains("Alice is already enrolled in .NET201.", output);
    }

    [Fact]
    public void Enroll_UnknownUser_ShowsMessage()
    {
        var (_, output) = Run("1", "Zoe", "4");

        Assert.Contains("No user matches 'Zoe'.", output);
    }

    [Fact]
    public void Drop_WhenNobodyOnCourse_ShowsMessage()
    {
        var (_, output) = Run("2", "4");

        Assert.Contains("nobody to drop", output);
    }

    [Fact]
    public void Drop_EnrolledUser_PromotesFirstWaitlisted()
    {
        // Enroll Alice, Bob (full), Carol (waitlisted); then drop #1 (Alice).
        var (service, output) = Run("1", "1", "1", "2", "1", "3", "2", "1", "4");

        Assert.Contains("Alice has been dropped from .NET201. Carol was promoted from the waitlist and is now enrolled.", output);
        Assert.Equal(["Bob", "Carol"], service.Status(".NET201").Enrolled);
        Assert.Empty(service.Status(".NET201").Waitlisted);
    }

    [Fact]
    public void Drop_ByName_FreesSeatAndShowsStoredName()
    {
        var (service, output) = Run("1", "1", "2", "aLiCe", "4");

        Assert.Contains("Alice has been dropped from .NET201.", output);
        Assert.DoesNotContain("aLiCe has been dropped", output);
        Assert.Empty(service.Status(".NET201").Enrolled);
    }

    [Fact]
    public void Drop_WaitlistedUserByName_ShowsStoredName()
    {
        // Enroll Alice, Bob (full), Carol (waitlisted); then drop "CAROL".
        var (_, output) = Run("1", "1", "1", "2", "1", "3", "2", "CAROL", "4");

        Assert.Contains("Carol has been removed from the waitlist for .NET201.", output);
    }

    [Fact]
    public void Drop_ByNameWithPromotion_ShowsStoredNames()
    {
        // Enroll Alice, Bob (full), Carol (waitlisted); then drop "bob".
        var (_, output) = Run("1", "1", "1", "2", "1", "3", "2", "bob", "4");

        Assert.Contains("Bob has been dropped from .NET201. Carol was promoted from the waitlist and is now enrolled.", output);
    }

    [Fact]
    public void Drop_UserNotOnCourse_ShowsNotFound()
    {
        var (_, output) = Run("1", "1", "2", "Zoe", "4");

        Assert.Contains("Zoe is not enrolled or waitlisted in .NET201.", output);
    }

    [Fact]
    public void Status_ListsEnrolledAndWaitlistedUsers()
    {
        var (_, output) = Run("1", "1", "1", "2", "1", "3", "3", "4");

        Assert.Contains("Status for .NET201:", output);
        Assert.Contains("Enrolled (2 of 2):", output);
        Assert.Contains("1. Alice", output);
        Assert.Contains("2. Bob", output);
        Assert.Contains("Waitlisted (1):", output);
        Assert.Contains("1. Carol", output);
    }

    [Fact]
    public void Status_WhenEmpty_ShowsNone()
    {
        var (_, output) = Run("3", "4");

        Assert.Contains("Enrolled (0 of 2):", output);
        Assert.Contains("(none)", output);
    }
}
