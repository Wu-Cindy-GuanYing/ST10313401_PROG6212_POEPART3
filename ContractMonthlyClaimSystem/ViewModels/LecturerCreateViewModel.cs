using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.ViewModels
{
    public class LecturerCreateViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Range(0.01, 1000)]
        [Display(Name = "Hourly Rate")]
        public decimal HourlyRate { get; set; }

        [Required]
        [Display(Name = "Temporary Password")]
        public string TemporaryPassword { get; set; }

        [Display(Name = "Active Lecturer")]
        public bool IsActive { get; set; } = true;
    }
}
