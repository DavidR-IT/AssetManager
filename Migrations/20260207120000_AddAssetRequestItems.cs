using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AssetManager.Migrations
{
    /// <inheritdoc />
    public partial class AddAssetRequestItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AssetId",
                table: "AssetRequests",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "AssetRequestItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetRequestId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetRequestItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetRequestItems_AssetRequests_AssetRequestId",
                        column: x => x.AssetRequestId,
                        principalTable: "AssetRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetRequestItems_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetRequestItems_AssetId",
                table: "AssetRequestItems",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetRequestItems_AssetRequestId_AssetId",
                table: "AssetRequestItems",
                columns: new[] { "AssetRequestId", "AssetId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetRequestItems");

            migrationBuilder.AlterColumn<int>(
                name: "AssetId",
                table: "AssetRequests",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
