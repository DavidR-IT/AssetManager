using System.ComponentModel.DataAnnotations;

namespace AssetManager.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "Issuer"; // Admin, Approval, Issuer, Viewer

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<Asset>? Assets { get; set; }
        public ICollection<AssetRequest>? AssetRequests { get; set; }
    }
}
