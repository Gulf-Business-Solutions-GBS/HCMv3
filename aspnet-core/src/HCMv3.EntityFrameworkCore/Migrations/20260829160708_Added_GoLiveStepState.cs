using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_GoLiveStepState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoLiveStepStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    StepId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_GoLiveStepStates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoLiveStepStates_TenantId_StepId",
                table: "GoLiveStepStates",
                columns: new[] { "TenantId", "StepId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoLiveStepStates");
        }
    }
}
