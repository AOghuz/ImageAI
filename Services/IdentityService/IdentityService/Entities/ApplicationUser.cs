namespace IdentityService.Entities
{
    using Microsoft.AspNetCore.Identity;

    // ApplicationUser sınıfı IdentityUser'dan türemekte
    public class ApplicationUser : IdentityUser
    {
        public decimal Balance { get; set; }  // Kullanıcıya bakiye eklemek için
    }
}
