namespace IdentityService.Entities
{
    public class Images
    {
        public int ImageId { get; set; }
        public string UserId { get; set; }  // ApplicationUser ID'si
        public string FilePath { get; set; } // Orijinal resmin yolu
        public string ResultFilePath { get; set; } // İşlenmiş resmin yolu
        public DateTime UploadDate { get; set; } // Yüklenme tarihi

        public ApplicationUser User { get; set; }  // Kullanıcı ile ilişkilendiriyoruz
    }
}
