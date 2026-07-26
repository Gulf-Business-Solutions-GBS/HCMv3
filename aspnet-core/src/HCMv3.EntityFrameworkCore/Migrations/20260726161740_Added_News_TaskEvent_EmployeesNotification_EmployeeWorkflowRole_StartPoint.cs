using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_News_TaskEvent_EmployeesNotification_EmployeeWorkflowRole_StartPoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StartPoint",
                table: "EmployeeDutyPeriods",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "EmployeeWorkflowRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    WorkflowRoleId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeWorkflowRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkflowRoles_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkflowRoles_WorkflowRoles_WorkflowRoleId",
                        column: x => x.WorkflowRoleId,
                        principalTable: "WorkflowRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NewsTypeId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TitleAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OrganizationUnitTypeId = table.Column<int>(type: "int", nullable: true),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: true),
                    SendNotification = table.Column<bool>(type: "bit", nullable: false),
                    SendEmail = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CommentAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false),
                    DocumentHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_News", x => x.Id);
                    table.ForeignKey(
                        name: "FK_News_AbpOrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_News_NewsTypes_NewsTypeId",
                        column: x => x.NewsTypeId,
                        principalTable: "NewsTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_News_OrganizationUnitTypes_OrganizationUnitTypeId",
                        column: x => x.OrganizationUnitTypeId,
                        principalTable: "OrganizationUnitTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TaskEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RefEmployeeId = table.Column<int>(type: "int", nullable: true),
                    NotificationConfigId = table.Column<int>(type: "int", nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    PercentCompleted = table.Column<int>(type: "int", nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemindIn = table.Column<TimeSpan>(type: "time", nullable: true),
                    AlarmTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ObjectType = table.Column<int>(type: "int", nullable: false),
                    ObjectOId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TaskEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskEvents_Employees_RefEmployeeId",
                        column: x => x.RefEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TaskEvents_NotificationConfigs_NotificationConfigId",
                        column: x => x.NotificationConfigId,
                        principalTable: "NotificationConfigs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeesNotificationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    DismissTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaskEventId = table.Column<int>(type: "int", nullable: true),
                    EmailNotificationLogId = table.Column<int>(type: "int", nullable: true),
                    NotificationType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeesNotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeesNotificationLogs_EmailNotificationLogs_EmailNotificationLogId",
                        column: x => x.EmailNotificationLogId,
                        principalTable: "EmailNotificationLogs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeesNotificationLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeesNotificationLogs_TaskEvents_TaskEventId",
                        column: x => x.TaskEventId,
                        principalTable: "TaskEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeesNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TaskEventId = table.Column<int>(type: "int", nullable: true),
                    IsNotificationDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsNotificationRead = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmployeesNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeesNotifications_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeesNotifications_TaskEvents_TaskEventId",
                        column: x => x.TaskEventId,
                        principalTable: "TaskEvents",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeesNotificationLogs_EmailNotificationLogId",
                table: "EmployeesNotificationLogs",
                column: "EmailNotificationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeesNotificationLogs_EmployeeId",
                table: "EmployeesNotificationLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeesNotificationLogs_TaskEventId",
                table: "EmployeesNotificationLogs",
                column: "TaskEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeesNotifications_EmployeeId",
                table: "EmployeesNotifications",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeesNotifications_TaskEventId",
                table: "EmployeesNotifications",
                column: "TaskEventId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkflowRoles_EmployeeId",
                table: "EmployeeWorkflowRoles",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkflowRoles_WorkflowRoleId",
                table: "EmployeeWorkflowRoles",
                column: "WorkflowRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_News_NewsTypeId",
                table: "News",
                column: "NewsTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_News_OrganizationUnitId",
                table: "News",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_News_OrganizationUnitTypeId",
                table: "News",
                column: "OrganizationUnitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskEvents_NotificationConfigId",
                table: "TaskEvents",
                column: "NotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskEvents_RefEmployeeId",
                table: "TaskEvents",
                column: "RefEmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeesNotificationLogs");

            migrationBuilder.DropTable(
                name: "EmployeesNotifications");

            migrationBuilder.DropTable(
                name: "EmployeeWorkflowRoles");

            migrationBuilder.DropTable(
                name: "News");

            migrationBuilder.DropTable(
                name: "TaskEvents");

            migrationBuilder.DropColumn(
                name: "StartPoint",
                table: "EmployeeDutyPeriods");
        }
    }
}
