using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_AbsenceViolationStep_Notification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // These two single-column indexes are covered by the wider indexes that
            // Added_WorkflowStep_To_WorkFlowApprovers and Added_Feedback_Fields_To_WorkflowRemarks created by
            // hand, each starting with the same column. Declaring those wider indexes in HCMv3DbContext -- which
            // those two migrations omitted -- leaves these as duplicates, and EF drops them here.
            migrationBuilder.DropIndex(
                name: "IX_WorkflowRemarks_CCReceiverId",
                table: "WorkflowRemarks");

            migrationBuilder.DropIndex(
                name: "IX_WorkFlowApprovers_WorkflowRequestId",
                table: "WorkFlowApprovers");

            // AllowDirectEntry is deliberately NOT added here. EF offered it because
            // Added_AllowDirectEntry_To_WorkflowTransactionTypes was hand-written and never updated the model
            // snapshot; that migration has not run yet and is the one that adds the column. Adding it twice
            // fails on "column names in each table must be unique".

            migrationBuilder.AddColumn<bool>(
                name: "HoldTillPunchFound",
                table: "AbsenceViolationRuleDetails",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "NotificationConfigId",
                table: "AbsenceViolationRuleDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceViolationRuleDetails_NotificationConfigId",
                table: "AbsenceViolationRuleDetails",
                column: "NotificationConfigId");

            migrationBuilder.AddForeignKey(
                name: "FK_AbsenceViolationRuleDetails_NotificationConfigs_NotificationConfigId",
                table: "AbsenceViolationRuleDetails",
                column: "NotificationConfigId",
                principalTable: "NotificationConfigs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AbsenceViolationRuleDetails_NotificationConfigs_NotificationConfigId",
                table: "AbsenceViolationRuleDetails");

            migrationBuilder.DropIndex(
                name: "IX_AbsenceViolationRuleDetails_NotificationConfigId",
                table: "AbsenceViolationRuleDetails");

            migrationBuilder.DropColumn(
                name: "HoldTillPunchFound",
                table: "AbsenceViolationRuleDetails");

            migrationBuilder.DropColumn(
                name: "NotificationConfigId",
                table: "AbsenceViolationRuleDetails");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRemarks_CCReceiverId",
                table: "WorkflowRemarks",
                column: "CCReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkFlowApprovers_WorkflowRequestId",
                table: "WorkFlowApprovers",
                column: "WorkflowRequestId");
        }
    }
}
