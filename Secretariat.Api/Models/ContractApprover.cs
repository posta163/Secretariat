namespace Secretariat.Api.Models
{
    public class ContractApprover
    {
        public int Id { get; set; }

        public int ContractId { get; set; }

        public Contract? Contract { get; set; }

        public int ApproverUserId { get; set; }

        public AppUser? ApproverUser { get; set; }

        public ContractApproverRole Role { get; set; }

        public ContractApprovalStatus Status { get; set; }
            = ContractApprovalStatus.Pending;

        public DateTime? ReviewedAt { get; set; }
    }
}