using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_PerformanceManagement_TravelManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MinReviewerLimit = table.Column<int>(type: "int", nullable: false),
                    MaxReviewerLimit = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_FormTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Hotels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hotels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KpiByJobTitles",
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
                    table.PrimaryKey("PK_KpiByJobTitles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiByJobTitles_JobTitles_JobTitleId",
                        column: x => x.JobTitleId,
                        principalTable: "JobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KpiByOrgUnits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_KpiByOrgUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiByOrgUnits_AbpOrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KpiCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HexColor = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
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
                    table.PrimaryKey("PK_KpiCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PACycles",
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
                    table.PrimaryKey("PK_PACycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PAPeriods",
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
                    table.PrimaryKey("PK_PAPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PARatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsCommentRequired = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    MaxLimit = table.Column<int>(type: "int", nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    DescriptionAr = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    FromRange = table.Column<double>(type: "float", nullable: false),
                    ToRange = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_PARatings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TravelAirlines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelAirlines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TravelCancelReasons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelCancelReasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TravelDestinations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatorUserId = table.Column<long>(type: "bigint", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastModifierUserId = table.Column<long>(type: "bigint", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeleterUserId = table.Column<long>(type: "bigint", nullable: true),
                    DeletionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelDestinations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TravelTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_TravelTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelTypes_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "KpiInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    KpiCategoryId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_KpiInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiInfos_KpiCategories_KpiCategoryId",
                        column: x => x.KpiCategoryId,
                        principalTable: "KpiCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAForms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAPeriodId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OverallScore = table.Column<double>(type: "float", nullable: false),
                    ModeratedScore = table.Column<double>(type: "float", nullable: false),
                    PlanSubmitByAppraiser = table.Column<bool>(type: "bit", nullable: false),
                    PlanSubmitByAppraisee = table.Column<bool>(type: "bit", nullable: false),
                    Quarter1SubmitByAppraiser = table.Column<bool>(type: "bit", nullable: false),
                    Quarter1SubmitByAppraisee = table.Column<bool>(type: "bit", nullable: false),
                    MRSubmitByAppraiser = table.Column<bool>(type: "bit", nullable: false),
                    MRSubmitByAppraisee = table.Column<bool>(type: "bit", nullable: false),
                    Quarter3SubmitByAppraiser = table.Column<bool>(type: "bit", nullable: false),
                    Quarter3SubmitByAppraisee = table.Column<bool>(type: "bit", nullable: false),
                    ERSubmitByAppraiser = table.Column<bool>(type: "bit", nullable: false),
                    ERSubmitByAppraisee = table.Column<bool>(type: "bit", nullable: false),
                    AppraiserSignedOff = table.Column<bool>(type: "bit", nullable: false),
                    AppraiseeSignedOff = table.Column<bool>(type: "bit", nullable: false),
                    AppraiseeStrength = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    AreasOfImprovement = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ReviewofPreviousGoals = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    NewlySetGoals = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OverallComments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_PAForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAForms_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAForms_PAPeriods_PAPeriodId",
                        column: x => x.PAPeriodId,
                        principalTable: "PAPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAPeerNominees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAPeriodId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PeerId = table.Column<int>(type: "int", nullable: false),
                    Submitted = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PAPeerNominees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAPeerNominees_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAPeerNominees_Employees_PeerId",
                        column: x => x.PeerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAPeerNominees_PAPeriods_PAPeriodId",
                        column: x => x.PAPeriodId,
                        principalTable: "PAPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PATimelineExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAPeriodId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
                    ExtensionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_PATimelineExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PATimelineExceptions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PATimelineExceptions_PAPeriods_PAPeriodId",
                        column: x => x.PAPeriodId,
                        principalTable: "PAPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PATimelines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAPeriodId = table.Column<int>(type: "int", nullable: false),
                    ActivityType = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PATimelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PATimelines_PAPeriods_PAPeriodId",
                        column: x => x.PAPeriodId,
                        principalTable: "PAPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAEmployees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PACycleId = table.Column<int>(type: "int", nullable: false),
                    FormTypeId = table.Column<int>(type: "int", nullable: false),
                    OverallAverageRatingId = table.Column<int>(type: "int", nullable: true),
                    OverallAverageScore = table.Column<double>(type: "float", nullable: false),
                    PeerAverageRatingId = table.Column<int>(type: "int", nullable: true),
                    PeerAverageScore = table.Column<double>(type: "float", nullable: false),
                    EmployeeAverageRatingId = table.Column<int>(type: "int", nullable: true),
                    EmployeeAverageScore = table.Column<double>(type: "float", nullable: false),
                    RequestComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WorkflowComments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ProposedIncrement = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ProposedJobTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AreaOfImprovement = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PAEmployees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAEmployees_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAEmployees_FormTypes_FormTypeId",
                        column: x => x.FormTypeId,
                        principalTable: "FormTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAEmployees_PACycles_PACycleId",
                        column: x => x.PACycleId,
                        principalTable: "PACycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAEmployees_PARatings_EmployeeAverageRatingId",
                        column: x => x.EmployeeAverageRatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PAEmployees_PARatings_OverallAverageRatingId",
                        column: x => x.OverallAverageRatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PAEmployees_PARatings_PeerAverageRatingId",
                        column: x => x.PeerAverageRatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TravelPerDiems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    FromDestinationId = table.Column<int>(type: "int", nullable: false),
                    ToDestinationId = table.Column<int>(type: "int", nullable: false),
                    EmployeeGroupId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TravelPerDiems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelPerDiems_EmployeeGroups_EmployeeGroupId",
                        column: x => x.EmployeeGroupId,
                        principalTable: "EmployeeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelPerDiems_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TravelPerDiems_TravelDestinations_FromDestinationId",
                        column: x => x.FromDestinationId,
                        principalTable: "TravelDestinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TravelPerDiems_TravelDestinations_ToDestinationId",
                        column: x => x.ToDestinationId,
                        principalTable: "TravelDestinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Travels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TravelTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalTravelCost = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_Travels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Travels_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Travels_TravelTypes_TravelTypeId",
                        column: x => x.TravelTypeId,
                        principalTable: "TravelTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KpiByJobTitleDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    KpiByJobTitleId = table.Column<int>(type: "int", nullable: false),
                    KpiInfoId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_KpiByJobTitleDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiByJobTitleDetails_KpiByJobTitles_KpiByJobTitleId",
                        column: x => x.KpiByJobTitleId,
                        principalTable: "KpiByJobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KpiByJobTitleDetails_KpiInfos_KpiInfoId",
                        column: x => x.KpiInfoId,
                        principalTable: "KpiInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "KpiByOrgUnitDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    KpiByOrgUnitId = table.Column<int>(type: "int", nullable: false),
                    KpiInfoId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_KpiByOrgUnitDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiByOrgUnitDetails_KpiByOrgUnits_KpiByOrgUnitId",
                        column: x => x.KpiByOrgUnitId,
                        principalTable: "KpiByOrgUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_KpiByOrgUnitDetails_KpiInfos_KpiInfoId",
                        column: x => x.KpiInfoId,
                        principalTable: "KpiInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAAnnualYearReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAFormId = table.Column<int>(type: "int", nullable: false),
                    KpiInfoId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppraiseeRatingId = table.Column<int>(type: "int", nullable: true),
                    RatingId = table.Column<int>(type: "int", nullable: true),
                    AppraiseeComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppraiserComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PAAnnualYearReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAAnnualYearReviews_KpiInfos_KpiInfoId",
                        column: x => x.KpiInfoId,
                        principalTable: "KpiInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAAnnualYearReviews_PAForms_PAFormId",
                        column: x => x.PAFormId,
                        principalTable: "PAForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAAnnualYearReviews_PARatings_AppraiseeRatingId",
                        column: x => x.AppraiseeRatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PAAnnualYearReviews_PARatings_RatingId",
                        column: x => x.RatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PAGoals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAFormId = table.Column<int>(type: "int", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_PAGoals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAGoals_PAForms_PAFormId",
                        column: x => x.PAFormId,
                        principalTable: "PAForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAMidYearReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAFormId = table.Column<int>(type: "int", nullable: false),
                    KpiInfoId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppraiseeRatingId = table.Column<int>(type: "int", nullable: true),
                    RatingId = table.Column<int>(type: "int", nullable: true),
                    AppraiseeComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppraiserComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PAMidYearReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAMidYearReviews_KpiInfos_KpiInfoId",
                        column: x => x.KpiInfoId,
                        principalTable: "KpiInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAMidYearReviews_PAForms_PAFormId",
                        column: x => x.PAFormId,
                        principalTable: "PAForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAMidYearReviews_PARatings_AppraiseeRatingId",
                        column: x => x.AppraiseeRatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PAMidYearReviews_PARatings_RatingId",
                        column: x => x.RatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PAPeerReviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAFormId = table.Column<int>(type: "int", nullable: false),
                    PeerId = table.Column<int>(type: "int", nullable: false),
                    KpiInfoId = table.Column<int>(type: "int", nullable: false),
                    RatingId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_PAPeerReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAPeerReviews_Employees_PeerId",
                        column: x => x.PeerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAPeerReviews_KpiInfos_KpiInfoId",
                        column: x => x.KpiInfoId,
                        principalTable: "KpiInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAPeerReviews_PAForms_PAFormId",
                        column: x => x.PAFormId,
                        principalTable: "PAForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAPeerReviews_PARatings_RatingId",
                        column: x => x.RatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PAPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAFormId = table.Column<int>(type: "int", nullable: false),
                    KpiInfoId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppraiseeRatingId = table.Column<int>(type: "int", nullable: true),
                    RatingId = table.Column<int>(type: "int", nullable: true),
                    AppraiseeComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppraiserComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PAPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAPlans_KpiInfos_KpiInfoId",
                        column: x => x.KpiInfoId,
                        principalTable: "KpiInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAPlans_PAForms_PAFormId",
                        column: x => x.PAFormId,
                        principalTable: "PAForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAPlans_PARatings_AppraiseeRatingId",
                        column: x => x.AppraiseeRatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PAPlans_PARatings_RatingId",
                        column: x => x.RatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PAQuarter1Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAFormId = table.Column<int>(type: "int", nullable: false),
                    KpiInfoId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppraiseeRatingId = table.Column<int>(type: "int", nullable: true),
                    RatingId = table.Column<int>(type: "int", nullable: true),
                    AppraiseeComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppraiserComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PAQuarter1Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAQuarter1Reviews_KpiInfos_KpiInfoId",
                        column: x => x.KpiInfoId,
                        principalTable: "KpiInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAQuarter1Reviews_PAForms_PAFormId",
                        column: x => x.PAFormId,
                        principalTable: "PAForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAQuarter1Reviews_PARatings_AppraiseeRatingId",
                        column: x => x.AppraiseeRatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PAQuarter1Reviews_PARatings_RatingId",
                        column: x => x.RatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PAQuarter3Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAFormId = table.Column<int>(type: "int", nullable: false),
                    KpiInfoId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AppraiseeRatingId = table.Column<int>(type: "int", nullable: true),
                    RatingId = table.Column<int>(type: "int", nullable: true),
                    AppraiseeComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AppraiserComments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_PAQuarter3Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAQuarter3Reviews_KpiInfos_KpiInfoId",
                        column: x => x.KpiInfoId,
                        principalTable: "KpiInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAQuarter3Reviews_PAForms_PAFormId",
                        column: x => x.PAFormId,
                        principalTable: "PAForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAQuarter3Reviews_PARatings_AppraiseeRatingId",
                        column: x => x.AppraiseeRatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PAQuarter3Reviews_PARatings_RatingId",
                        column: x => x.RatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PAEmployeeDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAEmployeeId = table.Column<int>(type: "int", nullable: false),
                    KpiInfoId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<double>(type: "float", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_PAEmployeeDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAEmployeeDetails_KpiInfos_KpiInfoId",
                        column: x => x.KpiInfoId,
                        principalTable: "KpiInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAEmployeeDetails_PAEmployees_PAEmployeeId",
                        column: x => x.PAEmployeeId,
                        principalTable: "PAEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PAEmployeeReviewers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAEmployeeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AverageRatingId = table.Column<int>(type: "int", nullable: true),
                    AverageScore = table.Column<double>(type: "float", nullable: false),
                    TotalMarks = table.Column<double>(type: "float", nullable: false),
                    IsPeer = table.Column<bool>(type: "bit", nullable: false),
                    ReviewSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    PeerAddedById = table.Column<int>(type: "int", nullable: true),
                    CustomTextField1 = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CustomTextField2 = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CustomTextField3 = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_PAEmployeeReviewers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAEmployeeReviewers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAEmployeeReviewers_Employees_PeerAddedById",
                        column: x => x.PeerAddedById,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAEmployeeReviewers_PAEmployees_PAEmployeeId",
                        column: x => x.PAEmployeeId,
                        principalTable: "PAEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAEmployeeReviewers_PARatings_AverageRatingId",
                        column: x => x.AverageRatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TravelDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TravelId = table.Column<int>(type: "int", nullable: false),
                    DepartureId = table.Column<int>(type: "int", nullable: false),
                    ArrivalId = table.Column<int>(type: "int", nullable: false),
                    DepartureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DepartureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HotelId = table.Column<int>(type: "int", nullable: true),
                    TravelAirlineId = table.Column<int>(type: "int", nullable: true),
                    NightStay = table.Column<bool>(type: "bit", nullable: false),
                    TotalNights = table.Column<int>(type: "int", nullable: false),
                    TotalAmount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TravelDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelDetails_Hotels_HotelId",
                        column: x => x.HotelId,
                        principalTable: "Hotels",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TravelDetails_TravelAirlines_TravelAirlineId",
                        column: x => x.TravelAirlineId,
                        principalTable: "TravelAirlines",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TravelDetails_TravelDestinations_ArrivalId",
                        column: x => x.ArrivalId,
                        principalTable: "TravelDestinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TravelDetails_TravelDestinations_DepartureId",
                        column: x => x.DepartureId,
                        principalTable: "TravelDestinations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TravelDetails_Travels_TravelId",
                        column: x => x.TravelId,
                        principalTable: "Travels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TravelFeedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TravelId = table.Column<int>(type: "int", nullable: true),
                    ClaimDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClaimAmount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TravelFeedbacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelFeedbacks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelFeedbacks_Travels_TravelId",
                        column: x => x.TravelId,
                        principalTable: "Travels",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PAEmployeeRatings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PAEmployeeId = table.Column<int>(type: "int", nullable: false),
                    PAEmployeeReviewerId = table.Column<int>(type: "int", nullable: false),
                    PAEmployeeDetailId = table.Column<int>(type: "int", nullable: false),
                    Marks = table.Column<double>(type: "float", nullable: false),
                    RatingId = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_PAEmployeeRatings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAEmployeeRatings_PAEmployeeDetails_PAEmployeeDetailId",
                        column: x => x.PAEmployeeDetailId,
                        principalTable: "PAEmployeeDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAEmployeeRatings_PAEmployeeReviewers_PAEmployeeReviewerId",
                        column: x => x.PAEmployeeReviewerId,
                        principalTable: "PAEmployeeReviewers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAEmployeeRatings_PAEmployees_PAEmployeeId",
                        column: x => x.PAEmployeeId,
                        principalTable: "PAEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PAEmployeeRatings_PARatings_RatingId",
                        column: x => x.RatingId,
                        principalTable: "PARatings",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TravelDetailCosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TravelDetailId = table.Column<int>(type: "int", nullable: false),
                    ExpenseTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TravelDetailCosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravelDetailCosts_ExpenseTypes_ExpenseTypeId",
                        column: x => x.ExpenseTypeId,
                        principalTable: "ExpenseTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TravelDetailCosts_TravelDetails_TravelDetailId",
                        column: x => x.TravelDetailId,
                        principalTable: "TravelDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KpiByJobTitleDetails_KpiByJobTitleId",
                table: "KpiByJobTitleDetails",
                column: "KpiByJobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiByJobTitleDetails_KpiInfoId",
                table: "KpiByJobTitleDetails",
                column: "KpiInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiByJobTitles_JobTitleId",
                table: "KpiByJobTitles",
                column: "JobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiByOrgUnitDetails_KpiByOrgUnitId",
                table: "KpiByOrgUnitDetails",
                column: "KpiByOrgUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiByOrgUnitDetails_KpiInfoId",
                table: "KpiByOrgUnitDetails",
                column: "KpiInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiByOrgUnits_OrganizationUnitId",
                table: "KpiByOrgUnits",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiInfos_KpiCategoryId",
                table: "KpiInfos",
                column: "KpiCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PAAnnualYearReviews_AppraiseeRatingId",
                table: "PAAnnualYearReviews",
                column: "AppraiseeRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAAnnualYearReviews_KpiInfoId",
                table: "PAAnnualYearReviews",
                column: "KpiInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_PAAnnualYearReviews_PAFormId",
                table: "PAAnnualYearReviews",
                column: "PAFormId");

            migrationBuilder.CreateIndex(
                name: "IX_PAAnnualYearReviews_RatingId",
                table: "PAAnnualYearReviews",
                column: "RatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployeeDetails_KpiInfoId",
                table: "PAEmployeeDetails",
                column: "KpiInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployeeDetails_PAEmployeeId",
                table: "PAEmployeeDetails",
                column: "PAEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployeeRatings_PAEmployeeDetailId",
                table: "PAEmployeeRatings",
                column: "PAEmployeeDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployeeRatings_PAEmployeeId",
                table: "PAEmployeeRatings",
                column: "PAEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployeeRatings_PAEmployeeReviewerId",
                table: "PAEmployeeRatings",
                column: "PAEmployeeReviewerId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployeeRatings_RatingId",
                table: "PAEmployeeRatings",
                column: "RatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployeeReviewers_AverageRatingId",
                table: "PAEmployeeReviewers",
                column: "AverageRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployeeReviewers_EmployeeId",
                table: "PAEmployeeReviewers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployeeReviewers_PAEmployeeId",
                table: "PAEmployeeReviewers",
                column: "PAEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployeeReviewers_PeerAddedById",
                table: "PAEmployeeReviewers",
                column: "PeerAddedById");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployees_EmployeeAverageRatingId",
                table: "PAEmployees",
                column: "EmployeeAverageRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployees_EmployeeId",
                table: "PAEmployees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployees_FormTypeId",
                table: "PAEmployees",
                column: "FormTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployees_OverallAverageRatingId",
                table: "PAEmployees",
                column: "OverallAverageRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployees_PACycleId",
                table: "PAEmployees",
                column: "PACycleId");

            migrationBuilder.CreateIndex(
                name: "IX_PAEmployees_PeerAverageRatingId",
                table: "PAEmployees",
                column: "PeerAverageRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAForms_EmployeeId",
                table: "PAForms",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PAForms_PAPeriodId",
                table: "PAForms",
                column: "PAPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PAGoals_PAFormId",
                table: "PAGoals",
                column: "PAFormId");

            migrationBuilder.CreateIndex(
                name: "IX_PAMidYearReviews_AppraiseeRatingId",
                table: "PAMidYearReviews",
                column: "AppraiseeRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAMidYearReviews_KpiInfoId",
                table: "PAMidYearReviews",
                column: "KpiInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_PAMidYearReviews_PAFormId",
                table: "PAMidYearReviews",
                column: "PAFormId");

            migrationBuilder.CreateIndex(
                name: "IX_PAMidYearReviews_RatingId",
                table: "PAMidYearReviews",
                column: "RatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPeerNominees_EmployeeId",
                table: "PAPeerNominees",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPeerNominees_PAPeriodId",
                table: "PAPeerNominees",
                column: "PAPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPeerNominees_PeerId",
                table: "PAPeerNominees",
                column: "PeerId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPeerReviews_KpiInfoId",
                table: "PAPeerReviews",
                column: "KpiInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPeerReviews_PAFormId",
                table: "PAPeerReviews",
                column: "PAFormId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPeerReviews_PeerId",
                table: "PAPeerReviews",
                column: "PeerId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPeerReviews_RatingId",
                table: "PAPeerReviews",
                column: "RatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPlans_AppraiseeRatingId",
                table: "PAPlans",
                column: "AppraiseeRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPlans_KpiInfoId",
                table: "PAPlans",
                column: "KpiInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPlans_PAFormId",
                table: "PAPlans",
                column: "PAFormId");

            migrationBuilder.CreateIndex(
                name: "IX_PAPlans_RatingId",
                table: "PAPlans",
                column: "RatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAQuarter1Reviews_AppraiseeRatingId",
                table: "PAQuarter1Reviews",
                column: "AppraiseeRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAQuarter1Reviews_KpiInfoId",
                table: "PAQuarter1Reviews",
                column: "KpiInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_PAQuarter1Reviews_PAFormId",
                table: "PAQuarter1Reviews",
                column: "PAFormId");

            migrationBuilder.CreateIndex(
                name: "IX_PAQuarter1Reviews_RatingId",
                table: "PAQuarter1Reviews",
                column: "RatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAQuarter3Reviews_AppraiseeRatingId",
                table: "PAQuarter3Reviews",
                column: "AppraiseeRatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PAQuarter3Reviews_KpiInfoId",
                table: "PAQuarter3Reviews",
                column: "KpiInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_PAQuarter3Reviews_PAFormId",
                table: "PAQuarter3Reviews",
                column: "PAFormId");

            migrationBuilder.CreateIndex(
                name: "IX_PAQuarter3Reviews_RatingId",
                table: "PAQuarter3Reviews",
                column: "RatingId");

            migrationBuilder.CreateIndex(
                name: "IX_PATimelineExceptions_EmployeeId",
                table: "PATimelineExceptions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PATimelineExceptions_PAPeriodId",
                table: "PATimelineExceptions",
                column: "PAPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PATimelines_PAPeriodId",
                table: "PATimelines",
                column: "PAPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelDetailCosts_ExpenseTypeId",
                table: "TravelDetailCosts",
                column: "ExpenseTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelDetailCosts_TravelDetailId",
                table: "TravelDetailCosts",
                column: "TravelDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelDetails_ArrivalId",
                table: "TravelDetails",
                column: "ArrivalId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelDetails_DepartureId",
                table: "TravelDetails",
                column: "DepartureId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelDetails_HotelId",
                table: "TravelDetails",
                column: "HotelId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelDetails_TravelAirlineId",
                table: "TravelDetails",
                column: "TravelAirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelDetails_TravelId",
                table: "TravelDetails",
                column: "TravelId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelFeedbacks_EmployeeId",
                table: "TravelFeedbacks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelFeedbacks_TravelId",
                table: "TravelFeedbacks",
                column: "TravelId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPerDiems_EmployeeGroupId",
                table: "TravelPerDiems",
                column: "EmployeeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPerDiems_EmployeeSubGroupId",
                table: "TravelPerDiems",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPerDiems_FromDestinationId",
                table: "TravelPerDiems",
                column: "FromDestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelPerDiems_ToDestinationId",
                table: "TravelPerDiems",
                column: "ToDestinationId");

            migrationBuilder.CreateIndex(
                name: "IX_Travels_EmployeeId",
                table: "Travels",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Travels_TravelTypeId",
                table: "Travels",
                column: "TravelTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelTypes_WageTypeId",
                table: "TravelTypes",
                column: "WageTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KpiByJobTitleDetails");

            migrationBuilder.DropTable(
                name: "KpiByOrgUnitDetails");

            migrationBuilder.DropTable(
                name: "PAAnnualYearReviews");

            migrationBuilder.DropTable(
                name: "PAEmployeeRatings");

            migrationBuilder.DropTable(
                name: "PAGoals");

            migrationBuilder.DropTable(
                name: "PAMidYearReviews");

            migrationBuilder.DropTable(
                name: "PAPeerNominees");

            migrationBuilder.DropTable(
                name: "PAPeerReviews");

            migrationBuilder.DropTable(
                name: "PAPlans");

            migrationBuilder.DropTable(
                name: "PAQuarter1Reviews");

            migrationBuilder.DropTable(
                name: "PAQuarter3Reviews");

            migrationBuilder.DropTable(
                name: "PATimelineExceptions");

            migrationBuilder.DropTable(
                name: "PATimelines");

            migrationBuilder.DropTable(
                name: "TravelCancelReasons");

            migrationBuilder.DropTable(
                name: "TravelDetailCosts");

            migrationBuilder.DropTable(
                name: "TravelFeedbacks");

            migrationBuilder.DropTable(
                name: "TravelPerDiems");

            migrationBuilder.DropTable(
                name: "KpiByJobTitles");

            migrationBuilder.DropTable(
                name: "KpiByOrgUnits");

            migrationBuilder.DropTable(
                name: "PAEmployeeDetails");

            migrationBuilder.DropTable(
                name: "PAEmployeeReviewers");

            migrationBuilder.DropTable(
                name: "PAForms");

            migrationBuilder.DropTable(
                name: "ExpenseTypes");

            migrationBuilder.DropTable(
                name: "TravelDetails");

            migrationBuilder.DropTable(
                name: "KpiInfos");

            migrationBuilder.DropTable(
                name: "PAEmployees");

            migrationBuilder.DropTable(
                name: "PAPeriods");

            migrationBuilder.DropTable(
                name: "Hotels");

            migrationBuilder.DropTable(
                name: "TravelAirlines");

            migrationBuilder.DropTable(
                name: "TravelDestinations");

            migrationBuilder.DropTable(
                name: "Travels");

            migrationBuilder.DropTable(
                name: "KpiCategories");

            migrationBuilder.DropTable(
                name: "FormTypes");

            migrationBuilder.DropTable(
                name: "PACycles");

            migrationBuilder.DropTable(
                name: "PARatings");

            migrationBuilder.DropTable(
                name: "TravelTypes");
        }
    }
}
