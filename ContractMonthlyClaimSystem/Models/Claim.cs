// Models/Claim.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Models
{
    public class Claim
    {
        public enum ClaimStatus { Pending = 0, ApprovedByCoordinator = 1, ApprovedByManager = 2, Rejected = 3, Paid = 4 }

        public int Id { get; set; }
        public int LecturerId { get; set; }

        [Required, MaxLength(200)]
        public string LecturerName { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime Month { get; set; } = DateTime.UtcNow;

        [Precision(9, 2)]
        [Range(typeof(decimal), "0", "1000")]
        public decimal TotalHours { get; set; }

        [Precision(18, 2)]
        [Range(typeof(decimal), "0", "9999999")]
        public decimal TotalAmount { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Pending;
        public DateTime SubmittedDate { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedDate { get; set; }

        public List<ClaimItem> ClaimItems { get; set; } = new();
        public List<Document> Documents { get; set; } = new();
    }
}