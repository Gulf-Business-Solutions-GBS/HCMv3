using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Navigation_TenantId_Nullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserScreenUsages_TenantId_UserId_ScreenKey",
                table: "UserScreenUsages");

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                table: "UserScreenUsages",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                table: "SharedQuickLinks",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                table: "Screens",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_UserScreenUsages_TenantId_UserId_ScreenKey",
                table: "UserScreenUsages",
                columns: new[] { "TenantId", "UserId", "ScreenKey" },
                unique: true,
                filter: "[TenantId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserScreenUsages_TenantId_UserId_ScreenKey",
                table: "UserScreenUsages");

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                table: "UserScreenUsages",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                table: "SharedQuickLinks",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TenantId",
                table: "Screens",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserScreenUsages_TenantId_UserId_ScreenKey",
                table: "UserScreenUsages",
                columns: new[] { "TenantId", "UserId", "ScreenKey" },
                unique: true);
        }
    }
}
