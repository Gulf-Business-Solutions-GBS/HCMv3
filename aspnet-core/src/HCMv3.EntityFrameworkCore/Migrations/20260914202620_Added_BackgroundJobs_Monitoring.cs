using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_BackgroundJobs_Monitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BackgroundJobFailures",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: true),
                    JobId = table.Column<long>(type: "bigint", nullable: false),
                    JobName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TryCount = table.Column<int>(type: "int", nullable: false),
                    FailedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_BackgroundJobFailures", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackgroundWorkerStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkerName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IntervalMilliseconds = table.Column<int>(type: "int", nullable: false),
                    LastStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSucceeded = table.Column<bool>(type: "bit", nullable: true),
                    LastSuccessTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastFailureTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    RunCount = table.Column<long>(type: "bigint", nullable: false),
                    FailureCount = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_BackgroundWorkerStatuses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobFailures_JobId",
                table: "BackgroundJobFailures",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundJobFailures_TenantId_FailedTime",
                table: "BackgroundJobFailures",
                columns: new[] { "TenantId", "FailedTime" });

            migrationBuilder.CreateIndex(
                name: "IX_BackgroundWorkerStatuses_WorkerName",
                table: "BackgroundWorkerStatuses",
                column: "WorkerName",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BackgroundJobFailures");

            migrationBuilder.DropTable(
                name: "BackgroundWorkerStatuses");
        }
    }
}
