using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Workflow_Step_Notification_Configs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationGroups_NotificationConfigs_NotificationConfigId",
                table: "NotificationGroups");

            migrationBuilder.AddColumn<int>(
                name: "AssignNotificationConfigId",
                table: "RequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NotificationConfigId",
                table: "RequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RejectNotificationConfigId",
                table: "RequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RemarksNotificationConfigId",
                table: "RequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReminderNotificationConfigId",
                table: "RequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SendBackNotificationConfigId",
                table: "RequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SubmitNotificationConfigId",
                table: "RequestDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRoleGroup",
                table: "NotificationGroups",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRoleId",
                table: "NotificationGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowRoleOrgId",
                table: "NotificationGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmailTemplateId",
                table: "NotificationConfigs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_AssignNotificationConfigId",
                table: "RequestDetails",
                column: "AssignNotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_NotificationConfigId",
                table: "RequestDetails",
                column: "NotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_RejectNotificationConfigId",
                table: "RequestDetails",
                column: "RejectNotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_RemarksNotificationConfigId",
                table: "RequestDetails",
                column: "RemarksNotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_ReminderNotificationConfigId",
                table: "RequestDetails",
                column: "ReminderNotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_SendBackNotificationConfigId",
                table: "RequestDetails",
                column: "SendBackNotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_SubmitNotificationConfigId",
                table: "RequestDetails",
                column: "SubmitNotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationGroups_WorkflowRoleId",
                table: "NotificationGroups",
                column: "WorkflowRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationGroups_WorkflowRoleOrgId",
                table: "NotificationGroups",
                column: "WorkflowRoleOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationConfigs_EmailTemplateId",
                table: "NotificationConfigs",
                column: "EmailTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationConfigs_EmailTemplates_EmailTemplateId",
                table: "NotificationConfigs",
                column: "EmailTemplateId",
                principalTable: "EmailTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationGroups_NotificationConfigs_NotificationConfigId",
                table: "NotificationGroups",
                column: "NotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationGroups_OrganizationUnitTypes_WorkflowRoleOrgId",
                table: "NotificationGroups",
                column: "WorkflowRoleOrgId",
                principalTable: "OrganizationUnitTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationGroups_WorkflowRoles_WorkflowRoleId",
                table: "NotificationGroups",
                column: "WorkflowRoleId",
                principalTable: "WorkflowRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_AssignNotificationConfigId",
                table: "RequestDetails",
                column: "AssignNotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_NotificationConfigId",
                table: "RequestDetails",
                column: "NotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_RejectNotificationConfigId",
                table: "RequestDetails",
                column: "RejectNotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_RemarksNotificationConfigId",
                table: "RequestDetails",
                column: "RemarksNotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_ReminderNotificationConfigId",
                table: "RequestDetails",
                column: "ReminderNotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_SendBackNotificationConfigId",
                table: "RequestDetails",
                column: "SendBackNotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_SubmitNotificationConfigId",
                table: "RequestDetails",
                column: "SubmitNotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotificationConfigs_EmailTemplates_EmailTemplateId",
                table: "NotificationConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationGroups_NotificationConfigs_NotificationConfigId",
                table: "NotificationGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationGroups_OrganizationUnitTypes_WorkflowRoleOrgId",
                table: "NotificationGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_NotificationGroups_WorkflowRoles_WorkflowRoleId",
                table: "NotificationGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_AssignNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_NotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_RejectNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_RemarksNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_ReminderNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_SendBackNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_RequestDetails_NotificationConfigs_SubmitNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_RequestDetails_AssignNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_RequestDetails_NotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_RequestDetails_RejectNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_RequestDetails_RemarksNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_RequestDetails_ReminderNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_RequestDetails_SendBackNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_RequestDetails_SubmitNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropIndex(
                name: "IX_NotificationGroups_WorkflowRoleId",
                table: "NotificationGroups");

            migrationBuilder.DropIndex(
                name: "IX_NotificationGroups_WorkflowRoleOrgId",
                table: "NotificationGroups");

            migrationBuilder.DropIndex(
                name: "IX_NotificationConfigs_EmailTemplateId",
                table: "NotificationConfigs");

            migrationBuilder.DropColumn(
                name: "AssignNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropColumn(
                name: "NotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropColumn(
                name: "RejectNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropColumn(
                name: "RemarksNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropColumn(
                name: "ReminderNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropColumn(
                name: "SendBackNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropColumn(
                name: "SubmitNotificationConfigId",
                table: "RequestDetails");

            migrationBuilder.DropColumn(
                name: "WorkflowRoleGroup",
                table: "NotificationGroups");

            migrationBuilder.DropColumn(
                name: "WorkflowRoleId",
                table: "NotificationGroups");

            migrationBuilder.DropColumn(
                name: "WorkflowRoleOrgId",
                table: "NotificationGroups");

            migrationBuilder.DropColumn(
                name: "EmailTemplateId",
                table: "NotificationConfigs");

            migrationBuilder.AddForeignKey(
                name: "FK_NotificationGroups_NotificationConfigs_NotificationConfigId",
                table: "NotificationGroups",
                column: "NotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id");
        }
    }
}
