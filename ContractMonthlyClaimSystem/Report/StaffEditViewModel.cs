using System.ComponentModel.DataAnnotations;

public class StaffEditViewModel
{
    public string Id { get; set; }

    [Required]
    [Display(Name = "Full Name")]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; }

    [Display(Name = "Program Coordinator")]
    public bool IsProgramCoordinator { get; set; }

    [Display(Name = "Academic Manager")]
    public bool IsAcademicManager { get; set; }
}