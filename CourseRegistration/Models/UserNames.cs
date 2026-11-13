namespace CourseRegistration.Models;

/// <summary>
/// The single rule for deciding whether two user names refer to the same user.
/// Every component that compares user names should use <see cref="Comparer"/>.
/// </summary>
public static class UserNames
{
    /// <summary>User names are compared ignoring case (ordinal, culture-independent).</summary>
    public static StringComparer Comparer { get; } = StringComparer.OrdinalIgnoreCase;
}
