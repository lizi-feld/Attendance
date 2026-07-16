namespace Attendance.Application.DTOs;

/// <summary>
/// Represents the full calendar view for a selected month of attendance history.
/// </summary>
public sealed class AttendanceHistoryMonthDto
{
    /// <summary>
    /// שנה
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// חודש
    /// </summary>
    public int Month { get; init; }

    /// <summary>
    /// סיכום חודשני להצגה בכותרת/כותרת תחתונה של הטבלה.
    /// </summary>
    public AttendanceHistoryMonthSummaryDto Summary { get; init; } = new();

    /// <summary>
    /// כל הימים בחודש עם המידע התלוי-יום להצגה בטבלה.
    /// </summary>
    public IReadOnlyList<AttendanceHistoryDayDto> Days { get; init; } = Array.Empty<AttendanceHistoryDayDto>();
}

/// <summary>
/// Summary data for the month history table footer.
/// </summary>
public sealed class AttendanceHistoryMonthSummaryDto
{
    /// <summary>
    /// סך כל שעות העבודה בחודש.
    /// </summary>
    public string TotalWorkedHours { get; init; } = "00:00";

    /// <summary>
    /// שעות רגילות בחודש.
    /// </summary>
    public string RegularHours { get; init; } = "00:00";

    /// <summary>
    /// הפער מול השעות הרגילות.
    /// </summary>
    public string DeficitHours { get; init; } = "00:00";

    /// <summary>
    /// שעות ההפסקות.
    /// </summary>
    public string BreakHours { get; init; } = "00:00";

    /// <summary>
    /// מספר ההערות/ההסברים הקיימים בחודש.
    /// </summary>
    public int NotesCount { get; init; }
}

/// <summary>
/// Represents a single calendar day in the attendance history view.
/// </summary>
public sealed class AttendanceHistoryDayDto
{
    /// <summary>
    /// התאריך הפיזי של היום בחודש.
    /// </summary>
    public DateOnly Date { get; init; }

    /// <summary>
    /// מספר היום בחודש.
    /// </summary>
    public int DayNumber => Date.Day;

    /// <summary>
    /// שם היום בשבוע.
    /// </summary>
    public string DayOfWeek { get; init; } = string.Empty;

    /// <summary>
    /// תווית תצוגה ליום בחודש, לדוגמה: 01/07/2026.
    /// </summary>
    public string DisplayDateLabel { get; init; } = string.Empty;

    /// <summary>
    /// תווית תצוגה של שם היום בעברית, לדוגמה: יום שלישי.
    /// </summary>
    public string DayLabel { get; init; } = string.Empty;

    /// <summary>
    /// תווית סוג היום, לדוגמה: רגיל, סופ"ש.
    /// </summary>
    public string DayTypeLabel { get; init; } = string.Empty;

    /// <summary>
    /// האם קיימת רשומה עבור היום.
    /// </summary>
    public bool HasAttendanceRecord { get; init; }

    /// <summary>
    /// האם היום הוא סופי שבוע.
    /// </summary>
    public bool IsWeekend { get; init; }

    /// <summary>
    /// האם היום עתידי ביחס למועד הנוכחי.
    /// </summary>
    public bool IsFutureDate { get; init; }

    /// <summary>
    /// האם היום מושבת/לא ניתן לערוך.
    /// </summary>
    public bool IsDisabled { get; init; }

    /// <summary>
    /// האם היום ניתן לעריכה.
    /// </summary>
    public bool IsEditable { get; init; }

    /// <summary>
    /// האם החודש נסגר.
    /// </summary>
    public bool IsMonthClosed { get; init; }

    /// <summary>
    /// האם ניתן לבצע כניסה ביום זה.
    /// </summary>
    public bool CanClockIn { get; init; }

    /// <summary>
    /// האם ניתן לבצע יציאה ביום זה.
    /// </summary>
    public bool CanClockOut { get; init; }

    /// <summary>
    /// הסטטוס התפעולי של היום.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// זמן הכניסה אם קיים.
    /// </summary>
    public DateTime? ClockInTime { get; init; }

    /// <summary>
    /// זמן היציאה אם קיים.
    /// </summary>
    public DateTime? ClockOutTime { get; init; }

    /// <summary>
    /// סך שעות העבודה ליום.
    /// </summary>
    public string? TotalWorkedHours { get; init; }

    /// <summary>
    /// סוג השורה/היום להצגה בטבלה.
    /// </summary>
    public string RowType { get; init; } = "Empty";

    /// <summary>
    /// האם הוגדר אזהרה עבור היום.
    /// </summary>
    public bool HasAlert { get; init; }

    /// <summary>
    /// האם היום מציג חוסר שעות/פער.
    /// </summary>
    public bool HasDeficit { get; init; }

    /// <summary>
    /// טקסט האזהרה אם קיים.
    /// </summary>
    public string? AlertText { get; init; }

    /// <summary>
    /// טקסט מאזן/פער להצגה בטבלה.
    /// </summary>
    public string? DisplayBalance { get; init; }

    /// <summary>
    /// הסבר/הערה לתצוגת היום.
    /// </summary>
    public string? Explanation { get; init; }
}

