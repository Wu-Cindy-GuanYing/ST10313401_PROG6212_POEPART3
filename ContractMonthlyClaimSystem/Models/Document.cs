using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class Document
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public Claim? Claim { get; set; }

        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [MaxLength(255)]
        public string OriginalFileName { get; set; } = string.Empty;

        public byte[] FileContent { get; set; } = Array.Empty<byte>();
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(128)]
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
    }
}