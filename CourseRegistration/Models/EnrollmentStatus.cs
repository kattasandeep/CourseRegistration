namespace CourseRegistration.Models;

/// <summary>Snapshot of a course's enrolled and waitlisted users at a point in time.</summary>
/// <param name="CourseCode">The course this status describes.</param>
/// <param name="Capacity">Maximum number of enrolled users.</param>
/// <param name="Enrolled">Enrolled users, in the order they got a seat.</param>
/// <param name="Waitlisted">Waitlisted users, first in line first.</param>
public sealed record EnrollmentStatus(
    string CourseCode,
    int Capacity,
    IReadOnlyList<string> Enrolled,
    IReadOnlyList<string> Waitlisted)
{
    /// <summary>Number of open seats.</summary>
    public int AvailableSeats => Capacity - Enrolled.Count;
}
