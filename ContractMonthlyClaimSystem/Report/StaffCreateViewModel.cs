using System.ComponentModel.DataAnnotations;

public class StaffCreateViewModel
{
    [Required]
    [Display(Name = "Full Name")]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    public string Email { get; set; }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Temporary Password")]
    public string TemporaryPassword { get; set; }
}