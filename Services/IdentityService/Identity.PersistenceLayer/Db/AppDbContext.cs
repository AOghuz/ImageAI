using Identity.EntityLayer.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.PersistenceLayer.Db
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, Guid>
    {
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // AppUser
            b.Entity<AppUser>(e =>
            {
                e.Property(x => x.DisplayName).HasMaxLength(128);
                e.Property(x => x.Email).HasMaxLength(256);
                e.Property(x => x.UserName).HasMaxLength(256);
            });

            // RefreshToken
            b.Entity<RefreshToken>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Token).IsRequired().HasMaxLength(512);
                e.HasIndex(x => x.Token).IsUnique();
                e.HasIndex(x => x.UserId);
            });
        }
    }
}
