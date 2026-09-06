using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Secretariat.Api.CurrentUser;
using Secretariat.Api.Data;
using Secretariat.Api.Models;

namespace Secretariat.Api.Controllers
{
    [ApiController]
    [Route("api/internal-correspondence")]
    public class InternalCorrespondenceController : ControllerBase
    {
        private readonly SecretariatDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public InternalCorrespondenceController(
            SecretariatDbContext context,
            ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpPost]
        public async Task<ActionResult<InternalCorrespondence>> Create(
            InternalCorrespondence internalCorrespondence)
        {
            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            internalCorrespondence.Id = 0;
            internalCorrespondence.CreatedAt = DateTime.UtcNow;
            internalCorrespondence.CreatedByUserId = currentUser.Id;
            internalCorrespondence.Status =
                InternalCorrespondenceStatus.New;

            var year = DateTime.UtcNow.Year;

            var count = await _context.InternalCorrespondences
                .CountAsync(c => c.CreatedAt.Year == year);

            internalCorrespondence.Number =
                $"KWN/{year}/{count + 1:D4}";

            _context.InternalCorrespondences.Add(
                internalCorrespondence);

            await _context.SaveChangesAsync();

            return Created(
                $"/api/internal-correspondence/{internalCorrespondence.Id}",
                internalCorrespondence);
        }



        [HttpGet]
        public async Task<ActionResult<IEnumerable<InternalCorrespondence>>> GetAll()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized("Nie udało się ustalić użytkownika.");
            }

            var internalCorrespondences = await _context.InternalCorrespondences
                .Include(c => c.CreatedByUser)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return Ok(internalCorrespondences);
        }
    }
}