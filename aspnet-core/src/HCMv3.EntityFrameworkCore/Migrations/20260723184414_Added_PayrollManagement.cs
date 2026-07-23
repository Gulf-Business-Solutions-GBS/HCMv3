using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_PayrollManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AbsenceDeductionRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RuleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
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
                    table.PrimaryKey("PK_AbsenceDeductionRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceViolationActionRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ActionLogType = table.Column<int>(type: "int", nullable: false),
                    LogStatus = table.Column<int>(type: "int", nullable: false),
                    EntityObjectId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceViolationActionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceViolationRules",
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
                    table.PrimaryKey("PK_AbsenceViolationRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AXBatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    JournalBatchNumber = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
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
                    table.PrimaryKey("PK_AXBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayrollRunType = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CostRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostRegisters_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IndemnityMaxNumber = table.Column<double>(type: "float", nullable: false),
                    IndemnityMaxUnit = table.Column<int>(type: "int", nullable: false),
                    IndemnityMaxAccruePercentage = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePayrollStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EarliestRetroAccountingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountedTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RunPayrollUpto = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EarliestMasterDataChange = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoNoAccountAfter = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PersonLocked = table.Column<bool>(type: "bit", nullable: false),
                    PayrollCorrection = table.Column<bool>(type: "bit", nullable: false),
                    EarliestRecalculationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PersonalCalendarFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NextTimeEvaluationFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeePayrollStatuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePayrollStatuses_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GratuityAccrualSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    GratuityDaysAccrued = table.Column<double>(type: "float", nullable: false),
                    GratuityDaysBalance = table.Column<double>(type: "float", nullable: false),
                    GratuityAmount = table.Column<double>(type: "float", nullable: false),
                    GratuityAmountPaid = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_GratuityAccrualSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GratuityAccrualSummaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndemnityPayRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SeparationType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxNumber = table.Column<double>(type: "float", nullable: false),
                    MaxUnit = table.Column<int>(type: "int", nullable: false),
                    MaxPayPercentage = table.Column<double>(type: "float", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_IndemnityPayRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndemnityPayRules_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PayrollAccountTypes",
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
                    table.PrimaryKey("PK_PayrollAccountTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RunTimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InPeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StopFlag = table.Column<bool>(type: "bit", nullable: false),
                    NoOfEmployee = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PayrollLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PayrollPeriods",
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
                    table.PrimaryKey("PK_PayrollPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollReasonTypes",
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
                    table.PrimaryKey("PK_PayrollReasonTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SocialSecurities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NationalityId = table.Column<int>(type: "int", nullable: false),
                    EarningsCap = table.Column<double>(type: "float", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChildAllowance = table.Column<double>(type: "float", nullable: false),
                    SpecialNeedChildAllowance = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_SocialSecurities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialSecurities_Nationalities_NationalityId",
                        column: x => x.NationalityId,
                        principalTable: "Nationalities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TempBenefitTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Grade = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Ticket = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SimCard = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Incentive = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BonusSIP = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Uniform = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MedicalInsurance = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LeaveDays = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MedicalLeave = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MaternityLeave = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StaffDiscount = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SpecialAllowance = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ManagerAllowance = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CompanyAsset = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SchoolAllowance = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NoticePeriod = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Indemnity = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Retirement = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MinSalary = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MidSalary = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MaxSalary = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TransportationAllowance = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    HousingAllowance = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    OtherAllowance = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_TempBenefitTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketAccrualSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TicketAccrued = table.Column<double>(type: "float", nullable: false),
                    TicketUsed = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TicketAccrualSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketAccrualSummaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketClasses",
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
                    table.PrimaryKey("PK_TicketClasses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketPaxTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AgeFrom = table.Column<double>(type: "float", nullable: false),
                    AgeTo = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TicketPaxTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketRoutes",
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
                    table.PrimaryKey("PK_TicketRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WageAccGroups",
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
                    table.PrimaryKey("PK_WageAccGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WageTypeRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    ForPeriod = table.Column<int>(type: "int", nullable: false),
                    InPeriod = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_WageTypeRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WageTypeRegisters_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WageTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShortNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ShortNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    BillingType = table.Column<int>(type: "int", nullable: false),
                    BillingMethod = table.Column<int>(type: "int", nullable: false),
                    ProrateMethod = table.Column<int>(type: "int", nullable: false),
                    FixedDays = table.Column<double>(type: "float", nullable: false),
                    Rate = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    ArrearSourceWageTypeId = table.Column<int>(type: "int", nullable: true),
                    PayrollOrder = table.Column<int>(type: "int", nullable: false),
                    IsSecured = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInIndemnity = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInPaidLeave = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInBasicPay = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInArrearPay = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInCalculatedPay = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInAdvancePay = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInOvertimePay = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInFIPosting = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInCutoff = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInDisciplinary = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInVariablePay = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInSocialSecurity = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInPartialPay = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInLateEarlyDed = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInServiceBonus = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInFund = table.Column<bool>(type: "bit", nullable: false),
                    DontImpactNetPay = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WageTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WageTypes_WageTypes_ArrearSourceWageTypeId",
                        column: x => x.ArrearSourceWageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbsenceDedRuleDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AbsenceDeductionRuleId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DaysCount = table.Column<double>(type: "float", nullable: false),
                    DeductionUnit = table.Column<int>(type: "int", nullable: false),
                    DeductionNumber = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceDedRuleDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbsenceDedRuleDetails_AbsenceDeductionRules_AbsenceDeductionRuleId",
                        column: x => x.AbsenceDeductionRuleId,
                        principalTable: "AbsenceDeductionRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceViolationActionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AbsenceViolationActionRecordId = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ObjectOId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceViolationActionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbsenceViolationActionLogs_AbsenceViolationActionRecords_AbsenceViolationActionRecordId",
                        column: x => x.AbsenceViolationActionRecordId,
                        principalTable: "AbsenceViolationActionRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    IncludeWeekend = table.Column<bool>(type: "bit", nullable: false),
                    IncludeRestDay = table.Column<bool>(type: "bit", nullable: false),
                    IncludePublicHoliday = table.Column<bool>(type: "bit", nullable: false),
                    LeaveBasisPeriod = table.Column<int>(type: "int", nullable: false),
                    LeaveBasisFixedDays = table.Column<double>(type: "float", nullable: false),
                    HaveQuotaAccrual = table.Column<bool>(type: "bit", nullable: false),
                    AccrualPeriod = table.Column<int>(type: "int", nullable: false),
                    CustomStartMonth = table.Column<int>(type: "int", nullable: false),
                    MinActiveDaysInService = table.Column<double>(type: "float", nullable: false),
                    CanApplyAgain = table.Column<bool>(type: "bit", nullable: false),
                    CanApplyAfterDays = table.Column<double>(type: "float", nullable: false),
                    CanApplyBeforeDays = table.Column<int>(type: "int", nullable: false),
                    ReInitialize = table.Column<bool>(type: "bit", nullable: false),
                    ReInitializeAfter = table.Column<int>(type: "int", nullable: false),
                    ApplyLeaveInAdvance = table.Column<bool>(type: "bit", nullable: false),
                    ApplyLeaveInAdvanceDays = table.Column<int>(type: "int", nullable: false),
                    ApplyMaxLeave = table.Column<bool>(type: "bit", nullable: false),
                    MaxLimitInDays = table.Column<int>(type: "int", nullable: false),
                    CreateUnpaidAboveMaxLimitDays = table.Column<bool>(type: "bit", nullable: false),
                    ApplyMinLeave = table.Column<bool>(type: "bit", nullable: false),
                    MinLimitInDays = table.Column<int>(type: "int", nullable: false),
                    ResetFrequency = table.Column<int>(type: "int", nullable: false),
                    TotalMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxCount = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: true),
                    PolicyRestrictType = table.Column<int>(type: "int", nullable: false),
                    IncludeInWorkflow = table.Column<bool>(type: "bit", nullable: false),
                    IsReturnDateRequired = table.Column<bool>(type: "bit", nullable: false),
                    LeaveGender = table.Column<int>(type: "int", nullable: false),
                    LeaveReligion = table.Column<int>(type: "int", nullable: false),
                    ExcludePaidLeaveAccrual = table.Column<bool>(type: "bit", nullable: false),
                    QuotaType = table.Column<int>(type: "int", nullable: false),
                    AllowBackDatedDays = table.Column<int>(type: "int", nullable: false),
                    IncludeInSalaryProfile = table.Column<bool>(type: "bit", nullable: false),
                    TotalDaysAllowed = table.Column<double>(type: "float", nullable: false),
                    LeaveEntitlement = table.Column<int>(type: "int", nullable: false),
                    AllowDuringProbation = table.Column<bool>(type: "bit", nullable: false),
                    AllowToReturnEarly = table.Column<bool>(type: "bit", nullable: false),
                    AllowInTerminationNoticeOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsAttachmentRequired = table.Column<bool>(type: "bit", nullable: false),
                    AllowPartialLeave = table.Column<bool>(type: "bit", nullable: false),
                    SendNotification = table.Column<bool>(type: "bit", nullable: false),
                    DontAllowToStartOnSunday = table.Column<bool>(type: "bit", nullable: false),
                    DontAllowToStartOnMonday = table.Column<bool>(type: "bit", nullable: false),
                    DontAllowToStartOnTuesday = table.Column<bool>(type: "bit", nullable: false),
                    DontAllowToStartOnWednesday = table.Column<bool>(type: "bit", nullable: false),
                    DontAllowToStartOnThursday = table.Column<bool>(type: "bit", nullable: false),
                    DontAllowToStartOnFriday = table.Column<bool>(type: "bit", nullable: false),
                    DontAllowToStartOnSaturday = table.Column<bool>(type: "bit", nullable: false),
                    EnableNegativeBalance = table.Column<bool>(type: "bit", nullable: false),
                    NegativeBalanceLimit = table.Column<int>(type: "int", nullable: false),
                    AbsenceViolationRuleId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_AbsenceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbsenceTypes_AbsenceViolationRules_AbsenceViolationRuleId",
                        column: x => x.AbsenceViolationRuleId,
                        principalTable: "AbsenceViolationRules",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AbsenceViolationRuleDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AbsenceViolationRuleId = table.Column<int>(type: "int", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    DayInterval = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    ActionTypeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_AbsenceViolationRuleDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbsenceViolationRuleDetails_AbsenceViolationRules_AbsenceViolationRuleId",
                        column: x => x.AbsenceViolationRuleId,
                        principalTable: "AbsenceViolationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceViolationRuleExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AbsenceViolationRuleId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceViolationRuleExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbsenceViolationRuleExceptions_AbsenceViolationRules_AbsenceViolationRuleId",
                        column: x => x.AbsenceViolationRuleId,
                        principalTable: "AbsenceViolationRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AbsenceViolationRuleExceptions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeGroupId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TicketClassId = table.Column<int>(type: "int", nullable: true),
                    PayOvertime = table.Column<bool>(type: "bit", nullable: false),
                    AirTicketPayType = table.Column<int>(type: "int", nullable: false),
                    MaxHealthReimburseAmount = table.Column<double>(type: "float", nullable: false),
                    HealthInsurancePeriod = table.Column<int>(type: "int", nullable: false),
                    AutoPayAirTicket = table.Column<bool>(type: "bit", nullable: false),
                    MaxDependantAirTicketCount = table.Column<int>(type: "int", nullable: false),
                    MaxDependantHealthReimburseCount = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroups_EmployeeGroups_EmployeeGroupId",
                        column: x => x.EmployeeGroupId,
                        principalTable: "EmployeeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndemnityPayRuleDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    IndemnityPayRuleId = table.Column<int>(type: "int", nullable: false),
                    MinYearOfService = table.Column<double>(type: "float", nullable: false),
                    MaxYearOfService = table.Column<double>(type: "float", nullable: false),
                    Number = table.Column<double>(type: "float", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    PayPercentage = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_IndemnityPayRuleDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndemnityPayRuleDetails_IndemnityPayRules_IndemnityPayRuleId",
                        column: x => x.IndemnityPayRuleId,
                        principalTable: "IndemnityPayRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AccountTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AccountInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountInfos_PayrollAccountTypes_AccountTypeId",
                        column: x => x.AccountTypeId,
                        principalTable: "PayrollAccountTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ForPeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InPeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayrollRunType = table.Column<int>(type: "int", nullable: false),
                    PayrollResultType = table.Column<int>(type: "int", nullable: false),
                    PayrollReasonTypeId = table.Column<int>(type: "int", nullable: false),
                    OffCyclePaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Locked = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SalaryRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryRegisters_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId",
                        column: x => x.PayrollReasonTypeId,
                        principalTable: "PayrollReasonTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationSalaryRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ForPeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InPeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PayrollRunType = table.Column<int>(type: "int", nullable: false),
                    PayrollResultType = table.Column<int>(type: "int", nullable: false),
                    PayrollReasonTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SimulationSalaryRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationSalaryRegisters_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SimulationSalaryRegisters_PayrollReasonTypes_PayrollReasonTypeId",
                        column: x => x.PayrollReasonTypeId,
                        principalTable: "PayrollReasonTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialSecurityDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SocialSecurityId = table.Column<int>(type: "int", nullable: false),
                    MinEarnings = table.Column<double>(type: "float", nullable: false),
                    MaxEarnings = table.Column<double>(type: "float", nullable: false),
                    EmployeeContribution = table.Column<double>(type: "float", nullable: false),
                    EmployerContribution = table.Column<double>(type: "float", nullable: false),
                    MinEarningsEmployeeContribution = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_SocialSecurityDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialSecurityDetails_SocialSecurities_SocialSecurityId",
                        column: x => x.SocialSecurityId,
                        principalTable: "SocialSecurities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialSecurityEducationAllowances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SocialSecurityId = table.Column<int>(type: "int", nullable: false),
                    EducationBranchId = table.Column<int>(type: "int", nullable: false),
                    EducationId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SocialSecurityEducationAllowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialSecurityEducationAllowances_EducationBranches_EducationBranchId",
                        column: x => x.EducationBranchId,
                        principalTable: "EducationBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialSecurityEducationAllowances_Educations_EducationId",
                        column: x => x.EducationId,
                        principalTable: "Educations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialSecurityEducationAllowances_SocialSecurities_SocialSecurityId",
                        column: x => x.SocialSecurityId,
                        principalTable: "SocialSecurities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialSecurityMaritalAllowances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SocialSecurityId = table.Column<int>(type: "int", nullable: false),
                    EducationBranchId = table.Column<int>(type: "int", nullable: false),
                    MaritalStatusId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SocialSecurityMaritalAllowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialSecurityMaritalAllowances_EducationBranches_EducationBranchId",
                        column: x => x.EducationBranchId,
                        principalTable: "EducationBranches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialSecurityMaritalAllowances_MaritalStatuses_MaritalStatusId",
                        column: x => x.MaritalStatusId,
                        principalTable: "MaritalStatuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialSecurityMaritalAllowances_SocialSecurities_SocialSecurityId",
                        column: x => x.SocialSecurityId,
                        principalTable: "SocialSecurities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SocialSecurityRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SocialSecurityId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QualificationId = table.Column<int>(type: "int", nullable: false),
                    Gender = table.Column<int>(type: "int", nullable: false),
                    MaritalStatusId = table.Column<int>(type: "int", nullable: true),
                    Dependant = table.Column<int>(type: "int", nullable: false),
                    Disability = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SocialSecurityRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SocialSecurityRules_MaritalStatuses_MaritalStatusId",
                        column: x => x.MaritalStatusId,
                        principalTable: "MaritalStatuses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SocialSecurityRules_Qualifications_QualificationId",
                        column: x => x.QualificationId,
                        principalTable: "Qualifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SocialSecurityRules_SocialSecurities_SocialSecurityId",
                        column: x => x.SocialSecurityId,
                        principalTable: "SocialSecurities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TempEmployeeBenefits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TempBenefitTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TempEmployeeBenefits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TempEmployeeBenefits_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TempEmployeeBenefits_TempBenefitTypes_TempBenefitTypeId",
                        column: x => x.TempBenefitTypeId,
                        principalTable: "TempBenefitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketFares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TicketRouteId = table.Column<int>(type: "int", nullable: false),
                    TicketPaxTypeId = table.Column<int>(type: "int", nullable: false),
                    TicketClassId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OneWayTicketFare = table.Column<double>(type: "float", nullable: false),
                    ReturnTicketFare = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TicketFares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketFares_TicketClasses_TicketClassId",
                        column: x => x.TicketClassId,
                        principalTable: "TicketClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketFares_TicketPaxTypes_TicketPaxTypeId",
                        column: x => x.TicketPaxTypeId,
                        principalTable: "TicketPaxTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketFares_TicketRoutes_TicketRouteId",
                        column: x => x.TicketRouteId,
                        principalTable: "TicketRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWageAccGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WageAccGroupId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeWageAccGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccGroups_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccGroups_WageAccGroups_WageAccGroupId",
                        column: x => x.WageAccGroupId,
                        principalTable: "WageAccGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrgUnitWageAccGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WageAccGroupId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_OrgUnitWageAccGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrgUnitWageAccGroups_AbpOrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrgUnitWageAccGroups_WageAccGroups_WageAccGroupId",
                        column: x => x.WageAccGroupId,
                        principalTable: "WageAccGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TicketBasisPeriod = table.Column<int>(type: "int", nullable: false),
                    TicketBasisFixedDays = table.Column<double>(type: "float", nullable: false),
                    Lapse = table.Column<bool>(type: "bit", nullable: false),
                    LapseAfter = table.Column<int>(type: "int", nullable: false),
                    StopDependantAccrualAfter = table.Column<int>(type: "int", nullable: false),
                    MinCompensation = table.Column<double>(type: "float", nullable: false),
                    DeductionPeriod = table.Column<int>(type: "int", nullable: false),
                    QuotaEntitlement = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_TicketTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketTypes_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WageTypeExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    ExceptWageTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_WageTypeExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WageTypeExceptions_WageTypes_ExceptWageTypeId",
                        column: x => x.ExceptWageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WageTypeExceptions_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WageTypeSecurityRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_WageTypeSecurityRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WageTypeSecurityRoles_AbpRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AbpRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WageTypeSecurityRoles_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbsencePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AbsenceTypeId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AbsencePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbsencePayments_AbsenceTypes_AbsenceTypeId",
                        column: x => x.AbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AbsencePayments_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AbsenceTypeExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AbsenceTypeId = table.Column<int>(type: "int", nullable: false),
                    ExceptAbsenceTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AbsenceTypeExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AbsenceTypeExceptions_AbsenceTypes_AbsenceTypeId",
                        column: x => x.AbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AbsenceTypeExceptions_AbsenceTypes_ExceptAbsenceTypeId",
                        column: x => x.ExceptAbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    HaveQuotaAccrual = table.Column<bool>(type: "bit", nullable: false),
                    IncludeWeekend = table.Column<bool>(type: "bit", nullable: false),
                    IncludeRestDay = table.Column<bool>(type: "bit", nullable: false),
                    IncludePublicHoliday = table.Column<bool>(type: "bit", nullable: false),
                    CalcOnCalendarDays = table.Column<bool>(type: "bit", nullable: false),
                    IsFullDay = table.Column<bool>(type: "bit", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: true),
                    AccrualPeriod = table.Column<int>(type: "int", nullable: false),
                    LinkLeaveQuotaId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_AttendanceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceTypes_AbsenceTypes_LinkLeaveQuotaId",
                        column: x => x.LinkLeaveQuotaId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LeaveAccrualSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AbsenceTypeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    AccrualDays = table.Column<double>(type: "float", nullable: false),
                    UsedDays = table.Column<double>(type: "float", nullable: false),
                    CumulativeAccruedDays = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_LeaveAccrualSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveAccrualSummaries_AbsenceTypes_AbsenceTypeId",
                        column: x => x.AbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaveAccrualSummaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WageTypeLeaves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    AbsenceTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_WageTypeLeaves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WageTypeLeaves_AbsenceTypes_AbsenceTypeId",
                        column: x => x.AbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WageTypeLeaves_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupAbsences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    AbsenceTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeYear = table.Column<double>(type: "float", nullable: false),
                    TotalDays = table.Column<int>(type: "int", nullable: false),
                    MaxLimit = table.Column<int>(type: "int", nullable: false),
                    AbsenceDeductionRuleId = table.Column<int>(type: "int", nullable: true),
                    MaxLimitReachNotificationInDays = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupAbsences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupAbsences_AbsenceDeductionRules_AbsenceDeductionRuleId",
                        column: x => x.AbsenceDeductionRuleId,
                        principalTable: "AbsenceDeductionRules",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupAbsences_AbsenceTypes_AbsenceTypeId",
                        column: x => x.AbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupAbsences_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupAbsenceTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    AbsenceTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupAbsenceTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupAbsenceTypes_AbsenceTypes_AbsenceTypeId",
                        column: x => x.AbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupAbsenceTypes_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupAssetAllowances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeSubGroupAssetAllowances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupAssetAllowances_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupBenefits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    BenefitTypeId = table.Column<int>(type: "int", nullable: false),
                    MaxAmount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupBenefits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupBenefits_BenefitTypes_BenefitTypeId",
                        column: x => x.BenefitTypeId,
                        principalTable: "BenefitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupBenefits_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupBonuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeYear = table.Column<double>(type: "float", nullable: false),
                    Percentage = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupBonuses_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupCommissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupCommissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupCommissions_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupFunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    FromPay = table.Column<double>(type: "float", nullable: false),
                    ToPay = table.Column<double>(type: "float", nullable: false),
                    FundDedType = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<double>(type: "float", nullable: false),
                    MinAmount = table.Column<double>(type: "float", nullable: false),
                    MaxAmount = table.Column<double>(type: "float", nullable: false),
                    FixAmount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupFunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupFunds_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupGratuities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeYear = table.Column<double>(type: "float", nullable: false),
                    GratuityDays = table.Column<double>(type: "float", nullable: false),
                    BasisDays = table.Column<double>(type: "float", nullable: false),
                    ImpactFromDateOfJoining = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupGratuities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupGratuities_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupHealthReimburses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DependantTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    AgeLimit = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupHealthReimburses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupHealthReimburses_DependantTypes_DependantTypeId",
                        column: x => x.DependantTypeId,
                        principalTable: "DependantTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupHealthReimburses_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    PayInterval = table.Column<int>(type: "int", nullable: false),
                    PayMonth = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupPayments_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
                    TicketTypeId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TicketCount = table.Column<int>(type: "int", nullable: false),
                    MaxAmount = table.Column<double>(type: "float", nullable: false),
                    FixAmount = table.Column<double>(type: "float", nullable: false),
                    FareType = table.Column<int>(type: "int", nullable: false),
                    AgeLimit = table.Column<double>(type: "float", nullable: false),
                    AccrualPeriod = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupTickets_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupWageTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupWageTypes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupWageTypes_EmployeeSubGroups_EmployeeSubGroupId",
                        column: x => x.EmployeeSubGroupId,
                        principalTable: "EmployeeSubGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CostRegisterDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CostRegisterId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    AccountInfoId = table.Column<int>(type: "int", nullable: false),
                    CostElement1Id = table.Column<int>(type: "int", nullable: true),
                    CostElement2Id = table.Column<int>(type: "int", nullable: true),
                    CostElement3Id = table.Column<int>(type: "int", nullable: true),
                    CostElement4Id = table.Column<int>(type: "int", nullable: true),
                    CostElement5Id = table.Column<int>(type: "int", nullable: true),
                    CostElement6Id = table.Column<int>(type: "int", nullable: true),
                    CostElement7Id = table.Column<int>(type: "int", nullable: true),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_CostRegisterDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_AccountInfos_AccountInfoId",
                        column: x => x.AccountInfoId,
                        principalTable: "AccountInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_CostElements_CostElement1Id",
                        column: x => x.CostElement1Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_CostElements_CostElement2Id",
                        column: x => x.CostElement2Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_CostElements_CostElement3Id",
                        column: x => x.CostElement3Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_CostElements_CostElement4Id",
                        column: x => x.CostElement4Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_CostElements_CostElement5Id",
                        column: x => x.CostElement5Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_CostElements_CostElement6Id",
                        column: x => x.CostElement6Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_CostElements_CostElement7Id",
                        column: x => x.CostElement7Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_CostRegisters_CostRegisterId",
                        column: x => x.CostRegisterId,
                        principalTable: "CostRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CostRegisterDetails_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeWageAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountInfoId = table.Column<int>(type: "int", nullable: false),
                    OffsetAccountInfoId = table.Column<int>(type: "int", nullable: true),
                    CostElement1Id = table.Column<int>(type: "int", nullable: true),
                    CostElement2Id = table.Column<int>(type: "int", nullable: true),
                    CostElement3Id = table.Column<int>(type: "int", nullable: true),
                    CostElement4Id = table.Column<int>(type: "int", nullable: true),
                    CostElement5Id = table.Column<int>(type: "int", nullable: true),
                    CostElement6Id = table.Column<int>(type: "int", nullable: true),
                    CostElement7Id = table.Column<int>(type: "int", nullable: true),
                    CostPercentage = table.Column<double>(type: "float", nullable: false),
                    MergeOffsetAccount = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeWageAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_AccountInfos_AccountInfoId",
                        column: x => x.AccountInfoId,
                        principalTable: "AccountInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_AccountInfos_OffsetAccountInfoId",
                        column: x => x.OffsetAccountInfoId,
                        principalTable: "AccountInfos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_CostElements_CostElement1Id",
                        column: x => x.CostElement1Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_CostElements_CostElement2Id",
                        column: x => x.CostElement2Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_CostElements_CostElement3Id",
                        column: x => x.CostElement3Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_CostElements_CostElement4Id",
                        column: x => x.CostElement4Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_CostElements_CostElement5Id",
                        column: x => x.CostElement5Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_CostElements_CostElement6Id",
                        column: x => x.CostElement6Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_CostElements_CostElement7Id",
                        column: x => x.CostElement7Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeWageAccounts_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollAreas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PayrollPeriodId = table.Column<int>(type: "int", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    CutOffDay = table.Column<int>(type: "int", nullable: false),
                    FICompanyId = table.Column<int>(type: "int", nullable: true),
                    FIBankId = table.Column<int>(type: "int", nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_PayrollAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAreas_AccountInfos_FIBankId",
                        column: x => x.FIBankId,
                        principalTable: "AccountInfos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollAreas_CostElements_FICompanyId",
                        column: x => x.FICompanyId,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollAreas_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollAreas_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollAreas_PayrollPeriods_PayrollPeriodId",
                        column: x => x.PayrollPeriodId,
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WageAccGroupDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WageAccGroupId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    AccountInfoId = table.Column<int>(type: "int", nullable: false),
                    OffsetAccountInfoType = table.Column<int>(type: "int", nullable: false),
                    OffsetAccountInfoId = table.Column<int>(type: "int", nullable: true),
                    CostElement1Id = table.Column<int>(type: "int", nullable: true),
                    CostElement2Id = table.Column<int>(type: "int", nullable: true),
                    CostElement3Id = table.Column<int>(type: "int", nullable: true),
                    CostElement4Id = table.Column<int>(type: "int", nullable: true),
                    CostElement5Id = table.Column<int>(type: "int", nullable: true),
                    CostElement6Id = table.Column<int>(type: "int", nullable: true),
                    CostElement7Id = table.Column<int>(type: "int", nullable: true),
                    CostPercentage = table.Column<double>(type: "float", nullable: false),
                    MergeOffsetAccount = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WageAccGroupDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_AccountInfos_AccountInfoId",
                        column: x => x.AccountInfoId,
                        principalTable: "AccountInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_AccountInfos_OffsetAccountInfoId",
                        column: x => x.OffsetAccountInfoId,
                        principalTable: "AccountInfos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_CostElements_CostElement1Id",
                        column: x => x.CostElement1Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_CostElements_CostElement2Id",
                        column: x => x.CostElement2Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_CostElements_CostElement3Id",
                        column: x => x.CostElement3Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_CostElements_CostElement4Id",
                        column: x => x.CostElement4Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_CostElements_CostElement5Id",
                        column: x => x.CostElement5Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_CostElements_CostElement6Id",
                        column: x => x.CostElement6Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_CostElements_CostElement7Id",
                        column: x => x.CostElement7Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_WageAccGroups_WageAccGroupId",
                        column: x => x.WageAccGroupId,
                        principalTable: "WageAccGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WageAccGroupDetails_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WageAccounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccountInfoId = table.Column<int>(type: "int", nullable: false),
                    OffsetAccountInfoId = table.Column<int>(type: "int", nullable: true),
                    CostElement1Id = table.Column<int>(type: "int", nullable: true),
                    CostElement2Id = table.Column<int>(type: "int", nullable: true),
                    CostElement3Id = table.Column<int>(type: "int", nullable: true),
                    CostElement4Id = table.Column<int>(type: "int", nullable: true),
                    CostElement5Id = table.Column<int>(type: "int", nullable: true),
                    CostElement6Id = table.Column<int>(type: "int", nullable: true),
                    CostElement7Id = table.Column<int>(type: "int", nullable: true),
                    CostPercentage = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_WageAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WageAccounts_AccountInfos_AccountInfoId",
                        column: x => x.AccountInfoId,
                        principalTable: "AccountInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WageAccounts_AccountInfos_OffsetAccountInfoId",
                        column: x => x.OffsetAccountInfoId,
                        principalTable: "AccountInfos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccounts_CostElements_CostElement1Id",
                        column: x => x.CostElement1Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccounts_CostElements_CostElement2Id",
                        column: x => x.CostElement2Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccounts_CostElements_CostElement3Id",
                        column: x => x.CostElement3Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccounts_CostElements_CostElement4Id",
                        column: x => x.CostElement4Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccounts_CostElements_CostElement5Id",
                        column: x => x.CostElement5Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccounts_CostElements_CostElement6Id",
                        column: x => x.CostElement6Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccounts_CostElements_CostElement7Id",
                        column: x => x.CostElement7Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WageAccounts_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryRegisterDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SalaryRegisterId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<double>(type: "float", nullable: false),
                    Number = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_SalaryRegisterDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryRegisterDetails_SalaryRegisters_SalaryRegisterId",
                        column: x => x.SalaryRegisterId,
                        principalTable: "SalaryRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalaryRegisterDetails_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationSalaryRegisterDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SimulationSalaryRegisterId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    Rate = table.Column<double>(type: "float", nullable: false),
                    Number = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_SimulationSalaryRegisterDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationSalaryRegisterDetails_SimulationSalaryRegisters_SimulationSalaryRegisterId",
                        column: x => x.SimulationSalaryRegisterId,
                        principalTable: "SimulationSalaryRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SimulationSalaryRegisterDetails_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubGroupBenefitDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeSubGroupBenefitId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DependantTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    AgeLimit = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubGroupBenefitDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupBenefitDetails_DependantTypes_DependantTypeId",
                        column: x => x.DependantTypeId,
                        principalTable: "DependantTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSubGroupBenefitDetails_EmployeeSubGroupBenefits_EmployeeSubGroupBenefitId",
                        column: x => x.EmployeeSubGroupBenefitId,
                        principalTable: "EmployeeSubGroupBenefits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollCalendars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PayrollAreaId = table.Column<int>(type: "int", nullable: false),
                    PayrollMonth = table.Column<int>(type: "int", nullable: false),
                    PayrollYear = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PayrollCalendars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollCalendars_PayrollAreas_PayrollAreaId",
                        column: x => x.PayrollAreaId,
                        principalTable: "PayrollAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollControlRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PayrollAreaId = table.Column<int>(type: "int", nullable: false),
                    PayrollMonth = table.Column<int>(type: "int", nullable: false),
                    PayrollYear = table.Column<int>(type: "int", nullable: false),
                    CalendarMonthStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CalendarMonthEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodLocked = table.Column<bool>(type: "bit", nullable: false),
                    PeriodClosed = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PayrollControlRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollControlRecords_PayrollAreas_PayrollAreaId",
                        column: x => x.PayrollAreaId,
                        principalTable: "PayrollAreas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryRegisterDaysDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SalaryRegisterDetailId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rate = table.Column<double>(type: "float", nullable: false),
                    Number = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_SalaryRegisterDaysDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryRegisterDaysDetails_SalaryRegisterDetails_SalaryRegisterDetailId",
                        column: x => x.SalaryRegisterDetailId,
                        principalTable: "SalaryRegisterDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationSalaryRegisterDaysDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SimulationSalaryRegisterDetailId = table.Column<int>(type: "int", nullable: false),
                    Period = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Rate = table.Column<double>(type: "float", nullable: false),
                    Number = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_SimulationSalaryRegisterDaysDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationSalaryRegisterDaysDetails_SimulationSalaryRegisterDetails_SimulationSalaryRegisterDetailId",
                        column: x => x.SimulationSalaryRegisterDetailId,
                        principalTable: "SimulationSalaryRegisterDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceDedRuleDetails_AbsenceDeductionRuleId",
                table: "AbsenceDedRuleDetails",
                column: "AbsenceDeductionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsencePayments_AbsenceTypeId",
                table: "AbsencePayments",
                column: "AbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsencePayments_WageTypeId",
                table: "AbsencePayments",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceTypeExceptions_AbsenceTypeId",
                table: "AbsenceTypeExceptions",
                column: "AbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceTypeExceptions_ExceptAbsenceTypeId",
                table: "AbsenceTypeExceptions",
                column: "ExceptAbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceTypes_AbsenceViolationRuleId",
                table: "AbsenceTypes",
                column: "AbsenceViolationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceViolationActionLogs_AbsenceViolationActionRecordId",
                table: "AbsenceViolationActionLogs",
                column: "AbsenceViolationActionRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceViolationRuleDetails_AbsenceViolationRuleId",
                table: "AbsenceViolationRuleDetails",
                column: "AbsenceViolationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceViolationRuleExceptions_AbsenceViolationRuleId",
                table: "AbsenceViolationRuleExceptions",
                column: "AbsenceViolationRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_AbsenceViolationRuleExceptions_EmployeeId",
                table: "AbsenceViolationRuleExceptions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountInfos_AccountTypeId",
                table: "AccountInfos",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceTypes_LinkLeaveQuotaId",
                table: "AttendanceTypes",
                column: "LinkLeaveQuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_AccountInfoId",
                table: "CostRegisterDetails",
                column: "AccountInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_CostElement1Id",
                table: "CostRegisterDetails",
                column: "CostElement1Id");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_CostElement2Id",
                table: "CostRegisterDetails",
                column: "CostElement2Id");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_CostElement3Id",
                table: "CostRegisterDetails",
                column: "CostElement3Id");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_CostElement4Id",
                table: "CostRegisterDetails",
                column: "CostElement4Id");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_CostElement5Id",
                table: "CostRegisterDetails",
                column: "CostElement5Id");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_CostElement6Id",
                table: "CostRegisterDetails",
                column: "CostElement6Id");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_CostElement7Id",
                table: "CostRegisterDetails",
                column: "CostElement7Id");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_CostRegisterId",
                table: "CostRegisterDetails",
                column: "CostRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_CurrencyId",
                table: "CostRegisterDetails",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisterDetails_WageTypeId",
                table: "CostRegisterDetails",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CostRegisters_EmployeeId",
                table: "CostRegisters",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayrollStatuses_EmployeeId",
                table: "EmployeePayrollStatuses",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupAbsences_AbsenceDeductionRuleId",
                table: "EmployeeSubGroupAbsences",
                column: "AbsenceDeductionRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupAbsences_AbsenceTypeId",
                table: "EmployeeSubGroupAbsences",
                column: "AbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupAbsences_EmployeeSubGroupId",
                table: "EmployeeSubGroupAbsences",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupAbsenceTypes_AbsenceTypeId",
                table: "EmployeeSubGroupAbsenceTypes",
                column: "AbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupAbsenceTypes_EmployeeSubGroupId",
                table: "EmployeeSubGroupAbsenceTypes",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupAssetAllowances_EmployeeSubGroupId",
                table: "EmployeeSubGroupAssetAllowances",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupBenefitDetails_DependantTypeId",
                table: "EmployeeSubGroupBenefitDetails",
                column: "DependantTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupBenefitDetails_EmployeeSubGroupBenefitId",
                table: "EmployeeSubGroupBenefitDetails",
                column: "EmployeeSubGroupBenefitId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupBenefits_BenefitTypeId",
                table: "EmployeeSubGroupBenefits",
                column: "BenefitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupBenefits_EmployeeSubGroupId",
                table: "EmployeeSubGroupBenefits",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupBonuses_EmployeeSubGroupId",
                table: "EmployeeSubGroupBonuses",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupCommissions_EmployeeSubGroupId",
                table: "EmployeeSubGroupCommissions",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupFunds_EmployeeSubGroupId",
                table: "EmployeeSubGroupFunds",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupGratuities_EmployeeSubGroupId",
                table: "EmployeeSubGroupGratuities",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupHealthReimburses_DependantTypeId",
                table: "EmployeeSubGroupHealthReimburses",
                column: "DependantTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupHealthReimburses_EmployeeSubGroupId",
                table: "EmployeeSubGroupHealthReimburses",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupPayments_EmployeeSubGroupId",
                table: "EmployeeSubGroupPayments",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroups_EmployeeGroupId",
                table: "EmployeeSubGroups",
                column: "EmployeeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupTickets_EmployeeSubGroupId",
                table: "EmployeeSubGroupTickets",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubGroupWageTypes_EmployeeSubGroupId",
                table: "EmployeeSubGroupWageTypes",
                column: "EmployeeSubGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccGroups_EmployeeId",
                table: "EmployeeWageAccGroups",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccGroups_WageAccGroupId",
                table: "EmployeeWageAccGroups",
                column: "WageAccGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_AccountInfoId",
                table: "EmployeeWageAccounts",
                column: "AccountInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_CostElement1Id",
                table: "EmployeeWageAccounts",
                column: "CostElement1Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_CostElement2Id",
                table: "EmployeeWageAccounts",
                column: "CostElement2Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_CostElement3Id",
                table: "EmployeeWageAccounts",
                column: "CostElement3Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_CostElement4Id",
                table: "EmployeeWageAccounts",
                column: "CostElement4Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_CostElement5Id",
                table: "EmployeeWageAccounts",
                column: "CostElement5Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_CostElement6Id",
                table: "EmployeeWageAccounts",
                column: "CostElement6Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_CostElement7Id",
                table: "EmployeeWageAccounts",
                column: "CostElement7Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_EmployeeId",
                table: "EmployeeWageAccounts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_OffsetAccountInfoId",
                table: "EmployeeWageAccounts",
                column: "OffsetAccountInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeWageAccounts_WageTypeId",
                table: "EmployeeWageAccounts",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_GratuityAccrualSummaries_EmployeeId",
                table: "GratuityAccrualSummaries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_IndemnityPayRuleDetails_IndemnityPayRuleId",
                table: "IndemnityPayRuleDetails",
                column: "IndemnityPayRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_IndemnityPayRules_CountryId",
                table: "IndemnityPayRules",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAccrualSummaries_AbsenceTypeId",
                table: "LeaveAccrualSummaries",
                column: "AbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveAccrualSummaries_EmployeeId",
                table: "LeaveAccrualSummaries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitWageAccGroups_OrganizationUnitId",
                table: "OrgUnitWageAccGroups",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitWageAccGroups_WageAccGroupId",
                table: "OrgUnitWageAccGroups",
                column: "WageAccGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAreas_CountryId",
                table: "PayrollAreas",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAreas_CurrencyId",
                table: "PayrollAreas",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAreas_FIBankId",
                table: "PayrollAreas",
                column: "FIBankId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAreas_FICompanyId",
                table: "PayrollAreas",
                column: "FICompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAreas_PayrollPeriodId",
                table: "PayrollAreas",
                column: "PayrollPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollCalendars_PayrollAreaId",
                table: "PayrollCalendars",
                column: "PayrollAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollControlRecords_PayrollAreaId",
                table: "PayrollControlRecords",
                column: "PayrollAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollLogs_EmployeeId",
                table: "PayrollLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRegisterDaysDetails_SalaryRegisterDetailId",
                table: "SalaryRegisterDaysDetails",
                column: "SalaryRegisterDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRegisterDetails_SalaryRegisterId",
                table: "SalaryRegisterDetails",
                column: "SalaryRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRegisterDetails_WageTypeId",
                table: "SalaryRegisterDetails",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRegisters_EmployeeId",
                table: "SalaryRegisters",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryRegisters_PayrollReasonTypeId",
                table: "SalaryRegisters",
                column: "PayrollReasonTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSalaryRegisterDaysDetails_SimulationSalaryRegisterDetailId",
                table: "SimulationSalaryRegisterDaysDetails",
                column: "SimulationSalaryRegisterDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSalaryRegisterDetails_SimulationSalaryRegisterId",
                table: "SimulationSalaryRegisterDetails",
                column: "SimulationSalaryRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSalaryRegisterDetails_WageTypeId",
                table: "SimulationSalaryRegisterDetails",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSalaryRegisters_EmployeeId",
                table: "SimulationSalaryRegisters",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationSalaryRegisters_PayrollReasonTypeId",
                table: "SimulationSalaryRegisters",
                column: "PayrollReasonTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurities_NationalityId",
                table: "SocialSecurities",
                column: "NationalityId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurityDetails_SocialSecurityId",
                table: "SocialSecurityDetails",
                column: "SocialSecurityId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurityEducationAllowances_EducationBranchId",
                table: "SocialSecurityEducationAllowances",
                column: "EducationBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurityEducationAllowances_EducationId",
                table: "SocialSecurityEducationAllowances",
                column: "EducationId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurityEducationAllowances_SocialSecurityId",
                table: "SocialSecurityEducationAllowances",
                column: "SocialSecurityId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurityMaritalAllowances_EducationBranchId",
                table: "SocialSecurityMaritalAllowances",
                column: "EducationBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurityMaritalAllowances_MaritalStatusId",
                table: "SocialSecurityMaritalAllowances",
                column: "MaritalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurityMaritalAllowances_SocialSecurityId",
                table: "SocialSecurityMaritalAllowances",
                column: "SocialSecurityId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurityRules_MaritalStatusId",
                table: "SocialSecurityRules",
                column: "MaritalStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurityRules_QualificationId",
                table: "SocialSecurityRules",
                column: "QualificationId");

            migrationBuilder.CreateIndex(
                name: "IX_SocialSecurityRules_SocialSecurityId",
                table: "SocialSecurityRules",
                column: "SocialSecurityId");

            migrationBuilder.CreateIndex(
                name: "IX_TempEmployeeBenefits_EmployeeId",
                table: "TempEmployeeBenefits",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TempEmployeeBenefits_TempBenefitTypeId",
                table: "TempEmployeeBenefits",
                column: "TempBenefitTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketAccrualSummaries_EmployeeId",
                table: "TicketAccrualSummaries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFares_TicketClassId",
                table: "TicketFares",
                column: "TicketClassId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFares_TicketPaxTypeId",
                table: "TicketFares",
                column: "TicketPaxTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketFares_TicketRouteId",
                table: "TicketFares",
                column: "TicketRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_WageTypeId",
                table: "TicketTypes",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_AccountInfoId",
                table: "WageAccGroupDetails",
                column: "AccountInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_CostElement1Id",
                table: "WageAccGroupDetails",
                column: "CostElement1Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_CostElement2Id",
                table: "WageAccGroupDetails",
                column: "CostElement2Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_CostElement3Id",
                table: "WageAccGroupDetails",
                column: "CostElement3Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_CostElement4Id",
                table: "WageAccGroupDetails",
                column: "CostElement4Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_CostElement5Id",
                table: "WageAccGroupDetails",
                column: "CostElement5Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_CostElement6Id",
                table: "WageAccGroupDetails",
                column: "CostElement6Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_CostElement7Id",
                table: "WageAccGroupDetails",
                column: "CostElement7Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_OffsetAccountInfoId",
                table: "WageAccGroupDetails",
                column: "OffsetAccountInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_WageAccGroupId",
                table: "WageAccGroupDetails",
                column: "WageAccGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccGroupDetails_WageTypeId",
                table: "WageAccGroupDetails",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccounts_AccountInfoId",
                table: "WageAccounts",
                column: "AccountInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccounts_CostElement1Id",
                table: "WageAccounts",
                column: "CostElement1Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccounts_CostElement2Id",
                table: "WageAccounts",
                column: "CostElement2Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccounts_CostElement3Id",
                table: "WageAccounts",
                column: "CostElement3Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccounts_CostElement4Id",
                table: "WageAccounts",
                column: "CostElement4Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccounts_CostElement5Id",
                table: "WageAccounts",
                column: "CostElement5Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccounts_CostElement6Id",
                table: "WageAccounts",
                column: "CostElement6Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccounts_CostElement7Id",
                table: "WageAccounts",
                column: "CostElement7Id");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccounts_OffsetAccountInfoId",
                table: "WageAccounts",
                column: "OffsetAccountInfoId");

            migrationBuilder.CreateIndex(
                name: "IX_WageAccounts_WageTypeId",
                table: "WageAccounts",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WageTypeExceptions_ExceptWageTypeId",
                table: "WageTypeExceptions",
                column: "ExceptWageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WageTypeExceptions_WageTypeId",
                table: "WageTypeExceptions",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WageTypeLeaves_AbsenceTypeId",
                table: "WageTypeLeaves",
                column: "AbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WageTypeLeaves_WageTypeId",
                table: "WageTypeLeaves",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WageTypeRegisters_EmployeeId",
                table: "WageTypeRegisters",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WageTypes_ArrearSourceWageTypeId",
                table: "WageTypes",
                column: "ArrearSourceWageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WageTypeSecurityRoles_RoleId",
                table: "WageTypeSecurityRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WageTypeSecurityRoles_WageTypeId",
                table: "WageTypeSecurityRoles",
                column: "WageTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AbsenceDedRuleDetails");

            migrationBuilder.DropTable(
                name: "AbsencePayments");

            migrationBuilder.DropTable(
                name: "AbsenceTypeExceptions");

            migrationBuilder.DropTable(
                name: "AbsenceViolationActionLogs");

            migrationBuilder.DropTable(
                name: "AbsenceViolationRuleDetails");

            migrationBuilder.DropTable(
                name: "AbsenceViolationRuleExceptions");

            migrationBuilder.DropTable(
                name: "AttendanceTypes");

            migrationBuilder.DropTable(
                name: "AXBatches");

            migrationBuilder.DropTable(
                name: "CostRegisterDetails");

            migrationBuilder.DropTable(
                name: "EmployeePayrollStatuses");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupAbsences");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupAbsenceTypes");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupAssetAllowances");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupBenefitDetails");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupBonuses");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupCommissions");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupFunds");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupGratuities");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupHealthReimburses");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupPayments");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupTickets");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupWageTypes");

            migrationBuilder.DropTable(
                name: "EmployeeWageAccGroups");

            migrationBuilder.DropTable(
                name: "EmployeeWageAccounts");

            migrationBuilder.DropTable(
                name: "GratuityAccrualSummaries");

            migrationBuilder.DropTable(
                name: "IndemnityPayRuleDetails");

            migrationBuilder.DropTable(
                name: "LeaveAccrualSummaries");

            migrationBuilder.DropTable(
                name: "OrgUnitWageAccGroups");

            migrationBuilder.DropTable(
                name: "PayrollCalendars");

            migrationBuilder.DropTable(
                name: "PayrollControlRecords");

            migrationBuilder.DropTable(
                name: "PayrollLogs");

            migrationBuilder.DropTable(
                name: "SalaryRegisterDaysDetails");

            migrationBuilder.DropTable(
                name: "SimulationSalaryRegisterDaysDetails");

            migrationBuilder.DropTable(
                name: "SocialSecurityDetails");

            migrationBuilder.DropTable(
                name: "SocialSecurityEducationAllowances");

            migrationBuilder.DropTable(
                name: "SocialSecurityMaritalAllowances");

            migrationBuilder.DropTable(
                name: "SocialSecurityRules");

            migrationBuilder.DropTable(
                name: "TempEmployeeBenefits");

            migrationBuilder.DropTable(
                name: "TicketAccrualSummaries");

            migrationBuilder.DropTable(
                name: "TicketFares");

            migrationBuilder.DropTable(
                name: "TicketTypes");

            migrationBuilder.DropTable(
                name: "WageAccGroupDetails");

            migrationBuilder.DropTable(
                name: "WageAccounts");

            migrationBuilder.DropTable(
                name: "WageTypeExceptions");

            migrationBuilder.DropTable(
                name: "WageTypeLeaves");

            migrationBuilder.DropTable(
                name: "WageTypeRegisters");

            migrationBuilder.DropTable(
                name: "WageTypeSecurityRoles");

            migrationBuilder.DropTable(
                name: "AbsenceViolationActionRecords");

            migrationBuilder.DropTable(
                name: "CostRegisters");

            migrationBuilder.DropTable(
                name: "AbsenceDeductionRules");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroupBenefits");

            migrationBuilder.DropTable(
                name: "IndemnityPayRules");

            migrationBuilder.DropTable(
                name: "PayrollAreas");

            migrationBuilder.DropTable(
                name: "SalaryRegisterDetails");

            migrationBuilder.DropTable(
                name: "SimulationSalaryRegisterDetails");

            migrationBuilder.DropTable(
                name: "SocialSecurities");

            migrationBuilder.DropTable(
                name: "TempBenefitTypes");

            migrationBuilder.DropTable(
                name: "TicketClasses");

            migrationBuilder.DropTable(
                name: "TicketPaxTypes");

            migrationBuilder.DropTable(
                name: "TicketRoutes");

            migrationBuilder.DropTable(
                name: "WageAccGroups");

            migrationBuilder.DropTable(
                name: "AbsenceTypes");

            migrationBuilder.DropTable(
                name: "EmployeeSubGroups");

            migrationBuilder.DropTable(
                name: "AccountInfos");

            migrationBuilder.DropTable(
                name: "PayrollPeriods");

            migrationBuilder.DropTable(
                name: "SalaryRegisters");

            migrationBuilder.DropTable(
                name: "SimulationSalaryRegisters");

            migrationBuilder.DropTable(
                name: "WageTypes");

            migrationBuilder.DropTable(
                name: "AbsenceViolationRules");

            migrationBuilder.DropTable(
                name: "EmployeeGroups");

            migrationBuilder.DropTable(
                name: "PayrollAccountTypes");

            migrationBuilder.DropTable(
                name: "PayrollReasonTypes");
        }
    }
}
