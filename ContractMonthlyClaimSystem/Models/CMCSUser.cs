/*
 namespace ContractMonthlyClaimSystem.Models
{
    public class CMCSUser
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Role { get; set; } // Lecturer, Co-ordinator, Manager
    }
}

*/

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ContractMonthlyClaimSystem.Models
{
    public class CMCSUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Role { get; set; } = "Lecturer"; // Lecturer, Coordinator, Manager, HR

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? InitialPassword { get; set; } // For HR to see initial password
    }
}
