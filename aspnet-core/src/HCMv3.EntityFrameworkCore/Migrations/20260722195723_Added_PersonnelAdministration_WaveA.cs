using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_PersonnelAdministration_WaveA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FirstNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SecondNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ThirdNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FirstNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SecondNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ThirdNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FullNameEn = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FullNameAr = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    MaritalStatusId = table.Column<int>(type: "int", nullable: true),
                    NationalityId = table.Column<int>(type: "int", nullable: false),
                    ReligionId = table.Column<int>(type: "int", nullable: true),
                    FlatNumn = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    FloorNum = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    BuildingNum = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    StreetNum = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    BlockNum = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LaneNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    PACINumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    City = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    GovernorateId = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    Telephone = table.Column<long>(type: "bigint", nullable: false),
                    Mobile = table.Column<long>(type: "bigint", nullable: false),
                    CurrentAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PermanentAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PermanentTelephone = table.Column<long>(type: "bigint", nullable: false),
                    FatherNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FatherNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MotherNameEn = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MotherNameAr = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    QualificationId = table.Column<int>(type: "int", nullable: true),
                    BloodGroup = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CivilID = table.Column<long>(type: "bigint", nullable: false),
                    LoginID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    AccessID = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AlternateEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BirthPlaceId = table.Column<int>(type: "int", nullable: true),
                    PreviousEmpCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReferenceCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EmergencyContact = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmergencyNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    EmergencyRelation = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EmergencyContactAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CriticalIllness = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    BusinessLandline = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BusinessExtension = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    RecruitmentTypeId = table.Column<int>(type: "int", nullable: true),
                    Photo = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    JoiningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProbationDays = table.Column<int>(type: "int", nullable: false),
                    ProbationEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_Cities_BirthPlaceId",
                        column: x => x.BirthPlaceId,
                        principalTable: "Cities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Governorates_GovernorateId",
                        column: x => x.GovernorateId,
                        principalTable: "Governorates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_MaritalStatuses_MaritalStatusId",
                        column: x => x.MaritalStatusId,
                        principalTable: "MaritalStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Nationalities_NationalityId",
                        column: x => x.NationalityId,
                        principalTable: "Nationalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Employees_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_RecruitmentTypes_RecruitmentTypeId",
                        column: x => x.RecruitmentTypeId,
                        principalTable: "RecruitmentTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Religions_ReligionId",
                        column: x => x.ReligionId,
                        principalTable: "Religions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Employees_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCertifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Course = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    InstitutionId = table.Column<int>(type: "int", nullable: false),
                    Duration = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Certificate = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Grade = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeCertifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCertifications_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeCertifications_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DateTypeId = table.Column<int>(type: "int", nullable: true),
                    DateData = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DayData = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeDates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDates_DateTypes_DateTypeId",
                        column: x => x.DateTypeId,
                        principalTable: "DateTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeDates_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDelegations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DelegateToId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeDelegations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDelegations_Employees_DelegateToId",
                        column: x => x.DelegateToId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeDelegations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DocumentTypeId = table.Column<int>(type: "int", nullable: false),
                    DocumentNum = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    IssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_DocumentTypes_DocumentTypeId",
                        column: x => x.DocumentTypeId,
                        principalTable: "DocumentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDottedLineManagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeDottedLineManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDottedLineManagers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeDottedLineManagers_Employees_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeEducations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EducationId = table.Column<int>(type: "int", nullable: false),
                    EducationBranchId = table.Column<int>(type: "int", nullable: false),
                    UniversityId = table.Column<int>(type: "int", nullable: true),
                    InstitutionId = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    DurationOfCourse = table.Column<double>(type: "float", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    YearOfPassing = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeEducations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeEducations_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeEducations_EducationBranches_EducationBranchId",
                        column: x => x.EducationBranchId,
                        principalTable: "EducationBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeEducations_Educations_EducationId",
                        column: x => x.EducationId,
                        principalTable: "Educations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeEducations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeEducations_Institutions_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "Institutions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeEducations_Universities_UniversityId",
                        column: x => x.UniversityId,
                        principalTable: "Universities",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeManagers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ManagerId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeManagers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeManagers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeManagers_Employees_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    RelationshipTypeId = table.Column<int>(type: "int", nullable: false),
                    RelatedEmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRelationships_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeRelationships_Employees_RelatedEmployeeId",
                        column: x => x.RelatedEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeRelationships_RelationshipTypes_RelationshipTypeId",
                        column: x => x.RelationshipTypeId,
                        principalTable: "RelationshipTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSignatures",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Signature = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeSignatures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSignatures_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeVisaInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AuthorizedSign = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CurrentFileNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CurrentContractNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CurrentLicenseNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OldFileNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OldContractNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OldLicenseNo = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    MOIFileNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PreviousResidencyPeriod = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CurrentResidencyPeriod = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    OldContractSalary = table.Column<double>(type: "float", nullable: false),
                    CurrentContractSalary = table.Column<double>(type: "float", nullable: false),
                    NewProfesion = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NewProfessionAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OldProfession = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OldCompany = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OldSponser = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TypeOfPassport = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IssuePlacePassport = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PassportIssueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PassportExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TerminationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VisaNum = table.Column<long>(type: "bigint", nullable: false),
                    ResidencyNum = table.Column<long>(type: "bigint", nullable: false),
                    PassportTypeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeVisaInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeVisaInfos_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeVisaInfos_PassportTypes_PassportTypeId",
                        column: x => x.PassportTypeId,
                        principalTable: "PassportTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWorkExperiences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Position = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReasonForLeaving = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeWorkExperiences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWorkExperiences_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocumentMvmts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeDocumentId = table.Column<int>(type: "int", nullable: false),
                    WithdrawDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeDocumentMvmts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocumentMvmts_EmployeeDocuments_EmployeeDocumentId",
                        column: x => x.EmployeeDocumentId,
                        principalTable: "EmployeeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCertifications_EmployeeId",
                table: "EmployeeCertifications",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCertifications_InstitutionId",
                table: "EmployeeCertifications",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDates_DateTypeId",
                table: "EmployeeDates",
                column: "DateTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDates_EmployeeId",
                table: "EmployeeDates",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDelegations_DelegateToId",
                table: "EmployeeDelegations",
                column: "DelegateToId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDelegations_EmployeeId",
                table: "EmployeeDelegations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocumentMvmts_EmployeeDocumentId",
                table: "EmployeeDocumentMvmts",
                column: "EmployeeDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_DocumentTypeId",
                table: "EmployeeDocuments",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_EmployeeId",
                table: "EmployeeDocuments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDottedLineManagers_EmployeeId",
                table: "EmployeeDottedLineManagers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDottedLineManagers_ManagerId",
                table: "EmployeeDottedLineManagers",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_CountryId",
                table: "EmployeeEducations",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_EducationBranchId",
                table: "EmployeeEducations",
                column: "EducationBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_EducationId",
                table: "EmployeeEducations",
                column: "EducationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_EmployeeId",
                table: "EmployeeEducations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_InstitutionId",
                table: "EmployeeEducations",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeEducations_UniversityId",
                table: "EmployeeEducations",
                column: "UniversityId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeManagers_EmployeeId",
                table: "EmployeeManagers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeManagers_ManagerId",
                table: "EmployeeManagers",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRelationships_EmployeeId",
                table: "EmployeeRelationships",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRelationships_RelatedEmployeeId",
                table: "EmployeeRelationships",
                column: "RelatedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRelationships_RelationshipTypeId",
                table: "EmployeeRelationships",
                column: "RelationshipTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_BirthPlaceId",
                table: "Employees",
                column: "BirthPlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CountryId",
                table: "Employees",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_GovernorateId",
                table: "Employees",
                column: "GovernorateId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_MaritalStatusId",
                table: "Employees",
                column: "MaritalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_NationalityId",
                table: "Employees",
                column: "NationalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_QualificationId",
                table: "Employees",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_RecruitmentTypeId",
                table: "Employees",
                column: "RecruitmentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_ReligionId",
                table: "Employees",
                column: "ReligionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_TitleId",
                table: "Employees",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSignatures_EmployeeId",
                table: "EmployeeSignatures",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeVisaInfos_EmployeeId",
                table: "EmployeeVisaInfos",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeVisaInfos_PassportTypeId",
                table: "EmployeeVisaInfos",
                column: "PassportTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWorkExperiences_EmployeeId",
                table: "EmployeeWorkExperiences",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeCertifications");

            migrationBuilder.DropTable(
                name: "EmployeeDates");

            migrationBuilder.DropTable(
                name: "EmployeeDelegations");

            migrationBuilder.DropTable(
                name: "EmployeeDocumentMvmts");

            migrationBuilder.DropTable(
                name: "EmployeeDottedLineManagers");

            migrationBuilder.DropTable(
                name: "EmployeeEducations");

            migrationBuilder.DropTable(
                name: "EmployeeManagers");

            migrationBuilder.DropTable(
                name: "EmployeeRelationships");

            migrationBuilder.DropTable(
                name: "EmployeeSignatures");

            migrationBuilder.DropTable(
                name: "EmployeeVisaInfos");

            migrationBuilder.DropTable(
                name: "EmployeeWorkExperiences");

            migrationBuilder.DropTable(
                name: "EmployeeDocuments");

            migrationBuilder.DropTable(
                name: "Employees");
        }
    }
}
