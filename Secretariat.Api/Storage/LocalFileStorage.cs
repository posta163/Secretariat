namespace Secretariat.Api.Services.Storage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly IWebHostEnvironment _environment;

        public LocalFileStorage(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public async Task<FileStorageResult> SaveAsync(
            IFormFile file,
            int correspondenceId)
        {
            var extension = Path.GetExtension(file.FileName);

            var storedFileName =
                $"{Guid.NewGuid()}{extension}";

            var relativeDirectory = Path.Combine(
                "uploads",
                "correspondence",
                correspondenceId.ToString());

            var physicalDirectory = Path.Combine(
                _environment.ContentRootPath,
                relativeDirectory);

            Directory.CreateDirectory(physicalDirectory);

            var physicalPath = Path.Combine(
                physicalDirectory,
                storedFileName);

            await using var stream =
                new FileStream(
                    physicalPath,
                    FileMode.Create);

            await file.CopyToAsync(stream);

            var relativePath = Path.Combine(
                relativeDirectory,
                storedFileName);

            return new FileStorageResult(
                storedFileName,
                relativePath);
        }

        public Task<Stream> OpenReadAsync(string relativePath)
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