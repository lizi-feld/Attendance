namespace Attendance.Application.Constants;

/// <summary>
/// Shared, single-source-of-truth policy for absence-document uploads.
/// Referenced by both the API controller (request size limit) and the file storage
/// implementation (extension allow-list), so the two never drift apart.
/// </summary>
public static class DocumentUploadPolicy
{
    /// <summary>Maximum accepted upload size, in bytes (10 MB).</summary>
    public const int MaxFileSizeBytes = 10 * 1024 * 1024;

    /// <summary>
    /// File extensions accepted for absence supporting documents, lowercase and dot-prefixed.
    /// </summary>
    public static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
}
