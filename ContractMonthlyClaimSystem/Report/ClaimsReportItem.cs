using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Report
{
    public class ClaimsReportItem
    {
        public string LecturerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime Month { get; set; }
        public decimal TotalHours { get; set; }
        public decimal TotalAmount { get; set; }
        public Claim.ClaimStatus Status { get; set; }
        public DateTime SubmittedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
    }
}
