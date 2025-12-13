using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IdentityService.Entities;
using static System.Net.Mime.MediaTypeNames;

namespace IdentityService
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Balance> Balances { get; set; }
        public DbSet<Images> Images { get; set; }
    }
}
