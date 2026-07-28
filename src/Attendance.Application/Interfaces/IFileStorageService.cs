namespace Attendance.Application.Interfaces;

/// <summary>
/// Defines the contract for persisting uploaded files to durable storage.
/// Implementations live in the Infrastructure layer and are injected via DI.
/// Deliberately framework-agnostic (stream-based) so the Application layer
/// never depends on ASP.NET Core's <c>IFormFile</c>.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves the given file stream to storage under a freshly generated, collision-free name.
    /// </summary>
    /// <param name="fileStream">The file content to persist.</param>
    /// <param name="originalFileName">
    /// The original file name as submitted by the client. Only its extension is trusted and used
    /// (validated against an allow-list) — the stored file name itself is always freshly generated,
    /// so the caller-supplied name never reaches the file system.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The relative URL/path where the file was stored.</returns>
    /// <exception cref="Domain.Exceptions.DomainException">
    /// Thrown when <paramref name="originalFileName"/>'s extension is not on the allowed list.
    /// </exception>
    Task<string> SaveFileAsync(
        Stream fileStream,
        string originalFileName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a previously stored file for reading.
    /// </summary>
    /// <param name="storedFileName">
    /// The generated file name previously returned (as part of a URL) by <see cref="SaveFileAsync"/>.
    /// Validated strictly before touching the file system — never trust this as a raw path.
    /// </param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>
    /// A readable <see cref="Stream"/> over the file's contents, or <c>null</c> when
    /// <paramref name="storedFileName"/> is not a validly-formed stored name or no such file exists.
    /// </returns>
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken cancellationToken = default);
}
