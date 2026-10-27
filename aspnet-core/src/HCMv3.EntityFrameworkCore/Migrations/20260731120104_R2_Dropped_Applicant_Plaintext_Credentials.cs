using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// DESTRUCTIVE AND NOT REVERSIBLE IN PRACTICE -- READ BEFORE APPLYING.
    ///
    /// Drops the legacy plaintext careers-portal credential columns <c>Applicants.UserName</c> and
    /// <c>Applicants.Password</c>, replaced by <c>Applicants.UserId</c> (see the preceding
    /// <c>R2_Added_Applicant_UserId_Link</c> migration) pointing at a real ABP user.
    ///
    /// <c>Down()</c> restores the two COLUMNS but not their CONTENTS. Every value in them is lost the moment
    /// <c>Up()</c> runs; rolling back yields two empty nvarchar(256) columns. If any applicant rows carry a
    /// non-null <c>UserName</c>, those identities must be turned into ABP users FIRST. There is deliberately
    /// no automated carry-across here, because there is no safe one: the stored passwords are plaintext and
    /// must never be re-hashed into ABP accounts, so affected candidates go through password reset either
    /// way.
    ///
    /// Check before applying:
    /// <code>
    /// SELECT COUNT(*) AS Total,
    ///        SUM(CASE WHEN UserName IS NOT NULL AND UserName &lt;&gt; '' THEN 1 ELSE 0 END) AS WithUserName,
    ///        SUM(CASE WHEN Password IS NOT NULL AND Password &lt;&gt; '' THEN 1 ELSE 0 END) AS WithPassword
    /// FROM Applicants;
    /// </code>
    /// </summary>
    public partial class R2_Dropped_Applicant_Plaintext_Credentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Password",
                table: "Applicants");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "Applicants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Password",
                table: "Applicants",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "Applicants",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
