using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_WorkflowTransactionType_Catalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowGroupId",
                table: "EmployeeOrgAssignments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkflowTransactionTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsWorkflowEnabled = table.Column<bool>(type: "bit", nullable: false),
                    IsEssEnabled = table.Column<bool>(type: "bit", nullable: false),
                    DefaultRequestMasterId = table.Column<int>(type: "int", nullable: true),
                    EssRoute = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EssPermissionName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SubTypeSource = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WorkflowTransactionTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTransactionTypes_RequestMasters_DefaultRequestMasterId",
                        column: x => x.DefaultRequestMasterId,
                        principalTable: "RequestMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOrgAssignments_WorkflowGroupId",
                table: "EmployeeOrgAssignments",
                column: "WorkflowGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransactionTypes_DefaultRequestMasterId",
                table: "WorkflowTransactionTypes",
                column: "DefaultRequestMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTransactionTypes_TenantId_Code",
                table: "WorkflowTransactionTypes",
                columns: new[] { "TenantId", "Code" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeOrgAssignments_WorkflowGroups_WorkflowGroupId",
                table: "EmployeeOrgAssignments",
                column: "WorkflowGroupId",
                principalTable: "WorkflowGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeOrgAssignments_WorkflowGroups_WorkflowGroupId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropTable(
                name: "WorkflowTransactionTypes");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeOrgAssignments_WorkflowGroupId",
                table: "EmployeeOrgAssignments");

            migrationBuilder.DropColumn(
                name: "WorkflowGroupId",
                table: "EmployeeOrgAssignments");
        }
    }
}
