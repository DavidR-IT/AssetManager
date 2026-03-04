using AssetManager.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetManager.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Asset> Assets { get; set; }
        public DbSet<AssetRequest> AssetRequests { get; set; }
        public DbSet<AssetRequestItem> AssetRequestItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision for PurchasePrice
            modelBuilder.Entity<Asset>()
                .Property(a => a.PurchasePrice)
                .HasPrecision(18, 2);

            // Configure User email to be unique
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configure relationships
            modelBuilder.Entity<Asset>()
                .HasOne(a => a.User)
                .WithMany(u => u.Assets)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<AssetRequest>()
                .HasOne(ar => ar.User)
                .WithMany(u => u.AssetRequests)
                .HasForeignKey(ar => ar.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure many-to-many relationship between AssetRequest and Asset via AssetRequestItem
            modelBuilder.Entity<AssetRequestItem>()
                .HasOne(ari => ari.AssetRequest)
                .WithMany(ar => ar.RequestedAssets)
                .HasForeignKey(ari => ari.AssetRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AssetRequestItem>()
                .HasOne(ari => ari.Asset)
                .WithMany()
                .HasForeignKey(ari => ari.AssetId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete on assets

            // Ensure unique asset per request (no duplicate assets in same request)
            modelBuilder.Entity<AssetRequestItem>()
                .HasIndex(ari => new { ari.AssetRequestId, ari.AssetId })
                .IsUnique();
        }
    }
}
