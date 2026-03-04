using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManager.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrphanedRequestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Department", table: "AssetRequests");
            migrationBuilder.DropColumn(name: "ManagerApproved", table: "AssetRequests");
            migrationBuilder.DropColumn(name: "ManagerName", table: "AssetRequests");
            migrationBuilder.DropColumn(name: "Priority", table: "AssetRequests");
            migrationBuilder.DropColumn(name: "RequiredByDate", table: "AssetRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "AssetRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ManagerApproved",
                table: "AssetRequests",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ManagerName",
                table: "AssetRequests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Priority",
                table: "AssetRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Normal");

            migrationBuilder.AddColumn<DateTime>(
                name: "RequiredByDate",
                table: "AssetRequests",
                type: "datetime2",
                nullable: true);
        }
    }
}
