using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManager.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationDBContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetRequests_Assets_AssetId",
                table: "AssetRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetRequests_Assets_AssetId",
                table: "AssetRequests",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetRequests_Assets_AssetId",
                table: "AssetRequests");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetRequests_Assets_AssetId",
                table: "AssetRequests",
                column: "AssetId",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
