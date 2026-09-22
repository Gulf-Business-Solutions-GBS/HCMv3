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
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM Employees WHERE Photo IS NOT NULL)
    OR EXISTS (SELECT 1 FROM Applicants WHERE Photo IS NOT NULL)
    OR EXISTS (SELECT 1 FROM EmployeeSignatures WHERE Signature IS NOT NULL)
BEGIN
    THROW 51000, 'Images are still stored in the database. Run the previous release first so the background mover copies them to the file store, then apply this migration.', 1;
END");

            migrationBuilder.DropColumn(
                name: "Signature",
                table: "EmployeeSignatures");

            migrationBuilder.DropColumn(
                name: "Photo",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "Photo",
                table: "Applicants");
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
