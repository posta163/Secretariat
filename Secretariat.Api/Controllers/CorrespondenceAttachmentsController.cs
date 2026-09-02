using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Secretariat.Api.Data;
using Secretariat.Api.Models;
using Secretariat.Api.Services.Storage;

namespace Secretariat.Api.Controllers
{
    [ApiController]
    [Route("api/correspondence/{correspondenceId:int}/attachments")]
    public class CorrespondenceAttachmentsController : ControllerBase
    {
        private readonly SecretariatDbContext _context;
        private readonly IFileStorage _fileStorage;

        public CorrespondenceAttachmentsController(
            SecretariatDbContext context,
            IFileStorage fileStorage)
        {
            _context = context;
            _fileStorage = fileStorage;
        }

        [HttpPost]
        public async Task<IActionResult> Upload(
            int correspondenceId,
            IFormFile file)
        {


            var correspondenceExists =
                await _context.Correspondences
                    .AnyAsync(c => c.Id == correspondenceId);

            if (!correspondenceExists)
            {
                return NotFound("Korespondencja nie istnieje.");
            }

            if (file == null )
            {
                return BadRequest("Nie wybrano pliku.");
            }

            var storageResult = await _fileStorage.SaveAsync(
                file,
                correspondenceId);

            var attachment = new CorrespondenceAttachment
            {
                OriginalFileName = file.FileName,
                StoredFileName = storageResult.StoredFileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                FilePath = storageResult.RelativePath,
                UploadedAt = DateTime.UtcNow,
                CorrespondenceId = correspondenceId
            };


            _context.CorrespondenceAttachments.Add(attachment);

            await _context.SaveChangesAsync();

            return Created(
                $"/api/correspondence/{correspondenceId}/attachments/{attachment.Id}",
                new
                {
                    attachment.Id,
                    attachment.OriginalFileName,
                    attachment.ContentType,
                    attachment.FileSize,
                    attachment.UploadedAt,
                    attachment.CorrespondenceId
                });
        }


        [HttpGet]
        public async Task<IActionResult> GetAll(int correspondenceId)
        {
            var correspondenceExists = await _context.Correspondences
                .AnyAsync(c => c.Id == correspondenceId);

            if (!correspondenceExists)
            {
                return NotFound("Korespondencja nie istnieje.");
            }

            var attachments = await _context.CorrespondenceAttachments
                .Where(a => a.CorrespondenceId == correspondenceId)
                .Select(a => new
                {
                    a.Id,
                    a.OriginalFileName,
                    a.ContentType,
                    a.FileSize,
                    a.UploadedAt
                })
                .ToListAsync();

            return Ok(attachments);
        }

        [HttpGet("{attachmentId:int}/download")]
        public async Task<IActionResult> Download(
        
        int correspondenceId,
        int attachmentId)
        {
            var attachment = await _context.CorrespondenceAttachments
                .FirstOrDefaultAsync(a =>
                    a.Id == attachmentId &&
                    a.CorrespondenceId == correspondenceId);

            if (attachment == null)
            {
                return NotFound("Załącznik nie istnieje.");
            }

            var stream = await _fileStorage.OpenReadAsync(
                attachment.FilePath);

            return File(
                stream,
                attachment.ContentType,
                attachment.OriginalFileName);
        }
    }
}