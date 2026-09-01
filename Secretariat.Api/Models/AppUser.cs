namespace Secretariat.Api.Models
{
    public class AppUser
    {
        public int Id { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? EntraObjectId { get; set; }
    }
}