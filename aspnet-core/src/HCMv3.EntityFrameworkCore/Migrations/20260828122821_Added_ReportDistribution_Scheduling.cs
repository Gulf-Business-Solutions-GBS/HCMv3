using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_ReportDistribution_Scheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportKind = table.Column<int>(type: "int", nullable: false),
                    ReportKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Format = table.Column<int>(type: "int", nullable: false),
                    NotificationEmails = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    NotifyOnStart = table.Column<bool>(type: "bit", nullable: false),
                    NotifyOnCompletion = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BurstDimension = table.Column<int>(type: "int", nullable: false),
                    BurstOrganizationUnitTypeId = table.Column<int>(type: "int", nullable: true),
                    BurstParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OccurrenceType = table.Column<int>(type: "int", nullable: false),
                    RecurringPattern = table.Column<int>(type: "int", nullable: true),
                    FirstOccurrence = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndingOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRunTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextRunTime = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_ReportSchedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportScheduleDestinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReportScheduleId = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    EmailSubjectTemplate = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmailBodyTemplate = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    HostAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FtpLogin = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FtpPasswordSettingName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    UseSftp = table.Column<bool>(type: "bit", nullable: false),
                    FtpPassiveMode = table.Column<bool>(type: "bit", nullable: false),
                    RemotePath = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    StorageFolder = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_ReportScheduleDestinations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportScheduleDestinations_ReportSchedules_ReportScheduleId",
                        column: x => x.ReportScheduleId,
                        principalTable: "ReportSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportScheduleFilters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReportScheduleId = table.Column<int>(type: "int", nullable: false),
                    ParameterName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ParameterValue = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    RelativeDateToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_ReportScheduleFilters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportScheduleFilters_ReportSchedules_ReportScheduleId",
                        column: x => x.ReportScheduleId,
                        principalTable: "ReportSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportScheduleRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReportScheduleId = table.Column<int>(type: "int", nullable: false),
                    Rule = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    ScopeToBurstUnit = table.Column<bool>(type: "bit", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BypassRecipientScope = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ReportScheduleRecipients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportScheduleRecipients_ReportSchedules_ReportScheduleId",
                        column: x => x.ReportScheduleId,
                        principalTable: "ReportSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportScheduleRuns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReportScheduleId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsManual = table.Column<bool>(type: "bit", nullable: false),
                    SliceCount = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_ReportScheduleRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportScheduleRuns_ReportSchedules_ReportScheduleId",
                        column: x => x.ReportScheduleId,
                        principalTable: "ReportSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportScheduleRunSlices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReportScheduleRunId = table.Column<long>(type: "bigint", nullable: false),
                    BurstKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BurstKeyDisplay = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RecipientUserId = table.Column<long>(type: "bigint", nullable: true),
                    RecipientEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    WasRecipientScoped = table.Column<bool>(type: "bit", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_ReportScheduleRunSlices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportScheduleRunSlices_ReportScheduleRuns_ReportScheduleRunId",
                        column: x => x.ReportScheduleRunId,
                        principalTable: "ReportScheduleRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportScheduleDestinations_ReportScheduleId",
                table: "ReportScheduleDestinations",
                column: "ReportScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportScheduleFilters_ReportScheduleId",
                table: "ReportScheduleFilters",
                column: "ReportScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportScheduleRecipients_ReportScheduleId",
                table: "ReportScheduleRecipients",
                column: "ReportScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportScheduleRuns_ReportScheduleId",
                table: "ReportScheduleRuns",
                column: "ReportScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportScheduleRunSlices_ReportScheduleRunId",
                table: "ReportScheduleRunSlices",
                column: "ReportScheduleRunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportScheduleDestinations");

            migrationBuilder.DropTable(
                name: "ReportScheduleFilters");

            migrationBuilder.DropTable(
                name: "ReportScheduleRecipients");

            migrationBuilder.DropTable(
                name: "ReportScheduleRunSlices");

            migrationBuilder.DropTable(
                name: "ReportScheduleRuns");

            migrationBuilder.DropTable(
                name: "ReportSchedules");
        }
    }
}
