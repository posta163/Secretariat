using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Secretariat.Api.CurrentUser;
using Secretariat.Api.Data;
using Secretariat.Api.Models;


namespace Secretariat.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CorrespondenceController : ControllerBase
    {
        private readonly SecretariatDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public CorrespondenceController(
            SecretariatDbContext context,
            ICurrentUserService currentUserService)
                {
                 _context = context;
                 _currentUserService = currentUserService;
                 }





        
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Correspondence>>> GetAll()
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized("Nie udało się ustalić użytkownika.");
            }

            var query = _context.Correspondences
                .Include(c => c.RecipientUser)
                .AsQueryable();

            if (currentUser.Role == UserRole.Employee ||
                currentUser.Role == UserRole.Approver)
            {
                query = query.Where(c =>
                    c.RecipientUserId == currentUser.Id);
            }

            var correspondences = await query.ToListAsync();

            return Ok(correspondences);
        }



        [HttpPost]
        public async Task<ActionResult<Correspondence>> Create(Correspondence correspondence)
        {






            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            if (currentUser.Role != UserRole.Secretariat &&
                currentUser.Role != UserRole.Administrator)
            {
                return StatusCode(
                    403,
                    "Tylko Sekretariat i Administrator mogą zarządzać korespondencją.");
            }






            correspondence.Id = 0;
            correspondence.CreatedDate = DateTime.UtcNow;
            correspondence.IsRead = false;
            correspondence.ReadAt = null;

            if (correspondence.RecipientUserId.HasValue)
            {
                var recipientExists = await _context.AppUsers
                    .AnyAsync(u => u.Id == correspondence.RecipientUserId.Value);

                if (!recipientExists)
                {
                    return BadRequest("Wybrany adresat nie istnieje.");
                }
            }


            if (correspondence.Type == CorrespondenceType.Unknown)
            {
                return BadRequest("Należy wybrać typ korespondencji.");
            }

            if (correspondence.RelatedIncomingCorrespondenceId.HasValue)
            {
                if (correspondence.Type != CorrespondenceType.Outgoing)
                {
                    return BadRequest(
                        "Tylko korespondencja wychodząca może być odpowiedzią na korespondencję przychodzącą.");
                }

                var relatedIncoming = await _context.Correspondences
                    .FirstOrDefaultAsync(c =>
                        c.Id == correspondence.RelatedIncomingCorrespondenceId.Value);

                if (relatedIncoming == null)
                {
                    return BadRequest(
                        "Wskazana korespondencja przychodząca nie istnieje.");
                }

                if (relatedIncoming.Type != CorrespondenceType.Incoming)
                {
                    return BadRequest(
                        "Powiązana korespondencja musi być typu przychodzącego.");
                }
            }

            var year = DateTime.UtcNow.Year;

            var prefix = correspondence.Type switch
            {
                CorrespondenceType.Incoming => "KP",
                CorrespondenceType.Outgoing => "KW",
                _ => throw new InvalidOperationException("Nieobsługiwany typ korespondencji.")
            };

            var count = await _context.Correspondences
                .CountAsync(c =>
                    c.CreatedDate.Year == year &&
                    c.Type == correspondence.Type);

            correspondence.Number =
                $"{prefix}/{year}/{count + 1:D4}";



            _context.Correspondences.Add(correspondence);

            await _context.SaveChangesAsync();

            return Created(
                $"/api/correspondence/{correspondence.Id}",
                correspondence);
        }




        [HttpGet("{id}")]
        public async Task<ActionResult<Correspondence>> GetById(int id)
        {
            var correspondence = await _context.Correspondences
                .Include(c => c.RecipientUser)
                .Include(c => c.RelatedIncomingCorrespondence)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (correspondence == null)
            {
                return NotFound();
            }



            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            if (!CanRead(currentUser, correspondence))
            {
                return StatusCode(
                    403,
                    "Nie masz dostępu do tej korespondencji.");
            }


            return Ok(correspondence);
        }



        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Correspondence updatedCorrespondence)
        {



            var currentUser =
                await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
            {
                return Unauthorized(
                    "Nie udało się ustalić użytkownika.");
            }

            if (currentUser.Role != UserRole.Secretariat &&
                currentUser.Role != UserRole.Administrator)
            {
                return StatusCode(
                    403,
                    "Tylko Sekretariat i Administrator mogą zarządzać korespondencją.");
            }



            var correspondence = await _context.Correspondences
            .Include(c => c.RecipientUser)
            .FirstOrDefaultAsync(c => c.Id == id);

            if (correspondence == null)
            {
                return NotFound();
            }

            correspondence.Number = updatedCorrespondence.Number;
            correspondence.Sender = updatedCorrespondence.Sender;
            correspondence.Subject = updatedCorrespondence.Subject;
            correspondence.ReceivedDate = updatedCorrespondence.ReceivedDate;

            await _context.SaveChangesAsync();

            return NoContent();
        }
      
        
        
        
        
        
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {

            var currentUser =await _currentUserService.GetCurrentUserAsync();

            if (currentUser == null)
                return Unauthorized();


            var correspondence = await _context.Correspondences.FindAsync(id);

            if (correspondence == null)
            {
                return NotFound();
            }
            if (!CanRead(currentUser, correspondence))
            {
                return StatusCode(
                    403,
                    "Nie masz dostępu do tej korespondencji.");
            }

            if (correspondence.IsRead)
            {
                return NoContent();
            }

            correspondence.IsRead = true;
            correspondence.ReadAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }



        private static bool CanRead(
            AppUser user,
            Correspondence correspondence)
        {
            return user.Role == UserRole.Administrator
                || user.Role == UserRole.Secretariat
                || correspondence.RecipientUserId == user.Id;
        }
    }
}