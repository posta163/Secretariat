using Microsoft.EntityFrameworkCore;
using Secretariat.Api.Models;

namespace Secretariat.Api.Data
{
    public class SecretariatDbContext : DbContext
    {
        public SecretariatDbContext(DbContextOptions<SecretariatDbContext> options)
            : base(options)
        {
        }

        public DbSet<Correspondence> Correspondences { get; set; }

        public DbSet<AppUser> AppUsers { get; set; }

        public DbSet<CorrespondenceAttachment> CorrespondenceAttachments { get; set; }

        public DbSet<InternalCorrespondence> InternalCorrespondences { get; set; }

        public DbSet<InternalCorrespondenceApprover> InternalCorrespondenceApprovers { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<InternalCorrespondenceApprover>()
                .HasOne(a => a.InternalCorrespondence)
                .WithMany(c => c.Approvers)
                .HasForeignKey(a => a.InternalCorrespondenceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InternalCorrespondenceApprover>()
                .HasOne(a => a.ApproverUser)
                .WithMany()
                .HasForeignKey(a => a.ApproverUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<InternalCorrespondenceApprover>()
                .HasIndex(a => new
                {
                    a.InternalCorrespondenceId,
                    a.ApproverUserId
                })
                .IsUnique();


            // Relacja: umowa -> autor wniosku
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.CreatedByUser)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relacja: umowa -> osoba odpowiedzialna
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.ResponsibleUser)
                .WithMany()
                .HasForeignKey(c => c.ResponsibleUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Numer umowy musi być unikalny
            modelBuilder.Entity<Contract>()
                .HasIndex(c => c.Number)
                .IsUnique();
        }

        public DbSet<Contract> Contracts { get; set; }

    }

    }