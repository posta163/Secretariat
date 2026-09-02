using Microsoft.AspNetCore.Http;

namespace Secretariat.Api.Services.Storage
{
    public interface IFileStorage
    {
        Task<FileStorageResult> SaveAsync(
            IFormFile file,
            int correspondenceId);

        Task<Stream> OpenReadAsync(string relativePath);
    }

    public record FileStorageResult(
        string StoredFileName,
        string RelativePath);
}