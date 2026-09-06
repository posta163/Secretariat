using Microsoft.EntityFrameworkCore;
using Secretariat.Api.Data;
using Secretariat.Api.Models;
using Secretariat.Api.CurrentUser;

namespace Secretariat.Api.CurrentUser
{
    public class LocalCurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SecretariatDbContext _context;

        public LocalCurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            SecretariatDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public async Task<AppUser?> GetCurrentUserAsync()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext == null)
            {
                return null;
            }

            if (!httpContext.Request.Headers.TryGetValue(
                "X-User-Id",
                out var userIdHeader))
            {
                return null;
            }

            if (!int.TryParse(userIdHeader, out var userId))
            {
                return null;
            }

            return await _context.AppUsers
                .FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}