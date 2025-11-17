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

            // Map to the exact singular table names
            modelBuilder.Entity<Claim>(entity =>
            {
                entity.ToTable("CLAIM");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.LecturerId)
                    .HasColumnName("LECTURERID");

                entity.Property(e => e.LecturerName)
                    .HasColumnName("LECTURERNAME")
                    .HasMaxLength(200);

                entity.Property(e => e.Month)
                    .HasColumnName("MONTH")
                    .HasColumnType("DATE");

                entity.Property(e => e.TotalHours)
                    .HasColumnName("TOTALHOURS")
                    .HasPrecision(9, 2);

                entity.Property(e => e.TotalAmount)
                    .HasColumnName("TOTALAMOUNT")
                    .HasPrecision(18, 2);

                entity.Property(e => e.Status)
                    .HasColumnName("STATUS")
                    .HasConversion<int>();

                entity.Property(e => e.SubmittedDate)
                    .HasColumnName("SUBMITTEDDATE")
                    .HasColumnType("DATE");

                entity.Property(e => e.ApprovedDate)
                    .HasColumnName("APPROVEDDATE")
                    .HasColumnType("DATE");

                // Relationships
                entity.HasMany(c => c.ClaimItems)
                    .WithOne(ci => ci.Claim)
                    .HasForeignKey(ci => ci.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.Documents)
                    .WithOne(d => d.Claim)
                    .HasForeignKey(d => d.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Map ClaimItem entity
            modelBuilder.Entity<ClaimItem>(entity =>
            {
                entity.ToTable("CLAIMITEM");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ClaimId)
                    .HasColumnName("CLAIMID");

                entity.Property(e => e.Date)
                    .HasColumnName("CLAIMDATE")
                    .HasColumnType("DATE");

                entity.Property(e => e.Hours)
                    .HasColumnName("HOURS")
                    .HasPrecision(9, 2);

                entity.Property(e => e.Rate)
                    .HasColumnName("RATE")
                    .HasPrecision(18, 2);

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(500);
            });

            // Map Document entity
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("DOCUMENT");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ClaimId)
                    .HasColumnName("CLAIMID");

                entity.Property(e => e.FileName)
                    .HasColumnName("FILENAME")
                    .HasMaxLength(255);

                entity.Property(e => e.OriginalFileName)
                    .HasColumnName("ORIGINALFILENAME")
                    .HasMaxLength(255);

                entity.Property(e => e.FileContent)
                    .HasColumnName("FILECONTENT")
                    .HasColumnType("BLOB");

                entity.Property(e => e.UploadedDate)
                    .HasColumnName("UPLOADEDDATE")
                    .HasColumnType("DATE");

                entity.Property(e => e.ContentType)
                    .HasColumnName("CONTENTTYPE")
                    .HasMaxLength(128);

                entity.Property(e => e.SizeBytes)
                    .HasColumnName("SIZEBYTES");
            });

            // Map Lecturer entity
            modelBuilder.Entity<Lecturer>(entity =>
            {
                entity.ToTable("LECTURER");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.Name)
                    .HasColumnName("NAME")
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.Email)
                    .HasColumnName("EMAIL")
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.HourlyRate)
                    .HasColumnName("HOURLYRATE")
                    .HasPrecision(18, 2);

                entity.Property(e => e.IsActive)
                    .HasColumnName("ISACTIVE")
                    .HasDefaultValue(true);

                entity.Property(e => e.CreatedDate)
                    .HasColumnName("CREATEDDATE")
                    .HasColumnType("DATE");
            });
        }
    }
}

/*
using ContractMonthlyClaimSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Claim> Claims => Set<Claim>();
        public DbSet<ClaimItem> ClaimItems => Set<ClaimItem>();
        public DbSet<Document> Documents => Set<Document>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map to the exact singular table names
            modelBuilder.Entity<Claim>(entity =>
            {
                entity.ToTable("CLAIM");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.LecturerId)
                    .HasColumnName("LECTURERID");

                entity.Property(e => e.LecturerName)
                    .HasColumnName("LECTURERNAME")
                    .HasMaxLength(200);

                entity.Property(e => e.Month)
                    .HasColumnName("MONTH")
                    .HasColumnType("DATE"); // Explicit DATE type for Oracle

                entity.Property(e => e.TotalHours)
                    .HasColumnName("TOTALHOURS")
                    .HasPrecision(9, 2);

                entity.Property(e => e.TotalAmount)
                    .HasColumnName("TOTALAMOUNT")
                    .HasPrecision(18, 2);

                entity.Property(e => e.Status)
                    .HasColumnName("STATUS")
                    .HasConversion<int>();

                entity.Property(e => e.SubmittedDate)
                    .HasColumnName("SUBMITTEDDATE")
                    .HasColumnType("DATE"); // Explicit DATE type for Oracle

                entity.Property(e => e.ApprovedDate)
                    .HasColumnName("APPROVEDDATE")
                    .HasColumnType("DATE"); // Explicit DATE type for Oracle

                // Relationships
                entity.HasMany(c => c.ClaimItems)
                    .WithOne(ci => ci.Claim)
                    .HasForeignKey(ci => ci.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(c => c.Documents)
                    .WithOne(d => d.Claim)
                    .HasForeignKey(d => d.ClaimId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Map ClaimItem entity
            modelBuilder.Entity<ClaimItem>(entity =>
            {
                entity.ToTable("CLAIMITEM");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ClaimId)
                    .HasColumnName("CLAIMID");

                entity.Property(e => e.Date)
                    .HasColumnName("CLAIMDATE")
                    .HasColumnType("DATE"); // Explicit DATE type for Oracle

                entity.Property(e => e.Hours)
                    .HasColumnName("HOURS")
                    .HasPrecision(9, 2);

                entity.Property(e => e.Rate)
                    .HasColumnName("RATE")
                    .HasPrecision(18, 2);

                entity.Property(e => e.Description)
                    .HasColumnName("DESCRIPTION")
                    .HasMaxLength(500);
            });

            // Map Document entity
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("DOCUMENT");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .ValueGeneratedOnAdd();

                entity.Property(e => e.ClaimId)
                    .HasColumnName("CLAIMID");

                entity.Property(e => e.FileName)
                    .HasColumnName("FILENAME")
                    .HasMaxLength(255);

                entity.Property(e => e.OriginalFileName)
                    .HasColumnName("ORIGINALFILENAME")
                    .HasMaxLength(255);

                // Explicit BLOB configuration for Oracle
                entity.Property(e => e.FileContent)
                    .HasColumnName("FILECONTENT")
                    .HasColumnType("BLOB");

                entity.Property(e => e.UploadedDate)
                    .HasColumnName("UPLOADEDDATE")
                    .HasColumnType("DATE"); // Explicit DATE type for Oracle

                entity.Property(e => e.ContentType)
                    .HasColumnName("CONTENTTYPE")
                    .HasMaxLength(128);

                entity.Property(e => e.SizeBytes)
                    .HasColumnName("SIZEBYTES");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
*/