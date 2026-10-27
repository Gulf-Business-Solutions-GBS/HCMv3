using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_Round_B_Appraisal_Rejoin_Notifications_Licensing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NotificationSubTypeId",
                table: "NotificationMasters",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CreateNewEmployeeCode",
                table: "EmployeeRejoins",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AppraisalStepSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RequestMasterId = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    CanAddKpi = table.Column<bool>(type: "bit", nullable: false),
                    CanAddPeer = table.Column<bool>(type: "bit", nullable: false),
                    CanAddRatings = table.Column<bool>(type: "bit", nullable: false),
                    CanViewPeerRatings = table.Column<bool>(type: "bit", nullable: false),
                    CanViewRatings = table.Column<bool>(type: "bit", nullable: false),
                    CanPeerAddRatings = table.Column<bool>(type: "bit", nullable: false),
                    CanViewOverallAverageRating = table.Column<bool>(type: "bit", nullable: false),
                    CanEmployeeSelfRate = table.Column<bool>(type: "bit", nullable: false),
                    AutoAddManager = table.Column<bool>(type: "bit", nullable: false),
                    AutoAddRelationShip = table.Column<bool>(type: "bit", nullable: false),
                    AutoSubmitOnLastFill = table.Column<bool>(type: "bit", nullable: false),
                    AllowedPeerCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AppraisalStepSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppraisalStepSettings_RequestMasters_RequestMasterId",
                        column: x => x.RequestMasterId,
                        principalTable: "RequestMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotificationSentLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NotificationMasterId = table.Column<int>(type: "int", nullable: true),
                    NotificationName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SourceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RecordId = table.Column<long>(type: "bigint", nullable: false),
                    SentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecipientCount = table.Column<int>(type: "int", nullable: false),
                    WasSent = table.Column<bool>(type: "bit", nullable: false),
                    IsFinal = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_NotificationSentLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationSentLogs_NotificationMasters_NotificationMasterId",
                        column: x => x.NotificationMasterId,
                        principalTable: "NotificationMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserModuleLicences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Module = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_UserModuleLicences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AppraisalStepSettingRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AppraisalStepSettingId = table.Column<int>(type: "int", nullable: false),
                    RelationshipTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AppraisalStepSettingRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppraisalStepSettingRelationships_AppraisalStepSettings_AppraisalStepSettingId",
                        column: x => x.AppraisalStepSettingId,
                        principalTable: "AppraisalStepSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppraisalStepSettingRelationships_RelationshipTypes_RelationshipTypeId",
                        column: x => x.RelationshipTypeId,
                        principalTable: "RelationshipTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppraisalStepSettingRelationships_AppraisalStepSettingId",
                table: "AppraisalStepSettingRelationships",
                column: "AppraisalStepSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_AppraisalStepSettingRelationships_RelationshipTypeId",
                table: "AppraisalStepSettingRelationships",
                column: "RelationshipTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AppraisalStepSettingRelationships_TenantId_AppraisalStepSettingId_RelationshipTypeId",
                table: "AppraisalStepSettingRelationships",
                columns: new[] { "TenantId", "AppraisalStepSettingId", "RelationshipTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppraisalStepSettings_RequestMasterId",
                table: "AppraisalStepSettings",
                column: "RequestMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_AppraisalStepSettings_TenantId_RequestMasterId_Step",
                table: "AppraisalStepSettings",
                columns: new[] { "TenantId", "RequestMasterId", "Step" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSentLogs_NotificationMasterId",
                table: "NotificationSentLogs",
                column: "NotificationMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSentLogs_TenantId_NotificationMasterId_RecordId",
                table: "NotificationSentLogs",
                columns: new[] { "TenantId", "NotificationMasterId", "RecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationSentLogs_TenantId_NotificationName_RecordId",
                table: "NotificationSentLogs",
                columns: new[] { "TenantId", "NotificationName", "RecordId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserModuleLicences_TenantId_Module",
                table: "UserModuleLicences",
                columns: new[] { "TenantId", "Module" });

            migrationBuilder.CreateIndex(
                name: "IX_UserModuleLicences_TenantId_UserId_Module",
                table: "UserModuleLicences",
                columns: new[] { "TenantId", "UserId", "Module" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppraisalStepSettingRelationships");

            migrationBuilder.DropTable(
                name: "NotificationSentLogs");

            migrationBuilder.DropTable(
                name: "UserModuleLicences");

            migrationBuilder.DropTable(
                name: "AppraisalStepSettings");

            migrationBuilder.DropColumn(
                name: "NotificationSubTypeId",
                table: "NotificationMasters");

            migrationBuilder.DropColumn(
                name: "CreateNewEmployeeCode",
                table: "EmployeeRejoins");
        }
    }
}
