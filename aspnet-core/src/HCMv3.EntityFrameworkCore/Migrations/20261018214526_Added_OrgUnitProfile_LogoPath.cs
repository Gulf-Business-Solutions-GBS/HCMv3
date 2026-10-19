using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_OrgUnitProfile_LogoPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LogoPath",
                table: "OrgUnitProfiles",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoPath",
                table: "OrgUnitProfiles");
        }
    }
}
