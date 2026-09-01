using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Secretariat.Api.Data;
using Secretariat.Api.Models;

namespace Secretariat.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppUsersController : ControllerBase
    {
        private readonly SecretariatDbContext _context;

        public AppUsersController(SecretariatDbContext context)
        {
            _context = context;
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
            user.Id = 0;

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            return Created(
                $"/api/appusers/{user.Id}",
                user);
        }
    }
}