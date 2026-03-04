using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetManager.Models
{
    /// <summary>
    /// Junction table for many-to-many relationship between AssetRequest and Asset
    /// Allows a single request to contain multiple assets
    /// </summary>
    public class AssetRequestItem
    {
        public int Id { get; set; }

        [Required]
        public int AssetRequestId { get; set; }

        [Required]
        public int AssetId { get; set; }

        // Navigation properties
        [ForeignKey("AssetRequestId")]
        public virtual AssetRequest? AssetRequest { get; set; }

        [ForeignKey("AssetId")]
        public virtual Asset? Asset { get; set; }
    }
}
