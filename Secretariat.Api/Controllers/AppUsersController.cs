using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Secretariat.Api.CurrentUser;
using Secretariat.Api.Data;
using Secretariat.Api.Models;

namespace Secretariat.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppUsersController : ControllerBase
    {
        private readonly SecretariatDbContext _context;
        private readonly ICurrentUserService _currentUserService;

        public AppUsersController(SecretariatDbContext context, ICurrentUserService currentUserService)
        {
            _context = context;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AppUser>>> GetAll()
        {
            var users = await _context.AppUsers.ToListAsync();

            return Ok(users);
        }

        [HttpPost]
        public async Task<ActionResult<AppUser>> Create(AppUser user)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Nie rozpoznano użytkownika.");
            if (currentUser.Role != UserRole.Administrator)
                return StatusCode(StatusCodes.Status403Forbidden, "Tylko administrator może zarządzać użytkownikami.");

            var validationError = ValidateUser(user);
            if (validationError != null)
                return BadRequest(validationError);

            user.Id = 0;
            user.DisplayName = user.DisplayName.Trim();
            user.Email = user.Email.Trim();

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            return Created(
                $"/api/appusers/{user.Id}",
                user);
        }



        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(
            int id,
            AppUser request)
        {
            var currentUser = await _currentUserService.GetCurrentUserAsync();
            if (currentUser == null)
                return Unauthorized("Nie rozpoznano użytkownika.");
            if (currentUser.Role != UserRole.Administrator)
                return StatusCode(StatusCodes.Status403Forbidden, "Tylko administrator może zarządzać użytkownikami.");

            var user = await _context.AppUsers.FindAsync(id);

            if (user == null)
            {
                return NotFound("Użytkownik nie istnieje.");
            }

            var validationError = ValidateUser(request);
            if (validationError != null)
                return BadRequest(validationError);

            if (currentUser.Id == id && request.Role != UserRole.Administrator)
            {
                return BadRequest("Nie możesz odebrać sobie roli administratora. Zmianę musi wykonać inny administrator.");
            }

            user.DisplayName = request.DisplayName.Trim();
            user.Email = request.Email.Trim();
            user.Role = request.Role;
            user.EntraObjectId = request.EntraObjectId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static string? ValidateUser(AppUser request)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Email))
                return "Imię i nazwisko oraz e-mail są wymagane.";
            if (request.DisplayName.Trim().Length > 200)
                return "Imię i nazwisko mogą mieć maksymalnie 200 znaków.";
            if (request.Email.Trim().Length > 254 || !new EmailAddressAttribute().IsValid(request.Email.Trim()))
                return "Podaj poprawny adres e-mail.";
            if (!Enum.IsDefined(request.Role))
                return "Nieprawidłowa rola użytkownika.";

            return null;
        }
    }
}
