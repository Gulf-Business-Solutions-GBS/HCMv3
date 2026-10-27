using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HCMv3.Migrations
{
    /// <inheritdoc />
    public partial class Added_TimeManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BreakShifts",
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
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false),
                    IsBreakStartNextDay = table.Column<bool>(type: "bit", nullable: false),
                    IsBreakEndNextDay = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_BreakShifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTimesheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TransDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualStartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    BreakStartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    BreakEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ActualEndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    TotalHours = table.Column<double>(type: "float", nullable: false),
                    Approved = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeTimesheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTimesheets_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HolidayCalendars",
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
                    table.PrimaryKey("PK_HolidayCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyTimeRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnnualLeaveDays = table.Column<double>(type: "float", nullable: false),
                    UnpaidLeaveDays = table.Column<double>(type: "float", nullable: false),
                    SickLeavePaidDays = table.Column<double>(type: "float", nullable: false),
                    SickLeaveUnpaidDays = table.Column<double>(type: "float", nullable: false),
                    GratuityDays = table.Column<double>(type: "float", nullable: false),
                    PublicHolidays = table.Column<double>(type: "float", nullable: false),
                    WeeklyOffs = table.Column<double>(type: "float", nullable: false),
                    RestDays = table.Column<double>(type: "float", nullable: false),
                    AnnualLeaveAccrualDays = table.Column<double>(type: "float", nullable: false),
                    AnnualLeaveOpeningBal = table.Column<double>(type: "float", nullable: false),
                    AnnualLeaveClosingBal = table.Column<double>(type: "float", nullable: false),
                    TicketAccrualDays = table.Column<double>(type: "float", nullable: false),
                    TicketAccrualOpeningBal = table.Column<double>(type: "float", nullable: false),
                    TicketAccrualClosingBal = table.Column<double>(type: "float", nullable: false),
                    AirTicketPaidDays = table.Column<double>(type: "float", nullable: false),
                    GratuityPaidDays = table.Column<double>(type: "float", nullable: false),
                    ActiveDays = table.Column<double>(type: "float", nullable: false),
                    AdvanceLeavePaids = table.Column<double>(type: "float", nullable: false),
                    AdvanceTicketPaids = table.Column<double>(type: "float", nullable: false),
                    PlannedWorkingMins = table.Column<double>(type: "float", nullable: false),
                    ActualWorkingMins = table.Column<double>(type: "float", nullable: false),
                    ActualAbsenceMins = table.Column<double>(type: "float", nullable: false),
                    NormalDayOTMins = table.Column<double>(type: "float", nullable: false),
                    RestDayOTMins = table.Column<double>(type: "float", nullable: false),
                    HolidayOTMins = table.Column<double>(type: "float", nullable: false),
                    WeekendOTMins = table.Column<double>(type: "float", nullable: false),
                    PlannedWorkingDays = table.Column<double>(type: "float", nullable: false),
                    ActualWorkingDays = table.Column<double>(type: "float", nullable: false),
                    ExtraWorkingDays = table.Column<double>(type: "float", nullable: false),
                    LieuDays = table.Column<double>(type: "float", nullable: false),
                    AdvLeaveRecoveryDays = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_MonthlyTimeRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyTimeRegisters_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RosterDutyPeriods",
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
                    table.PrimaryKey("PK_RosterDutyPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RosterGroups",
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
                    table.PrimaryKey("PK_RosterGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeAttAPIs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    TokenUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    TransactionsUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NoOfRecords = table.Column<int>(type: "int", nullable: false),
                    DevicesUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PunchInCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PunchOutCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BreakOutCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BreakInCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_TimeAttAPIs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RunTimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_TimeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TimePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShouldSignIn = table.Column<bool>(type: "bit", nullable: false),
                    ShouldSignOut = table.Column<bool>(type: "bit", nullable: false),
                    ShouldBreakSignIn = table.Column<bool>(type: "bit", nullable: false),
                    ShouldBreakSignOut = table.Column<bool>(type: "bit", nullable: false),
                    CalcLateInAfterTolerance = table.Column<bool>(type: "bit", nullable: false),
                    CalcEarlyOutAfterTolerance = table.Column<bool>(type: "bit", nullable: false),
                    LateInTolerance = table.Column<double>(type: "float", nullable: false),
                    EarlyOutTolerance = table.Column<double>(type: "float", nullable: false),
                    CalcBreakLateInAfterTolerance = table.Column<bool>(type: "bit", nullable: false),
                    CalcBreakEarlyOutAfterTolerance = table.Column<bool>(type: "bit", nullable: false),
                    BreakLateInTolerance = table.Column<double>(type: "float", nullable: false),
                    BreakEarlyOutTolerance = table.Column<double>(type: "float", nullable: false),
                    ClockInLimit = table.Column<double>(type: "float", nullable: false),
                    ClockOutLimit = table.Column<double>(type: "float", nullable: false),
                    BreakInLimit = table.Column<double>(type: "float", nullable: false),
                    BreakOutLimit = table.Column<double>(type: "float", nullable: false),
                    MonthlyToleranceType = table.Column<int>(type: "int", nullable: false),
                    MonthlyAfterTolerance = table.Column<double>(type: "float", nullable: false),
                    TimeEventRule = table.Column<int>(type: "int", nullable: false),
                    TimeMgmtStatus = table.Column<int>(type: "int", nullable: false),
                    OTMinLimit = table.Column<double>(type: "float", nullable: false),
                    MarkWeekoffOnHoliday = table.Column<bool>(type: "bit", nullable: false),
                    AllowPunchInFromWorkLocationOnly = table.Column<bool>(type: "bit", nullable: false),
                    AllowPunchOutFromWorkLocationOnly = table.Column<bool>(type: "bit", nullable: false),
                    IsFaceRequired = table.Column<bool>(type: "bit", nullable: false),
                    IsMissedPunchInNotiEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MissedPunchInNotiTimeLimit = table.Column<double>(type: "float", nullable: false),
                    IsLatePunchInNotiEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LatePunchInNotiTimeLimit = table.Column<double>(type: "float", nullable: false),
                    IsMissedPunchOutNotiEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MissedPunchOutNotiTimeLimit = table.Column<double>(type: "float", nullable: false),
                    IsEarlyPunchOutNotiEnabled = table.Column<bool>(type: "bit", nullable: false),
                    EarlyPunchOutNotiTimeLimit = table.Column<double>(type: "float", nullable: false),
                    AllowPunchInFromExtWorklocation = table.Column<bool>(type: "bit", nullable: false),
                    AllowPunchOutFromExtWorklocation = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyLateInTolerance = table.Column<double>(type: "float", nullable: false),
                    MonthlyEarlyOutTolerance = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TimePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeTransferTypes",
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
                    table.PrimaryKey("PK_TimeTransferTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeTypes",
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
                    table.PrimaryKey("PK_TimeTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransTypes",
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
                    table.PrimaryKey("PK_TransTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YearlyTimeRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnnualLeaveDays = table.Column<double>(type: "float", nullable: false),
                    UnpaidLeaveDays = table.Column<double>(type: "float", nullable: false),
                    SickLeavePaidDays = table.Column<double>(type: "float", nullable: false),
                    SickLeaveUnpaidDays = table.Column<double>(type: "float", nullable: false),
                    GratuityDays = table.Column<double>(type: "float", nullable: false),
                    PublicHolidays = table.Column<double>(type: "float", nullable: false),
                    WeeklyOffs = table.Column<double>(type: "float", nullable: false),
                    RestDays = table.Column<double>(type: "float", nullable: false),
                    AnnualLeaveAccrualDays = table.Column<double>(type: "float", nullable: false),
                    AnnualLeaveOpeningBal = table.Column<double>(type: "float", nullable: false),
                    AnnualLeaveClosingBal = table.Column<double>(type: "float", nullable: false),
                    TicketAccrualDays = table.Column<double>(type: "float", nullable: false),
                    TicketAccrualOpeningBal = table.Column<double>(type: "float", nullable: false),
                    TicketAccrualClosingBal = table.Column<double>(type: "float", nullable: false),
                    AirTicketPaidDays = table.Column<double>(type: "float", nullable: false),
                    GratuityPaidDays = table.Column<double>(type: "float", nullable: false),
                    ActiveDays = table.Column<double>(type: "float", nullable: false),
                    AdvanceLeavePaids = table.Column<double>(type: "float", nullable: false),
                    AdvanceTicketPaids = table.Column<double>(type: "float", nullable: false),
                    PlannedWorkingMins = table.Column<double>(type: "float", nullable: false),
                    ActualWorkingMins = table.Column<double>(type: "float", nullable: false),
                    ActualAbsenceMins = table.Column<double>(type: "float", nullable: false),
                    NormalDayOTMins = table.Column<double>(type: "float", nullable: false),
                    RestDayOTMins = table.Column<double>(type: "float", nullable: false),
                    HolidayOTMins = table.Column<double>(type: "float", nullable: false),
                    WeekendOTMins = table.Column<double>(type: "float", nullable: false),
                    PlannedWorkingDays = table.Column<double>(type: "float", nullable: false),
                    ActualWorkingDays = table.Column<double>(type: "float", nullable: false),
                    ExtraWorkingDays = table.Column<double>(type: "float", nullable: false),
                    LieuDays = table.Column<double>(type: "float", nullable: false),
                    AdvLeaveRecoveryDays = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_YearlyTimeRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YearlyTimeRegisters_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DailyDuties",
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
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    IsNightShift = table.Column<bool>(type: "bit", nullable: false),
                    BreakShiftId = table.Column<int>(type: "int", nullable: true),
                    AllowTransStart = table.Column<bool>(type: "bit", nullable: false),
                    AllowTransEnd = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_DailyDuties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyDuties_BreakShifts_BreakShiftId",
                        column: x => x.BreakShiftId,
                        principalTable: "BreakShifts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Duties",
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
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    BreakShiftId = table.Column<int>(type: "int", nullable: true),
                    OvertimeTypeId = table.Column<int>(type: "int", nullable: true),
                    AllowTransStart = table.Column<bool>(type: "bit", nullable: false),
                    AllowTransEnd = table.Column<bool>(type: "bit", nullable: false),
                    IsNightShift = table.Column<bool>(type: "bit", nullable: false),
                    IsWeeklyOff = table.Column<bool>(type: "bit", nullable: false),
                    IsRestDay = table.Column<bool>(type: "bit", nullable: false),
                    IsHalfDay = table.Column<bool>(type: "bit", nullable: false),
                    FlexiStartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    FlexiEndTime = table.Column<TimeSpan>(type: "time", nullable: true),
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
                    table.PrimaryKey("PK_Duties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Duties_BreakShifts_BreakShiftId",
                        column: x => x.BreakShiftId,
                        principalTable: "BreakShifts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Duties_OvertimeTypes_OvertimeTypeId",
                        column: x => x.OvertimeTypeId,
                        principalTable: "OvertimeTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HolidayCalendarId = table.Column<int>(type: "int", nullable: false),
                    OvertimeTypeId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holidays_HolidayCalendars_HolidayCalendarId",
                        column: x => x.HolidayCalendarId,
                        principalTable: "HolidayCalendars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Holidays_OvertimeTypes_OvertimeTypeId",
                        column: x => x.OvertimeTypeId,
                        principalTable: "OvertimeTypes",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RosterGroupDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RosterGroupId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RosterGroupDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RosterGroupDetails_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RosterGroupDetails_RosterGroups_RosterGroupId",
                        column: x => x.RosterGroupId,
                        principalTable: "RosterGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeTimePolicies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TimePolicyId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeTimePolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeTimePolicies_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeTimePolicies_TimePolicies_TimePolicyId",
                        column: x => x.TimePolicyId,
                        principalTable: "TimePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimeTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TransferDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransferTypeId = table.Column<int>(type: "int", nullable: false),
                    TransferCount = table.Column<double>(type: "float", nullable: false),
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
                    table.PrimaryKey("PK_TimeTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeTransfers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeTransfers_TimeTransferTypes_TransferTypeId",
                        column: x => x.TransferTypeId,
                        principalTable: "TimeTransferTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyTimeRegisterDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    MonthlyTimeRegisterId = table.Column<int>(type: "int", nullable: false),
                    TimeTypeId = table.Column<int>(type: "int", nullable: false),
                    TimeValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_MonthlyTimeRegisterDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyTimeRegisterDetails_MonthlyTimeRegisters_MonthlyTimeRegisterId",
                        column: x => x.MonthlyTimeRegisterId,
                        principalTable: "MonthlyTimeRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MonthlyTimeRegisterDetails_TimeTypes_TimeTypeId",
                        column: x => x.TimeTypeId,
                        principalTable: "TimeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimeEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PIN = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ActualTransTypeId = table.Column<int>(type: "int", nullable: true),
                    TransTypeId = table.Column<int>(type: "int", nullable: false),
                    TransDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TransTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AccessUnitId = table.Column<int>(type: "int", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransSource = table.Column<int>(type: "int", nullable: false),
                    RecordSource = table.Column<long>(type: "bigint", nullable: false),
                    DeviceAgentString = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    RangeDifference = table.Column<double>(type: "float", nullable: false),
                    PunchInRange = table.Column<bool>(type: "bit", nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    WorkLocationId = table.Column<int>(type: "int", nullable: true),
                    GeoSelfiePath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_TimeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeEvents_AccessUnits_AccessUnitId",
                        column: x => x.AccessUnitId,
                        principalTable: "AccessUnits",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimeEvents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeEvents_TransTypes_ActualTransTypeId",
                        column: x => x.ActualTransTypeId,
                        principalTable: "TransTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimeEvents_TransTypes_TransTypeId",
                        column: x => x.TransTypeId,
                        principalTable: "TransTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeEvents_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "YearlyTimeRegisterDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    YearlyTimeRegisterId = table.Column<int>(type: "int", nullable: false),
                    TimeTypeId = table.Column<int>(type: "int", nullable: false),
                    TimeValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_YearlyTimeRegisterDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YearlyTimeRegisterDetails_TimeTypes_TimeTypeId",
                        column: x => x.TimeTypeId,
                        principalTable: "TimeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YearlyTimeRegisterDetails_YearlyTimeRegisters_YearlyTimeRegisterId",
                        column: x => x.YearlyTimeRegisterId,
                        principalTable: "YearlyTimeRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RosterDutyPeriodDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RosterDutyPeriodId = table.Column<int>(type: "int", nullable: false),
                    Week = table.Column<int>(type: "int", nullable: false),
                    Day1DutyId = table.Column<int>(type: "int", nullable: true),
                    Day2DutyId = table.Column<int>(type: "int", nullable: true),
                    Day3DutyId = table.Column<int>(type: "int", nullable: true),
                    Day4DutyId = table.Column<int>(type: "int", nullable: true),
                    Day5DutyId = table.Column<int>(type: "int", nullable: true),
                    Day6DutyId = table.Column<int>(type: "int", nullable: true),
                    Day7DutyId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_RosterDutyPeriodDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RosterDutyPeriodDetails_DailyDuties_Day1DutyId",
                        column: x => x.Day1DutyId,
                        principalTable: "DailyDuties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterDutyPeriodDetails_DailyDuties_Day2DutyId",
                        column: x => x.Day2DutyId,
                        principalTable: "DailyDuties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterDutyPeriodDetails_DailyDuties_Day3DutyId",
                        column: x => x.Day3DutyId,
                        principalTable: "DailyDuties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterDutyPeriodDetails_DailyDuties_Day4DutyId",
                        column: x => x.Day4DutyId,
                        principalTable: "DailyDuties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterDutyPeriodDetails_DailyDuties_Day5DutyId",
                        column: x => x.Day5DutyId,
                        principalTable: "DailyDuties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterDutyPeriodDetails_DailyDuties_Day6DutyId",
                        column: x => x.Day6DutyId,
                        principalTable: "DailyDuties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterDutyPeriodDetails_DailyDuties_Day7DutyId",
                        column: x => x.Day7DutyId,
                        principalTable: "DailyDuties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RosterDutyPeriodDetails_RosterDutyPeriods_RosterDutyPeriodId",
                        column: x => x.RosterDutyPeriodId,
                        principalTable: "RosterDutyPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DutyPeriods",
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
                    HolidayCalendarId = table.Column<int>(type: "int", nullable: true),
                    SundayDutyId = table.Column<int>(type: "int", nullable: true),
                    MondayDutyId = table.Column<int>(type: "int", nullable: true),
                    TuesdayDutyId = table.Column<int>(type: "int", nullable: true),
                    WednesdayDutyId = table.Column<int>(type: "int", nullable: true),
                    ThursdayDutyId = table.Column<int>(type: "int", nullable: true),
                    FridayDutyId = table.Column<int>(type: "int", nullable: true),
                    SaturdayDutyId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_DutyPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DutyPeriods_Duties_FridayDutyId",
                        column: x => x.FridayDutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriods_Duties_MondayDutyId",
                        column: x => x.MondayDutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriods_Duties_SaturdayDutyId",
                        column: x => x.SaturdayDutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriods_Duties_SundayDutyId",
                        column: x => x.SundayDutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriods_Duties_ThursdayDutyId",
                        column: x => x.ThursdayDutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriods_Duties_TuesdayDutyId",
                        column: x => x.TuesdayDutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriods_Duties_WednesdayDutyId",
                        column: x => x.WednesdayDutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriods_HolidayCalendars_HolidayCalendarId",
                        column: x => x.HolidayCalendarId,
                        principalTable: "HolidayCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSubstituteDuties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DutyId = table.Column<int>(type: "int", nullable: false),
                    SubstituteDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeSubstituteDuties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSubstituteDuties_Duties_DutyId",
                        column: x => x.DutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeSubstituteDuties_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HolidayPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    HolidayId = table.Column<int>(type: "int", nullable: false),
                    HolidayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
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
                    table.PrimaryKey("PK_HolidayPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HolidayPeriods_Holidays_HolidayId",
                        column: x => x.HolidayId,
                        principalTable: "Holidays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DutyPeriodDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    DutyPeriodId = table.Column<int>(type: "int", nullable: false),
                    Week = table.Column<int>(type: "int", nullable: false),
                    Day1DutyId = table.Column<int>(type: "int", nullable: true),
                    Day2DutyId = table.Column<int>(type: "int", nullable: true),
                    Day3DutyId = table.Column<int>(type: "int", nullable: true),
                    Day4DutyId = table.Column<int>(type: "int", nullable: true),
                    Day5DutyId = table.Column<int>(type: "int", nullable: true),
                    Day6DutyId = table.Column<int>(type: "int", nullable: true),
                    Day7DutyId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_DutyPeriodDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DutyPeriodDetails_Duties_Day1DutyId",
                        column: x => x.Day1DutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriodDetails_Duties_Day2DutyId",
                        column: x => x.Day2DutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriodDetails_Duties_Day3DutyId",
                        column: x => x.Day3DutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriodDetails_Duties_Day4DutyId",
                        column: x => x.Day4DutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriodDetails_Duties_Day5DutyId",
                        column: x => x.Day5DutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriodDetails_Duties_Day6DutyId",
                        column: x => x.Day6DutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriodDetails_Duties_Day7DutyId",
                        column: x => x.Day7DutyId,
                        principalTable: "Duties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DutyPeriodDetails_DutyPeriods_DutyPeriodId",
                        column: x => x.DutyPeriodId,
                        principalTable: "DutyPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDutyPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DutyPeriodId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeDutyPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDutyPeriods_DutyPeriods_DutyPeriodId",
                        column: x => x.DutyPeriodId,
                        principalTable: "DutyPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDutyPeriods_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RosterDutySchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DutyPeriodId = table.Column<int>(type: "int", nullable: false),
                    StartPoint = table.Column<int>(type: "int", nullable: false),
                    HolidayCalendarId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_RosterDutySchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RosterDutySchedules_DutyPeriods_DutyPeriodId",
                        column: x => x.DutyPeriodId,
                        principalTable: "DutyPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RosterDutySchedules_HolidayCalendars_HolidayCalendarId",
                        column: x => x.HolidayCalendarId,
                        principalTable: "HolidayCalendars",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RosterGroupSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    RosterGroupId = table.Column<int>(type: "int", nullable: false),
                    DutyPeriodId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartPoint = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RosterGroupSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RosterGroupSchedules_DutyPeriods_DutyPeriodId",
                        column: x => x.DutyPeriodId,
                        principalTable: "DutyPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RosterGroupSchedules_RosterGroups_RosterGroupId",
                        column: x => x.RosterGroupId,
                        principalTable: "RosterGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationTimeRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DutyPeriodId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_SimulationTimeRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationTimeRegisters_DutyPeriods_DutyPeriodId",
                        column: x => x.DutyPeriodId,
                        principalTable: "DutyPeriods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SimulationTimeRegisters_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimeRegisters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DayDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DutyPeriodId = table.Column<int>(type: "int", nullable: true),
                    DutyId = table.Column<int>(type: "int", nullable: true),
                    TimePolicyId = table.Column<int>(type: "int", nullable: true),
                    ShiftStartTime1 = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BreakStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BreakEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ShiftEndTime1 = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BreakOut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BreakIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualOut = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualStartTime1 = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualBreakStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualBreakEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualEndTime1 = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedWorkingMins = table.Column<double>(type: "float", nullable: false),
                    ActualWorkingMins = table.Column<double>(type: "float", nullable: false),
                    TotalWorkingMins = table.Column<double>(type: "float", nullable: false),
                    ActualAbsenceMins = table.Column<double>(type: "float", nullable: false),
                    LeaveDay = table.Column<double>(type: "float", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: true),
                    LeaveMinutes = table.Column<double>(type: "float", nullable: false),
                    AnnualLeaveDay = table.Column<double>(type: "float", nullable: false),
                    UnpaidLeaveDay = table.Column<double>(type: "float", nullable: false),
                    SickLeavePaidDay = table.Column<double>(type: "float", nullable: false),
                    SickLeaveUnpaidDay = table.Column<double>(type: "float", nullable: false),
                    PublicHoliday = table.Column<double>(type: "float", nullable: false),
                    WeeklyOff = table.Column<double>(type: "float", nullable: false),
                    RestDay = table.Column<double>(type: "float", nullable: false),
                    AnnualLeaveAccrualPerDay = table.Column<double>(type: "float", nullable: false),
                    TicketAccrualPerDay = table.Column<double>(type: "float", nullable: false),
                    AirTicketPaidDay = table.Column<double>(type: "float", nullable: false),
                    ActiveDay = table.Column<double>(type: "float", nullable: false),
                    InactiveDay = table.Column<double>(type: "float", nullable: false),
                    OvertimeMinutes = table.Column<double>(type: "float", nullable: false),
                    OvertimeTypeId = table.Column<int>(type: "int", nullable: true),
                    PlannedWorkingDay = table.Column<double>(type: "float", nullable: false),
                    ActualWorkingDay = table.Column<double>(type: "float", nullable: false),
                    AttendanceDay = table.Column<double>(type: "float", nullable: false),
                    AttendanceTypeId = table.Column<int>(type: "int", nullable: true),
                    AttendanceMinutes = table.Column<double>(type: "float", nullable: false),
                    AbsentDay = table.Column<double>(type: "float", nullable: false),
                    LateIn = table.Column<double>(type: "float", nullable: false),
                    DeductionLateIn = table.Column<double>(type: "float", nullable: false),
                    EarlyOut = table.Column<double>(type: "float", nullable: false),
                    DeductionEarlyOut = table.Column<double>(type: "float", nullable: false),
                    BreakEarlyOut = table.Column<double>(type: "float", nullable: false),
                    BreakLateIn = table.Column<double>(type: "float", nullable: false),
                    BreakDeductionEarlyOut = table.Column<double>(type: "float", nullable: false),
                    BreakDeductionLateIn = table.Column<double>(type: "float", nullable: false),
                    DedLateInEarlyOutMins = table.Column<double>(type: "float", nullable: false),
                    DedLateInEarlyOutDay = table.Column<double>(type: "float", nullable: false),
                    DedAfterMonthlyTol = table.Column<double>(type: "float", nullable: false),
                    IncludeInLeavePay = table.Column<double>(type: "float", nullable: false),
                    LeaveAdjDay = table.Column<double>(type: "float", nullable: false),
                    ApprovedActualWorkingDay = table.Column<double>(type: "float", nullable: false),
                    ApprovedAbsentDay = table.Column<double>(type: "float", nullable: false),
                    ApprovedDedLateInEarlyOutMins = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_TimeRegisters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeRegisters_Duties_DutyId",
                        column: x => x.DutyId,
                        principalTable: "Duties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimeRegisters_DutyPeriods_DutyPeriodId",
                        column: x => x.DutyPeriodId,
                        principalTable: "DutyPeriods",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimeRegisters_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeRegisters_OvertimeTypes_OvertimeTypeId",
                        column: x => x.OvertimeTypeId,
                        principalTable: "OvertimeTypes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimeRegisters_TimePolicies_TimePolicyId",
                        column: x => x.TimePolicyId,
                        principalTable: "TimePolicies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "WorkLocationDutyPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    WorkLocationId = table.Column<int>(type: "int", nullable: false),
                    DutyPeriodId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_WorkLocationDutyPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkLocationDutyPeriods_DutyPeriods_DutyPeriodId",
                        column: x => x.DutyPeriodId,
                        principalTable: "DutyPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkLocationDutyPeriods_WorkLocations_WorkLocationId",
                        column: x => x.WorkLocationId,
                        principalTable: "WorkLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDutySchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    RosterDutyScheduleId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeDutySchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDutySchedules_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeDutySchedules_RosterDutySchedules_RosterDutyScheduleId",
                        column: x => x.RosterDutyScheduleId,
                        principalTable: "RosterDutySchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RosterDailyDuties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    CalendarDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RosterDutyScheduleId = table.Column<int>(type: "int", nullable: false),
                    DailyDutyId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_RosterDailyDuties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RosterDailyDuties_DailyDuties_DailyDutyId",
                        column: x => x.DailyDutyId,
                        principalTable: "DailyDuties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RosterDailyDuties_RosterDutySchedules_RosterDutyScheduleId",
                        column: x => x.RosterDutyScheduleId,
                        principalTable: "RosterDutySchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SimulationTimeRegisterDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    SimulationTimeRegisterId = table.Column<int>(type: "int", nullable: false),
                    TimeTypeId = table.Column<int>(type: "int", nullable: false),
                    TimeValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_SimulationTimeRegisterDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SimulationTimeRegisterDetails_SimulationTimeRegisters_SimulationTimeRegisterId",
                        column: x => x.SimulationTimeRegisterId,
                        principalTable: "SimulationTimeRegisters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SimulationTimeRegisterDetails_TimeTypes_TimeTypeId",
                        column: x => x.TimeTypeId,
                        principalTable: "TimeTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyDuties_BreakShiftId",
                table: "DailyDuties",
                column: "BreakShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Duties_BreakShiftId",
                table: "Duties",
                column: "BreakShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Duties_OvertimeTypeId",
                table: "Duties",
                column: "OvertimeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriodDetails_Day1DutyId",
                table: "DutyPeriodDetails",
                column: "Day1DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriodDetails_Day2DutyId",
                table: "DutyPeriodDetails",
                column: "Day2DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriodDetails_Day3DutyId",
                table: "DutyPeriodDetails",
                column: "Day3DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriodDetails_Day4DutyId",
                table: "DutyPeriodDetails",
                column: "Day4DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriodDetails_Day5DutyId",
                table: "DutyPeriodDetails",
                column: "Day5DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriodDetails_Day6DutyId",
                table: "DutyPeriodDetails",
                column: "Day6DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriodDetails_Day7DutyId",
                table: "DutyPeriodDetails",
                column: "Day7DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriodDetails_DutyPeriodId",
                table: "DutyPeriodDetails",
                column: "DutyPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriods_FridayDutyId",
                table: "DutyPeriods",
                column: "FridayDutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriods_HolidayCalendarId",
                table: "DutyPeriods",
                column: "HolidayCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriods_MondayDutyId",
                table: "DutyPeriods",
                column: "MondayDutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriods_SaturdayDutyId",
                table: "DutyPeriods",
                column: "SaturdayDutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriods_SundayDutyId",
                table: "DutyPeriods",
                column: "SundayDutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriods_ThursdayDutyId",
                table: "DutyPeriods",
                column: "ThursdayDutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriods_TuesdayDutyId",
                table: "DutyPeriods",
                column: "TuesdayDutyId");

            migrationBuilder.CreateIndex(
                name: "IX_DutyPeriods_WednesdayDutyId",
                table: "DutyPeriods",
                column: "WednesdayDutyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDutyPeriods_DutyPeriodId",
                table: "EmployeeDutyPeriods",
                column: "DutyPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDutyPeriods_EmployeeId",
                table: "EmployeeDutyPeriods",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDutySchedules_EmployeeId",
                table: "EmployeeDutySchedules",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDutySchedules_RosterDutyScheduleId",
                table: "EmployeeDutySchedules",
                column: "RosterDutyScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubstituteDuties_DutyId",
                table: "EmployeeSubstituteDuties",
                column: "DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSubstituteDuties_EmployeeId",
                table: "EmployeeSubstituteDuties",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTimePolicies_EmployeeId",
                table: "EmployeeTimePolicies",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTimePolicies_TimePolicyId",
                table: "EmployeeTimePolicies",
                column: "TimePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeTimesheets_EmployeeId",
                table: "EmployeeTimesheets",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayPeriods_HolidayId",
                table: "HolidayPeriods",
                column: "HolidayId");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_HolidayCalendarId",
                table: "Holidays",
                column: "HolidayCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_OvertimeTypeId",
                table: "Holidays",
                column: "OvertimeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyTimeRegisterDetails_MonthlyTimeRegisterId",
                table: "MonthlyTimeRegisterDetails",
                column: "MonthlyTimeRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyTimeRegisterDetails_TimeTypeId",
                table: "MonthlyTimeRegisterDetails",
                column: "TimeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyTimeRegisters_EmployeeId",
                table: "MonthlyTimeRegisters",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDailyDuties_DailyDutyId",
                table: "RosterDailyDuties",
                column: "DailyDutyId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDailyDuties_RosterDutyScheduleId",
                table: "RosterDailyDuties",
                column: "RosterDutyScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDutyPeriodDetails_Day1DutyId",
                table: "RosterDutyPeriodDetails",
                column: "Day1DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDutyPeriodDetails_Day2DutyId",
                table: "RosterDutyPeriodDetails",
                column: "Day2DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDutyPeriodDetails_Day3DutyId",
                table: "RosterDutyPeriodDetails",
                column: "Day3DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDutyPeriodDetails_Day4DutyId",
                table: "RosterDutyPeriodDetails",
                column: "Day4DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDutyPeriodDetails_Day5DutyId",
                table: "RosterDutyPeriodDetails",
                column: "Day5DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDutyPeriodDetails_Day6DutyId",
                table: "RosterDutyPeriodDetails",
                column: "Day6DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDutyPeriodDetails_Day7DutyId",
                table: "RosterDutyPeriodDetails",
                column: "Day7DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDutyPeriodDetails_RosterDutyPeriodId",
                table: "RosterDutyPeriodDetails",
                column: "RosterDutyPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDutySchedules_DutyPeriodId",
                table: "RosterDutySchedules",
                column: "DutyPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterDutySchedules_HolidayCalendarId",
                table: "RosterDutySchedules",
                column: "HolidayCalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterGroupDetails_EmployeeId",
                table: "RosterGroupDetails",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterGroupDetails_RosterGroupId",
                table: "RosterGroupDetails",
                column: "RosterGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterGroupSchedules_DutyPeriodId",
                table: "RosterGroupSchedules",
                column: "DutyPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_RosterGroupSchedules_RosterGroupId",
                table: "RosterGroupSchedules",
                column: "RosterGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationTimeRegisterDetails_SimulationTimeRegisterId",
                table: "SimulationTimeRegisterDetails",
                column: "SimulationTimeRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationTimeRegisterDetails_TimeTypeId",
                table: "SimulationTimeRegisterDetails",
                column: "TimeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationTimeRegisters_DutyPeriodId",
                table: "SimulationTimeRegisters",
                column: "DutyPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_SimulationTimeRegisters_EmployeeId",
                table: "SimulationTimeRegisters",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEvents_AccessUnitId",
                table: "TimeEvents",
                column: "AccessUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEvents_ActualTransTypeId",
                table: "TimeEvents",
                column: "ActualTransTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEvents_EmployeeId",
                table: "TimeEvents",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEvents_TransTypeId",
                table: "TimeEvents",
                column: "TransTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeEvents_WorkLocationId",
                table: "TimeEvents",
                column: "WorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeLogs_EmployeeId",
                table: "TimeLogs",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeRegisters_DutyId",
                table: "TimeRegisters",
                column: "DutyId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeRegisters_DutyPeriodId",
                table: "TimeRegisters",
                column: "DutyPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeRegisters_EmployeeId",
                table: "TimeRegisters",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeRegisters_OvertimeTypeId",
                table: "TimeRegisters",
                column: "OvertimeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeRegisters_TimePolicyId",
                table: "TimeRegisters",
                column: "TimePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeTransfers_EmployeeId",
                table: "TimeTransfers",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeTransfers_TransferTypeId",
                table: "TimeTransfers",
                column: "TransferTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkLocationDutyPeriods_DutyPeriodId",
                table: "WorkLocationDutyPeriods",
                column: "DutyPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkLocationDutyPeriods_WorkLocationId",
                table: "WorkLocationDutyPeriods",
                column: "WorkLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_YearlyTimeRegisterDetails_TimeTypeId",
                table: "YearlyTimeRegisterDetails",
                column: "TimeTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_YearlyTimeRegisterDetails_YearlyTimeRegisterId",
                table: "YearlyTimeRegisterDetails",
                column: "YearlyTimeRegisterId");

            migrationBuilder.CreateIndex(
                name: "IX_YearlyTimeRegisters_EmployeeId",
                table: "YearlyTimeRegisters",
                column: "EmployeeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DutyPeriodDetails");

            migrationBuilder.DropTable(
                name: "EmployeeDutyPeriods");

            migrationBuilder.DropTable(
                name: "EmployeeDutySchedules");

            migrationBuilder.DropTable(
                name: "EmployeeSubstituteDuties");

            migrationBuilder.DropTable(
                name: "EmployeeTimePolicies");

            migrationBuilder.DropTable(
                name: "EmployeeTimesheets");

            migrationBuilder.DropTable(
                name: "HolidayPeriods");

            migrationBuilder.DropTable(
                name: "MonthlyTimeRegisterDetails");

            migrationBuilder.DropTable(
                name: "RosterDailyDuties");

            migrationBuilder.DropTable(
                name: "RosterDutyPeriodDetails");

            migrationBuilder.DropTable(
                name: "RosterGroupDetails");

            migrationBuilder.DropTable(
                name: "RosterGroupSchedules");

            migrationBuilder.DropTable(
                name: "SimulationTimeRegisterDetails");

            migrationBuilder.DropTable(
                name: "TimeAttAPIs");

            migrationBuilder.DropTable(
                name: "TimeEvents");

            migrationBuilder.DropTable(
                name: "TimeLogs");

            migrationBuilder.DropTable(
                name: "TimeRegisters");

            migrationBuilder.DropTable(
                name: "TimeTransfers");

            migrationBuilder.DropTable(
                name: "WorkLocationDutyPeriods");

            migrationBuilder.DropTable(
                name: "YearlyTimeRegisterDetails");

            migrationBuilder.DropTable(
                name: "Holidays");

            migrationBuilder.DropTable(
                name: "MonthlyTimeRegisters");

            migrationBuilder.DropTable(
                name: "RosterDutySchedules");

            migrationBuilder.DropTable(
                name: "DailyDuties");

            migrationBuilder.DropTable(
                name: "RosterDutyPeriods");

            migrationBuilder.DropTable(
                name: "RosterGroups");

            migrationBuilder.DropTable(
                name: "SimulationTimeRegisters");

            migrationBuilder.DropTable(
                name: "TransTypes");

            migrationBuilder.DropTable(
                name: "TimePolicies");

            migrationBuilder.DropTable(
                name: "TimeTransferTypes");

            migrationBuilder.DropTable(
                name: "TimeTypes");

            migrationBuilder.DropTable(
                name: "YearlyTimeRegisters");

            migrationBuilder.DropTable(
                name: "DutyPeriods");

            migrationBuilder.DropTable(
                name: "Duties");

            migrationBuilder.DropTable(
                name: "HolidayCalendars");

            migrationBuilder.DropTable(
                name: "BreakShifts");
        }
    }
}
