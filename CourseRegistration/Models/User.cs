namespace CourseRegistration.Models;

/// <summary>
/// A person who can enroll in courses, with the list of courses they have already completed.
/// </summary>
public class User
{
    private readonly HashSet<string> _completedCourseCodes;

    /// <summary>The user's name. Used to identify the user within a course.</summary>
    public string Name { get; }

    /// <summary>Codes of courses the user has completed. Compared ignoring case.</summary>
    public IReadOnlySet<string> CompletedCourseCodes => _completedCourseCodes;

    public User(string name, IEnumerable<string>? completedCourseCodes = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("User name is required.", nameof(name));

        Name = name.Trim();
        _completedCourseCodes = new HashSet<string>(
            (completedCourseCodes ?? [])
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code.Trim()),
            StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>True if the user has completed the course with the given code.</summary>
    public bool HasCompleted(string courseCode) =>
        !string.IsNullOrWhiteSpace(courseCode) && _completedCourseCodes.Contains(courseCode.Trim());

    /// <summary>True if the user meets the course's prerequisite, or the course has none.</summary>
    public bool MeetsPrerequisiteFor(CourseOffering course)
    {
        ArgumentNullException.ThrowIfNull(course);
        return !course.HasPrerequisite || HasCompleted(course.PrerequisiteCourseCode!);
    }
}
