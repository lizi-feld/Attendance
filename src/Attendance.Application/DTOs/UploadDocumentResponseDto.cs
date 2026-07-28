namespace Attendance.Application.DTOs;

/// <summary>
/// HTTP response body for <c>POST /api/attendance/upload-document</c>.
/// </summary>
public sealed record UploadDocumentResponseDto
{
    /// <summary>Gets the relative URL/path where the uploaded file was stored.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Gets the original file name as submitted by the client.</summary>
    public string FileName { get; init; } = string.Empty;
}
