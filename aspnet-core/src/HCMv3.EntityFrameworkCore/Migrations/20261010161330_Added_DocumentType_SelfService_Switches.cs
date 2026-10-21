using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// The self-service switches on a document type: whether employees may withdraw it, whether it is offered
    /// to them when they raise a request, and whether it is hidden from their own view.
    ///
    /// <para>
    /// Existing types keep behaving as they did. Every one was offered to employees, so
    /// <c>IncludeInWorkflow</c> starts true. The one type employees could withdraw -- named by the tenant
    /// config <c>PassportWithdrawalDocumentTypeCode</c>, legacy's code "2" when unset -- is ticked
    /// <c>IsWithdrawable</c>; a tenant with no such type gets nothing ticked and goes on following the code
    /// rule (see <c>WithdrawableDocumentTypeResolver</c>).
    /// </para>
    /// </summary>
    public partial class Added_DocumentType_SelfService_Switches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeInWorkflow",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHiddenFromEmployee",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsWithdrawable",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Run through EXEC so SQL Server compiles it only when it runs. In the single idempotent deploy
            // script every migration sits in one IF block, and a plain UPDATE naming IsWithdrawable is
            // compiled before the ALTER above has added the column -- "Invalid column name" on a fresh
            // database, as happened on the first server install.
            migrationBuilder.Sql(@"
EXEC(N'
UPDATE t
SET t.IsWithdrawable = 1
FROM DocumentTypes t
OUTER APPLY (
    SELECT TOP 1 NULLIF(LTRIM(RTRIM(sc.KeyValue)), '''') AS Code
    FROM SysTenantConfigs sc
    WHERE sc.TenantId = t.TenantId
      AND sc.[Key] = ''PassportWithdrawalDocumentTypeCode''
      AND sc.IsDeleted = 0
) c
WHERE t.IsDeleted = 0
  AND t.Code = ISNULL(c.Code, ''2'');
');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeInWorkflow",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "IsHiddenFromEmployee",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "IsWithdrawable",
                table: "DocumentTypes");
        }
    }
}
