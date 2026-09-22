namespace Secretariat.Api.Models
{
    public class Contract
    {
        public int Id { get; set; }

        public string Number { get; set; } = string.Empty;

        public string Contractor { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public DateTime ContractDate { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public string? ContractualPenalties { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        public ContractStatus Status { get; set; }
            = ContractStatus.New;

        public int CreatedByUserId { get; set; }

        public AppUser? CreatedByUser { get; set; }

        public int ResponsibleUserId { get; set; }

        public AppUser? ResponsibleUser { get; set; }
    }
}