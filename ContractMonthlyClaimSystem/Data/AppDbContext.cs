using ContractMonthlyClaimSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Data
{
    public class AppDbContext : IdentityDbContext<CMCSUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Claim> Claims => Set<Claim>();
        public DbSet<ClaimItem> ClaimItems => Set<ClaimItem>();
        public DbSet<Document> Documents => Set<Document>();
        public DbSet<Lecturer> Lecturers => Set<Lecturer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Important for Identity

            

            // Optional: Add any custom configurations for SQL Server
            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.HasIndex(l => l.Email).IsUnique();
            });

            modelBuilder.Entity<Claim>(entity =>
            {
                entity.HasMany(c => c.ClaimItems)
                      .WithOne(ci => ci.Claim)
                      .HasForeignKey(ci => ci.ClaimId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.Documents)
                      .WithOne(d => d.Claim)
                      .HasForeignKey(d => d.ClaimId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}