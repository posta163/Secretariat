
namespace Secretariat.Api.Storage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment _environment;

        public LocalFileStorage(
            IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        // Dotychczasowy zapis załączników korespondencji.
        public Task<FileStorageResult> SaveAsync(
            IFormFile file,
            int correspondenceId)
        {
            return SaveToDirectoryAsync(
                file,
                "correspondence",
                correspondenceId);
        }

        // Nowy zapis załączników umów.
        public Task<FileStorageResult> SaveContractAsync(
            IFormFile file,
            int contractId)
        {
            return SaveToDirectoryAsync(
                file,
                "contracts",
                contractId);
        }

        // Wspólny mechanizm zapisywania plików.
        private async Task<FileStorageResult> SaveToDirectoryAsync(
            IFormFile file,
            string category,
            int documentId)
        {
            var extension = Path.GetExtension(file.FileName);

            var storedFileName =
                $"{Guid.NewGuid()}{extension}";

            var relativeDirectory = Path.Combine(
                "uploads",
                category,
                documentId.ToString());

            var physicalDirectory = Path.Combine(
                _environment.ContentRootPath,
                relativeDirectory);

            Directory.CreateDirectory(physicalDirectory);

            var physicalPath = Path.Combine(
                physicalDirectory,
                storedFileName);

            await using var stream = new FileStream(
                physicalPath,
                FileMode.CreateNew,
                FileAccess.Write);

            await file.CopyToAsync(stream);

            var relativePath = Path.Combine(
                relativeDirectory,
                storedFileName);

            return new FileStorageResult(
                storedFileName,
                relativePath);
        }

        // Odczyt pliku na podstawie ścieżki
        // zapisanej wcześniej w bazie danych.
        public Task<Stream> OpenReadAsync(
            string relativePath)
        {
            var physicalPath = Path.Combine(
                _environment.ContentRootPath,
                relativePath);

            Stream stream = new FileStream(
                physicalPath,
                FileMode.Open,
                FileAccess.Read);

            return Task.FromResult(stream);
        }
    }
}