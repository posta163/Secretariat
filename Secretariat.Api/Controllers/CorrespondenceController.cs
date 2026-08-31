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
    }
}