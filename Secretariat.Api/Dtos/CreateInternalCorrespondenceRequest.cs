using System.ComponentModel.DataAnnotations;

namespace Secretariat.Api.Dtos
{
    public class CreateInternalCorrespondenceRequest
    {
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public List<int> ApproverUserIds { get; set; } = new();
    }
}