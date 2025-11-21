using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Report
{
    public class ClaimsReportParameters
    {
        public Claim.ClaimStatus? Status { get; set; }
        public DateTime StartDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-6);
        public DateTime EndDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
    }
}
