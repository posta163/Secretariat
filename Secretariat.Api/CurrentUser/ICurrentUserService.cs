using Secretariat.Api.Models;

namespace Secretariat.Api.CurrentUser
{
    public interface ICurrentUserService
    {
        Task<AppUser?> GetCurrentUserAsync();
    }
}