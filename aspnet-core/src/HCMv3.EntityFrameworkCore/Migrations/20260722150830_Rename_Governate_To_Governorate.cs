using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Rename_Governate_To_Governorate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Governates",
                newName: "Governorates");

            migrationBuilder.RenameIndex(
                name: "PK_Governates",
                newName: "PK_Governorates",
                table: "Governorates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Governorates",
                newName: "Governates");

            migrationBuilder.RenameIndex(
                name: "PK_Governorates",
                newName: "PK_Governates",
                table: "Governates");
        }
    }
}
