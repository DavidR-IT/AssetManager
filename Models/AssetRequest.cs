using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Models
{
    public class AssetRequest
    {
        public int Id { get; set; }

        // DEPRECATED: Kept for backward compatibility with existing data
        // New requests will use RequestedAssets collection instead
        [Display(Name = "Asset")]
        public int? AssetId { get; set; }

        [Required(ErrorMessage = "Requested by is required")]
        [Display(Name = "Requested By")]
        [StringLength(256, ErrorMessage = "Name cannot exceed 256 characters")]
        public string RequestedBy { get; set; } = string.Empty;

        [Required(ErrorMessage = "Request type is required")]
        [Display(Name = "Request Type")]
        [StringLength(50, ErrorMessage = "Request type cannot exceed 50 characters")]
        public string RequestType { get; set; } = "Request"; // Default value

        [Required(ErrorMessage = "Status is required")]
        [Display(Name = "Status")]
        [StringLength(50, ErrorMessage = "Status cannot exceed 50 characters")]
        public string Status { get; set; } = "Pending"; // Default value

        [Display(Name = "Approved By")]
        [StringLength(256, ErrorMessage = "Name cannot exceed 256 characters")]
        public string? ApprovedBy { get; set; }

        [Display(Name = "Request Date")]
        [DataType(DataType.DateTime)]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Approval Date")]
        [DataType(DataType.DateTime)]
        public DateTime? ApprovalDate { get; set; }

        [Display(Name = "Justification")]
        [DataType(DataType.MultilineText)]
        [StringLength(500, ErrorMessage = "Justification cannot exceed 500 characters")]
        public string? Justification { get; set; }


        [Display(Name = "Return Date")]
        [DataType(DataType.Date)]
        public DateTime? ExpectedReturnDate { get; set; }


        [Display(Name = "Notes")]
        [DataType(DataType.MultilineText)]
        [StringLength(4000, ErrorMessage = "Notes cannot exceed 4000 characters")]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }

        // NEW: Collection of assets in this request (many-to-many via AssetRequestItem)
        public virtual ICollection<AssetRequestItem> RequestedAssets { get; set; } = new List<AssetRequestItem>();

        // Nullable so that deleting a user preserves their request history for auditing
        public int? UserId { get; set; }
        public User? User { get; set; }

        // Helper properties for display
        [Display(Name = "Is Pending")]
        public bool IsPending => Status == RequestStatuses.Pending;
        public bool IsApproved => Status == RequestStatuses.Approved;
        public bool IsRejected => Status == RequestStatuses.Rejected;

        [Display(Name = "Days Since Request")]
        public int DaysSinceRequest
        {
            get
            {
                if (RequestDate != default)
                {
                    return (DateTime.UtcNow - RequestDate).Days;
                }
                return 0;
            }
        }
    }
}
