namespace Application.Modules.Attachments;

/// <summary>
/// Stores and retrieves private attachment binaries independently of attachment metadata.
/// </summary>
public interface IAttachmentStorage
{
    Task SaveAsync(
        Stream content,
        string storedFileName,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken = default);
}