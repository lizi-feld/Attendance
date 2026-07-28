namespace Attendance.Domain.Enums;

/// <summary>
/// Categorises the reason for an employee's reported absence.
/// </summary>
public enum AbsenceType
{
    /// <summary>Planned annual/vacation leave.</summary>
    Vacation = 1,

    /// <summary>Personal sick leave. Requires a supporting document.</summary>
    SickLeave = 2,

    /// <summary>Leave taken to care for a sick child. Requires a supporting document.</summary>
    ChildSickLeave = 3,

    /// <summary>Pregnancy-related leave.</summary>
    Pregnancy = 4,

    /// <summary>Public holiday. Manually selected — no automatic calculation.</summary>
    Holiday = 5,

    /// <summary>Chol HaMoed (intermediate festival days). Manually selected.</summary>
    CholHaMoed = 6,

    /// <summary>Any other absence reason. Requires a supporting document.</summary>
    Other = 7
}
