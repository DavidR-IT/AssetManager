using System.ComponentModel.DataAnnotations;

namespace AssetManager.Models
{
    public enum AssetStatus
    {
        Ready = 0,
        Pending = 1,
        Assigned = 2,
        Maintenance = 3,
        Retired = 4
    }

    public class Asset
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Asset Tag is required")]
        [Display(Name = "Asset Tag")]
        [StringLength(50, ErrorMessage = "Asset Tag cannot exceed 50 characters")]
        public string AssetTag { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        [StringLength(100, ErrorMessage = "Category cannot exceed 100 characters")]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Brand is required")]
        [Display(Name = "Brand")]
        [StringLength(100, ErrorMessage = "Brand cannot exceed 100 characters")]
        public string Brand { get; set; } = string.Empty;

        [Required(ErrorMessage = "Model is required")]
        [Display(Name = "Model")]
        [StringLength(100, ErrorMessage = "Model cannot exceed 100 characters")]
        public string Model { get; set; } = string.Empty;

        [Display(Name = "Serial Number")]
        [StringLength(100, ErrorMessage = "Serial Number cannot exceed 100 characters")]
        public string? SerialNumber { get; set; }

        [Display(Name = "Purchase Date")]
        [DataType(DataType.Date)]
        public DateTime? PurchaseDate { get; set; }

        [Display(Name = "Purchase Price")]
        [DataType(DataType.Currency)]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be a positive number")]
        public decimal? PurchasePrice { get; set; }

        [Display(Name = "Status")]
        public AssetStatus Status { get; set; } = AssetStatus.Ready;

        [Display(Name = "Assigned To")]
        [StringLength(256, ErrorMessage = "Name cannot exceed 256 characters")]
        public string? AssignedTo { get; set; }

        [Display(Name = "Assigned Date")]
        [DataType(DataType.Date)]
        public DateTime? AssignedDate { get; set; }

        [Display(Name = "Location")]
        [StringLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string? Location { get; set; }

        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        [StringLength(4000, ErrorMessage = "Notes cannot exceed 4000 characters")]
        public string? Notes { get; set; }

        // Concurrency token — prevents race conditions on simultaneous edits
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;

        // Link to the user who currently has this asset assigned
        public int? UserId { get; set; }
        public User? User { get; set; }

        // Helper properties for display
        [Display(Name = "Is Available")]
        public bool IsAvailable => Status == AssetStatus.Ready;

        [Display(Name = "Full Description")]
        public string FullDescription => $"{AssetTag} - {Brand} {Model} ({Category})";

        // QR Code URL - generates URL that points to asset details
        public string GetQRCodeUrl(string baseUrl)
        {
            return $"{baseUrl}/Assets/ViewByQR/{Id}";
        }
    }
}