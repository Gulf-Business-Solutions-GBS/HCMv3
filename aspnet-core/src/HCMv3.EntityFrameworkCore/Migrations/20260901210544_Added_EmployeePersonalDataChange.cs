using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_EmployeePersonalDataChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeePersonalDataChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FirstNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SecondNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ThirdNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FirstNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SecondNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ThirdNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NationalityId = table.Column<int>(type: "int", nullable: true),
                    ReligionId = table.Column<int>(type: "int", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    MaritalStatusId = table.Column<int>(type: "int", nullable: true),
                    BirthPlaceId = table.Column<int>(type: "int", nullable: true),
                    FlatNumn = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FloorNum = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    BuildingNum = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    StreetNum = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    BlockNum = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PACINumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    City = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    GovernorateId = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    CurrentAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PermanentAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Telephone = table.Column<long>(type: "bigint", nullable: true),
                    Mobile = table.Column<long>(type: "bigint", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AlternateEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FatherNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FatherNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MotherNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MotherNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BloodGroup = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CriticalIllness = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EmergencyContact = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmergencyNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EmergencyRelation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkflowRequestId = table.Column<int>(type: "int", nullable: true),
                    WorkflowStatus = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeePersonalDataChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePersonalDataChanges_Cities_BirthPlaceId",
                        column: x => x.BirthPlaceId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePersonalDataChanges_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePersonalDataChanges_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePersonalDataChanges_Governorates_GovernorateId",
                        column: x => x.GovernorateId,
                        principalTable: "Governorates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePersonalDataChanges_MaritalStatuses_MaritalStatusId",
                        column: x => x.MaritalStatusId,
                        principalTable: "MaritalStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePersonalDataChanges_Nationalities_NationalityId",
                        column: x => x.NationalityId,
                        principalTable: "Nationalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePersonalDataChanges_Religions_ReligionId",
                        column: x => x.ReligionId,
                        principalTable: "Religions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeePersonalDataChanges_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePersonalDataChanges_BirthPlaceId",
                table: "EmployeePersonalDataChanges",
                column: "BirthPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePersonalDataChanges_CountryId",
                table: "EmployeePersonalDataChanges",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePersonalDataChanges_EmployeeId",
                table: "EmployeePersonalDataChanges",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePersonalDataChanges_GovernorateId",
                table: "EmployeePersonalDataChanges",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePersonalDataChanges_MaritalStatusId",
                table: "EmployeePersonalDataChanges",
                column: "MaritalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePersonalDataChanges_NationalityId",
                table: "EmployeePersonalDataChanges",
                column: "NationalityId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePersonalDataChanges_ReligionId",
                table: "EmployeePersonalDataChanges",
                column: "ReligionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePersonalDataChanges_TenantId_EmployeeId_WorkflowStatus",
                table: "EmployeePersonalDataChanges",
                columns: new[] { "TenantId", "EmployeeId", "WorkflowStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePersonalDataChanges_TitleId",
                table: "EmployeePersonalDataChanges",
                column: "TitleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeePersonalDataChanges");
        }
    }
}
