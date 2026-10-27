using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <summary>
    /// Moves the System Objects table from "SysObjects" to "AppSysObjects".
    ///
    /// SQL Server resolves the name SysObjects -- dbo-qualified or not, any case -- to its own compatibility view
    /// sys.sysobjects, so every query the app made against the table failed and nothing was ever stored in it.
    ///
    /// Dropped and re-created rather than renamed, by hand: EF's generated RenameTable/DropPrimaryKey go through
    /// sp_rename and ALTER TABLE, which hit the same name resolution and fail ("No item by the name of
    /// 'dbo.SysObjects' could be found"). DROP TABLE is the one statement that reaches the real table -- checked on a
    /// scratch database. Nothing is lost: the table could never be written to, so it is empty everywhere.
    /// </summary>
    public partial class Renamed_SysObjects_To_AppSysObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'SysObjects' AND schema_id = SCHEMA_ID(N'dbo')) " +
                "DROP TABLE [dbo].[SysObjects];");

            CreateSysObjectTable(migrationBuilder, "AppSysObjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AppSysObjects");

            CreateSysObjectTable(migrationBuilder, "SysObjects");
        }

        /// <summary>The columns exactly as Added_SystemSecurity_SystemSettings created them.</summary>
        private static void CreateSysObjectTable(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.CreateTable(
                name: tableName,
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPayrollSensitive = table.Column<bool>(type: "bit", nullable: false),
                    RespectPermission = table.Column<bool>(type: "bit", nullable: false),
                    MustRetroRun = table.Column<bool>(type: "bit", nullable: false),
                    IsTimeSensitive = table.Column<bool>(type: "bit", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_" + tableName, x => x.Id);
                });
        }
    }
}
