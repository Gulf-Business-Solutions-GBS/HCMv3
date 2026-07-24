using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_PersonnelAdministration_DeferredBucket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PayrollAreaId",
                table: "OrgUnitProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeAbsAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AbsenceTypeId = table.Column<int>(type: "int", nullable: false),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuotaAdjusted = table.Column<double>(type: "float", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AdjustmentPaymentType = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeAbsAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsAdjustments_AbsenceTypes_AbsenceTypeId",
                        column: x => x.AbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsAdjustments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAbsenceQuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AbsenceTypeId = table.Column<int>(type: "int", nullable: false),
                    EntitlementFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntitlementTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeductionFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeductionTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuotaAccrued = table.Column<double>(type: "float", nullable: false),
                    ActualQuotaAccrued = table.Column<double>(type: "float", nullable: false),
                    QuotaUsed = table.Column<double>(type: "float", nullable: false),
                    QuotaAdjusted = table.Column<double>(type: "float", nullable: false),
                    QuotaForfeited = table.Column<double>(type: "float", nullable: false),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeAbsenceQuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsenceQuotas_AbsenceTypes_AbsenceTypeId",
                        column: x => x.AbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsenceQuotas_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAbsences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AbsenceTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HalfdayType = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AbsenceHours = table.Column<double>(type: "float", nullable: false),
                    IsTravelling = table.Column<bool>(type: "bit", nullable: false),
                    CalendarDays = table.Column<double>(type: "float", nullable: false),
                    TotalLeaveDays = table.Column<double>(type: "float", nullable: false),
                    LeavePaymentType = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdvanceLeavePaid = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    ReturnDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsReturned = table.Column<bool>(type: "bit", nullable: false),
                    InitialEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExtAbsenceTypeId = table.Column<int>(type: "int", nullable: true),
                    ExtStartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtEndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExtTotalLeaveDays = table.Column<double>(type: "float", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeAbsences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsences_AbsenceTypes_AbsenceTypeId",
                        column: x => x.AbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsences_AbsenceTypes_ExtAbsenceTypeId",
                        column: x => x.ExtAbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAbsences_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAbsQuotaForfeits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AbsenceTypeId = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuotaForfeited = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeAbsQuotaForfeits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsQuotaForfeits_AbsenceTypes_AbsenceTypeId",
                        column: x => x.AbsenceTypeId,
                        principalTable: "AbsenceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAbsQuotaForfeits_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAccountAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeAccountAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAccountAssignments_CostElements_CostElement1Id",
                        column: x => x.CostElement1Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAccountAssignments_CostElements_CostElement2Id",
                        column: x => x.CostElement2Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAccountAssignments_CostElements_CostElement3Id",
                        column: x => x.CostElement3Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAccountAssignments_CostElements_CostElement4Id",
                        column: x => x.CostElement4Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAccountAssignments_CostElements_CostElement5Id",
                        column: x => x.CostElement5Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAccountAssignments_CostElements_CostElement6Id",
                        column: x => x.CostElement6Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAccountAssignments_CostElements_CostElement7Id",
                        column: x => x.CostElement7Id,
                        principalTable: "CostElements",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeAccountAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAssetInvoices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InvoiceAmount = table.Column<double>(type: "float", nullable: false),
                    EmployeePlanAmount = table.Column<double>(type: "float", nullable: false),
                    DeductionAmount = table.Column<double>(type: "float", nullable: false),
                    DeductFromEmployee = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeAssetInvoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAssetInvoices_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAssetInvoices_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssetDeductionMode = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAssets_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAssets_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAttendanceQuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EntitlementFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntitlementTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeductionFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeductionTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuotaAccrued = table.Column<double>(type: "float", nullable: false),
                    QuotaUsed = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeAttendanceQuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendanceQuotas_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAttendances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AttendanceTypeId = table.Column<int>(type: "int", nullable: false),
                    AttendanceUnit = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AttendanceHours = table.Column<double>(type: "float", nullable: false),
                    TotalAttendDays = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendances_AttendanceTypes_AttendanceTypeId",
                        column: x => x.AttendanceTypeId,
                        principalTable: "AttendanceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeAttendances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeBanks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    BankId = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BankCountryId = table.Column<int>(type: "int", nullable: true),
                    PaymentType = table.Column<int>(type: "int", nullable: false),
                    AccountNum = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PaymentCurrencyId = table.Column<int>(type: "int", nullable: true),
                    IBAN = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BankBranch = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PayerBankId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeBanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeBanks_Banks_BankId",
                        column: x => x.BankId,
                        principalTable: "Banks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeBanks_Banks_PayerBankId",
                        column: x => x.PayerBankId,
                        principalTable: "Banks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeBanks_Countries_BankCountryId",
                        column: x => x.BankCountryId,
                        principalTable: "Countries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeBanks_Currencies_PaymentCurrencyId",
                        column: x => x.PaymentCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeBanks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeBasicPays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    PayType = table.Column<int>(type: "int", nullable: false),
                    PayInterval = table.Column<int>(type: "int", nullable: false),
                    EmployeeActionId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeBasicPays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeBasicPays_EmployeeActions_EmployeeActionId",
                        column: x => x.EmployeeActionId,
                        principalTable: "EmployeeActions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeBasicPays_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeBenefitExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ExceptionBenefitType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeBenefitExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeBenefitExceptions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCostDistributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CostCount = table.Column<int>(type: "int", nullable: false),
                    UsedDays = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeCostDistributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCostDistributions_AbpOrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeCostDistributions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDependants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DependantTypeId = table.Column<int>(type: "int", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TicketTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SpecialNeed = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInSocialSecurity = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInHealthInsurance = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeDependants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDependants_DependantTypes_DependantTypeId",
                        column: x => x.DependantTypeId,
                        principalTable: "DependantTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDependants_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDependants_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "TicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDisciplinaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: true),
                    WorkLocationId = table.Column<int>(type: "int", nullable: true),
                    DisciplinaryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisciplinaryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DisciplinaryTypeId = table.Column<int>(type: "int", nullable: false),
                    ReportedById = table.Column<int>(type: "int", nullable: true),
                    WLSendToEmp = table.Column<bool>(type: "bit", nullable: false),
                    WLRecvFromEmp = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    DisciplinaryDeductionTypeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeDisciplinaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDisciplinaries_AbpOrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeDisciplinaries_DisciplinaryDeductionTypes_DisciplinaryDeductionTypeId",
                        column: x => x.DisciplinaryDeductionTypeId,
                        principalTable: "DisciplinaryDeductionTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeDisciplinaries_DisciplinaryTypes_DisciplinaryTypeId",
                        column: x => x.DisciplinaryTypeId,
                        principalTable: "DisciplinaryTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDisciplinaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDisciplinaries_Employees_ReportedById",
                        column: x => x.ReportedById,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeDisciplinaries_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeExtdWorkLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    WorkLocationId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeExtdWorkLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeExtdWorkLocations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeExtdWorkLocations_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeFunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    PaidAmount = table.Column<double>(type: "float", nullable: false),
                    BalanceAmount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeFunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeFunds_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeGratuities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    GratuityPaidDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GratuityPaidDays = table.Column<double>(type: "float", nullable: false),
                    ResetGratuityCalc = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeGratuities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeGratuities_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeGratuityQuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EntitlementFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntitlementTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeductionFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeductionTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuotaAccrued = table.Column<double>(type: "float", nullable: false),
                    QuotaUsed = table.Column<double>(type: "float", nullable: false),
                    QuotaAdjusted = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeGratuityQuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeGratuityQuotas_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeGrievances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    GrievanceTypeId = table.Column<int>(type: "int", nullable: false),
                    GrievanceDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeGrievances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeGrievances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeGrievances_GrievanceTypes_GrievanceTypeId",
                        column: x => x.GrievanceTypeId,
                        principalTable: "GrievanceTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeHealthReimburses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeHealthReimburses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeHealthReimburses_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLoans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LoanTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecoveryStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualAmount = table.Column<double>(type: "float", nullable: false),
                    DueAmount = table.Column<double>(type: "float", nullable: false),
                    BalanceDueAmount = table.Column<double>(type: "float", nullable: false),
                    FixedAmount = table.Column<double>(type: "float", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecoverFracInLastTrans = table.Column<bool>(type: "bit", nullable: false),
                    IncludeInLoanPay = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeLoans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeLoans_LoanTypes_LoanTypeId",
                        column: x => x.LoanTypeId,
                        principalTable: "LoanTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeNonPayrollTrans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NotificationSent = table.Column<bool>(type: "bit", nullable: false),
                    PayableBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PaymentMethod = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_EmployeeNonPayrollTrans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeNonPayrollTrans_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeNonPayrollTrans_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeNonPayrollTrans_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeOnHolds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OnHold = table.Column<bool>(type: "bit", nullable: false),
                    OnHoldBasicPay = table.Column<bool>(type: "bit", nullable: false),
                    OnHoldLeavePay = table.Column<bool>(type: "bit", nullable: false),
                    OnHoldIndemnityPay = table.Column<bool>(type: "bit", nullable: false),
                    OnHoldAirTicketPay = table.Column<bool>(type: "bit", nullable: false),
                    OnHoldLoanDeduction = table.Column<bool>(type: "bit", nullable: false),
                    OnHoldAllEarnings = table.Column<bool>(type: "bit", nullable: false),
                    OnHoldAllDeductions = table.Column<bool>(type: "bit", nullable: false),
                    BankTransfer = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmployeeActionId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeOnHolds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeOnHolds_EmployeeActions_EmployeeActionId",
                        column: x => x.EmployeeActionId,
                        principalTable: "EmployeeActions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeOnHolds_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeOvertimes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OvertimeTypeId = table.Column<int>(type: "int", nullable: false),
                    OvertimeHours = table.Column<double>(type: "float", nullable: false),
                    ActualOvertimeHours = table.Column<double>(type: "float", nullable: false),
                    ApprovedOvertimeHours = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeOvertimes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeOvertimes_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeOvertimes_OvertimeTypes_OvertimeTypeId",
                        column: x => x.OvertimeTypeId,
                        principalTable: "OvertimeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeePayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UnitType = table.Column<int>(type: "int", nullable: false),
                    CostCenterId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeePayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeePayments_CostCenters_CostCenterId",
                        column: x => x.CostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeePayments_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeePayments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeePayments_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRecurringPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PayInterval = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeRecurringPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRecurringPayments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeRecurringPayments_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRemunerations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentNumber = table.Column<double>(type: "float", nullable: false),
                    PaymentUnit = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeRemunerations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRemunerations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeRemunerations_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRewards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    RewardType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RewardDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Certificate = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeRewards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRewards_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSocialSecurities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SocialSecurityNum = table.Column<long>(type: "bigint", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SocialSecurityContrib = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSocialSecurities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSocialSecurities_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSocialSecurityClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ClaimDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeContributionAmount = table.Column<double>(type: "float", nullable: false),
                    EmployerContributionAmount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSocialSecurityClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSocialSecurityClaims_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSponsors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SponsorId = table.Column<int>(type: "int", nullable: false),
                    GovtJobTitleId = table.Column<int>(type: "int", nullable: false),
                    EmpSponsorRelation = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContractSalary = table.Column<double>(type: "float", nullable: false),
                    SalaryCurrencyId = table.Column<int>(type: "int", nullable: false),
                    ResidencyType = table.Column<int>(type: "int", nullable: false),
                    WorkVisaTypeId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSponsors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSponsors_Currencies_SalaryCurrencyId",
                        column: x => x.SalaryCurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSponsors_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSponsors_GovtJobTitles_GovtJobTitleId",
                        column: x => x.GovtJobTitleId,
                        principalTable: "GovtJobTitles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSponsors_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeSponsors_WorkVisaTypes_WorkVisaTypeId",
                        column: x => x.WorkVisaTypeId,
                        principalTable: "WorkVisaTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TaskTypeId = table.Column<int>(type: "int", nullable: false),
                    TaskDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TaskStatus = table.Column<int>(type: "int", nullable: false),
                    ReminderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MonitoringType = table.Column<int>(type: "int", nullable: false),
                    MonitoringCount = table.Column<int>(type: "int", nullable: false),
                    MonitoringUnit = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTasks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeTasks_TaskTypes_TaskTypeId",
                        column: x => x.TaskTypeId,
                        principalTable: "TaskTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTicketQuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TicketTypeId = table.Column<int>(type: "int", nullable: false),
                    EntitlementFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntitlementTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeductionFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DeductionTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuotaAccrued = table.Column<double>(type: "float", nullable: false),
                    QuotaUsed = table.Column<double>(type: "float", nullable: false),
                    QuotaAdjusted = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeTicketQuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTicketQuotas_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeTicketQuotas_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "TicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTicketRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TicketRouteId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeTicketRoutes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTicketRoutes_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeTicketRoutes_TicketRoutes_TicketRouteId",
                        column: x => x.TicketRouteId,
                        principalTable: "TicketRoutes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TicketTypeId = table.Column<int>(type: "int", nullable: false),
                    TicketDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Compensate = table.Column<double>(type: "float", nullable: false),
                    TicketPaymentType = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    AdvancedTicketPaid = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    OverrideAmount = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeTickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTickets_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeTickets_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "TicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTktAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TicketTypeId = table.Column<int>(type: "int", nullable: false),
                    AdjustmentType = table.Column<int>(type: "int", nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    QuotaAccrued = table.Column<double>(type: "float", nullable: false),
                    QuotaAdjusted = table.Column<double>(type: "float", nullable: false),
                    TicketPaymentType = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeTktAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTktAdjustments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeTktAdjustments_TicketTypes_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalTable: "TicketTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SponsorBanks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SponsorId = table.Column<int>(type: "int", nullable: false),
                    EmployeeBankId = table.Column<int>(type: "int", nullable: true),
                    PaymentBankId = table.Column<int>(type: "int", nullable: false),
                    AccountNumber = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IBAN = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
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
                    table.PrimaryKey("PK_SponsorBanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SponsorBanks_Banks_EmployeeBankId",
                        column: x => x.EmployeeBankId,
                        principalTable: "Banks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SponsorBanks_Banks_PaymentBankId",
                        column: x => x.PaymentBankId,
                        principalTable: "Banks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SponsorBanks_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeBasicPayDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeBasicPayId = table.Column<int>(type: "int", nullable: false),
                    WageTypeId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    DependantNumber = table.Column<double>(type: "float", nullable: false),
                    DependantUnit = table.Column<int>(type: "int", nullable: false),
                    DependantWageTypeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_EmployeeBasicPayDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeBasicPayDetails_Currencies_CurrencyId",
                        column: x => x.CurrencyId,
                        principalTable: "Currencies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeBasicPayDetails_EmployeeBasicPays_EmployeeBasicPayId",
                        column: x => x.EmployeeBasicPayId,
                        principalTable: "EmployeeBasicPays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeBasicPayDetails_WageTypes_DependantWageTypeId",
                        column: x => x.DependantWageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeBasicPayDetails_WageTypes_WageTypeId",
                        column: x => x.WageTypeId,
                        principalTable: "WageTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDependantExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeDependantId = table.Column<int>(type: "int", nullable: false),
                    ExceptionBenefitType = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeDependantExceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDependantExceptions_EmployeeDependants_EmployeeDependantId",
                        column: x => x.EmployeeDependantId,
                        principalTable: "EmployeeDependants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeFundDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeFundId = table.Column<int>(type: "int", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    FundPayType = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeFundDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeFundDetails_EmployeeFunds_EmployeeFundId",
                        column: x => x.EmployeeFundId,
                        principalTable: "EmployeeFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeFundExemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeFundId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeFundExemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeFundExemptions_EmployeeFunds_EmployeeFundId",
                        column: x => x.EmployeeFundId,
                        principalTable: "EmployeeFunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLoanExemptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeLoanId = table.Column<int>(type: "int", nullable: false),
                    ExemptionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeLoanExemptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLoanExemptions_EmployeeLoans_EmployeeLoanId",
                        column: x => x.EmployeeLoanId,
                        principalTable: "EmployeeLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeLoanRecoveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeLoanId = table.Column<int>(type: "int", nullable: false),
                    RecoveryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecoveryAmount = table.Column<double>(type: "float", nullable: false),
                    LoanRecoveryMethod = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeLoanRecoveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeLoanRecoveries_EmployeeLoans_EmployeeLoanId",
                        column: x => x.EmployeeLoanId,
                        principalTable: "EmployeeLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanInstallments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeLoanId = table.Column<int>(type: "int", nullable: false),
                    InstallmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstallmentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InstallmentStatus = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_LoanInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanInstallments_EmployeeLoans_EmployeeLoanId",
                        column: x => x.EmployeeLoanId,
                        principalTable: "EmployeeLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeFnFPayments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EmployeeActionId = table.Column<int>(type: "int", nullable: true),
                    EmployeeBasicPayDetailId = table.Column<int>(type: "int", nullable: true),
                    CompensationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeaveCompensateDays = table.Column<double>(type: "float", nullable: false),
                    GratuityCompensateDays = table.Column<double>(type: "float", nullable: false),
                    IndemnityCompensateAmount = table.Column<double>(type: "float", nullable: false),
                    IndemnityPayAdjustment = table.Column<double>(type: "float", nullable: false),
                    TicketCompensateCount = table.Column<double>(type: "float", nullable: false),
                    Dependant1TicketCompensateCount = table.Column<double>(type: "float", nullable: false),
                    Dependant2TicketCompensateCount = table.Column<double>(type: "float", nullable: false),
                    Dependant3TicketCompensateCount = table.Column<double>(type: "float", nullable: false),
                    CompensateLoanAmount = table.Column<double>(type: "float", nullable: false),
                    CompensateBonusPay = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeFnFPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeFnFPayments_EmployeeActions_EmployeeActionId",
                        column: x => x.EmployeeActionId,
                        principalTable: "EmployeeActions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeFnFPayments_EmployeeBasicPayDetails_EmployeeBasicPayDetailId",
                        column: x => x.EmployeeBasicPayDetailId,
                        principalTable: "EmployeeBasicPayDetails",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EmployeeFnFPayments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrgUnitProfiles_PayrollAreaId",
                table: "OrgUnitProfiles",
                column: "PayrollAreaId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsAdjustments_AbsenceTypeId",
                table: "EmployeeAbsAdjustments",
                column: "AbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsAdjustments_EmployeeId",
                table: "EmployeeAbsAdjustments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsenceQuotas_AbsenceTypeId",
                table: "EmployeeAbsenceQuotas",
                column: "AbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsenceQuotas_EmployeeId",
                table: "EmployeeAbsenceQuotas",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsences_AbsenceTypeId",
                table: "EmployeeAbsences",
                column: "AbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsences_EmployeeId",
                table: "EmployeeAbsences",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsences_ExtAbsenceTypeId",
                table: "EmployeeAbsences",
                column: "ExtAbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsQuotaForfeits_AbsenceTypeId",
                table: "EmployeeAbsQuotaForfeits",
                column: "AbsenceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAbsQuotaForfeits_EmployeeId",
                table: "EmployeeAbsQuotaForfeits",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccountAssignments_CostElement1Id",
                table: "EmployeeAccountAssignments",
                column: "CostElement1Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccountAssignments_CostElement2Id",
                table: "EmployeeAccountAssignments",
                column: "CostElement2Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccountAssignments_CostElement3Id",
                table: "EmployeeAccountAssignments",
                column: "CostElement3Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccountAssignments_CostElement4Id",
                table: "EmployeeAccountAssignments",
                column: "CostElement4Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccountAssignments_CostElement5Id",
                table: "EmployeeAccountAssignments",
                column: "CostElement5Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccountAssignments_CostElement6Id",
                table: "EmployeeAccountAssignments",
                column: "CostElement6Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccountAssignments_CostElement7Id",
                table: "EmployeeAccountAssignments",
                column: "CostElement7Id");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccountAssignments_EmployeeId",
                table: "EmployeeAccountAssignments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssetInvoices_AssetId",
                table: "EmployeeAssetInvoices",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssetInvoices_EmployeeId",
                table: "EmployeeAssetInvoices",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssets_AssetId",
                table: "EmployeeAssets",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssets_EmployeeId",
                table: "EmployeeAssets",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendanceQuotas_EmployeeId",
                table: "EmployeeAttendanceQuotas",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendances_AttendanceTypeId",
                table: "EmployeeAttendances",
                column: "AttendanceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAttendances_EmployeeId",
                table: "EmployeeAttendances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBanks_BankCountryId",
                table: "EmployeeBanks",
                column: "BankCountryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBanks_BankId",
                table: "EmployeeBanks",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBanks_EmployeeId",
                table: "EmployeeBanks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBanks_PayerBankId",
                table: "EmployeeBanks",
                column: "PayerBankId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBanks_PaymentCurrencyId",
                table: "EmployeeBanks",
                column: "PaymentCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBasicPayDetails_CurrencyId",
                table: "EmployeeBasicPayDetails",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBasicPayDetails_DependantWageTypeId",
                table: "EmployeeBasicPayDetails",
                column: "DependantWageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBasicPayDetails_EmployeeBasicPayId",
                table: "EmployeeBasicPayDetails",
                column: "EmployeeBasicPayId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBasicPayDetails_WageTypeId",
                table: "EmployeeBasicPayDetails",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBasicPays_EmployeeActionId",
                table: "EmployeeBasicPays",
                column: "EmployeeActionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBasicPays_EmployeeId",
                table: "EmployeeBasicPays",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeBenefitExceptions_EmployeeId",
                table: "EmployeeBenefitExceptions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCostDistributions_EmployeeId",
                table: "EmployeeCostDistributions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCostDistributions_OrganizationUnitId",
                table: "EmployeeCostDistributions",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDependantExceptions_EmployeeDependantId",
                table: "EmployeeDependantExceptions",
                column: "EmployeeDependantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDependants_DependantTypeId",
                table: "EmployeeDependants",
                column: "DependantTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDependants_EmployeeId",
                table: "EmployeeDependants",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDependants_TicketTypeId",
                table: "EmployeeDependants",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDisciplinaries_DisciplinaryDeductionTypeId",
                table: "EmployeeDisciplinaries",
                column: "DisciplinaryDeductionTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDisciplinaries_DisciplinaryTypeId",
                table: "EmployeeDisciplinaries",
                column: "DisciplinaryTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDisciplinaries_EmployeeId",
                table: "EmployeeDisciplinaries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDisciplinaries_OrganizationUnitId",
                table: "EmployeeDisciplinaries",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDisciplinaries_ReportedById",
                table: "EmployeeDisciplinaries",
                column: "ReportedById");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDisciplinaries_WorkLocationId",
                table: "EmployeeDisciplinaries",
                column: "WorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExtdWorkLocations_EmployeeId",
                table: "EmployeeExtdWorkLocations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeExtdWorkLocations_WorkLocationId",
                table: "EmployeeExtdWorkLocations",
                column: "WorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFnFPayments_EmployeeActionId",
                table: "EmployeeFnFPayments",
                column: "EmployeeActionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFnFPayments_EmployeeBasicPayDetailId",
                table: "EmployeeFnFPayments",
                column: "EmployeeBasicPayDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFnFPayments_EmployeeId",
                table: "EmployeeFnFPayments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFundDetails_EmployeeFundId",
                table: "EmployeeFundDetails",
                column: "EmployeeFundId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFundExemptions_EmployeeFundId",
                table: "EmployeeFundExemptions",
                column: "EmployeeFundId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeFunds_EmployeeId",
                table: "EmployeeFunds",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGratuities_EmployeeId",
                table: "EmployeeGratuities",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGratuityQuotas_EmployeeId",
                table: "EmployeeGratuityQuotas",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGrievances_EmployeeId",
                table: "EmployeeGrievances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeGrievances_GrievanceTypeId",
                table: "EmployeeGrievances",
                column: "GrievanceTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeHealthReimburses_EmployeeId",
                table: "EmployeeHealthReimburses",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoanExemptions_EmployeeLoanId",
                table: "EmployeeLoanExemptions",
                column: "EmployeeLoanId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoanRecoveries_EmployeeLoanId",
                table: "EmployeeLoanRecoveries",
                column: "EmployeeLoanId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_EmployeeId",
                table: "EmployeeLoans",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeLoans_LoanTypeId",
                table: "EmployeeLoans",
                column: "LoanTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeNonPayrollTrans_CurrencyId",
                table: "EmployeeNonPayrollTrans",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeNonPayrollTrans_EmployeeId",
                table: "EmployeeNonPayrollTrans",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeNonPayrollTrans_WageTypeId",
                table: "EmployeeNonPayrollTrans",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOnHolds_EmployeeActionId",
                table: "EmployeeOnHolds",
                column: "EmployeeActionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOnHolds_EmployeeId",
                table: "EmployeeOnHolds",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertimes_EmployeeId",
                table: "EmployeeOvertimes",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOvertimes_OvertimeTypeId",
                table: "EmployeeOvertimes",
                column: "OvertimeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayments_CostCenterId",
                table: "EmployeePayments",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayments_CurrencyId",
                table: "EmployeePayments",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayments_EmployeeId",
                table: "EmployeePayments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayments_WageTypeId",
                table: "EmployeePayments",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecurringPayments_EmployeeId",
                table: "EmployeeRecurringPayments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRecurringPayments_WageTypeId",
                table: "EmployeeRecurringPayments",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRemunerations_EmployeeId",
                table: "EmployeeRemunerations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRemunerations_WageTypeId",
                table: "EmployeeRemunerations",
                column: "WageTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRewards_EmployeeId",
                table: "EmployeeRewards",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSocialSecurities_EmployeeId",
                table: "EmployeeSocialSecurities",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSocialSecurityClaims_EmployeeId",
                table: "EmployeeSocialSecurityClaims",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSponsors_EmployeeId",
                table: "EmployeeSponsors",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSponsors_GovtJobTitleId",
                table: "EmployeeSponsors",
                column: "GovtJobTitleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSponsors_SalaryCurrencyId",
                table: "EmployeeSponsors",
                column: "SalaryCurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSponsors_SponsorId",
                table: "EmployeeSponsors",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSponsors_WorkVisaTypeId",
                table: "EmployeeSponsors",
                column: "WorkVisaTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTasks_EmployeeId",
                table: "EmployeeTasks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTasks_TaskTypeId",
                table: "EmployeeTasks",
                column: "TaskTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTicketQuotas_EmployeeId",
                table: "EmployeeTicketQuotas",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTicketQuotas_TicketTypeId",
                table: "EmployeeTicketQuotas",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTicketRoutes_EmployeeId",
                table: "EmployeeTicketRoutes",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTicketRoutes_TicketRouteId",
                table: "EmployeeTicketRoutes",
                column: "TicketRouteId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTickets_EmployeeId",
                table: "EmployeeTickets",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTickets_TicketTypeId",
                table: "EmployeeTickets",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTktAdjustments_EmployeeId",
                table: "EmployeeTktAdjustments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTktAdjustments_TicketTypeId",
                table: "EmployeeTktAdjustments",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanInstallments_EmployeeLoanId",
                table: "LoanInstallments",
                column: "EmployeeLoanId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorBanks_EmployeeBankId",
                table: "SponsorBanks",
                column: "EmployeeBankId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorBanks_PaymentBankId",
                table: "SponsorBanks",
                column: "PaymentBankId");

            migrationBuilder.CreateIndex(
                name: "IX_SponsorBanks_SponsorId",
                table: "SponsorBanks",
                column: "SponsorId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrgUnitProfiles_PayrollAreas_PayrollAreaId",
                table: "OrgUnitProfiles",
                column: "PayrollAreaId",
                principalTable: "PayrollAreas",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrgUnitProfiles_PayrollAreas_PayrollAreaId",
                table: "OrgUnitProfiles");

            migrationBuilder.DropTable(
                name: "EmployeeAbsAdjustments");

            migrationBuilder.DropTable(
                name: "EmployeeAbsenceQuotas");

            migrationBuilder.DropTable(
                name: "EmployeeAbsences");

            migrationBuilder.DropTable(
                name: "EmployeeAbsQuotaForfeits");

            migrationBuilder.DropTable(
                name: "EmployeeAccountAssignments");

            migrationBuilder.DropTable(
                name: "EmployeeAssetInvoices");

            migrationBuilder.DropTable(
                name: "EmployeeAssets");

            migrationBuilder.DropTable(
                name: "EmployeeAttendanceQuotas");

            migrationBuilder.DropTable(
                name: "EmployeeAttendances");

            migrationBuilder.DropTable(
                name: "EmployeeBanks");

            migrationBuilder.DropTable(
                name: "EmployeeBenefitExceptions");

            migrationBuilder.DropTable(
                name: "EmployeeCostDistributions");

            migrationBuilder.DropTable(
                name: "EmployeeDependantExceptions");

            migrationBuilder.DropTable(
                name: "EmployeeDisciplinaries");

            migrationBuilder.DropTable(
                name: "EmployeeExtdWorkLocations");

            migrationBuilder.DropTable(
                name: "EmployeeFnFPayments");

            migrationBuilder.DropTable(
                name: "EmployeeFundDetails");

            migrationBuilder.DropTable(
                name: "EmployeeFundExemptions");

            migrationBuilder.DropTable(
                name: "EmployeeGratuities");

            migrationBuilder.DropTable(
                name: "EmployeeGratuityQuotas");

            migrationBuilder.DropTable(
                name: "EmployeeGrievances");

            migrationBuilder.DropTable(
                name: "EmployeeHealthReimburses");

            migrationBuilder.DropTable(
                name: "EmployeeLoanExemptions");

            migrationBuilder.DropTable(
                name: "EmployeeLoanRecoveries");

            migrationBuilder.DropTable(
                name: "EmployeeNonPayrollTrans");

            migrationBuilder.DropTable(
                name: "EmployeeOnHolds");

            migrationBuilder.DropTable(
                name: "EmployeeOvertimes");

            migrationBuilder.DropTable(
                name: "EmployeePayments");

            migrationBuilder.DropTable(
                name: "EmployeeRecurringPayments");

            migrationBuilder.DropTable(
                name: "EmployeeRemunerations");

            migrationBuilder.DropTable(
                name: "EmployeeRewards");

            migrationBuilder.DropTable(
                name: "EmployeeSocialSecurities");

            migrationBuilder.DropTable(
                name: "EmployeeSocialSecurityClaims");

            migrationBuilder.DropTable(
                name: "EmployeeSponsors");

            migrationBuilder.DropTable(
                name: "EmployeeTasks");

            migrationBuilder.DropTable(
                name: "EmployeeTicketQuotas");

            migrationBuilder.DropTable(
                name: "EmployeeTicketRoutes");

            migrationBuilder.DropTable(
                name: "EmployeeTickets");

            migrationBuilder.DropTable(
                name: "EmployeeTktAdjustments");

            migrationBuilder.DropTable(
                name: "LoanInstallments");

            migrationBuilder.DropTable(
                name: "SponsorBanks");

            migrationBuilder.DropTable(
                name: "EmployeeDependants");

            migrationBuilder.DropTable(
                name: "EmployeeBasicPayDetails");

            migrationBuilder.DropTable(
                name: "EmployeeFunds");

            migrationBuilder.DropTable(
                name: "EmployeeLoans");

            migrationBuilder.DropTable(
                name: "EmployeeBasicPays");

            migrationBuilder.DropIndex(
                name: "IX_OrgUnitProfiles_PayrollAreaId",
                table: "OrgUnitProfiles");

            migrationBuilder.DropColumn(
                name: "PayrollAreaId",
                table: "OrgUnitProfiles");
        }
    }
}
