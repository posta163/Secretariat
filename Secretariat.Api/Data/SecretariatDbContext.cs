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
        }

        }

    }