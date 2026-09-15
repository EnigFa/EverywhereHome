namespace Project.Application.Files;

public interface IFileStorage
{
    Task<string> SaveListingPhotoAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
    void TryDeleteLocal(string url);
}
