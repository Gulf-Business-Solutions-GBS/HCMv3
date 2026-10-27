using HCMv3.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// Lets a transaction type say whether it may be recorded without going for approval.
    ///
    /// <para>
    /// The first of the two gates on a bypass: the type must permit it at all. The second is the
    /// <c>RecordWithoutApproval</c> permission, which needs no schema. Defaulted to false, so every
    /// workflow-enabled type keeps routing until somebody deliberately says otherwise.
    /// </para>
    ///
    /// <para>
    /// WRITTEN BY HAND rather than scaffolded, and it has to stay that way until the snapshot is repaired.
    /// Scaffolding produces two kinds of damage here. It picks up another workstream's pending entity
    /// changes, which then ship under this migration's name. And because the two index-adding migrations
    /// before this one were also hand-written -- so the model snapshot never learned about them -- a
    /// scaffold "helpfully" DROPS <c>IX_WorkFlowApprovers_WorkflowRequestId_WorkflowStep</c> and
    /// <c>IX_WorkflowRemarks_CCReceiverId_RespondedAt</c>, both of which exist in the database and are
    /// relied on by the inbox queries.
    /// </para>
    ///
    /// <para>
    /// Both attributes below are required for the same reason: with no generated <c>.Designer.cs</c>,
    /// nothing else tells EF Core which context this belongs to or what its id is, and the migration is
    /// silently ignored.
    /// </para>
    /// </summary>
    [DbContext(typeof(HCMv3DbContext))]
    [Migration("20260927130000_Added_AllowDirectEntry_To_WorkflowTransactionTypes")]
    public partial class Added_AllowDirectEntry_To_WorkflowTransactionTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowDirectEntry",
                table: "WorkflowTransactionTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowDirectEntry",
                table: "WorkflowTransactionTypes");
        }
    }
}
