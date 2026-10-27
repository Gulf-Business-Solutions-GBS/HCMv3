using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Restored_Legacy_Fields_Gap_Batch5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SponsorId",
                table: "WorkLocations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PositionId",
                table: "Vacancies",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "PerDiemPayPercentage",
                table: "TravelDetails",
                type: "float",
                nullable: false,
                // Legacy defaulted a leg to 100%; existing legs keep paying in full.
                defaultValue: 100.0);

            migrationBuilder.AddColumn<int>(
                name: "InstitutionId",
                table: "TrainingCourses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "TrainingCourses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NotificationConfigId",
                table: "PersonalActionReasons",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PACorrectionNotificationId",
                table: "PayrollAreas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PAExitNotificationId",
                table: "PayrollAreas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PAReleaseNotificationId",
                table: "PayrollAreas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmploymentType",
                table: "NumberSeries",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompetencyId",
                table: "KpiInfos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CountryId",
                table: "Institutions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmployeeCertificationId",
                table: "EmployeeTrainings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocPlaceId",
                table: "EmployeeDocuments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CertificationType",
                table: "EmployeeCertifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Duties",
                type: "bit",
                nullable: false,
                // Every existing duty is in use; only a duty switched off later stops being offered.
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowToAll",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WageTypeId",
                table: "DisciplinaryTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeInWorkflow",
                table: "AssetTypes",
                type: "bit",
                nullable: false,
                // Existing types stay requestable in self-service, which now filters on this flag. New types
                // start unticked, as in legacy.
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "WageTypeId",
                table: "AssetTypes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeductionWageTypeId",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EarningWageTypeId",
                table: "Assets",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "JobTitleDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    JobTitleId = table.Column<int>(type: "int", nullable: false),
                    WorkflowRoleId = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobTitleDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobTitleDetails_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JobTitleDetails_WorkflowRoles_WorkflowRoleId",
                        column: x => x.WorkflowRoleId,
                        principalTable: "WorkflowRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkLocations_SponsorId",
                table: "WorkLocations",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_PositionId",
                table: "Vacancies",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_RecruiterId",
                table: "Vacancies",
                column: "RecruiterId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingCourses_InstitutionId",
                table: "TrainingCourses",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalActionReasons_NotificationConfigId",
                table: "PersonalActionReasons",
                column: "NotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAreas_PACorrectionNotificationId",
                table: "PayrollAreas",
                column: "PACorrectionNotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAreas_PAExitNotificationId",
                table: "PayrollAreas",
                column: "PAExitNotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAreas_PAReleaseNotificationId",
                table: "PayrollAreas",
                column: "PAReleaseNotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiInfos_CompetencyId",
                table: "KpiInfos",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Institutions_CountryId",
                table: "Institutions",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTrainings_EmployeeCertificationId",
                table: "EmployeeTrainings",
                column: "EmployeeCertificationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_DocPlaceId",
                table: "EmployeeDocuments",
                column: "DocPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_DisciplinaryTypes_WageTypeId",
                table: "DisciplinaryTypes",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetTypes_WageTypeId",
                table: "AssetTypes",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_DeductionWageTypeId",
                table: "Assets",
                column: "DeductionWageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_EarningWageTypeId",
                table: "Assets",
                column: "EarningWageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitleDetails_JobTitleId",
                table: "JobTitleDetails",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitleDetails_WorkflowRoleId",
                table: "JobTitleDetails",
                column: "WorkflowRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_WageTypes_DeductionWageTypeId",
                table: "Assets",
                column: "DeductionWageTypeId",
                principalTable: "WageTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Assets_WageTypes_EarningWageTypeId",
                table: "Assets",
                column: "EarningWageTypeId",
                principalTable: "WageTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AssetTypes_WageTypes_WageTypeId",
                table: "AssetTypes",
                column: "WageTypeId",
                principalTable: "WageTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DisciplinaryTypes_WageTypes_WageTypeId",
                table: "DisciplinaryTypes",
                column: "WageTypeId",
                principalTable: "WageTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeDocuments_DocumentPlaces_DocPlaceId",
                table: "EmployeeDocuments",
                column: "DocPlaceId",
                principalTable: "DocumentPlaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeTrainings_EmployeeCertifications_EmployeeCertificationId",
                table: "EmployeeTrainings",
                column: "EmployeeCertificationId",
                principalTable: "EmployeeCertifications",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Institutions_Countries_CountryId",
                table: "Institutions",
                column: "CountryId",
                principalTable: "Countries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_KpiInfos_Competencies_CompetencyId",
                table: "KpiInfos",
                column: "CompetencyId",
                principalTable: "Competencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAreas_NotificationConfigs_PACorrectionNotificationId",
                table: "PayrollAreas",
                column: "PACorrectionNotificationId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAreas_NotificationConfigs_PAExitNotificationId",
                table: "PayrollAreas",
                column: "PAExitNotificationId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollAreas_NotificationConfigs_PAReleaseNotificationId",
                table: "PayrollAreas",
                column: "PAReleaseNotificationId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalActionReasons_NotificationConfigs_NotificationConfigId",
                table: "PersonalActionReasons",
                column: "NotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingCourses_Institutions_InstitutionId",
                table: "TrainingCourses",
                column: "InstitutionId",
                principalTable: "Institutions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // RecruiterId had no foreign key until now; a value pointing at no employee would stop the key.
            migrationBuilder.Sql("UPDATE Vacancies SET RecruiterId = NULL WHERE RecruiterId IS NOT NULL AND RecruiterId NOT IN (SELECT Id FROM Employees)");

            migrationBuilder.AddForeignKey(
                name: "FK_Vacancies_Employees_RecruiterId",
                table: "Vacancies",
                column: "RecruiterId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Vacancies_Positions_PositionId",
                table: "Vacancies",
                column: "PositionId",
                principalTable: "Positions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WorkLocations_Sponsors_SponsorId",
                table: "WorkLocations",
                column: "SponsorId",
                principalTable: "Sponsors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assets_WageTypes_DeductionWageTypeId",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_Assets_WageTypes_EarningWageTypeId",
                table: "Assets");

            migrationBuilder.DropForeignKey(
                name: "FK_AssetTypes_WageTypes_WageTypeId",
                table: "AssetTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_DisciplinaryTypes_WageTypes_WageTypeId",
                table: "DisciplinaryTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeDocuments_DocumentPlaces_DocPlaceId",
                table: "EmployeeDocuments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeTrainings_EmployeeCertifications_EmployeeCertificationId",
                table: "EmployeeTrainings");

            migrationBuilder.DropForeignKey(
                name: "FK_Institutions_Countries_CountryId",
                table: "Institutions");

            migrationBuilder.DropForeignKey(
                name: "FK_KpiInfos_Competencies_CompetencyId",
                table: "KpiInfos");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAreas_NotificationConfigs_PACorrectionNotificationId",
                table: "PayrollAreas");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAreas_NotificationConfigs_PAExitNotificationId",
                table: "PayrollAreas");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollAreas_NotificationConfigs_PAReleaseNotificationId",
                table: "PayrollAreas");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalActionReasons_NotificationConfigs_NotificationConfigId",
                table: "PersonalActionReasons");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingCourses_Institutions_InstitutionId",
                table: "TrainingCourses");

            migrationBuilder.DropForeignKey(
                name: "FK_Vacancies_Employees_RecruiterId",
                table: "Vacancies");

            migrationBuilder.DropForeignKey(
                name: "FK_Vacancies_Positions_PositionId",
                table: "Vacancies");

            migrationBuilder.DropForeignKey(
                name: "FK_WorkLocations_Sponsors_SponsorId",
                table: "WorkLocations");

            migrationBuilder.DropTable(
                name: "JobTitleDetails");

            migrationBuilder.DropIndex(
                name: "IX_WorkLocations_SponsorId",
                table: "WorkLocations");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_PositionId",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_Vacancies_RecruiterId",
                table: "Vacancies");

            migrationBuilder.DropIndex(
                name: "IX_TrainingCourses_InstitutionId",
                table: "TrainingCourses");

            migrationBuilder.DropIndex(
                name: "IX_PersonalActionReasons_NotificationConfigId",
                table: "PersonalActionReasons");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAreas_PACorrectionNotificationId",
                table: "PayrollAreas");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAreas_PAExitNotificationId",
                table: "PayrollAreas");

            migrationBuilder.DropIndex(
                name: "IX_PayrollAreas_PAReleaseNotificationId",
                table: "PayrollAreas");

            migrationBuilder.DropIndex(
                name: "IX_KpiInfos_CompetencyId",
                table: "KpiInfos");

            migrationBuilder.DropIndex(
                name: "IX_Institutions_CountryId",
                table: "Institutions");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeTrainings_EmployeeCertificationId",
                table: "EmployeeTrainings");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeDocuments_DocPlaceId",
                table: "EmployeeDocuments");

            migrationBuilder.DropIndex(
                name: "IX_DisciplinaryTypes_WageTypeId",
                table: "DisciplinaryTypes");

            migrationBuilder.DropIndex(
                name: "IX_AssetTypes_WageTypeId",
                table: "AssetTypes");

            migrationBuilder.DropIndex(
                name: "IX_Assets_DeductionWageTypeId",
                table: "Assets");

            migrationBuilder.DropIndex(
                name: "IX_Assets_EarningWageTypeId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "SponsorId",
                table: "WorkLocations");

            migrationBuilder.DropColumn(
                name: "PositionId",
                table: "Vacancies");

            migrationBuilder.DropColumn(
                name: "PerDiemPayPercentage",
                table: "TravelDetails");

            migrationBuilder.DropColumn(
                name: "InstitutionId",
                table: "TrainingCourses");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "TrainingCourses");

            migrationBuilder.DropColumn(
                name: "NotificationConfigId",
                table: "PersonalActionReasons");

            migrationBuilder.DropColumn(
                name: "PACorrectionNotificationId",
                table: "PayrollAreas");

            migrationBuilder.DropColumn(
                name: "PAExitNotificationId",
                table: "PayrollAreas");

            migrationBuilder.DropColumn(
                name: "PAReleaseNotificationId",
                table: "PayrollAreas");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "NumberSeries");

            migrationBuilder.DropColumn(
                name: "CompetencyId",
                table: "KpiInfos");

            migrationBuilder.DropColumn(
                name: "CountryId",
                table: "Institutions");

            migrationBuilder.DropColumn(
                name: "EmployeeCertificationId",
                table: "EmployeeTrainings");

            migrationBuilder.DropColumn(
                name: "DocPlaceId",
                table: "EmployeeDocuments");

            migrationBuilder.DropColumn(
                name: "CertificationType",
                table: "EmployeeCertifications");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Duties");

            migrationBuilder.DropColumn(
                name: "ShowToAll",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "WageTypeId",
                table: "DisciplinaryTypes");

            migrationBuilder.DropColumn(
                name: "IncludeInWorkflow",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "WageTypeId",
                table: "AssetTypes");

            migrationBuilder.DropColumn(
                name: "DeductionWageTypeId",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "EarningWageTypeId",
                table: "Assets");
        }
    }
}
