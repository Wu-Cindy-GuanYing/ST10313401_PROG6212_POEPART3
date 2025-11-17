using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.ViewModels
{
    public class ClaimCreateVm
    {
        [Display(Name = "Hours Worked")]
        [Range(0.25, 500, ErrorMessage = "Hours must be between 0.25 and 500")]
        public decimal HoursWorked { get; set; }

        [Display(Name = "Hourly Rate")]
        [Range(0, 10000, ErrorMessage = "Rate must be between 0 and 10000")]
        public decimal HourlyRate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        [Display(Name = "Supporting Documents")]
        public List<IFormFile>? Uploads { get; set; }
    }
}