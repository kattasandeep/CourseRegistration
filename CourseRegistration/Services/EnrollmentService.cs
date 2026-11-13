using CourseRegistration.Models;

namespace CourseRegistration.Services;

/// <summary>
/// Manages enrollment for a set of course offerings. Each course has an enrolled list,
/// limited to the course capacity, and a first-in-first-out waitlist for everyone else.
/// </summary>
/// <remarks>
/// Course codes and user names are matched ignoring case and leading/trailing whitespace.
/// A user can only enroll or join the waitlist if they have completed the course's prerequisite.
/// This class is not thread-safe.
/// </remarks>
public class EnrollmentService
{
    private readonly Dictionary<string, CourseRoster> _rosters = new(StringComparer.OrdinalIgnoreCase);

    public EnrollmentService(IEnumerable<CourseOffering> courses)
    {
        ArgumentNullException.ThrowIfNull(courses);

        foreach (var course in courses)
        {
            ArgumentNullException.ThrowIfNull(course, nameof(courses));

            if (!_rosters.TryAdd(course.CourseCode, new CourseRoster(course)))
                throw new ArgumentException($"Duplicate course code '{course.CourseCode}'.", nameof(courses));
        }
    }

    /// <summary>
    /// Enrolls <paramref name="user"/> in the course if a seat is free; otherwise adds them
    /// to the end of the waitlist. A user who is already enrolled or waitlisted is not added again,
    /// and a user who has not completed the course's prerequisite is not added at all.
    /// </summary>
    /// <exception cref="ArgumentNullException">The user is null.</exception>
    /// <exception cref="KeyNotFoundException">The course code is not known to this service.</exception>
    public EnrollmentResult Enroll(string courseCode, User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var roster = GetRoster(courseCode);
        var name = user.Name;

        if (IndexOf(roster.Enrolled, name) >= 0)
            return EnrollmentResult.AlreadyEnrolled;

        if (IndexOf(roster.Waitlist, name) >= 0)
            return EnrollmentResult.AlreadyWaitlisted;

        if (!user.MeetsPrerequisiteFor(roster.Course))
            return EnrollmentResult.PrerequisiteNotMet;

        if (roster.Enrolled.Count < roster.Course.Capacity)
        {
            roster.Enrolled.Add(name);
            return EnrollmentResult.Enrolled;
        }

        roster.Waitlist.Add(name);
        return EnrollmentResult.Waitlisted;
    }

    /// <summary>
    /// Drops <paramref name="userName"/> from the course. If they held a seat, the first user on
    /// the waitlist is promoted into it. If they were waitlisted, they are removed from the waitlist.
    /// </summary>
    /// <exception cref="ArgumentException">The user name is blank.</exception>
    /// <exception cref="KeyNotFoundException">The course code is not known to this service.</exception>
    public DropResult Drop(string courseCode, string userName)
    {
        var roster = GetRoster(courseCode);
        var name = NormalizeName(userName);

        var enrolledIndex = IndexOf(roster.Enrolled, name);
        if (enrolledIndex >= 0)
        {
            var dropped = roster.Enrolled[enrolledIndex];
            roster.Enrolled.RemoveAt(enrolledIndex);

            if (roster.Waitlist.Count == 0)
                return new DropResult(DropOutcome.Dropped, DroppedUser: dropped);

            var promoted = roster.Waitlist[0];
            roster.Waitlist.RemoveAt(0);
            roster.Enrolled.Add(promoted);
            return new DropResult(DropOutcome.DroppedAndPromoted, promoted, dropped);
        }

        var waitlistIndex = IndexOf(roster.Waitlist, name);
        if (waitlistIndex >= 0)
        {
            var dropped = roster.Waitlist[waitlistIndex];
            roster.Waitlist.RemoveAt(waitlistIndex);
            return new DropResult(DropOutcome.RemovedFromWaitlist, DroppedUser: dropped);
        }

        return new DropResult(DropOutcome.NotFound);
    }

    /// <summary>Returns a snapshot of the course's enrolled and waitlisted users.</summary>
    /// <remarks>The returned lists are copies; later enrollments do not change them.</remarks>
    /// <exception cref="KeyNotFoundException">The course code is not known to this service.</exception>
    public EnrollmentStatus Status(string courseCode)
    {
        var roster = GetRoster(courseCode);

        return new EnrollmentStatus(
            roster.Course.CourseCode,
            roster.Course.Capacity,
            roster.Enrolled.ToArray(),
            roster.Waitlist.ToArray());
    }

    private CourseRoster GetRoster(string courseCode)
    {
        if (string.IsNullOrWhiteSpace(courseCode))
            throw new ArgumentException("Course code is required.", nameof(courseCode));

        if (!_rosters.TryGetValue(courseCode.Trim(), out var roster))
            throw new KeyNotFoundException($"Course '{courseCode.Trim()}' was not found.");

        return roster;
    }

    private static string NormalizeName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("User name is required.", nameof(userName));

        return userName.Trim();
    }

    private static int IndexOf(List<string> names, string name) =>
        names.FindIndex(n => UserNames.Comparer.Equals(n, name));

    private sealed class CourseRoster(CourseOffering course)
    {
        public CourseOffering Course { get; } = course;
        public List<string> Enrolled { get; } = [];
        public List<string> Waitlist { get; } = [];
    }
}
