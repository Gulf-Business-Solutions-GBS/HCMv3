using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Removed_GoLiveSetup_V1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Go-Live Setup V1 is gone; its saved wizard drafts go with it.
            migrationBuilder.DropTable(
                name: "GoLiveStepStates");

            // "Go-Live Setup V2" is now "Go-Live Setup". The records it marks and finds by their comment keep being
            // found, the run it tracks keeps its setting, and the V2 screen's usage folds into the one screen left.
            migrationBuilder.Sql(@"
UPDATE TimeTransfers SET Comments = 'Go-Live Setup: annual leave opening balance'
WHERE Comments = 'Go-Live Setup V2: annual leave opening balance';

UPDATE EmployeePayments SET Comments = 'Go-Live Setup: from the legacy salary comparison'
WHERE Comments = 'Go-Live Setup V2: from the legacy salary comparison';

UPDATE AbpSettings SET Name = 'App.GoLive.PayrollRun'
WHERE Name = 'App.GoLiveV2.PayrollRun'
  AND NOT EXISTS (SELECT 1 FROM AbpSettings s WHERE s.Name = 'App.GoLive.PayrollRun'
                  AND ISNULL(s.TenantId, -1) = ISNULL(AbpSettings.TenantId, -1)
                  AND ISNULL(s.UserId, -1) = ISNULL(AbpSettings.UserId, -1));

DELETE v2 FROM UserScreenUsages v2
WHERE v2.ScreenKey = 'app.admin.go-live-setup-v2'
  AND EXISTS (SELECT 1 FROM UserScreenUsages u WHERE u.ScreenKey = 'app.admin.go-live-setup'
              AND ISNULL(u.TenantId, -1) = ISNULL(v2.TenantId, -1) AND u.UserId = v2.UserId);
UPDATE UserScreenUsages SET ScreenKey = 'app.admin.go-live-setup' WHERE ScreenKey = 'app.admin.go-live-setup-v2';
UPDATE SharedQuickLinks SET ScreenKey = 'app.admin.go-live-setup' WHERE ScreenKey = 'app.admin.go-live-setup-v2';
DELETE FROM Screens WHERE ScreenKey = 'app.admin.go-live-setup-v2';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoLiveStepStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StepId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoLiveStepStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoLiveStepStates_TenantId_StepId",
                table: "GoLiveStepStates",
                columns: new[] { "TenantId", "StepId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
