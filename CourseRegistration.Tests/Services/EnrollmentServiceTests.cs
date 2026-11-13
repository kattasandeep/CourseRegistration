using CourseRegistration.Models;
using CourseRegistration.Services;

namespace CourseRegistration.Tests.Services;

public class EnrollmentServiceTests
{
    private const string Course = "CS101";

    private static EnrollmentService CreateService(int capacity = 2) =>
        new([new CourseOffering(Course, capacity)]);

    /// <summary>Course CS201 requires CS101.</summary>
    private static EnrollmentService CreateServiceWithPrerequisite(int capacity = 1) =>
        new([new CourseOffering("CS101", 10), new CourseOffering("CS201", capacity, "CS101")]);

    private static User U(string name, params string[] completedCourses) => new(name, completedCourses);

    // ---- Constructor ----

    [Fact]
    public void Constructor_WithNullCourses_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EnrollmentService(null!));
    }

    [Fact]
    public void Constructor_WithDuplicateCourseCodes_Throws()
    {
        var courses = new[] { new CourseOffering("CS101", 10), new CourseOffering("cs101", 20) };

        Assert.Throws<ArgumentException>(() => new EnrollmentService(courses));
    }

    // ---- Enroll ----

    [Fact]
    public void Enroll_WhenSeatAvailable_EnrollsUser()
    {
        var service = CreateService();

        var result = service.Enroll(Course, U("Alice"));

        Assert.Equal(EnrollmentResult.Enrolled, result);
        Assert.Equal(["Alice"], service.Status(Course).Enrolled);
    }

    [Fact]
    public void Enroll_UpToCapacity_EnrollsAll()
    {
        var service = CreateService(capacity: 2);

        Assert.Equal(EnrollmentResult.Enrolled, service.Enroll(Course, U("Alice")));
        Assert.Equal(EnrollmentResult.Enrolled, service.Enroll(Course, U("Bob")));

        var status = service.Status(Course);
        Assert.Equal(["Alice", "Bob"], status.Enrolled);
        Assert.Empty(status.Waitlisted);
        Assert.Equal(0, status.AvailableSeats);
    }

    [Fact]
    public void Enroll_WhenCourseFull_AddsToWaitlistInOrder()
    {
        var service = CreateService(capacity: 1);
        service.Enroll(Course, U("Alice"));

        Assert.Equal(EnrollmentResult.Waitlisted, service.Enroll(Course, U("Bob")));
        Assert.Equal(EnrollmentResult.Waitlisted, service.Enroll(Course, U("Carol")));

        var status = service.Status(Course);
        Assert.Equal(["Alice"], status.Enrolled);
        Assert.Equal(["Bob", "Carol"], status.Waitlisted);
    }

    [Theory]
    [InlineData("Alice")]
    [InlineData("alice")]
    [InlineData("  Alice ")]
    public void Enroll_WhenAlreadyEnrolled_ReturnsAlreadyEnrolledAndDoesNotDuplicate(string sameUser)
    {
        var service = CreateService();
        service.Enroll(Course, U("Alice"));

        var result = service.Enroll(Course, U(sameUser));

        Assert.Equal(EnrollmentResult.AlreadyEnrolled, result);
        Assert.Equal(["Alice"], service.Status(Course).Enrolled);
    }

    [Fact]
    public void Enroll_WhenAlreadyWaitlisted_ReturnsAlreadyWaitlistedAndDoesNotDuplicate()
    {
        var service = CreateService(capacity: 1);
        service.Enroll(Course, U("Alice"));
        service.Enroll(Course, U("Bob"));

        var result = service.Enroll(Course, U("BOB"));

        Assert.Equal(EnrollmentResult.AlreadyWaitlisted, result);
        Assert.Equal(["Bob"], service.Status(Course).Waitlisted);
    }

    [Fact]
    public void Enroll_WithNullUser_Throws()
    {
        var service = CreateService();

        var ex = Assert.Throws<ArgumentNullException>(() => service.Enroll(Course, null!));
        Assert.Equal("user", ex.ParamName);
    }

    [Fact]
    public void Enroll_WithUnknownCourse_Throws()
    {
        var service = CreateService();

        Assert.Throws<KeyNotFoundException>(() => service.Enroll("MATH200", U("Alice")));
    }

    [Fact]
    public void Enroll_CourseCodeIsCaseInsensitive()
    {
        var service = CreateService();

        Assert.Equal(EnrollmentResult.Enrolled, service.Enroll(" cs101 ", U("Alice")));
        Assert.Equal(["Alice"], service.Status(Course).Enrolled);
    }

    [Fact]
    public void Enroll_SameUserInDifferentCourses_IsAllowed()
    {
        var service = new EnrollmentService([new CourseOffering("CS101", 1), new CourseOffering("CS201", 1)]);
        var alice = U("Alice");

        Assert.Equal(EnrollmentResult.Enrolled, service.Enroll("CS101", alice));
        Assert.Equal(EnrollmentResult.Enrolled, service.Enroll("CS201", alice));
    }

    // ---- Enroll: prerequisites ----

    [Fact]
    public void Enroll_WithPrerequisiteCompleted_Enrolls()
    {
        var service = CreateServiceWithPrerequisite();

        var result = service.Enroll("CS201", U("Alice", "CS101"));

        Assert.Equal(EnrollmentResult.Enrolled, result);
        Assert.Equal(["Alice"], service.Status("CS201").Enrolled);
    }

    [Fact]
    public void Enroll_WithPrerequisiteNotCompleted_ReturnsPrerequisiteNotMetAndAddsNothing()
    {
        var service = CreateServiceWithPrerequisite();

        var result = service.Enroll("CS201", U("Alice", "MATH100"));

        Assert.Equal(EnrollmentResult.PrerequisiteNotMet, result);
        var status = service.Status("CS201");
        Assert.Empty(status.Enrolled);
        Assert.Empty(status.Waitlisted);
    }

    [Fact]
    public void Enroll_WithNoCompletedCourses_WhenPrerequisiteRequired_ReturnsPrerequisiteNotMet()
    {
        var service = CreateServiceWithPrerequisite();

        Assert.Equal(EnrollmentResult.PrerequisiteNotMet, service.Enroll("CS201", U("Alice")));
    }

    [Fact]
    public void Enroll_WhenFullAndPrerequisiteNotMet_DoesNotWaitlist()
    {
        var service = CreateServiceWithPrerequisite(capacity: 1);
        service.Enroll("CS201", U("Alice", "CS101"));

        var result = service.Enroll("CS201", U("Bob"));

        Assert.Equal(EnrollmentResult.PrerequisiteNotMet, result);
        Assert.Empty(service.Status("CS201").Waitlisted);
    }

    [Fact]
    public void Enroll_WhenFullAndPrerequisiteMet_Waitlists()
    {
        var service = CreateServiceWithPrerequisite(capacity: 1);
        service.Enroll("CS201", U("Alice", "CS101"));

        var result = service.Enroll("CS201", U("Bob", "CS101"));

        Assert.Equal(EnrollmentResult.Waitlisted, result);
        Assert.Equal(["Bob"], service.Status("CS201").Waitlisted);
    }

    [Theory]
    [InlineData("cs101")]
    [InlineData(" CS101 ")]
    public void Enroll_PrerequisiteMatchIgnoresCaseAndSpaces(string completed)
    {
        var service = CreateServiceWithPrerequisite();

        Assert.Equal(EnrollmentResult.Enrolled, service.Enroll("CS201", U("Alice", completed)));
    }

    [Fact]
    public void Enroll_CourseWithoutPrerequisite_DoesNotRequireCompletedCourses()
    {
        var service = CreateServiceWithPrerequisite();

        Assert.Equal(EnrollmentResult.Enrolled, service.Enroll("CS101", U("Alice")));
    }

    // ---- Drop ----

    [Fact]
    public void Drop_EnrolledUserWithEmptyWaitlist_FreesSeat()
    {
        var service = CreateService();
        service.Enroll(Course, U("Alice"));

        var result = service.Drop(Course, "Alice");

        Assert.Equal(new DropResult(DropOutcome.Dropped, DroppedUser: "Alice"), result);
        var status = service.Status(Course);
        Assert.Empty(status.Enrolled);
        Assert.Equal(2, status.AvailableSeats);
    }

    [Fact]
    public void Drop_EnrolledUser_PromotesFirstWaitlistedUser()
    {
        var service = CreateService(capacity: 1);
        service.Enroll(Course, U("Alice"));
        service.Enroll(Course, U("Bob"));
        service.Enroll(Course, U("Carol"));

        var result = service.Drop(Course, "Alice");

        Assert.Equal(new DropResult(DropOutcome.DroppedAndPromoted, "Bob", "Alice"), result);
        var status = service.Status(Course);
        Assert.Equal(["Bob"], status.Enrolled);
        Assert.Equal(["Carol"], status.Waitlisted);
    }

    [Fact]
    public void Drop_KeepsEnrolledCountAtCapacityWhileWaitlistHasUsers()
    {
        var service = CreateService(capacity: 2);
        foreach (var name in new[] { "Alice", "Bob", "Carol", "Dave" })
            service.Enroll(Course, U(name));

        service.Drop(Course, "Bob");
        service.Drop(Course, "Alice");

        var status = service.Status(Course);
        Assert.Equal(["Carol", "Dave"], status.Enrolled);
        Assert.Empty(status.Waitlisted);
    }

    [Fact]
    public void Drop_WaitlistedUser_RemovesFromWaitlistWithoutChangingEnrolled()
    {
        var service = CreateService(capacity: 1);
        service.Enroll(Course, U("Alice"));
        service.Enroll(Course, U("Bob"));
        service.Enroll(Course, U("Carol"));

        var result = service.Drop(Course, "Bob");

        Assert.Equal(new DropResult(DropOutcome.RemovedFromWaitlist, DroppedUser: "Bob"), result);
        var status = service.Status(Course);
        Assert.Equal(["Alice"], status.Enrolled);
        Assert.Equal(["Carol"], status.Waitlisted);
    }

    [Fact]
    public void Drop_UnknownUser_ReturnsNotFound()
    {
        var service = CreateService();
        service.Enroll(Course, U("Alice"));

        var result = service.Drop(Course, "Zoe");

        Assert.Equal(new DropResult(DropOutcome.NotFound), result);
        Assert.Equal(["Alice"], service.Status(Course).Enrolled);
    }

    [Fact]
    public void Drop_IsCaseInsensitive()
    {
        var service = CreateService();
        service.Enroll(Course, U("Alice"));

        Assert.Equal(DropOutcome.Dropped, service.Drop(Course, " ALICE ").Outcome);
    }

    [Fact]
    public void Drop_EnrolledUser_ReturnsStoredNameNotTypedName()
    {
        var service = CreateService();
        service.Enroll(Course, U("Alice"));

        var result = service.Drop(Course, " aLiCe ");

        Assert.Equal("Alice", result.DroppedUser);
    }

    [Fact]
    public void Drop_WaitlistedUser_ReturnsStoredNameNotTypedName()
    {
        var service = CreateService(capacity: 1);
        service.Enroll(Course, U("Alice"));
        service.Enroll(Course, U("Bob"));

        var result = service.Drop(Course, "BOB");

        Assert.Equal(DropOutcome.RemovedFromWaitlist, result.Outcome);
        Assert.Equal("Bob", result.DroppedUser);
    }

    [Fact]
    public void Drop_UnknownUser_HasNoDroppedUser()
    {
        var service = CreateService();

        Assert.Null(service.Drop(Course, "Zoe").DroppedUser);
    }

    [Fact]
    public void Drop_ThenReEnroll_WhenFull_GoesToBackOfWaitlist()
    {
        var service = CreateService(capacity: 1);
        service.Enroll(Course, U("Alice"));
        service.Enroll(Course, U("Bob"));
        service.Drop(Course, "Alice"); // Bob promoted

        var result = service.Enroll(Course, U("Alice"));

        Assert.Equal(EnrollmentResult.Waitlisted, result);
        Assert.Equal(["Alice"], service.Status(Course).Waitlisted);
    }

    [Fact]
    public void Drop_FromCourseWithPrerequisite_PromotesWaitlistedUser()
    {
        var service = CreateServiceWithPrerequisite(capacity: 1);
        service.Enroll("CS201", U("Alice", "CS101"));
        service.Enroll("CS201", U("Bob", "CS101"));

        var result = service.Drop("CS201", "Alice");

        Assert.Equal(new DropResult(DropOutcome.DroppedAndPromoted, "Bob", "Alice"), result);
    }

    [Fact]
    public void Drop_WithUnknownCourse_Throws()
    {
        var service = CreateService();

        Assert.Throws<KeyNotFoundException>(() => service.Drop("MATH200", "Alice"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Drop_WithBlankUserName_Throws(string? userName)
    {
        var service = CreateService();

        var ex = Assert.Throws<ArgumentException>(() => service.Drop(Course, userName!));
        Assert.Equal("userName", ex.ParamName);
    }

    // ---- Status ----

    [Fact]
    public void Status_ForNewCourse_IsEmpty()
    {
        var service = CreateService(capacity: 3);

        var status = service.Status(Course);

        Assert.Equal(Course, status.CourseCode);
        Assert.Equal(3, status.Capacity);
        Assert.Empty(status.Enrolled);
        Assert.Empty(status.Waitlisted);
        Assert.Equal(3, status.AvailableSeats);
    }

    [Fact]
    public void Status_ReturnsSnapshotUnaffectedByLaterChanges()
    {
        var service = CreateService(capacity: 1);
        service.Enroll(Course, U("Alice"));

        var before = service.Status(Course);
        service.Enroll(Course, U("Bob"));
        service.Drop(Course, "Alice");

        Assert.Equal(["Alice"], before.Enrolled);
        Assert.Empty(before.Waitlisted);
    }

    [Fact]
    public void Status_WithUnknownCourse_Throws()
    {
        var service = CreateService();

        Assert.Throws<KeyNotFoundException>(() => service.Status("MATH200"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Status_WithBlankCourseCode_Throws(string? courseCode)
    {
        var service = CreateService();

        var ex = Assert.Throws<ArgumentException>(() => service.Status(courseCode!));
        Assert.Equal("courseCode", ex.ParamName);
    }
}
