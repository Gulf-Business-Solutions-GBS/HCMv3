using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_SystemSecurity_SystemSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkUploadLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    LogType = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RunTimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InPeriod = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StopFlag = table.Column<bool>(type: "bit", nullable: false),
                    NoOfEmployee = table.Column<int>(type: "int", nullable: false),
                    RecordNo = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_BulkUploadLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BulkUploadLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailNotificationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    Exception = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_EmailNotificationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmailTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SmtpProfile = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Subject = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EmailBodyHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailBodyText = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_EmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ActionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    Module = table.Column<int>(type: "int", nullable: false),
                    ScreenName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ScreenCaption = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Parameters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionStatus = table.Column<int>(type: "int", nullable: false),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    Successfull = table.Column<int>(type: "int", nullable: false),
                    Failed = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ExecutionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageAppraisalSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    AppraisalCycle = table.Column<int>(type: "int", nullable: false),
                    EnablePAPeerReview = table.Column<bool>(type: "bit", nullable: false),
                    SetPeerMinLimit = table.Column<int>(type: "int", nullable: false),
                    SetPeerMaxLimit = table.Column<int>(type: "int", nullable: false),
                    EnableMarksInPAReview = table.Column<bool>(type: "bit", nullable: false),
                    MarksOn = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ManageAppraisalSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageDocumentSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EnableDocSaveInFileSystem = table.Column<bool>(type: "bit", nullable: false),
                    FileSystemAttachmentPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxAttachmentsPerTransaction = table.Column<int>(type: "int", nullable: false),
                    AllowedFileExtensions = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MaxFileSizePerAttachmentMB = table.Column<int>(type: "int", nullable: false),
                    MaxStorageSizeMB = table.Column<int>(type: "int", nullable: false),
                    StorageWarningThresholdMB = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ManageDocumentSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageFIPostingSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SeparateProvisionPosting = table.Column<bool>(type: "bit", nullable: false),
                    EnableAutoWageAccountEntry = table.Column<bool>(type: "bit", nullable: false),
                    AllowDuplicateCodeInCostElement = table.Column<bool>(type: "bit", nullable: false),
                    FiPostingAfterPayrollExit = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ManageFIPostingSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageGeneralSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DisableAutoEmpcode = table.Column<bool>(type: "bit", nullable: false),
                    DateEditMask = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateDisplayFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeEditMask = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeDisplayFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartupDashboard = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NoticePeriodDaysExcludingSubmissionDate = table.Column<bool>(type: "bit", nullable: false),
                    EnableGridDataAccessModeServer = table.Column<bool>(type: "bit", nullable: false),
                    StartHangfire = table.Column<bool>(type: "bit", nullable: false),
                    AutoFilterColumnCondition = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ManageGeneralSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageNotificationSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DisableNotificationWindow = table.Column<bool>(type: "bit", nullable: false),
                    AdvancePaymentReminderDateType = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ManageNotificationSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageOrgSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EnablePostingGroup = table.Column<bool>(type: "bit", nullable: false),
                    EnableBudgetingGroup = table.Column<bool>(type: "bit", nullable: false),
                    EnablePosition = table.Column<bool>(type: "bit", nullable: false),
                    EnableOrgChart3Levels = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ManageOrgSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManagePayrollSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ClientId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DefaultWorkingHours = table.Column<double>(type: "float", nullable: false),
                    PayslipSignature = table.Column<bool>(type: "bit", nullable: false),
                    LeaveAdvPaymentdateBefore15 = table.Column<bool>(type: "bit", nullable: false),
                    LeaveAdvPaymentdateAfter15 = table.Column<bool>(type: "bit", nullable: false),
                    LeaveProvisionAsPerCurrentSalary = table.Column<bool>(type: "bit", nullable: false),
                    EnableNotReturnFromLeaveLogic = table.Column<bool>(type: "bit", nullable: false),
                    EnableBasicpayMoreThenFixedDays = table.Column<bool>(type: "bit", nullable: false),
                    EnableUnpaidDedSlabWise = table.Column<bool>(type: "bit", nullable: false),
                    EnableFixedDaysLogicChange = table.Column<bool>(type: "bit", nullable: false),
                    GenerateRegisterOnNotReturnFromLeave = table.Column<bool>(type: "bit", nullable: false),
                    OffcycleBankFileCutoff = table.Column<int>(type: "int", nullable: false),
                    RoundNetPay = table.Column<bool>(type: "bit", nullable: false),
                    ApplyAnnualleaveNoImpactLogic = table.Column<bool>(type: "bit", nullable: false),
                    EnablePayrollEngineAutorun = table.Column<bool>(type: "bit", nullable: false),
                    Decimal6RoundingFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FullMonthLeaveAdjForFixedDays = table.Column<bool>(type: "bit", nullable: false),
                    PayLeaveAsPerCutoff = table.Column<bool>(type: "bit", nullable: false),
                    DecimalInSalaryRegister = table.Column<int>(type: "int", nullable: false),
                    DefaultLeavePaymentType = table.Column<int>(type: "int", nullable: false),
                    AllowDeleteBackdatedSalaryRegister = table.Column<bool>(type: "bit", nullable: false),
                    AutoFillFnFCompensation = table.Column<bool>(type: "bit", nullable: false),
                    EditableAutoFillFnFCompensation = table.Column<bool>(type: "bit", nullable: false),
                    FixedToActualWhenLeaveOverlapOnMonthStart = table.Column<bool>(type: "bit", nullable: false),
                    FixedToActualWhenLeaveOverlapOnMonthEnd = table.Column<bool>(type: "bit", nullable: false),
                    FixedToActualWhenUpaidLeaveFound = table.Column<bool>(type: "bit", nullable: false),
                    FixedToActualWhenAbsentDayFound = table.Column<bool>(type: "bit", nullable: false),
                    OvertimeHoursBasis = table.Column<int>(type: "int", nullable: false),
                    CarryAllowanceOnSalaryImport = table.Column<bool>(type: "bit", nullable: false),
                    SocialSecurityRateRule = table.Column<int>(type: "int", nullable: false),
                    AllowHealthReimburseBalCarryFwd = table.Column<bool>(type: "bit", nullable: false),
                    AllowSimulationRun = table.Column<bool>(type: "bit", nullable: false),
                    RunOffCycleForFutureDate = table.Column<bool>(type: "bit", nullable: false),
                    DisableRetroCalculation = table.Column<bool>(type: "bit", nullable: false),
                    PickOffcyclePayment = table.Column<int>(type: "int", nullable: false),
                    RegularLeaveEncashOnBasicPayOnly = table.Column<bool>(type: "bit", nullable: false),
                    FnFLeaveEncashOnBasicPayOnly = table.Column<bool>(type: "bit", nullable: false),
                    ShowBillingTypeInPaymentScreen = table.Column<bool>(type: "bit", nullable: false),
                    AutoUpdateLoanRecoveryEndDate = table.Column<bool>(type: "bit", nullable: false),
                    ChangePartialPeriodFactoringInFixedDaysBilling = table.Column<bool>(type: "bit", nullable: false),
                    PartialPeriodCutoffDayInFixedDaysBilling = table.Column<int>(type: "int", nullable: false),
                    EnableEffectiveDateInRecPayment = table.Column<bool>(type: "bit", nullable: false),
                    BankFileMinAmount = table.Column<double>(type: "float", nullable: false),
                    IncludeSalaryIncProvDiffInSalReg = table.Column<bool>(type: "bit", nullable: false),
                    IncludeSalaryIncProvDiffInSalRegEffDt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LockSalaryRegisterOnPayrollExit = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ManagePayrollSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManagePersonnelSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EnableSponsorGovtJobTitleQuota = table.Column<bool>(type: "bit", nullable: false),
                    MaxPhotoSize = table.Column<int>(type: "int", nullable: false),
                    ProbationDaysCalculation = table.Column<int>(type: "int", nullable: false),
                    AutoWageAssignOnHire = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ManagePersonnelSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageProjectCostingSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CalculateNetPayProjectWise = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ManageProjectCostingSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageReportSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    ShowSubordinatesPayslip = table.Column<bool>(type: "bit", nullable: false),
                    ExportGridReportWithHeader = table.Column<bool>(type: "bit", nullable: false),
                    ExportGridReportWithFooter = table.Column<bool>(type: "bit", nullable: false),
                    ShowPayslipOnDay = table.Column<int>(type: "int", nullable: false),
                    ShowPayslipFromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecimalInReports = table.Column<int>(type: "int", nullable: false),
                    ShowPeriodInPayrollSheet = table.Column<bool>(type: "bit", nullable: false),
                    ShowPeriodInLeaveProvision = table.Column<bool>(type: "bit", nullable: false),
                    ShowPeriodInIndemnityProvision = table.Column<bool>(type: "bit", nullable: false),
                    PeriodMonthLimitInPayrollSheet = table.Column<int>(type: "int", nullable: false),
                    PeriodMonthLimitInLeaveProvision = table.Column<int>(type: "int", nullable: false),
                    PeriodMonthLimitInIndemnityProvision = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ManageReportSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageSecuritySettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EnableRowLevelSecurity = table.Column<bool>(type: "bit", nullable: false),
                    EnableAuditLogInTimeEngine = table.Column<bool>(type: "bit", nullable: false),
                    EnableAuditLogInPayrollEngine = table.Column<bool>(type: "bit", nullable: false),
                    EnableAuditLogInProjectCostEngine = table.Column<bool>(type: "bit", nullable: false),
                    EnableAuditLogInCostEngine = table.Column<bool>(type: "bit", nullable: false),
                    EnableAuditLogInGridReport = table.Column<bool>(type: "bit", nullable: false),
                    EnableAuditLogInPrintReport = table.Column<bool>(type: "bit", nullable: false),
                    EnableReportExportPwd = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ManageSecuritySettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageSelfServiceSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RedirectAfterWFApproval = table.Column<int>(type: "int", nullable: false),
                    AllowPunchOutsideRange = table.Column<bool>(type: "bit", nullable: false),
                    AutoLogoutAfterPunch = table.Column<bool>(type: "bit", nullable: false),
                    AllowGeolocationSelfie = table.Column<bool>(type: "bit", nullable: false),
                    HideGeolocationInfo = table.Column<bool>(type: "bit", nullable: false),
                    HideGeolocationPunchInfo = table.Column<bool>(type: "bit", nullable: false),
                    EmailNotification = table.Column<bool>(type: "bit", nullable: false),
                    DefaultLoginPage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CaptchaKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowLeaveInfo = table.Column<bool>(type: "bit", nullable: false),
                    ShowCurrentLeaveBalance = table.Column<bool>(type: "bit", nullable: false),
                    ShowUpdatedLeaveBalance = table.Column<bool>(type: "bit", nullable: false),
                    ShowEmployeeInfo = table.Column<bool>(type: "bit", nullable: false),
                    IsAttachmentAllowed = table.Column<bool>(type: "bit", nullable: false),
                    IsAdditionalUnpaidLeaveAllowed = table.Column<bool>(type: "bit", nullable: false),
                    ShowPaymentDate = table.Column<bool>(type: "bit", nullable: false),
                    EnablePayWithPayroll = table.Column<bool>(type: "bit", nullable: false),
                    EnablePayInAdvance = table.Column<bool>(type: "bit", nullable: false),
                    IsPassportWithdrawalRequired = table.Column<bool>(type: "bit", nullable: false),
                    PassportWithdrawalSubmissionStep = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_ManageSelfServiceSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageTimeSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EarliestEvaluationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FutureEvaluationInDays = table.Column<int>(type: "int", nullable: false),
                    EnableRosterDuty = table.Column<bool>(type: "bit", nullable: false),
                    ExcludePaidLeaveAccrualOnAbsent = table.Column<bool>(type: "bit", nullable: false),
                    EnableTimeAttendanceIntegration = table.Column<bool>(type: "bit", nullable: false),
                    EnableMiddleTierTimeEvents = table.Column<bool>(type: "bit", nullable: false),
                    EnableAttendanceOnHoliday = table.Column<bool>(type: "bit", nullable: false),
                    EnableAttendanceOnWeekOff = table.Column<bool>(type: "bit", nullable: false),
                    EnableAttendanceOnRestDay = table.Column<bool>(type: "bit", nullable: false),
                    HourlyLeavelogicEnable = table.Column<bool>(type: "bit", nullable: false),
                    HourlyLeaveShouldSignIn = table.Column<bool>(type: "bit", nullable: false),
                    HourlyLeaveShouldSignOut = table.Column<bool>(type: "bit", nullable: false),
                    EnableTimeEngineAutorun = table.Column<bool>(type: "bit", nullable: false),
                    LeaveDeductionStartMonth = table.Column<int>(type: "int", nullable: false),
                    EnableOvertimeInTimeEngine = table.Column<bool>(type: "bit", nullable: false),
                    TimeEventImportPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CountWeekoffOnPH = table.Column<bool>(type: "bit", nullable: false),
                    RestrictUnpaidleaveIfAnnualleaveAvailable = table.Column<bool>(type: "bit", nullable: false),
                    EnableIsTravelling = table.Column<bool>(type: "bit", nullable: false),
                    AllowPaidleaveBackdateEntry = table.Column<bool>(type: "bit", nullable: false),
                    SickLeaveAttachMandatory = table.Column<bool>(type: "bit", nullable: false),
                    EnableMySqlIntegration = table.Column<bool>(type: "bit", nullable: false),
                    EnablePayFullWithPayroll = table.Column<bool>(type: "bit", nullable: false),
                    EnableAutoReturnInConsecutiveLeave = table.Column<bool>(type: "bit", nullable: false),
                    CountRestDayOnPH = table.Column<bool>(type: "bit", nullable: false),
                    EnableTimeAttIntegrationAPI = table.Column<bool>(type: "bit", nullable: false),
                    ForfeitRule = table.Column<int>(type: "int", nullable: false),
                    RosterPlannerFrequency = table.Column<int>(type: "int", nullable: false),
                    AllowFutureDatedReturnFromLeave = table.Column<bool>(type: "bit", nullable: false),
                    EnableAutoHalfDayLeave = table.Column<bool>(type: "bit", nullable: false),
                    EnableSettleFractionalLeaveBalance = table.Column<bool>(type: "bit", nullable: false),
                    EnableTimeRegisterApproval = table.Column<bool>(type: "bit", nullable: false),
                    AutoApproveTimeLog = table.Column<bool>(type: "bit", nullable: false),
                    TimeEventSyncBatchCount = table.Column<int>(type: "int", nullable: false),
                    TimeEventSyncFreq = table.Column<int>(type: "int", nullable: false),
                    MidTierSyncBatchCount = table.Column<int>(type: "int", nullable: false),
                    MidTierSyncFreq = table.Column<int>(type: "int", nullable: false),
                    EnableBookingQuotaInLeaveRequest = table.Column<bool>(type: "bit", nullable: false),
                    AllowDuplicateInTimeEvent = table.Column<bool>(type: "bit", nullable: false),
                    UseActualTransInPlannedTimePolicy = table.Column<bool>(type: "bit", nullable: false),
                    AbsenceViolationRuleId = table.Column<int>(type: "int", nullable: true),
                    AbsenceViolationEffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnableGeoFence = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_ManageTimeSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ManageWorkflowSettingses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EnableWorkflowGroup = table.Column<bool>(type: "bit", nullable: false),
                    EnableOrgUnitTypeInWorkflow = table.Column<bool>(type: "bit", nullable: false),
                    HideLeavePayDetailsInWorkflow = table.Column<bool>(type: "bit", nullable: false),
                    EnableTravelCostOnWorkflowstep = table.Column<int>(type: "int", nullable: false),
                    OvertimeRequestModificationStep = table.Column<int>(type: "int", nullable: false),
                    DynamicWorkflowSteps = table.Column<bool>(type: "bit", nullable: false),
                    SendBackToCreator = table.Column<bool>(type: "bit", nullable: false),
                    DelegateEmployeeListByOrganizationUnitTypeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_ManageWorkflowSettingses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NotificationMasters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NotificationType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    NotificationConfigId = table.Column<int>(type: "int", nullable: false),
                    Criterion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_NotificationMasters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotificationMasters_NotificationConfigs_NotificationConfigId",
                        column: x => x.NotificationConfigId,
                        principalTable: "NotificationConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollDrivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    LeaveDeductionStartMonth = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_PayrollDrivers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RetroReasonLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ChangedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RetroDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntityObjectName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EntityObjectId = table.Column<int>(type: "int", nullable: false),
                    RetroReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
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
                    table.PrimaryKey("PK_RetroReasonLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetroReasonLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SysObjects",
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
                    table.PrimaryKey("PK_SysObjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    NotificationName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NotificationConfigId = table.Column<int>(type: "int", nullable: false),
                    ReminderDays = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SystemNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemNotifications_NotificationConfigs_NotificationConfigId",
                        column: x => x.NotificationConfigId,
                        principalTable: "NotificationConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SysTenantConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    KeyValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ConfigCategory = table.Column<int>(type: "int", nullable: false),
                    ConfigDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SysTenantConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SysUserOrgUnitDatas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    OrgLevelId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<long>(type: "bigint", nullable: false),
                    ShowUnlocked = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_SysUserOrgUnitDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SysUserOrgUnitDatas_AbpOrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalTable: "AbpOrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SysUserOrgUnitDatas_AbpRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AbpRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SysUserOrgUnitDatas_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SysUserOrgUnitDatas_OrganizationUnitTypes_OrgLevelId",
                        column: x => x.OrgLevelId,
                        principalTable: "OrganizationUnitTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PunchNotificationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeDutyPeriodId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmailNotificationLogId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_PunchNotificationLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PunchNotificationLogs_EmailNotificationLogs_EmailNotificationLogId",
                        column: x => x.EmailNotificationLogId,
                        principalTable: "EmailNotificationLogs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PunchNotificationLogs_EmployeeDutyPeriods_EmployeeDutyPeriodId",
                        column: x => x.EmployeeDutyPeriodId,
                        principalTable: "EmployeeDutyPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TokenName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TokenValue = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsDateTimeType = table.Column<bool>(type: "bit", nullable: false),
                    EmailTemplateId = table.Column<int>(type: "int", nullable: false),
                    ObjectKey = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_EmailTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailTokens_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayslipConfigs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmailTemplateId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PayslipConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipConfigs_EmailTemplates_EmailTemplateId",
                        column: x => x.EmailTemplateId,
                        principalTable: "EmailTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BulkUploadLogs_EmployeeId",
                table: "BulkUploadLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailTokens_EmailTemplateId",
                table: "EmailTokens",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationMasters_NotificationConfigId",
                table: "NotificationMasters",
                column: "NotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_PayslipConfigs_EmailTemplateId",
                table: "PayslipConfigs",
                column: "EmailTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PunchNotificationLogs_EmailNotificationLogId",
                table: "PunchNotificationLogs",
                column: "EmailNotificationLogId");

            migrationBuilder.CreateIndex(
                name: "IX_PunchNotificationLogs_EmployeeDutyPeriodId",
                table: "PunchNotificationLogs",
                column: "EmployeeDutyPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_RetroReasonLogs_EmployeeId",
                table: "RetroReasonLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_NotificationConfigId",
                table: "SystemNotifications",
                column: "NotificationConfigId");

            migrationBuilder.CreateIndex(
                name: "IX_SysUserOrgUnitDatas_EmployeeId",
                table: "SysUserOrgUnitDatas",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SysUserOrgUnitDatas_OrganizationUnitId",
                table: "SysUserOrgUnitDatas",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SysUserOrgUnitDatas_OrgLevelId",
                table: "SysUserOrgUnitDatas",
                column: "OrgLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_SysUserOrgUnitDatas_RoleId",
                table: "SysUserOrgUnitDatas",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkUploadLogs");

            migrationBuilder.DropTable(
                name: "EmailTokens");

            migrationBuilder.DropTable(
                name: "ExecutionLogs");

            migrationBuilder.DropTable(
                name: "ManageAppraisalSettingses");

            migrationBuilder.DropTable(
                name: "ManageDocumentSettingses");

            migrationBuilder.DropTable(
                name: "ManageFIPostingSettingses");

            migrationBuilder.DropTable(
                name: "ManageGeneralSettingses");

            migrationBuilder.DropTable(
                name: "ManageNotificationSettingses");

            migrationBuilder.DropTable(
                name: "ManageOrgSettingses");

            migrationBuilder.DropTable(
                name: "ManagePayrollSettingses");

            migrationBuilder.DropTable(
                name: "ManagePersonnelSettingses");

            migrationBuilder.DropTable(
                name: "ManageProjectCostingSettingses");

            migrationBuilder.DropTable(
                name: "ManageReportSettingses");

            migrationBuilder.DropTable(
                name: "ManageSecuritySettingses");

            migrationBuilder.DropTable(
                name: "ManageSelfServiceSettingses");

            migrationBuilder.DropTable(
                name: "ManageTimeSettingses");

            migrationBuilder.DropTable(
                name: "ManageWorkflowSettingses");

            migrationBuilder.DropTable(
                name: "NotificationMasters");

            migrationBuilder.DropTable(
                name: "PayrollDrivers");

            migrationBuilder.DropTable(
                name: "PayslipConfigs");

            migrationBuilder.DropTable(
                name: "PunchNotificationLogs");

            migrationBuilder.DropTable(
                name: "RetroReasonLogs");

            migrationBuilder.DropTable(
                name: "SysObjects");

            migrationBuilder.DropTable(
                name: "SystemNotifications");

            migrationBuilder.DropTable(
                name: "SysTenantConfigs");

            migrationBuilder.DropTable(
                name: "SysUserOrgUnitDatas");

            migrationBuilder.DropTable(
                name: "EmailTemplates");

            migrationBuilder.DropTable(
                name: "EmailNotificationLogs");
        }
    }
}
