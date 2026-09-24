using HCMv3.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// Gives <c>WorkFlowApprovers</c> the step each row was assigned for, so the table can keep its history.
    ///
    /// <para>
    /// Until now the engine deleted every approver row on a request before inserting the next step's, so the
    /// table only ever described the step a request was sitting on. Everything earlier was gone: who was
    /// entitled to approve step 1, and whether they did, survived only as far as <c>RequestLog</c> recorded
    /// somebody ACTING -- which says nothing about the two colleagues who could have approved and did not.
    /// The participants panel had to reconstruct past steps from the action trail and label the result as
    /// inferred.
    /// </para>
    ///
    /// <para>
    /// <b>The backfill is not cosmetic.</b> Every existing row is set to its request's current
    /// <c>WorkflowStep</c>, because that is precisely what those rows have always meant -- they were the
    /// current step's rows and nothing else. Left at the default 0 they would match no request's step, and
    /// every query that now filters on the step would find nothing: every in-flight request would vanish
    /// from its approver's inbox, and the badge would read zero for everybody.
    /// </para>
    ///
    /// <para>
    /// Written by hand rather than scaffolded. <c>dotnet ef migrations add</c> would have swept in
    /// unrelated <c>AbsenceViolationRuleDetails</c> changes that are in flight in the same tree with no
    /// migration of their own -- the same hazard that produced the note on
    /// <c>Added_RequestNumberSequences</c>, one migration earlier.
    /// </para>
    /// </summary>
    [DbContext(typeof(HCMv3DbContext))]
    [Migration("20260924220000_Added_WorkflowStep_To_WorkFlowApprovers")]
    public partial class Added_WorkflowStep_To_WorkFlowApprovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WorkflowStep",
                table: "WorkFlowApprovers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // The backfill, over LIVE rows only.
            //
            // A live approver row can only ever have belonged to its request's current step, because the
            // engine deleted the rest on every step change -- so for those the current step is not a guess,
            // it is what the row already meant.
            //
            // Soft-deleted rows are deliberately left at 0. WorkFlowApprover is a FullAuditedEntity, so
            // those deletions only set IsDeleted and every superseded row from every past step is still
            // here -- but nothing records which step each belonged to, and they are old enough that their
            // request has usually moved on several steps since. Stamping them with the CURRENT step would
            // be writing a value we know to be wrong, to no purpose: ABP filters
            // them out of every query the application makes. 0 is the honest answer -- unknown -- and it
            // keeps them from ever matching a step filter.
            // Wrapped in EXEC, which is not decoration.
            //
            // EF emits the AddColumn and this statement into ONE SQL Server batch, and the whole batch is
            // parsed before any of it runs -- so the UPDATE is checked against the schema as it was BEFORE
            // the column was added, and fails with "Invalid column name 'WorkflowStep'". EXEC defers
            // compilation of the inner statement until the ALTER has actually happened. The alternative,
            // a second migration purely to run the backfill, would leave a window in which the column
            // exists and every step filter matches nothing.
            migrationBuilder.Sql(@"
                EXEC('
                    UPDATE a
                    SET a.WorkflowStep = r.WorkflowStep
                    FROM WorkFlowApprovers a
                    INNER JOIN WorkflowRequests r ON r.Id = a.WorkflowRequestId
                    WHERE a.IsDeleted = 0;');");

            // Every hot query is now "this request, this step" -- the inbox, the badge, the current-approver
            // check on every decision. WorkflowRequestId alone was selective enough when a request had one
            // step's worth of rows; it will not be once rows accumulate across steps.
            migrationBuilder.CreateIndex(
                name: "IX_WorkFlowApprovers_WorkflowRequestId_WorkflowStep",
                table: "WorkFlowApprovers",
                columns: new[] { "WorkflowRequestId", "WorkflowStep" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WorkFlowApprovers_WorkflowRequestId_WorkflowStep",
                table: "WorkFlowApprovers");

            migrationBuilder.DropColumn(
                name: "WorkflowStep",
                table: "WorkFlowApprovers");
        }
    }
}
