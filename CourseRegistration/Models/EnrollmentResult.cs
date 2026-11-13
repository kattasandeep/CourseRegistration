namespace CourseRegistration.Models;

/// <summary>Outcome of an enroll request.</summary>
public enum EnrollmentResult
{
    /// <summary>A seat was available and the user is now enrolled.</summary>
    Enrolled,

    /// <summary>The course was full, so the user was added to the end of the waitlist.</summary>
    Waitlisted,

    /// <summary>The user was already enrolled in the course; nothing changed.</summary>
    AlreadyEnrolled,

    /// <summary>The user was already on the waitlist for the course; nothing changed.</summary>
    AlreadyWaitlisted,

    /// <summary>The user has not completed the course's prerequisite, so they were neither enrolled nor waitlisted.</summary>
    PrerequisiteNotMet,
}
