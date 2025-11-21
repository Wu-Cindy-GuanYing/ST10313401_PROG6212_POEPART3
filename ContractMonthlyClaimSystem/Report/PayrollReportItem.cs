namespace ContractMonthlyClaimSystem.Report
{
    public class PayrollReportItem
    {
        public string LecturerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime ClaimMonth { get; set; }
    }
}
