using System.Text.RegularExpressions;
using Attendance.Application.Constants;
using Attendance.Application.Interfaces;
using Attendance.Domain.Exceptions;
using Microsoft.Extensions.Hosting;

namespace Attendance.Infrastructure.Services;

/// <summary>
/// Persists uploaded files to a local "uploads" directory under the application's content root.
/// </summary>
/// <remarks>
/// Stored file names are always freshly generated GUIDs — the caller-supplied name is never
/// written to disk verbatim, which rules out path traversal and filename-collision attacks.
/// Only the extension (validated against <see cref="DocumentUploadPolicy.AllowedExtensions"/>)
/// is taken from the original name. The returned URL points at the authenticated
/// <c>GET /api/attendance/documents/{fileName}</c> endpoint rather than a raw static path,
/// so access to (potentially sensitive) absence documents always goes through authorization.
/// </remarks>
public sealed class LocalFileStorageService : IFileStorageService
{
    private const string AbsenceDocumentsSegment = "absence-documents";

    private static readonly Regex StoredFileNamePattern = new(
        $"^[0-9a-f]{{32}}(?:{string.Join('|', DocumentUploadPolicy.AllowedExtensions.Select(Regex.Escape))})$",
        RegexOptions.Compiled);

    private readonly string _absoluteStorageDirectory;

    /// <summary>
    /// Initializes a new instance of <see cref="LocalFileStorageService"/>, ensuring the
    /// storage directory exists under the host's content root.
    /// </summary>
    public LocalFileStorageService(IHostEnvironment environment)
    {
        _absoluteStorageDirectory = Path.Combine(environment.ContentRootPath, "uploads", AbsenceDocumentsSegment);
        Directory.CreateDirectory(_absoluteStorageDirectory);
    }

    /// <inheritdoc />
    /// <exception cref="DomainException">
    /// Thrown when the file extension is not in <see cref="DocumentUploadPolicy.AllowedExtensions"/>.
    /// </exception>
    public async Task<string> SaveFileAsync(
        Stream fileStream,
        string originalFileName,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

        if (!DocumentUploadPolicy.AllowedExtensions.Contains(extension))
        {
            throw new DomainException(
                $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", DocumentUploadPolicy.AllowedExtensions)}.");
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var absolutePath = Path.Combine(_absoluteStorageDirectory, storedFileName);

        await using (var destination = File.Create(absolutePath))
        {
            await fileStream.CopyToAsync(destination, cancellationToken);
        }

        return $"/api/attendance/documents/{storedFileName}";
    }

    /// <inheritdoc />
    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default)
    {
        if (!StoredFileNamePattern.IsMatch(storedFileName))
            return Task.FromResult<Stream?>(null);

        var absolutePath = Path.Combine(_absoluteStorageDirectory, storedFileName);

        if (!File.Exists(absolutePath))
            return Task.FromResult<Stream?>(null);

        Stream stream = File.OpenRead(absolutePath);
        return Task.FromResult<Stream?>(stream);
    }
}
