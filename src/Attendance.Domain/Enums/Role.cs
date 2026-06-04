namespace Attendance.Domain.Enums;

/// <summary>
/// Defines the access roles available within the Time Attendance System.
/// </summary>
public enum Role
{
    /// <summary>Standard employee with access to their own clock-in/out and attendance history.</summary>
    Employee = 1,

    /// <summary>Administrator with full access to all employees, records, and the dashboard.</summary>
    Admin = 2
}
