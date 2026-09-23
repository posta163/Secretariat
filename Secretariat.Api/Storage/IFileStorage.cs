using Microsoft.AspNetCore.Http;

namespace Secretariat.Api.Storage
{
    public interface IFileStorage
    {
        Task<FileStorageResult> SaveAsync(
            IFormFile file,
            int correspondenceId);

        Task<FileStorageResult> SaveContractAsync(
          IFormFile file,
          int contractId);

        Task<Stream> OpenReadAsync(string relativePath);
    }

    public record FileStorageResult(
        string StoredFileName,
        string RelativePath);
}