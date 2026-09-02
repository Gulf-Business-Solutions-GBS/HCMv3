using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_ScreenCatalogIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserScreenUsages_TenantId_UserId_ScreenKey",
                table: "UserScreenUsages",
                columns: new[] { "TenantId", "UserId", "ScreenKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SharedQuickLinks_TenantId_DisplayOrder",
                table: "SharedQuickLinks",
                columns: new[] { "TenantId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Screens_TenantId_Code",
                table: "Screens",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Screens_TenantId_ScreenKey",
                table: "Screens",
                columns: new[] { "TenantId", "ScreenKey" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserScreenUsages_TenantId_UserId_ScreenKey",
                table: "UserScreenUsages");

            migrationBuilder.DropIndex(
                name: "IX_SharedQuickLinks_TenantId_DisplayOrder",
                table: "SharedQuickLinks");

            migrationBuilder.DropIndex(
                name: "IX_Screens_TenantId_Code",
                table: "Screens");

            migrationBuilder.DropIndex(
                name: "IX_Screens_TenantId_ScreenKey",
                table: "Screens");
        }
    }
}
