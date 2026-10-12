using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// The two date switches on a document type, from the previous system: whether an expiry date must be
    /// given, and whether an employee may hold two documents of the type whose dates overlap.
    ///
    /// <para>
    /// Types that already exist are set to allow overlap. Until now nothing checked it, so a tenant may well
    /// hold overlapping documents already, and switching the check on underneath them would refuse the next
    /// edit of a record that was accepted when it was entered. A type created from now on starts with the
    /// check on, as it did in the previous system.
    /// </para>
    /// </summary>
    public partial class Added_DocumentType_Date_Rules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowDateOverlap",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsExpiryDateRequired",
                table: "DocumentTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE DocumentTypes SET AllowDateOverlap = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowDateOverlap",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "IsExpiryDateRequired",
                table: "DocumentTypes");
        }
    }
}
