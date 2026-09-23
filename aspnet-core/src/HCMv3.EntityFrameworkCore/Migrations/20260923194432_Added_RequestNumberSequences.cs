using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// The tenant's request-number counter: one row per tenant, one sequence for every request type it has.
    ///
    /// <para>
    /// A new table only -- nothing existing is altered, so there is no backfill to do and nothing to lose.
    /// The rows already in <c>WorkflowRequests</c> keep the numbers they were given; the old format
    /// (<c>EXP-2026-00002</c>) and the new one (<c>EXP-000001</c>) cannot collide because they are different
    /// shapes, and the generator never reads the existing numbers at all -- it reads this counter, which
    /// starts at 1 per tenant.
    /// </para>
    ///
    /// <para>
    /// <b>The unique index on TenantId is load-bearing, not tidiness.</b> A second counter row for one
    /// tenant would let two callers each increment their own copy and issue the same number twice, and it is
    /// also what makes the generator's create-it-if-missing path safe: two callers reaching that path
    /// together means the loser gets a constraint violation and retries the increment, rather than quietly
    /// creating a duplicate that nothing would ever notice.
    /// </para>
    ///
    /// <para>
    /// EF scaffolded two unrelated <c>EmployeeOrgAssignments.PositionId</c> operations into this migration,
    /// picked up from another change that is in flight in the same tree. They have been removed by hand --
    /// applying them would drop a column somebody else had just added -- and the model snapshot has been
    /// corrected to match.
    /// </para>
    /// </summary>
    public partial class Added_RequestNumberSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RequestNumberSequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RequestNumberSequences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RequestNumberSequences_TenantId",
                table: "RequestNumberSequences",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RequestNumberSequences");
        }
    }
}
