namespace Secretariat.Api.Models
{
    public class ContractAttachment
    {
        public int Id { get; set; }

        public int ContractId { get; set; }

        public Contract? Contract { get; set; }

        public string OriginalFileName { get; set; }
            = string.Empty;

        public string StoredFileName { get; set; }
            = string.Empty;

        public string ContentType { get; set; }
            = string.Empty;

        public long FileSize { get; set; }

        public string FilePath { get; set; }
            = string.Empty;

        public DateTime UploadedAt { get; set; }

        public ContractAttachmentType Type { get; set; }
            = ContractAttachmentType.Original;

        public int UploadedByUserId { get; set; }

        public AppUser? UploadedByUser { get; set; }
    }
}