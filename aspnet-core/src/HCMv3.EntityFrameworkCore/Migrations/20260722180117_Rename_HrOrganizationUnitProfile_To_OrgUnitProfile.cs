using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Rename_HrOrganizationUnitProfile_To_OrgUnitProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "HrOrganizationUnitProfiles",
                newName: "OrgUnitProfiles");

            migrationBuilder.RenameIndex(
                name: "IX_HrOrganizationUnitProfiles_OrganizationUnitId",
                table: "OrgUnitProfiles",
                newName: "IX_OrgUnitProfiles_OrganizationUnitId");

            migrationBuilder.RenameIndex(
                name: "IX_HrOrganizationUnitProfiles_OrganizationUnitTypeId",
                table: "OrgUnitProfiles",
                newName: "IX_OrgUnitProfiles_OrganizationUnitTypeId");

            migrationBuilder.Sql(
                "EXEC sp_rename 'PK_HrOrganizationUnitProfiles', 'PK_OrgUnitProfiles';");

            migrationBuilder.Sql(
                "EXEC sp_rename 'FK_HrOrganizationUnitProfiles_AbpOrganizationUnits_OrganizationUnitId', 'FK_OrgUnitProfiles_AbpOrganizationUnits_OrganizationUnitId';");

            migrationBuilder.Sql(
                "EXEC sp_rename 'FK_HrOrganizationUnitProfiles_OrganizationUnitTypes_OrganizationUnitTypeId', 'FK_OrgUnitProfiles_OrganizationUnitTypes_OrganizationUnitTypeId';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "EXEC sp_rename 'FK_OrgUnitProfiles_OrganizationUnitTypes_OrganizationUnitTypeId', 'FK_HrOrganizationUnitProfiles_OrganizationUnitTypes_OrganizationUnitTypeId';");

            migrationBuilder.Sql(
                "EXEC sp_rename 'FK_OrgUnitProfiles_AbpOrganizationUnits_OrganizationUnitId', 'FK_HrOrganizationUnitProfiles_AbpOrganizationUnits_OrganizationUnitId';");

            migrationBuilder.Sql(
                "EXEC sp_rename 'PK_OrgUnitProfiles', 'PK_HrOrganizationUnitProfiles';");

            migrationBuilder.RenameIndex(
                name: "IX_OrgUnitProfiles_OrganizationUnitTypeId",
                table: "HrOrganizationUnitProfiles",
                newName: "IX_HrOrganizationUnitProfiles_OrganizationUnitTypeId");

            migrationBuilder.RenameIndex(
                name: "IX_OrgUnitProfiles_OrganizationUnitId",
                table: "HrOrganizationUnitProfiles",
                newName: "IX_HrOrganizationUnitProfiles_OrganizationUnitId");

            migrationBuilder.RenameTable(
                name: "OrgUnitProfiles",
                newName: "HrOrganizationUnitProfiles");
        }
    }
}
