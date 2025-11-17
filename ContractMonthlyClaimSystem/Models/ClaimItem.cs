// Models/ClaimItem.cs
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Models
{
    public class ClaimItem
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public Claim? Claim { get; set; }

        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Precision(9, 2)]
        [Range(typeof(decimal), "0.25", "500")]
        public decimal Hours { get; set; }

        [Precision(18, 2)]
        [Range(typeof(decimal), "0", "10000")]
        public decimal Rate { get; set; }

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public decimal CalculateTotalAmount()
        {
            return Hours * Rate;
        }
    }
}