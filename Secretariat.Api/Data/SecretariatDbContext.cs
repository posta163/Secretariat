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
    }


}