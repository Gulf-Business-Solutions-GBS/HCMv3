using HCMv3.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// Turns <c>WorkflowRemarks</c> from a bare list of who was copied in into a record of who asked whom,
    /// why, and whether they answered -- the "Get feedback" feature.
    ///
    /// <para>
    /// The table held only a request id and a receiver. That is enough to say somebody was involved and not
    /// enough for any of the questions actually asked of it: who pulled this person in, what did they want
    /// looked at, and is this ask still outstanding. Without the last one an inbox cannot tell a waiting ask
    /// from a settled one, so it can only either show every ask forever or none.
    /// </para>
    ///
    /// <para>
    /// All four columns are nullable or defaulted and no existing row is touched. Rows written before this
    /// have no asker and no step, which is true -- nothing recorded them -- and a null <c>RespondedAt</c>
    /// reads as "not answered", which is the safe way round: an old row surfaces in somebody's list where it
    /// can be dismissed, rather than being silently treated as closed.
    /// </para>
    ///
    /// <para>
    /// Hand-written rather than scaffolded, for the reason given on
    /// <c>Added_WorkflowStep_To_WorkFlowApprovers</c>: unrelated <c>AbsenceViolationRuleDetails</c> changes
    /// are still in flight in this tree with no migration of their own, and <c>migrations add</c> would
    /// sweep them in.
    /// </para>
    /// </summary>
    [DbContext(typeof(HCMv3DbContext))]
    [Migration("20260925120000_Added_Feedback_Fields_To_WorkflowRemarks")]
    public partial class Added_Feedback_Fields_To_WorkflowRemarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestedById",
                table: "WorkflowRemarks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkflowStep",
                table: "WorkflowRemarks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequestNote",
                table: "WorkflowRemarks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<System.DateTime>(
                name: "RespondedAt",
                table: "WorkflowRemarks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRemarks_RequestedById",
                table: "WorkflowRemarks",
                column: "RequestedById");

            // The inbox's query: "outstanding asks for this person". Both columns are in it, and without an
            // index it is a scan of every feedback row in the tenant on every inbox load.
            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRemarks_CCReceiverId_RespondedAt",
                table: "WorkflowRemarks",
                columns: new[] { "CCReceiverId", "RespondedAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_WorkflowRemarks_Employees_RequestedById",
                table: "WorkflowRemarks",
                column: "RequestedById",
                principalTable: "Employees",
                principalColumn: "Id",

                // Restrict, like every other Employee FK in this schema. Cascade here would add another path
                // from Employees to WorkflowRemarks alongside the existing CCReceiverId one, and SQL Server
                // refuses multiple cascade paths to the same table (error 1785).
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WorkflowRemarks_Employees_RequestedById",
                table: "WorkflowRemarks");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowRemarks_CCReceiverId_RespondedAt",
                table: "WorkflowRemarks");

            migrationBuilder.DropIndex(
                name: "IX_WorkflowRemarks_RequestedById",
                table: "WorkflowRemarks");

            migrationBuilder.DropColumn(name: "RespondedAt", table: "WorkflowRemarks");
            migrationBuilder.DropColumn(name: "RequestNote", table: "WorkflowRemarks");
            migrationBuilder.DropColumn(name: "WorkflowStep", table: "WorkflowRemarks");
            migrationBuilder.DropColumn(name: "RequestedById", table: "WorkflowRemarks");
        }
    }
}
