namespace IdentityService.Entities
{
    public class Balance
    {
        public int BalanceId { get; set; }
        public string UserId { get; set; }  // ApplicationUser ID'si
        public decimal Amount { get; set; } // Kullanıcının bakiyesi

        public ApplicationUser User { get; set; }  // Kullanıcı ile ilişkilendiriyoruz
    }
}
