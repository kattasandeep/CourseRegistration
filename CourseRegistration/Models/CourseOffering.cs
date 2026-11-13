namespace CourseRegistration.Models;

/// <summary>
/// A course that students can register for, with a seat limit and an optional prerequisite.
/// </summary>
public class CourseOffering
{
    /// <summary>Unique code that identifies the course, e.g. "CS101".</summary>
    public string CourseCode { get; }

    /// <summary>Maximum number of students that can register. Always greater than zero.</summary>
    public int Capacity { get; }

    /// <summary>Code of the course that must be completed first, or <c>null</c> if there is none.</summary>
    public string? PrerequisiteCourseCode { get; }

    public CourseOffering(string courseCode, int capacity, string? prerequisiteCourseCode = null)
    {
        if (string.IsNullOrWhiteSpace(courseCode))
            throw new ArgumentException("Course code is required.", nameof(courseCode));

        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "Capacity must be greater than zero.");

        var code = courseCode.Trim();
        var prerequisite = string.IsNullOrWhiteSpace(prerequisiteCourseCode) ? null : prerequisiteCourseCode.Trim();

        if (prerequisite is not null && string.Equals(prerequisite, code, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A course cannot be its own prerequisite.", nameof(prerequisiteCourseCode));

        CourseCode = code;
        Capacity = capacity;
        PrerequisiteCourseCode = prerequisite;
    }

    /// <summary>True when the course has a prerequisite.</summary>
    public bool HasPrerequisite => PrerequisiteCourseCode is not null;
}
