namespace Secretariat.Api.Models
{
    public class InternalCorrespondenceApprover
    {
        public int Id { get; set; }

        public int InternalCorrespondenceId { get; set; }

        public InternalCorrespondence? InternalCorrespondence { get; set; }

        public int ApproverUserId { get; set; }

        public AppUser? ApproverUser { get; set; }

        public InternalApprovalStatus Status { get; set; }
            = InternalApprovalStatus.Pending;

        public DateTime? ReviewedAt { get; set; }
    }
}