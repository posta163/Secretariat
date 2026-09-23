using System.ComponentModel.DataAnnotations;

namespace Secretariat.Api.Dtos
{
    public class CreateContractRequest
    {
        [Required]
        [MaxLength(200)]
        public string Contractor { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Subject { get; set; } = string.Empty;

        public DateTime ContractDate { get; set; }

        public DateTime? ValidFrom { get; set; }

        public DateTime? ValidTo { get; set; }

        public string? ContractualPenalties { get; set; }

        public string? Comment { get; set; }

        [Range(1, int.MaxValue)]
        public int ResponsibleUserId { get; set; }

        
        
        
        [Range(1, int.MaxValue)]
        public int ManagerUserId { get; set; }

        [Range(1, int.MaxValue)]
        public int DirectorUserId { get; set; }
    }
}