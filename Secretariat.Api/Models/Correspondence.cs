namespace Secretariat.Api.Models
{
    public class Correspondence
    {
        public int Id { get; set; }

        public string Number { get; set; } = string.Empty;

        public string Sender { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public DateTime ReceivedDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsRead { get; set; }

        public DateTime? ReadAt { get; set; }
        public int? RecipientUserId { get; set; }

        public AppUser? RecipientUser { get; set; }
    }
}
