namespace CourseRegistration.Models;

/// <summary>What happened when a user was dropped from a course.</summary>
public enum DropOutcome
{
    /// <summary>The user was removed from the enrolled list and nobody was waiting.</summary>
    Dropped,

    /// <summary>The user was removed from the enrolled list and the first waitlisted user took the seat.</summary>
    DroppedAndPromoted,

    /// <summary>The user was removed from the waitlist.</summary>
    RemovedFromWaitlist,

    /// <summary>The user was neither enrolled nor waitlisted; nothing changed.</summary>
    NotFound,
}

/// <summary>Result of a drop request.</summary>
/// <param name="Outcome">What happened.</param>
/// <param name="PromotedUser">The user moved from the waitlist into the course, or <c>null</c> if nobody was promoted.</param>
/// <param name="DroppedUser">
/// The dropped user's name as stored on the course (not as typed in the request),
/// or <c>null</c> when the outcome is <see cref="DropOutcome.NotFound"/>.
/// </param>
public sealed record DropResult(DropOutcome Outcome, string? PromotedUser = null, string? DroppedUser = null);
