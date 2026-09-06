using Secretariat.Api.Models;

namespace Secretariat.Api.Services.CurrentUser
{
    public interface ICurrentUserService
    {
        Task<AppUser?> GetCurrentUserAsync();
    }
}