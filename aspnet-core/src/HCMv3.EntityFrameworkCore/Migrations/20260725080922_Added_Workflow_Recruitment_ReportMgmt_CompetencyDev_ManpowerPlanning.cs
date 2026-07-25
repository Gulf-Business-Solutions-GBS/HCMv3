using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_Workflow_Recruitment_ReportMgmt_CompetencyDev_ManpowerPlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Applicants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TitleId = table.Column<int>(type: "int", nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FirstNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FirstNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SecondNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    SecondNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ThirdNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ThirdNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LastNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NationalityId = table.Column<int>(type: "int", nullable: true),
                    JobTitleId = table.Column<int>(type: "int", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    MaritalStatusId = table.Column<int>(type: "int", nullable: true),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JoiningDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProbationEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Photo = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    Telephone = table.Column<long>(type: "bigint", nullable: false),
                    Mobile = table.Column<long>(type: "bigint", nullable: false),
                    CurrentAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PermanentAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DrivingLicense = table.Column<int>(type: "int", nullable: false),
                    ReligionId = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    QualificationId = table.Column<int>(type: "int", nullable: true),
                    BloodGroup = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CivilID = table.Column<long>(type: "bigint", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PassportNum = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PlaceOfBirth = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Address1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletionPoints = table.Column<int>(type: "int", nullable: false),
                    IsOnbdStarted = table.Column<bool>(type: "bit", nullable: false),
                    IsAgency = table.Column<bool>(type: "bit", nullable: false),
                    IsOnbdInit = table.Column<bool>(type: "bit", nullable: false),
                    OnbCoordinatorId = table.Column<int>(type: "int", nullable: true),
                    OnbBuddyId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Applicants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Applicants_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applicants_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applicants_MaritalStatuses_MaritalStatusId",
                        column: x => x.MaritalStatusId,
                        principalTable: "MaritalStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applicants_Nationalities_NationalityId",
                        column: x => x.NationalityId,
                        principalTable: "Nationalities",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applicants_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applicants_Religions_ReligionId",
                        column: x => x.ReligionId,
                        principalTable: "Religions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Applicants_Titles_TitleId",
                        column: x => x.TitleId,
                        principalTable: "Titles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CDPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Closed = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_CDPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CDRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_CDRatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyByJobTitles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    JobTitleId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CompetencyByJobTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyByJobTitles_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BackColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ForeColor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("PK_CompetencyTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DomainTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_DomainTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManpowerPlanningPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ManpowerPlanningPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnbGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
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
                    table.PrimaryKey("PK_OnbGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnbTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    TemplateTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_OnbTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnbVideoLinks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Link = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_OnbVideoLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Vacant = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    JobTitleId = table.Column<int>(type: "int", nullable: true),
                    EmployeeGroupId = table.Column<int>(type: "int", nullable: true),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: true),
                    PayrollAreaId = table.Column<int>(type: "int", nullable: true),
                    WorkLocationId = table.Column<int>(type: "int", nullable: true),
                    CostCenterId = table.Column<int>(type: "int", nullable: true),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: true),
                    MinSalary = table.Column<double>(type: "float", nullable: false),
                    MidSalary = table.Column<double>(type: "float", nullable: false),
                    MaxSalary = table.Column<double>(type: "float", nullable: false),
                    IsBudgeted = table.Column<bool>(type: "bit", nullable: false),
                    FTELimit = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Positions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Positions_AbpOrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_EmployeeGroups_EmployeeGroupId",
                        column: x => x.EmployeeGroupId,
                        principalTable: "EmployeeGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_PayrollAreas_PayrollAreaId",
                        column: x => x.PayrollAreaId,
                        principalTable: "PayrollAreas",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Positions_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PrintReportPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PrintReportPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintReportPermissions_AbpRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AbpRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrintReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ReportCategory = table.Column<int>(type: "int", nullable: false),
                    CustomPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReportCreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_PrintReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_ReportMasters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ReportId = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ReportSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReportTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_ReportTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RequestMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WorkflowSteps = table.Column<int>(type: "int", nullable: false),
                    SecurityLevel = table.Column<int>(type: "int", nullable: false),
                    AutoClose = table.Column<bool>(type: "bit", nullable: false),
                    AutoCloseInDays = table.Column<int>(type: "int", nullable: false),
                    DefaultApproverId = table.Column<int>(type: "int", nullable: true),
                    IsCommentRequire = table.Column<bool>(type: "bit", nullable: false),
                    DueInDays = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RequestMasters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestMasters_Employees_DefaultApproverId",
                        column: x => x.DefaultApproverId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WorkflowGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    WorkflowStep = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: true),
                    RequestType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RequestNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RequestStatus = table.Column<int>(type: "int", nullable: false),
                    ApprovalProcessTime = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_WorkflowRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowRequests_Employees_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WorkflowRoleGroup = table.Column<int>(type: "int", nullable: false),
                    WorkflowRoleOrgId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_WorkflowRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowRoles_OrganizationUnitTypes_WorkflowRoleOrgId",
                        column: x => x.WorkflowRoleOrgId,
                        principalTable: "OrganizationUnitTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicantCVs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    JobTitleId = table.Column<int>(type: "int", nullable: true),
                    CVName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UploadDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_ApplicantCVs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicantCVs_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApplicantCVs_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CDForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CDPeriodId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: false),
                    ModeratedScore = table.Column<double>(type: "float", nullable: false),
                    EmployeeSignedOff = table.Column<bool>(type: "bit", nullable: false),
                    ManagerSignedOff = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_CDForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CDForms_CDPeriods_CDPeriodId",
                        column: x => x.CDPeriodId,
                        principalTable: "CDPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CDForms_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Competencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CompetencyTypeId = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_Competencies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Competencies_CompetencyTypes_CompetencyTypeId",
                        column: x => x.CompetencyTypeId,
                        principalTable: "CompetencyTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManpowerPlannings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: false),
                    PeriodId = table.Column<int>(type: "int", nullable: false),
                    GrandTotalAmount = table.Column<double>(type: "float", nullable: false),
                    RequestComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ManpowerPlannings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManpowerPlannings_AbpOrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManpowerPlannings_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ManpowerPlannings_ManpowerPlanningPeriods_PeriodId",
                        column: x => x.PeriodId,
                        principalTable: "ManpowerPlanningPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vacancies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequestionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JobTitleId = table.Column<int>(type: "int", nullable: true),
                    PublishOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecruiterId = table.Column<int>(type: "int", nullable: true),
                    TemplateOnbGroupId = table.Column<int>(type: "int", nullable: true),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: true),
                    IsBudgeted = table.Column<bool>(type: "bit", nullable: false),
                    VacancyStatus = table.Column<int>(type: "int", nullable: false),
                    SLADaysToFill = table.Column<int>(type: "int", nullable: false),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VacancyTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WorkLocationId = table.Column<int>(type: "int", nullable: true),
                    TotalVacancy = table.Column<int>(type: "int", nullable: false),
                    MonthlySalaryRange = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmploymentType = table.Column<int>(type: "int", nullable: false),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: false),
                    QualificationId = table.Column<int>(type: "int", nullable: true),
                    OnbGroupId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Vacancies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vacancies_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vacancies_OnbGroups_OnbGroupId",
                        column: x => x.OnbGroupId,
                        principalTable: "OnbGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vacancies_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Vacancies_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OnbTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TaskNameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TaskNameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OnbTemplateId = table.Column<int>(type: "int", nullable: true),
                    Guideline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDays = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_OnbTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnbTasks_OnbTemplates_OnbTemplateId",
                        column: x => x.OnbTemplateId,
                        principalTable: "OnbTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OnbTemplateDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    OnbTemplateId = table.Column<int>(type: "int", nullable: false),
                    TemplateId = table.Column<long>(type: "bigint", nullable: false),
                    TemplateData = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: true),
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
                    table.PrimaryKey("PK_OnbTemplateDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnbTemplateDetails_OnbTemplates_OnbTemplateId",
                        column: x => x.OnbTemplateId,
                        principalTable: "OnbTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PositionRelationships",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PositionRelationships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PositionRelationships_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PositionRelationships_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrintReportPermissionDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PrintReportPermissionId = table.Column<int>(type: "int", nullable: false),
                    PrintReportId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PrintReportPermissionDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrintReportPermissionDetails_PrintReportPermissions_PrintReportPermissionId",
                        column: x => x.PrintReportPermissionId,
                        principalTable: "PrintReportPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrintReportPermissionDetails_PrintReports_PrintReportId",
                        column: x => x.PrintReportId,
                        principalTable: "PrintReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReportMasterId = table.Column<int>(type: "int", nullable: false),
                    ReportSettingId = table.Column<int>(type: "int", nullable: true),
                    DrillDownLevel = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ReportDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportDetails_ReportMasters_ReportMasterId",
                        column: x => x.ReportMasterId,
                        principalTable: "ReportMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportDetails_ReportSettings_ReportSettingId",
                        column: x => x.ReportSettingId,
                        principalTable: "ReportSettings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReportSettingDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReportSettingId = table.Column<int>(type: "int", nullable: false),
                    FieldName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CaptionEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CaptionAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Order = table.Column<int>(type: "int", nullable: false),
                    ColumnDataType = table.Column<int>(type: "int", nullable: false),
                    DisplayFormat = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Width = table.Column<double>(type: "float", nullable: false),
                    VisibleInEn = table.Column<bool>(type: "bit", nullable: false),
                    VisibleInAr = table.Column<bool>(type: "bit", nullable: false),
                    DataHAlignment = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ReportSettingDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportSettingDetails_ReportSettings_ReportSettingId",
                        column: x => x.ReportSettingId,
                        principalTable: "ReportSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReportInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReportName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReportTypeId = table.Column<int>(type: "int", nullable: false),
                    DomainTypeId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ReportQuery = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ReportInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportInfos_DomainTypes_DomainTypeId",
                        column: x => x.DomainTypeId,
                        principalTable: "DomainTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReportInfos_ReportTypes_ReportTypeId",
                        column: x => x.ReportTypeId,
                        principalTable: "ReportTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowGroupDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WorkflowGroupId = table.Column<int>(type: "int", nullable: false),
                    RequestMasterId = table.Column<int>(type: "int", nullable: false),
                    RequestTypeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequestSubTypeId = table.Column<int>(type: "int", nullable: true),
                    CustomSubTypeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_WorkflowGroupDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowGroupDetails_RequestMasters_RequestMasterId",
                        column: x => x.RequestMasterId,
                        principalTable: "RequestMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowGroupDetails_WorkflowGroups_WorkflowGroupId",
                        column: x => x.WorkflowGroupId,
                        principalTable: "WorkflowGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WorkflowRequestId = table.Column<int>(type: "int", nullable: false),
                    ActionById = table.Column<int>(type: "int", nullable: true),
                    RequestActionComment = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ActionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    WorkflowStep = table.Column<int>(type: "int", nullable: false),
                    ActionDays = table.Column<int>(type: "int", nullable: false),
                    Platform = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RequestLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestLogs_Employees_ActionById",
                        column: x => x.ActionById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequestLogs_WorkflowRequests_WorkflowRequestId",
                        column: x => x.WorkflowRequestId,
                        principalTable: "WorkflowRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WFReadLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WorkflowRequestId = table.Column<int>(type: "int", nullable: false),
                    ReadDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    User = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_WFReadLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WFReadLogs_WorkflowRequests_WorkflowRequestId",
                        column: x => x.WorkflowRequestId,
                        principalTable: "WorkflowRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkFlowApprovers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WorkflowRequestId = table.Column<int>(type: "int", nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WorkFlowApprovers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkFlowApprovers_Employees_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkFlowApprovers_WorkflowRequests_WorkflowRequestId",
                        column: x => x.WorkflowRequestId,
                        principalTable: "WorkflowRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowRemarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WorkflowRequestId = table.Column<int>(type: "int", nullable: false),
                    CCReceiverId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_WorkflowRemarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowRemarks_Employees_CCReceiverId",
                        column: x => x.CCReceiverId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkflowRemarks_WorkflowRequests_WorkflowRequestId",
                        column: x => x.WorkflowRequestId,
                        principalTable: "WorkflowRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RequestMasterId = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    WorkflowRoleId = table.Column<int>(type: "int", nullable: true),
                    ShowPayment = table.Column<bool>(type: "bit", nullable: false),
                    CanSendBack = table.Column<bool>(type: "bit", nullable: false),
                    CanReject = table.Column<bool>(type: "bit", nullable: false),
                    ShouldEscalate = table.Column<bool>(type: "bit", nullable: false),
                    EscalateAfterDays = table.Column<int>(type: "int", nullable: false),
                    EscalateNextLevel = table.Column<bool>(type: "bit", nullable: false),
                    EscalateWorkflowRoleId = table.Column<int>(type: "int", nullable: true),
                    EscalateWorkflowRoleGroup = table.Column<int>(type: "int", nullable: false),
                    EscalateWorkflowRoleOrgId = table.Column<int>(type: "int", nullable: true),
                    EnableReadLog = table.Column<bool>(type: "bit", nullable: false),
                    Person = table.Column<int>(type: "int", nullable: false),
                    ApproverPerson = table.Column<int>(type: "int", nullable: false),
                    WorkflowRoleGroup = table.Column<int>(type: "int", nullable: false),
                    WorkflowRoleOrgId = table.Column<int>(type: "int", nullable: true),
                    ApprovalRequire = table.Column<int>(type: "int", nullable: false),
                    IsCommentRequire = table.Column<bool>(type: "bit", nullable: false),
                    SignatoryStep = table.Column<bool>(type: "bit", nullable: false),
                    ReminderDays = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RequestDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestDetails_OrganizationUnitTypes_EscalateWorkflowRoleOrgId",
                        column: x => x.EscalateWorkflowRoleOrgId,
                        principalTable: "OrganizationUnitTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequestDetails_OrganizationUnitTypes_WorkflowRoleOrgId",
                        column: x => x.WorkflowRoleOrgId,
                        principalTable: "OrganizationUnitTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequestDetails_RequestMasters_RequestMasterId",
                        column: x => x.RequestMasterId,
                        principalTable: "RequestMasters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestDetails_WorkflowRoles_EscalateWorkflowRoleId",
                        column: x => x.EscalateWorkflowRoleId,
                        principalTable: "WorkflowRoles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequestDetails_WorkflowRoles_WorkflowRoleId",
                        column: x => x.WorkflowRoleId,
                        principalTable: "WorkflowRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CVLanguages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ApplicantCVId = table.Column<int>(type: "int", nullable: false),
                    LanguageInfoId = table.Column<int>(type: "int", nullable: true),
                    Proficiency = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CVLanguages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVLanguages_ApplicantCVs_ApplicantCVId",
                        column: x => x.ApplicantCVId,
                        principalTable: "ApplicantCVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CVLanguages_LanguageInfos_LanguageInfoId",
                        column: x => x.LanguageInfoId,
                        principalTable: "LanguageInfos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CVQualifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ApplicantCVId = table.Column<int>(type: "int", nullable: false),
                    QualificationId = table.Column<int>(type: "int", nullable: true),
                    University = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Percentage = table.Column<double>(type: "float", nullable: false),
                    PassedYear = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CVQualifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVQualifications_ApplicantCVs_ApplicantCVId",
                        column: x => x.ApplicantCVId,
                        principalTable: "ApplicantCVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CVQualifications_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CVSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ApplicantCVId = table.Column<int>(type: "int", nullable: false),
                    Skill = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Proficiency = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CVSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVSkills_ApplicantCVs_ApplicantCVId",
                        column: x => x.ApplicantCVId,
                        principalTable: "ApplicantCVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CVWorkExps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ApplicantCVId = table.Column<int>(type: "int", nullable: false),
                    Company = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Position = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Responsibilities = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_CVWorkExps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CVWorkExps_ApplicantCVs_ApplicantCVId",
                        column: x => x.ApplicantCVId,
                        principalTable: "ApplicantCVs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CDAnnualYearReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CDFormId = table.Column<int>(type: "int", nullable: false),
                    CompetencyId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmployeeComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManagerComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CDRatingId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_CDAnnualYearReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CDAnnualYearReviews_CDForms_CDFormId",
                        column: x => x.CDFormId,
                        principalTable: "CDForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CDAnnualYearReviews_CDRatings_CDRatingId",
                        column: x => x.CDRatingId,
                        principalTable: "CDRatings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CDAnnualYearReviews_Competencies_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Competencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CDMidYearReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CDFormId = table.Column<int>(type: "int", nullable: false),
                    CompetencyId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmployeeComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManagerComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_CDMidYearReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CDMidYearReviews_CDForms_CDFormId",
                        column: x => x.CDFormId,
                        principalTable: "CDForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CDMidYearReviews_Competencies_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Competencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CDPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CDFormId = table.Column<int>(type: "int", nullable: false),
                    CompetencyId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    EmployeeComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ManagerComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_CDPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CDPlans_CDForms_CDFormId",
                        column: x => x.CDFormId,
                        principalTable: "CDForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CDPlans_Competencies_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Competencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetencyByJobTitleDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CompetencyByJobTitleId = table.Column<int>(type: "int", nullable: false),
                    CompetencyId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
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
                    table.PrimaryKey("PK_CompetencyByJobTitleDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetencyByJobTitleDetails_Competencies_CompetencyId",
                        column: x => x.CompetencyId,
                        principalTable: "Competencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetencyByJobTitleDetails_CompetencyByJobTitles_CompetencyByJobTitleId",
                        column: x => x.CompetencyByJobTitleId,
                        principalTable: "CompetencyByJobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManpowerPlanningDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ManpowerPlanningId = table.Column<int>(type: "int", nullable: false),
                    JobTitleId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: true),
                    HolderId = table.Column<int>(type: "int", nullable: true),
                    January = table.Column<double>(type: "float", nullable: false),
                    February = table.Column<double>(type: "float", nullable: false),
                    March = table.Column<double>(type: "float", nullable: false),
                    April = table.Column<double>(type: "float", nullable: false),
                    May = table.Column<double>(type: "float", nullable: false),
                    June = table.Column<double>(type: "float", nullable: false),
                    July = table.Column<double>(type: "float", nullable: false),
                    August = table.Column<double>(type: "float", nullable: false),
                    September = table.Column<double>(type: "float", nullable: false),
                    October = table.Column<double>(type: "float", nullable: false),
                    November = table.Column<double>(type: "float", nullable: false),
                    December = table.Column<double>(type: "float", nullable: false),
                    TotalAmount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_ManpowerPlanningDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManpowerPlanningDetails_Employees_HolderId",
                        column: x => x.HolderId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ManpowerPlanningDetails_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManpowerPlanningDetails_ManpowerPlannings_ManpowerPlanningId",
                        column: x => x.ManpowerPlanningId,
                        principalTable: "ManpowerPlannings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManpowerPlanningDetails_Positions_PositionId",
                        column: x => x.PositionId,
                        principalTable: "Positions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ApplicationStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    VacancyId = table.Column<int>(type: "int", nullable: true),
                    ApplicantCVId = table.Column<int>(type: "int", nullable: true),
                    AppStatus = table.Column<int>(type: "int", nullable: false),
                    CoverLetter = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_ApplicationStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationStatuses_ApplicantCVs_ApplicantCVId",
                        column: x => x.ApplicantCVId,
                        principalTable: "ApplicantCVs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ApplicationStatuses_Vacancies_VacancyId",
                        column: x => x.VacancyId,
                        principalTable: "Vacancies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OnbApplicantTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ApplicantId = table.Column<int>(type: "int", nullable: false),
                    OnbTaskId = table.Column<int>(type: "int", nullable: true),
                    DueDays = table.Column<int>(type: "int", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TemplateUserData = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_OnbApplicantTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnbApplicantTasks_Applicants_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "Applicants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnbApplicantTasks_OnbTasks_OnbTaskId",
                        column: x => x.OnbTaskId,
                        principalTable: "OnbTasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OnbGroupDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    OnbGroupId = table.Column<int>(type: "int", nullable: false),
                    OnbTaskId = table.Column<int>(type: "int", nullable: true),
                    JobTitleId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_OnbGroupDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnbGroupDetails_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OnbGroupDetails_OnbGroups_OnbGroupId",
                        column: x => x.OnbGroupId,
                        principalTable: "OnbGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OnbGroupDetails_OnbTasks_OnbTaskId",
                        column: x => x.OnbTaskId,
                        principalTable: "OnbTasks",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReportInfoDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ReportInfoId = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EntityName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NodeAlias = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PropertyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PropertyDisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PropertyType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BaseType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AssemblyName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ParentEntity = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ParentAssembly = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsAssociate = table.Column<bool>(type: "bit", nullable: false),
                    AssociateEntity = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AssociateProperty = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_ReportInfoDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReportInfoDetails_ReportInfos_ReportInfoId",
                        column: x => x.ReportInfoId,
                        principalTable: "ReportInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowGroupFieldAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WorkflowGroupDetailId = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    Field = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsEditable = table.Column<bool>(type: "bit", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WorkflowGroupFieldAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowGroupFieldAccesses_WorkflowGroupDetails_WorkflowGroupDetailId",
                        column: x => x.WorkflowGroupDetailId,
                        principalTable: "WorkflowGroupDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RequestDetailParallels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RequestDetailId = table.Column<int>(type: "int", nullable: false),
                    JobTitleId = table.Column<int>(type: "int", nullable: true),
                    WorkflowRoleId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_RequestDetailParallels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RequestDetailParallels_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RequestDetailParallels_RequestDetails_RequestDetailId",
                        column: x => x.RequestDetailId,
                        principalTable: "RequestDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RequestDetailParallels_WorkflowRoles_WorkflowRoleId",
                        column: x => x.WorkflowRoleId,
                        principalTable: "WorkflowRoles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InterviewDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ApplicationStatusId = table.Column<int>(type: "int", nullable: false),
                    InterviewDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InterviewTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Place = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Interviewer1Id = table.Column<int>(type: "int", nullable: true),
                    Interviewer2Id = table.Column<int>(type: "int", nullable: true),
                    Interviewer3Id = table.Column<int>(type: "int", nullable: true),
                    Comments1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_InterviewDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InterviewDetails_ApplicationStatuses_ApplicationStatusId",
                        column: x => x.ApplicationStatusId,
                        principalTable: "ApplicationStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OnbAppTaskComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    OnbApplicantTasksId = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    IsByAdmin = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_OnbAppTaskComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OnbAppTaskComments_OnbApplicantTasks_OnbApplicantTasksId",
                        column: x => x.OnbApplicantTasksId,
                        principalTable: "OnbApplicantTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantCVs_ApplicantId",
                table: "ApplicantCVs",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantCVs_JobTitleId",
                table: "ApplicantCVs",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_CountryId",
                table: "Applicants",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_JobTitleId",
                table: "Applicants",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_MaritalStatusId",
                table: "Applicants",
                column: "MaritalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_NationalityId",
                table: "Applicants",
                column: "NationalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_QualificationId",
                table: "Applicants",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_ReligionId",
                table: "Applicants",
                column: "ReligionId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_TitleId",
                table: "Applicants",
                column: "TitleId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatuses_ApplicantCVId",
                table: "ApplicationStatuses",
                column: "ApplicantCVId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationStatuses_VacancyId",
                table: "ApplicationStatuses",
                column: "VacancyId");

            migrationBuilder.CreateIndex(
                name: "IX_CDAnnualYearReviews_CDFormId",
                table: "CDAnnualYearReviews",
                column: "CDFormId");

            migrationBuilder.CreateIndex(
                name: "IX_CDAnnualYearReviews_CDRatingId",
                table: "CDAnnualYearReviews",
                column: "CDRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_CDAnnualYearReviews_CompetencyId",
                table: "CDAnnualYearReviews",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CDForms_CDPeriodId",
                table: "CDForms",
                column: "CDPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_CDForms_EmployeeId",
                table: "CDForms",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_CDMidYearReviews_CDFormId",
                table: "CDMidYearReviews",
                column: "CDFormId");

            migrationBuilder.CreateIndex(
                name: "IX_CDMidYearReviews_CompetencyId",
                table: "CDMidYearReviews",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CDPlans_CDFormId",
                table: "CDPlans",
                column: "CDFormId");

            migrationBuilder.CreateIndex(
                name: "IX_CDPlans_CompetencyId",
                table: "CDPlans",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_Competencies_CompetencyTypeId",
                table: "Competencies",
                column: "CompetencyTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyByJobTitleDetails_CompetencyByJobTitleId",
                table: "CompetencyByJobTitleDetails",
                column: "CompetencyByJobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyByJobTitleDetails_CompetencyId",
                table: "CompetencyByJobTitleDetails",
                column: "CompetencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetencyByJobTitles_JobTitleId",
                table: "CompetencyByJobTitles",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_CVLanguages_ApplicantCVId",
                table: "CVLanguages",
                column: "ApplicantCVId");

            migrationBuilder.CreateIndex(
                name: "IX_CVLanguages_LanguageInfoId",
                table: "CVLanguages",
                column: "LanguageInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_CVQualifications_ApplicantCVId",
                table: "CVQualifications",
                column: "ApplicantCVId");

            migrationBuilder.CreateIndex(
                name: "IX_CVQualifications_QualificationId",
                table: "CVQualifications",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_CVSkills_ApplicantCVId",
                table: "CVSkills",
                column: "ApplicantCVId");

            migrationBuilder.CreateIndex(
                name: "IX_CVWorkExps_ApplicantCVId",
                table: "CVWorkExps",
                column: "ApplicantCVId");

            migrationBuilder.CreateIndex(
                name: "IX_InterviewDetails_ApplicationStatusId",
                table: "InterviewDetails",
                column: "ApplicationStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ManpowerPlanningDetails_HolderId",
                table: "ManpowerPlanningDetails",
                column: "HolderId");

            migrationBuilder.CreateIndex(
                name: "IX_ManpowerPlanningDetails_JobTitleId",
                table: "ManpowerPlanningDetails",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_ManpowerPlanningDetails_ManpowerPlanningId",
                table: "ManpowerPlanningDetails",
                column: "ManpowerPlanningId");

            migrationBuilder.CreateIndex(
                name: "IX_ManpowerPlanningDetails_PositionId",
                table: "ManpowerPlanningDetails",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_ManpowerPlannings_EmployeeId",
                table: "ManpowerPlannings",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ManpowerPlannings_OrganizationUnitId",
                table: "ManpowerPlannings",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ManpowerPlannings_PeriodId",
                table: "ManpowerPlannings",
                column: "PeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_OnbApplicantTasks_ApplicantId",
                table: "OnbApplicantTasks",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_OnbApplicantTasks_OnbTaskId",
                table: "OnbApplicantTasks",
                column: "OnbTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_OnbAppTaskComments_OnbApplicantTasksId",
                table: "OnbAppTaskComments",
                column: "OnbApplicantTasksId");

            migrationBuilder.CreateIndex(
                name: "IX_OnbGroupDetails_JobTitleId",
                table: "OnbGroupDetails",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_OnbGroupDetails_OnbGroupId",
                table: "OnbGroupDetails",
                column: "OnbGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_OnbGroupDetails_OnbTaskId",
                table: "OnbGroupDetails",
                column: "OnbTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_OnbTasks_OnbTemplateId",
                table: "OnbTasks",
                column: "OnbTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_OnbTemplateDetails_OnbTemplateId",
                table: "OnbTemplateDetails",
                column: "OnbTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionRelationships_EmployeeId",
                table: "PositionRelationships",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PositionRelationships_PositionId",
                table: "PositionRelationships",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_CostCenterId",
                table: "Positions",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_EmployeeGroupId",
                table: "Positions",
                column: "EmployeeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_EmployeeSubGroupId",
                table: "Positions",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_JobTitleId",
                table: "Positions",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_OrganizationUnitId",
                table: "Positions",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_PayrollAreaId",
                table: "Positions",
                column: "PayrollAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_WorkLocationId",
                table: "Positions",
                column: "WorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintReportPermissionDetails_PrintReportId",
                table: "PrintReportPermissionDetails",
                column: "PrintReportId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintReportPermissionDetails_PrintReportPermissionId",
                table: "PrintReportPermissionDetails",
                column: "PrintReportPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintReportPermissions_RoleId",
                table: "PrintReportPermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDetails_ReportMasterId",
                table: "ReportDetails",
                column: "ReportMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportDetails_ReportSettingId",
                table: "ReportDetails",
                column: "ReportSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportInfoDetails_ReportInfoId",
                table: "ReportInfoDetails",
                column: "ReportInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportInfos_DomainTypeId",
                table: "ReportInfos",
                column: "DomainTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportInfos_ReportTypeId",
                table: "ReportInfos",
                column: "ReportTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportSettingDetails_ReportSettingId",
                table: "ReportSettingDetails",
                column: "ReportSettingId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetailParallels_JobTitleId",
                table: "RequestDetailParallels",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetailParallels_RequestDetailId",
                table: "RequestDetailParallels",
                column: "RequestDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetailParallels_WorkflowRoleId",
                table: "RequestDetailParallels",
                column: "WorkflowRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_EscalateWorkflowRoleId",
                table: "RequestDetails",
                column: "EscalateWorkflowRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_EscalateWorkflowRoleOrgId",
                table: "RequestDetails",
                column: "EscalateWorkflowRoleOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_RequestMasterId",
                table: "RequestDetails",
                column: "RequestMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_WorkflowRoleId",
                table: "RequestDetails",
                column: "WorkflowRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestDetails_WorkflowRoleOrgId",
                table: "RequestDetails",
                column: "WorkflowRoleOrgId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_ActionById",
                table: "RequestLogs",
                column: "ActionById");

            migrationBuilder.CreateIndex(
                name: "IX_RequestLogs_WorkflowRequestId",
                table: "RequestLogs",
                column: "WorkflowRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_RequestMasters_DefaultApproverId",
                table: "RequestMasters",
                column: "DefaultApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_JobTitleId",
                table: "Vacancies",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_OnbGroupId",
                table: "Vacancies",
                column: "OnbGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_QualificationId",
                table: "Vacancies",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_Vacancies_WorkLocationId",
                table: "Vacancies",
                column: "WorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_WFReadLogs_WorkflowRequestId",
                table: "WFReadLogs",
                column: "WorkflowRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkFlowApprovers_ApproverId",
                table: "WorkFlowApprovers",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkFlowApprovers_WorkflowRequestId",
                table: "WorkFlowApprovers",
                column: "WorkflowRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowGroupDetails_RequestMasterId",
                table: "WorkflowGroupDetails",
                column: "RequestMasterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowGroupDetails_WorkflowGroupId",
                table: "WorkflowGroupDetails",
                column: "WorkflowGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowGroupFieldAccesses_WorkflowGroupDetailId",
                table: "WorkflowGroupFieldAccesses",
                column: "WorkflowGroupDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRemarks_CCReceiverId",
                table: "WorkflowRemarks",
                column: "CCReceiverId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRemarks_WorkflowRequestId",
                table: "WorkflowRemarks",
                column: "WorkflowRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRequests_ApproverId",
                table: "WorkflowRequests",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRequests_EmployeeId",
                table: "WorkflowRequests",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowRoles_WorkflowRoleOrgId",
                table: "WorkflowRoles",
                column: "WorkflowRoleOrgId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CDAnnualYearReviews");

            migrationBuilder.DropTable(
                name: "CDMidYearReviews");

            migrationBuilder.DropTable(
                name: "CDPlans");

            migrationBuilder.DropTable(
                name: "CompetencyByJobTitleDetails");

            migrationBuilder.DropTable(
                name: "CVLanguages");

            migrationBuilder.DropTable(
                name: "CVQualifications");

            migrationBuilder.DropTable(
                name: "CVSkills");

            migrationBuilder.DropTable(
                name: "CVWorkExps");

            migrationBuilder.DropTable(
                name: "InterviewDetails");

            migrationBuilder.DropTable(
                name: "ManpowerPlanningDetails");

            migrationBuilder.DropTable(
                name: "OnbAppTaskComments");

            migrationBuilder.DropTable(
                name: "OnbGroupDetails");

            migrationBuilder.DropTable(
                name: "OnbTemplateDetails");

            migrationBuilder.DropTable(
                name: "OnbVideoLinks");

            migrationBuilder.DropTable(
                name: "PositionRelationships");

            migrationBuilder.DropTable(
                name: "PrintReportPermissionDetails");

            migrationBuilder.DropTable(
                name: "ReportDetails");

            migrationBuilder.DropTable(
                name: "ReportInfoDetails");

            migrationBuilder.DropTable(
                name: "ReportSettingDetails");

            migrationBuilder.DropTable(
                name: "RequestDetailParallels");

            migrationBuilder.DropTable(
                name: "RequestLogs");

            migrationBuilder.DropTable(
                name: "WFReadLogs");

            migrationBuilder.DropTable(
                name: "WorkFlowApprovers");

            migrationBuilder.DropTable(
                name: "WorkflowGroupFieldAccesses");

            migrationBuilder.DropTable(
                name: "WorkflowRemarks");

            migrationBuilder.DropTable(
                name: "CDRatings");

            migrationBuilder.DropTable(
                name: "CDForms");

            migrationBuilder.DropTable(
                name: "Competencies");

            migrationBuilder.DropTable(
                name: "CompetencyByJobTitles");

            migrationBuilder.DropTable(
                name: "ApplicationStatuses");

            migrationBuilder.DropTable(
                name: "ManpowerPlannings");

            migrationBuilder.DropTable(
                name: "OnbApplicantTasks");

            migrationBuilder.DropTable(
                name: "Positions");

            migrationBuilder.DropTable(
                name: "PrintReportPermissions");

            migrationBuilder.DropTable(
                name: "PrintReports");

            migrationBuilder.DropTable(
                name: "ReportMasters");

            migrationBuilder.DropTable(
                name: "ReportInfos");

            migrationBuilder.DropTable(
                name: "ReportSettings");

            migrationBuilder.DropTable(
                name: "RequestDetails");

            migrationBuilder.DropTable(
                name: "WorkflowGroupDetails");

            migrationBuilder.DropTable(
                name: "WorkflowRequests");

            migrationBuilder.DropTable(
                name: "CDPeriods");

            migrationBuilder.DropTable(
                name: "CompetencyTypes");

            migrationBuilder.DropTable(
                name: "ApplicantCVs");

            migrationBuilder.DropTable(
                name: "Vacancies");

            migrationBuilder.DropTable(
                name: "ManpowerPlanningPeriods");

            migrationBuilder.DropTable(
                name: "OnbTasks");

            migrationBuilder.DropTable(
                name: "DomainTypes");

            migrationBuilder.DropTable(
                name: "ReportTypes");

            migrationBuilder.DropTable(
                name: "WorkflowRoles");

            migrationBuilder.DropTable(
                name: "RequestMasters");

            migrationBuilder.DropTable(
                name: "WorkflowGroups");

            migrationBuilder.DropTable(
                name: "Applicants");

            migrationBuilder.DropTable(
                name: "OnbGroups");

            migrationBuilder.DropTable(
                name: "OnbTemplates");
        }
    }
}
