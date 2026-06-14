namespace Attendance.Application.Exceptions;

/// <summary>
/// Thrown when a requested <see cref="Attendance.Domain.Entities.AttendanceRecord"/> does not exist.
/// </summary>
public sealed class AttendanceRecordNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="AttendanceRecordNotFoundException"/>
    /// for the given record ID.
    /// </summary>
    public AttendanceRecordNotFoundException(int recordId)
        : base($"Attendance record {recordId} was not found.") { }
}
