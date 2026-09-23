
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Secretariat.Api.CurrentUser;
using Secretariat.Api.Data;
using Secretariat.Api.Models;
using Secretariat.Api.Storage;

namespace Secretariat.Api.Controllers
{
    [ApiController]
    [Route("api/contracts/{contractId:int}/attachments")]
    public class ContractAttachmentsController : ControllerBase
    {
        private readonly SecretariatDbContext _context;
        private readonly IFileStorage _fileStorage;
        private readonly ICurrentUserService _currentUserService;

        private const long MaxFileSize = 10 * 1024 * 1024;

        public ContractAttachmentsController(
            SecretariatDbContext context,
            IFileStorage fileStorage,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _fileStorage = fileStorage;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(15_000_000)]
        public async Task<IActionResult> Upload(
            int contractId,
            [FromForm] IFormFile? file,
            [FromForm] ContractAttachmentType type =
                ContractAttachmentType.Original)
        {
            // Ustalamy aktualnego użytkownika.
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            // Sprawdzamy istnienie umowy.
            var contract = await _context.Contracts
                .Include(c => c.Approvers)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
            {
                return NotFound("Umowa nie istnieje.");
            }

            // Sprawdzamy rodzaj załącznika.
            if (type != ContractAttachmentType.Original &&
                type != ContractAttachmentType.Signed)
            {
                return BadRequest(
                    "Nieobsługiwany rodzaj załącznika.");
            }

            var isPrivileged =
                currentUser.Role == UserRole.Secretariat ||
                currentUser.Role == UserRole.Administrator;

            var isAuthor =
                contract.CreatedByUserId == currentUser.Id;

            var isResponsible =
                contract.ResponsibleUserId == currentUser.Id;

            var approval = contract.Approvers
                .FirstOrDefault(a =>
                    a.ApproverUserId == currentUser.Id);

            // Kontrola uprawnień do zwykłego dokumentu.
            if (type == ContractAttachmentType.Original)
            {
                if (!isPrivileged && !isAuthor && !isResponsible)
                {
                    return StatusCode(
                        403,
                        "Nie masz uprawnień do dodawania dokumentów tej umowy.");
                }

                if (contract.Status != ContractStatus.New)
                {
                    return Conflict(
                        "Dokument podstawowy można dodawać tylko przed rozpoczęciem akceptacji.");
                }
            }

            // Kontrola uprawnień do podpisanej wersji.
            if (type == ContractAttachmentType.Signed)
            {
                if (currentUser.Role != UserRole.Approver ||
                    approval == null)
                {
                    return StatusCode(
                        403,
                        "Tylko przypisany akceptujący może przesłać podpisany dokument.");
                }

                if (contract.Status == ContractStatus.Approved ||
                    contract.Status == ContractStatus.Rejected ||
                    approval.Status != ContractApprovalStatus.Pending)
                {
                    return Conflict(
                        "Nie można przesłać podpisanego dokumentu po zakończeniu decyzji.");
                }
            }

            // Podstawowa walidacja pliku.
            if (file == null || file.Length == 0)
            {
                return BadRequest("Nie przesłano pliku.");
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest(
                    "Maksymalny rozmiar pliku wynosi 10 MB.");
            }

            var extension =
                Path.GetExtension(file.FileName).ToLowerInvariant();

            var allowedExtensions =
                type == ContractAttachmentType.Original
                    ? new[] { ".pdf", ".docx" }
                    : new[] { ".pdf", ".p7m" };

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(
                    "Niedozwolony format pliku.");
            }

            // Zapisujemy plik na dysku.
            var storedFile = await _fileStorage.SaveContractAsync(
                file,
                contractId);

            // Zapisujemy metadane w bazie.
            var attachment = new ContractAttachment
            {
                ContractId = contractId,
                OriginalFileName = Path.GetFileName(file.FileName),
                StoredFileName = storedFile.StoredFileName,
                FilePath = storedFile.RelativePath,
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                    ? "application/octet-stream"
                    : file.ContentType,
                FileSize = file.Length,
                UploadedAt = DateTime.UtcNow,
                UploadedByUserId = currentUser.Id,
                Type = type
            };

            _context.ContractAttachments.Add(attachment);

            await _context.SaveChangesAsync();

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    attachment.Id,
                    attachment.ContractId,
                    attachment.OriginalFileName,
                    attachment.FileSize,
                    attachment.Type,
                    attachment.UploadedAt,
                    attachment.UploadedByUserId
                });

        }

       
        
        
        
        [HttpGet]
        public async Task<IActionResult> GetAll(int contractId)
        {
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
                return Unauthorized("Nie udało się ustalić użytkownika.");

            var contract = await _context.Contracts
                .Include(c => c.Approvers)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
                return NotFound("Umowa nie istnieje.");

            // Dostęp do plików mają tylko uprawnione osoby.
            var canRead =
                currentUser.Role == UserRole.Administrator ||
                currentUser.Role == UserRole.Secretariat ||
                contract.CreatedByUserId == currentUser.Id ||
                contract.ResponsibleUserId == currentUser.Id ||
                contract.Approvers.Any(a =>
                    a.ApproverUserId == currentUser.Id);

            if (!canRead)
                return StatusCode(403, "Brak dostępu do załączników tej umowy.");

            var attachments = await _context.ContractAttachments
                .AsNoTracking()
                .Where(a => a.ContractId == contractId)
                .OrderByDescending(a => a.UploadedAt)
                .Select(a => new
                {
                    a.Id,
                    a.ContractId,
                    a.OriginalFileName,
                    a.FileSize,
                    a.ContentType,
                    a.Type,
                    a.UploadedAt,
                    a.UploadedByUserId
                })
                .ToListAsync();

            return Ok(attachments);
        }






        [HttpGet("{attachmentId:int}/download")]
        public async Task<IActionResult> Download(
            int contractId,
            int attachmentId)
        {
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
                return Unauthorized("Nie udało się ustalić użytkownika.");

            var contract = await _context.Contracts
                .Include(c => c.Approvers)
                .FirstOrDefaultAsync(c => c.Id == contractId);

            if (contract == null)
                return NotFound("Umowa nie istnieje.");

            var canRead =
                currentUser.Role == UserRole.Administrator ||
                currentUser.Role == UserRole.Secretariat ||
                contract.CreatedByUserId == currentUser.Id ||
                contract.ResponsibleUserId == currentUser.Id ||
                contract.Approvers.Any(a =>
                    a.ApproverUserId == currentUser.Id);

            if (!canRead)
                return StatusCode(403, "Brak dostępu do plików tej umowy.");

            var attachment = await _context.ContractAttachments
                .AsNoTracking()
                .FirstOrDefaultAsync(a =>
                    a.Id == attachmentId &&
                    a.ContractId == contractId);

            if (attachment == null)
                return NotFound("Załącznik nie istnieje.");

            try
            {
                var stream = await _fileStorage.OpenReadAsync(
                    attachment.FilePath);

                return File(
                    stream,
                    attachment.ContentType,
                    attachment.OriginalFileName);
            }
            catch (FileNotFoundException)
            {
                return NotFound("Plik nie istnieje na dysku.");
            }
            catch (DirectoryNotFoundException)
            {
                return NotFound("Katalog pliku nie istnieje.");
            }
        }
    }
}