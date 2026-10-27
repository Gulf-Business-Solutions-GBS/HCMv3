using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Dropped_Image_Columns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // REFUSES RATHER THAN DESTROYS. Photos and signatures live on disk now, moved out of these
            // columns by the background mover that shipped with the previous release. A server that has not
            // run that release yet still has the only copy here, and dropping the columns would delete every
            // employee photo with no way back. The migration therefore stops if anything is left: deploy the
            // previous release, let it run (it moves them within ten minutes), then apply this.
            // Each check is wrapped in EXEC and asks COL_LENGTH first, which is not decoration.
            //
            // SQL Server binds every column name in a batch when it parses it, before deciding which branch
            // to run. In the idempotent script this whole migration sits inside "IF NOT EXISTS (... this
            // migration ...)", so on a server that has already applied it the block never runs -- but the
            // batch is still parsed, and "WHERE Photo IS NOT NULL" fails with "Invalid column name 'Photo'"
            // against the very columns this migration dropped. The deployment then stops on a migration that
            // was finished weeks ago. EXEC defers the parse to the moment it runs; COL_LENGTH means it does
            // not run at all once the column is gone.
            foreach (var column in new[]
                     {
                         new { Table = "Employees", Column = "Photo" },
                         new { Table = "Applicants", Column = "Photo" },
                         new { Table = "EmployeeSignatures", Column = "Signature" },
                     })
            {
                migrationBuilder.Sql($@"
IF COL_LENGTH('{column.Table}', '{column.Column}') IS NOT NULL
BEGIN
    EXEC(N'
        IF EXISTS (SELECT 1 FROM [{column.Table}] WHERE [{column.Column}] IS NOT NULL)
        BEGIN
            THROW 51000, ''Images are still stored in the database. Run the previous release first so the background mover copies them to the file store, then apply this migration.'', 1;
        END');
END");
            }

            // Guarded for the same reason: a server part-way through an earlier attempt can have the column
            // already gone while the migration is unrecorded, and a plain DROP COLUMN would stop there.
            foreach (var column in new[]
                     {
                         new { Table = "EmployeeSignatures", Column = "Signature" },
                         new { Table = "Employees", Column = "Photo" },
                         new { Table = "Applicants", Column = "Photo" },
                     })
            {
                migrationBuilder.Sql($@"
IF COL_LENGTH('{column.Table}', '{column.Column}') IS NOT NULL
    ALTER TABLE [{column.Table}] DROP COLUMN [{column.Column}];");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "Signature",
                table: "EmployeeSignatures",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Photo",
                table: "Employees",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Photo",
                table: "Applicants",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
