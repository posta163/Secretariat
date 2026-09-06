namespace Secretariat.Api.Models
{
    public class InternalCorrespondence
    {
        public int Id { get; set; }

        public string Number { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public InternalCorrespondenceStatus Status { get; set; }
            = InternalCorrespondenceStatus.New;

        public int CreatedByUserId { get; set; }

        public AppUser? CreatedByUser { get; set; }
    }
}