using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Secretariat.Api.Data;
using Secretariat.Api.Models;

namespace Secretariat.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CorrespondenceController : ControllerBase
    {
        private readonly SecretariatDbContext _context;

        public CorrespondenceController(SecretariatDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Correspondence>>> GetAll()

        {
            var correspondences = await _context.Correspondences.ToListAsync();

            return Ok(correspondences);
        }

        [HttpPost]
        public async Task<ActionResult<Correspondence>> Create(Correspondence correspondence)
        {
            correspondence.Id = 0;
            correspondence.CreatedDate = DateTime.UtcNow;
            correspondence.IsRead = false;
            correspondence.ReadAt = null;

            _context.Correspondences.Add(correspondence);

            await _context.SaveChangesAsync();

            return Created(
                $"/api/correspondence/{correspondence.Id}",
                correspondence);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Correspondence>> GetById(int id)
        {
            var correspondence = await _context.Correspondences.FindAsync(id);

            if (correspondence == null)
            {
                return NotFound();
            }

            return Ok(correspondence);
        }
    }
}